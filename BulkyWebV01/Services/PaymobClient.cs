using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Bulky.Utility;
using Microsoft.Extensions.Options;

namespace BulkyWebV01.Services;

public sealed class PaymobClient
{
    private readonly HttpClient _http;
    private readonly PaymobSettings _settings;

    public PaymobClient(HttpClient http, IOptions<PaymobSettings> options)
    {
        _http = http;
        _settings = options.Value;

        if (!string.IsNullOrWhiteSpace(_settings.BaseUrl))
        {
            _http.BaseAddress = new Uri(_settings.BaseUrl.TrimEnd('/') + "/");
        }
    }

    public async Task<PaymobPaymentInitResult> CreatePaymentAsync(PaymobCreatePaymentRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            throw new InvalidOperationException("Paymob:ApiKey is not configured.");
        }

        if (_settings.IntegrationId <= 0)
        {
            throw new InvalidOperationException("Paymob:IntegrationId is not configured.");
        }

        if (_settings.IframeId <= 0)
        {
            throw new InvalidOperationException("Paymob:IframeId is not configured.");
        }

        var authToken = await GetAuthTokenAsync(cancellationToken);

        var paymobOrderId = await CreateOrderAsync(authToken, request, cancellationToken);

        var paymentKey = await CreatePaymentKeyAsync(authToken, paymobOrderId, request, cancellationToken);

        var url = $"{_settings.BaseUrl.TrimEnd('/')}/api/acceptance/iframes/{_settings.IframeId}?payment_token={Uri.EscapeDataString(paymentKey)}";
        return new PaymobPaymentInitResult(PaymobOrderId: paymobOrderId, PaymentUrl: url);
    }

    public async Task<PaymobTransaction?> GetTransactionAsync(long transactionId, CancellationToken cancellationToken = default)
    {
        var authToken = await GetAuthTokenAsync(cancellationToken);

        // Transaction inquiry endpoint varies by Paymob product version.
        // This endpoint is widely used with Accept.
        var response = await _http.GetAsync($"api/acceptance/transactions/{transactionId}?token={Uri.EscapeDataString(authToken)}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<PaymobTransactionResponse>(cancellationToken: cancellationToken);
        if (payload == null)
        {
            return null;
        }

        return new PaymobTransaction(
            Id: payload.Id,
            Success: payload.Success,
            Pending: payload.Pending,
            AmountCents: payload.AmountCents
        );
    }

    private async Task<string> GetAuthTokenAsync(CancellationToken cancellationToken)
    {
        var response = await _http.PostAsJsonAsync("api/auth/tokens", new { api_key = _settings.ApiKey }, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<AuthTokenResponse>(cancellationToken: cancellationToken);
        if (payload == null || string.IsNullOrWhiteSpace(payload.Token))
        {
            throw new InvalidOperationException("Paymob auth token response was empty.");
        }

        return payload.Token;
    }

    private async Task<long> CreateOrderAsync(string authToken, PaymobCreatePaymentRequest request, CancellationToken cancellationToken)
    {
        var orderRequest = new
        {
            auth_token = authToken,
            delivery_needed = false,
            amount_cents = request.AmountCents.ToString(),
            currency = request.Currency,
            merchant_order_id = request.MerchantOrderId,
            items = request.Items.Select(i => new
            {
                name = i.Name,
                amount_cents = i.AmountCents.ToString(),
                quantity = i.Quantity,
                description = i.Description
            }).ToArray()
        };

        var response = await _http.PostAsJsonAsync("api/ecommerce/orders", orderRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<CreateOrderResponse>(cancellationToken: cancellationToken);
        if (payload == null)
        {
            throw new InvalidOperationException("Paymob create order response was empty.");
        }

        return payload.Id;
    }

    private async Task<string> CreatePaymentKeyAsync(string authToken, long paymobOrderId, PaymobCreatePaymentRequest request, CancellationToken cancellationToken)
    {
        var billing = request.BillingData;

        var paymentKeyRequest = new
        {
            auth_token = authToken,
            amount_cents = request.AmountCents.ToString(),
            expiration = 3600,
            order_id = paymobOrderId,
            currency = request.Currency,
            integration_id = _settings.IntegrationId,
            billing_data = new
            {
                apartment = billing.Apartment,
                email = billing.Email,
                floor = billing.Floor,
                first_name = billing.FirstName,
                street = billing.Street,
                building = billing.Building,
                phone_number = billing.PhoneNumber,
                shipping_method = billing.ShippingMethod,
                postal_code = billing.PostalCode,
                city = billing.City,
                country = billing.Country,
                last_name = billing.LastName,
                state = billing.State
            }
        };

        var response = await _http.PostAsJsonAsync("api/acceptance/payment_keys", paymentKeyRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<CreatePaymentKeyResponse>(cancellationToken: cancellationToken);
        if (payload == null || string.IsNullOrWhiteSpace(payload.Token))
        {
            throw new InvalidOperationException("Paymob create payment key response was empty.");
        }

        return payload.Token;
    }

    private sealed class AuthTokenResponse
    {
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;
    }

    private sealed class CreateOrderResponse
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }
    }

    private sealed class CreatePaymentKeyResponse
    {
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;
    }

    private sealed class PaymobTransactionResponse
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("pending")]
        public bool Pending { get; set; }

        [JsonPropertyName("amount_cents")]
        public long AmountCents { get; set; }
    }
}

public sealed record PaymobCreatePaymentRequest(
    long AmountCents,
    string Currency,
    string MerchantOrderId,
    PaymobBillingData BillingData,
    IReadOnlyList<PaymobOrderItem> Items
);

public sealed record PaymobBillingData(
    string Apartment,
    string Email,
    string Floor,
    string FirstName,
    string LastName,
    string Street,
    string Building,
    string PhoneNumber,
    string ShippingMethod,
    string PostalCode,
    string City,
    string State,
    string Country
);

public sealed record PaymobOrderItem(
    string Name,
    long AmountCents,
    int Quantity,
    string? Description
);

public sealed record PaymobTransaction(
    long Id,
    bool Success,
    bool Pending,
    long AmountCents
);

public sealed record PaymobPaymentInitResult(
    long PaymobOrderId,
    string PaymentUrl
);
