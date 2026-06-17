using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace UserManager.Services;

public class EmailService
{
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;

    public EmailService(IConfiguration config)
    {
        _config = config;
        _httpClient = new HttpClient();
    }

    public async Task SendVerificationEmailAsync(string toEmail, string token)
    {
        var verifyUrl = $"{_config["AppUrl"]}/Auth/Verify?token={token}";
        var inboxId = _config["Email:MailtrapInboxId"];
        var apiToken = _config["Email:MailtrapApiToken"];

        var payload = new
        {
            from = new { email = _config["Email:From"], name = "UserManager" },
            to = new[] { new { email = toEmail } },
            subject = "Подтверждение почты",
            html = $"<p>Пожалуйста, подтвердите вашу почту, перейдя по ссылке ниже:</p><p><a href='{verifyUrl}'>Подтвердить почту</a></p>"
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post,
            $"https://sandbox.api.mailtrap.io/api/send/{inboxId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
        request.Content = content;

        var response = await _httpClient.SendAsync(request);
        var responseText = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"DEBUG Mailtrap response: {response.StatusCode} - {responseText}");

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Mailtrap API error: {response.StatusCode} - {responseText}");
        }
    }
}