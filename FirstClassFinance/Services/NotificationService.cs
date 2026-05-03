using FirstClassFinance.Data;
using FirstClassFinance.Interfaces;

namespace FirstClassFinance.Services;

public class NotificationService : INotificationService
{
    private readonly IEmailService _emailService;
    private readonly AppDBContext _context;
    
    public NotificationService(IEmailService emailService, AppDBContext context)
    {
        _emailService = emailService;
        _context = context;
    }

    public async Task EmailAdministrator(string subject, string body)
    {
        var administrator = _context.Users.Where(a => a.Role == "Administrator");
        foreach (var admin in administrator)
        {
            await _emailService.SendEmailAsync(admin.EmailAddress, subject, body);
        }
    }
    
    public async Task EmailManager(string subject, string body)
    {
        var manager = _context.Users.Where(a => a.Role == "Manager");
        foreach (var man in manager)
        {
            await _emailService.SendEmailAsync(man.EmailAddress, subject, body);
        }   
    }
    
    public async Task EmailAccountant(string subject, string body)
    {
        var accountant = _context.Users.Where(a => a.Role == "Accountant");
        foreach (var acc in accountant)
        {
            await _emailService.SendEmailAsync(acc.EmailAddress, subject, body);
        }  
    }

    public async Task EmailUser(string email, string subject, string body)
    {
        await _emailService.SendEmailAsync(email, subject, body);
    }
}