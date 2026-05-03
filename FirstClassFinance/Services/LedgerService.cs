using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FirstClassFinance.Data;
using FirstClassFinance.Interfaces;
using FirstClassFinance.Models;
using Microsoft.EntityFrameworkCore;

namespace FirstClassFinance.Services;

public class LedgerService : ILedgerService
{
    private readonly AppDBContext _context;

    public LedgerService(AppDBContext context)
    {
        _context = context;
    }

    public async Task PostToLedger(JournalModel journalEntry)
    {
        // enter each line into the appropriate account ledger
        foreach (var line in journalEntry.journalLines)
        {
            var account = await _context.Accounts.FindAsync(line.accountId);
            var lastAccountBalance = await _context.LedgerEntries
                .Where(a => a.accountId == line.accountId)
                .OrderByDescending(a => a.date)
                .Select(a => (decimal?)a.balance)
                .FirstOrDefaultAsync() ?? 0;

            decimal newBalance = lastAccountBalance;
            if (account != null)
            {
                if (line.entryType == "Debit")
                {
                    account.Debit += line.amount;
                    newBalance = (account.NormalSide == "Debit") 
                        ? lastAccountBalance + line.amount 
                        : lastAccountBalance - line.amount;
                }
                else // Credit
                {
                    account.Credit += line.amount;
                    newBalance = (account.NormalSide == "Credit") 
                        ? lastAccountBalance + line.amount 
                        : lastAccountBalance - line.amount;
                }
                account.Balance = newBalance;
            }
            else
            {
                // Fallback if account not found (should not happen with good DB integrity)
                newBalance = line.entryType == "Debit"
                    ? lastAccountBalance + line.amount
                    : lastAccountBalance - line.amount;
            }

            _context.LedgerEntries.Add(new LedgerEntryModel
            {
                accountId = line.accountId,
                journalEntryId = journalEntry.journalId, // post reference (PR)
                date = journalEntry.entryDate,
                debits = line.entryType == "Debit" ? line.amount : 0,
                credits = line.entryType == "Credit" ? line.amount : 0,
                balance = newBalance
            });
        }
        await _context.SaveChangesAsync();
    }

    public async Task<List<LedgerEntryModel>> GetLedgerEntries(int? accountId = null)
    {
        var query = _context.LedgerEntries.AsQueryable();

        if (accountId.HasValue)
        {
            query = query.Where(l => l.accountId == accountId.Value);
        }

        return await query.OrderBy(l => l.date).ToListAsync();
    }
}