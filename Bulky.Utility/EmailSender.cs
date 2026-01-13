using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Bulky.Utility
{
    public class EmailSender : IEmailSender
    {
        private readonly string _apiKey;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly HttpClient _httpClient;

        public EmailSender(IConfiguration config)
        {
            _apiKey = config.GetValue<string>("MailSend:ApiKey") ?? string.Empty;
            _fromEmail = config.GetValue<string>("MailSend:FromEmail") ?? "noreply@yourdomain.com";
            _fromName = config.GetValue<string>("MailSend:FromName") ?? "Bulky E-Commerce";
            _httpClient = new HttpClient();
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                // Log or handle missing API key
                Console.WriteLine("MailSend API Key is not configured.");
                return;
            }

            var payload = new
            {
                from = new { email = _fromEmail, name = _fromName },
                to = new[] { new { email = email } },
                subject = subject,
                html = htmlMessage
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.mailersend.com/v1/email")
            {
                Headers = { { "Authorization", $"Bearer {_apiKey}" } },
                Content = JsonContent.Create(payload)
            };

            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"MailSend error: {error}");
            }
        }
    }
}
