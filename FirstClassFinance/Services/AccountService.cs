using System;
using System.Collections.Generic;
using System.Linq;
using FirstClassFinance.Models;
using FirstClassFinance.Data;
using FirstClassFinance.Filters;
using FirstClassFinance.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FirstClassFinance.Services;

public class AccountService
{
    private readonly AppDBContext _context;
    private readonly EventLogService _eventLogService;
    private readonly INotificationService _notificationService;

    public AccountService(AppDBContext context, EventLogService eventLogService, INotificationService notificationService)
    {
        _context = context;
        _eventLogService = eventLogService;
        _notificationService = notificationService;
    }

    // adding new accounts
    public AccountModel AddAccount(AccountModel account)
    {
        bool exists = _context.Accounts.Any(a =>
            a.AccountNumber == account.AccountNumber ||
            a.AccountName == account.AccountName);

        if (exists)
            throw new Exception("Duplicate account number or name not allowed.");

        account.Balance = account.InitialBalance;

        _context.Accounts.Add(account);
        _context.SaveChanges();
        
        // log saves any changes made to the account and table
        _eventLogService.LogEvent(
            "Account",
            account.Id,
            userId: account.UserId,
            "Added an account",
            null,
            account
            );

        return account;
    }
    
    // account for updating accounts
    public AccountModel UpdateAccount(int id, AccountModel updatedAccount, int userId) // id is just the account id
    {
        var account = _context.Accounts.Find(id);
        if (account == null) throw new Exception("Account not found.");
        
        // log the account in its current state ie beforeState
        var before = _context.Accounts.AsNoTracking().FirstOrDefault(a => a.Id == id);
        
        // make the updates then store the afterState
        account.AccountName = updatedAccount.AccountName;
        account.Balance = updatedAccount.Balance;
        account.AccountDescription = updatedAccount.AccountDescription;
        account.Comment = updatedAccount.Comment;

        _context.SaveChanges();

        _eventLogService.LogEvent(
            "Account", 
            account.Id, 
            account.UserId, 
            "Updated an account", 
            before, 
            account);
        return account;
    }
    
    //managing deactivations
    public async Task DeactivateAccount(int id, int userId)
    {
        var account = _context.Accounts.Find(id);
        if (account == null) throw new Exception("Account not found.");
        if (account.Balance > 0) throw new Exception("Account has a balance greater than $0, cannot be deactivated.");
        
        var before = _context.Accounts.AsNoTracking().FirstOrDefault(a => a.Id == id);
        account.IsActive = false;
        _context.SaveChanges();

        try
        {
            await _notificationService.EmailAdministrator("Account Deactivated",
                $"Account '{account.AccountName}' (ID: {account.Id}) has been deactivated by user ID: {userId}.");
        }
        catch { /* Best effort */ }
        _eventLogService.LogEvent(
            "Account",
            account.Id,
            userId: account.UserId,
            "Deactivated an account",
            before,
            account
            );
    }
    
    // filter accounts by name, category, subcategory, and balance
    public List<AccountModel> FilterAccounts(
        string? name, 
        string? category, 
        string? subcategory, 
        decimal? balance,
        string? normalSide,
        string? statementType,
        int? order,
        decimal? debit,
        decimal? credit)
    {
        IQueryable<AccountModel> query = _context.Accounts.Where(a => a.IsActive);
        IAccountFilter filter = new BaseAccountFilter();

        if (!string.IsNullOrEmpty(name))
            filter = new NameFilter(filter, name);

        if (!string.IsNullOrEmpty(category))
            filter = new CategoryFilter(filter, category);

        if (!string.IsNullOrEmpty(subcategory))
            filter = new SubCategoryFilter(filter, subcategory);

        if (balance.HasValue)
            filter = new BalanceFilter(filter, balance);
        
        if (!string.IsNullOrEmpty(normalSide)) 
            filter = new NormalSideFilter(filter, normalSide);
        
        if(!string.IsNullOrEmpty(statementType)) 
            filter = new StatementFilter(filter, statementType);
        
        if (order.HasValue) 
            filter = new OrderFilter(filter, order);
        
        if (debit.HasValue) 
            filter = new DebitFilter(filter, debit);
        
        if (credit.HasValue) 
            filter = new CreditFilter(filter, credit);
        
        return filter.Apply(query).ToList();
    }
}