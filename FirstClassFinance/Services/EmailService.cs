using MimeKit;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MailKit.Net.Smtp;
using FirstClassFinance.Interfaces;

namespace FirstClassFinance.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    
    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendEmailAsync(string toReceiver, string subject, string body)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_config["EmailSettings:SenderName"], _config["EmailSettings:SenderEmail"]));
        message.To.Add(new MailboxAddress("", toReceiver));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = body
        };
        message.Body = bodyBuilder.ToMessageBody();

        using (var client = new SmtpClient())
        {
            await client.ConnectAsync(
                _config["EmailSettings:SmtpServer"], 
                int.Parse(_config["EmailSettings:SmtpPort"] ?? "587"), 
                MailKit.Security.SecureSocketOptions.StartTls
            );

            // Note: In a production environment, you should avoid hardcoding authentication
            // and use environment variables or a secret manager.
            await client.AuthenticateAsync(
                _config["EmailSettings:UserName"], 
                _config["EmailSettings:Password"]
            );

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}