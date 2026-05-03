namespace FirstClassFinance.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string toReceiver, string subject, string message);
}