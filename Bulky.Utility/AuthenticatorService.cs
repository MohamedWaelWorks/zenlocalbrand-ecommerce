using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Bulky.Utility
{
    public class AuthenticatorService
    {
        private readonly HttpClient _httpClient;
        private readonly string _secretCode;
        private readonly string _apiUrl;

        public AuthenticatorService(IConfiguration config, HttpClient httpClient)
        {
            _httpClient = httpClient;
            _secretCode = config.GetValue<string>("Authenticator:SecretCode") ?? string.Empty;
            _apiUrl = config.GetValue<string>("Authenticator:ApiUrl") ?? "http://authenticatorapi.com/api.asmx";
        }

        public async Task<bool> ValidatePinAsync(string pin)
        {
            if (string.IsNullOrEmpty(_secretCode) || string.IsNullOrEmpty(pin))
            {
                return false;
            }

            try
            {
                // SOAP 1.1 Request
                var soapRequest = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" 
               xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" 
               xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soap:Body>
    <ValidatePin xmlns=""http://authenticatorapi.com/"">
      <pin>{pin}</pin>
      <secretCode>{_secretCode}</secretCode>
    </ValidatePin>
  </soap:Body>
</soap:Envelope>";

                var content = new StringContent(soapRequest, Encoding.UTF8, "text/xml");
                content.Headers.Add("SOAPAction", "\"http://authenticatorapi.com/ValidatePin\"");

                var response = await _httpClient.PostAsync(_apiUrl, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var doc = XDocument.Parse(responseContent);
                
                XNamespace ns = "http://authenticatorapi.com/";
                var result = doc.Descendants(ns + "ValidatePinResult").FirstOrDefault();
                
                if (result != null && bool.TryParse(result.Value, out bool isValid))
                {
                    return isValid;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Authenticator API error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ValidatePinHttpGetAsync(string pin)
        {
            try
            {
                var url = $"{_apiUrl}/ValidatePin?pin={pin}&secretCode={_secretCode}";
                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var doc = XDocument.Parse(responseContent);
                
                XNamespace ns = "http://authenticatorapi.com/";
                var result = doc.Descendants(ns + "boolean").FirstOrDefault();
                
                if (result != null && bool.TryParse(result.Value, out bool isValid))
                {
                    return isValid;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Authenticator API error: {ex.Message}");
                return false;
            }
        }
    }
}
