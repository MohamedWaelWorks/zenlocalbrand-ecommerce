namespace Bulky.Utility;

public class PaymobSettings
{
    public string BaseUrl { get; set; } = "https://accept.paymob.com";

    // Found in Paymob dashboard
    public string ApiKey { get; set; } = string.Empty;
    public int IntegrationId { get; set; }
    public int IframeId { get; set; }

    // Optional but recommended for validating callbacks/redirection
    public string? HmacSecret { get; set; }

    public string Currency { get; set; } = "EGP";
}
