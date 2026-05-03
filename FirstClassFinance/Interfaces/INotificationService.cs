namespace FirstClassFinance.Interfaces;

public interface INotificationService
{
    Task EmailAdministrator(string subject, string body);
    Task EmailManager(string subject, string body);
    Task EmailAccountant(string subject, string body);
    Task EmailUser(string email, string subject, string body);
}