using MimeKit;
using MailKit.Net.Smtp;


namespace UserManager.Services;


public class EmailService
{
  private readonly IConfiguration _config;

  public EmailService(IConfiguration config)
  {
    _config = config;
  }

  public async Task SendVerificationEmailAsync(string toEmail, string token)
  {

    Console.WriteLine($"DEBUG Host: {_config["Email:Host"]}");
    Console.WriteLine($"DEBUG Username: {_config["Email:Username"]}");
    Console.WriteLine($"DEBUG From: {_config["Email:From"]}");
    
  
    var message = new MimeMessage();
    message.From.Add(MailboxAddress.Parse(_config["Email:From"]));
    message.To.Add(MailboxAddress.Parse(toEmail));
    message.Subject = "Подтверждение почты";

    var verifyUrl = $"{_config["AppUrl"]}/Auth/Verify?token={token}";

    message.Body = new TextPart("html")
    {
      Text = $"<p>Пожалуйста, подтвердите вашу почту, перейдя по ссылке ниже:</p><p><a href='{verifyUrl}'>Подтвердить почту</a></p>"
    };

    using var client = new SmtpClient();

    await client.ConnectAsync(_config["Email:Host"], int.Parse(_config["Email:Port"]!), MailKit.Security.SecureSocketOptions.StartTls);
    await client.AuthenticateAsync(_config["Email:Username"], _config["Email:Password"]);
    await client.SendAsync(message);
    await client.DisconnectAsync(true);

  }
}