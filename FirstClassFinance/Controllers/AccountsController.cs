using System.Linq;
using Microsoft.AspNetCore.Mvc;
using FirstClassFinance.Services;
using FirstClassFinance.Data;
using FirstClassFinance.Models;
using Microsoft.AspNetCore.Authorization;
using FirstClassFinance.Interfaces;
using System.Threading.Tasks;

namespace FirstClassFinance.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AccountsController : ControllerBase
{
    private readonly AccountService _accountService;
    private readonly AppDBContext _context;
    private readonly INotificationService _notificationService;

    public AccountsController(AccountService accountService, AppDBContext context, INotificationService notificationService)
    {
        _accountService = accountService;
        _context = context;
        _notificationService = notificationService;
    }

    [HttpPost("add-account")] // add or create a new account
    [Authorize(Roles="Administrator")]
    public IActionResult AddAccount([FromBody] AccountModel account)
    {
        var result = _accountService.AddAccount(account);
        return Ok(result);
    }
    
    [HttpGet("get-all-accounts")] // view all accounts in DB
    public IActionResult GetAllAccounts()
    {
        return Ok(_context.Accounts.Where(a => a.IsActive));
    }

    [HttpGet("{id}")]
    public IActionResult GetAccount(int id)
    {
        var account = _context.Accounts.Find(id);

        if (account == null)
            return NotFound();

        return Ok(account);
    }
    
    [HttpPut("{id}")] // updating specific accounts
    [Authorize(Roles="Administrator")]
    public IActionResult UpdateAccount(int id, [FromBody] AccountModel updated)
    {
        try
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var result = _accountService.UpdateAccount(id, updated, userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    
    [HttpPut("deactivate/{id}")] // removing old or inactive accounts
    [Authorize(Roles="Administrator")]
    public async Task<IActionResult> DeactivateAccount(int id)
    {
        try
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            await _accountService.DeactivateAccount(id, userId);
            return Ok("Account deactivated");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    
    [HttpGet("search")] // search via account name or number
    public IActionResult Search(string query)
    {
        var results = _context.Accounts
            .Where(a => a.AccountName.Contains(query) ||
                        a.AccountNumber.ToString().Contains(query))
            .ToList();

        return Ok(results);
    }
    
    // filter accounts by name, category, subcategory, and balance (those are the only filters available as of now)
    [HttpGet("filter")]
    public IActionResult FilterAccounts(
        string? name, string? category, 
        string? subcategory, decimal? balance, 
        string? normalSide, string? statementType,
        int? order, decimal? debit, decimal? credit
        )
    {
        var results = _accountService.FilterAccounts(name, category, subcategory, balance, normalSide, statementType, order, debit, credit);
        return Ok(results);
    }

    [HttpPost("email/manager")]
    public async Task<IActionResult> EmailManager([FromBody] EmailRequest request)
    {
        await _notificationService.EmailManager(request.Subject, request.Body);
        return Ok("Email sent to managers.");
    }

    [HttpPost("email/administrator")]
    public async Task<IActionResult> EmailAdministrator([FromBody] EmailRequest request)
    {
        await _notificationService.EmailAdministrator(request.Subject, request.Body);
        return Ok("Email sent to administrators.");
    }

    [HttpPost("email/accountant")]
    public async Task<IActionResult> EmailAccountant([FromBody] EmailRequest request)
    {
        await _notificationService.EmailAccountant(request.Subject, request.Body);
        return Ok("Email sent to accountants.");
    }
}

public class EmailRequest
{
    public string Subject { get; set; }
    public string Body { get; set; }
}