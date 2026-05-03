using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FirstClassFinance.Data;
using FirstClassFinance.Interfaces;
using FirstClassFinance.Models;
using Microsoft.EntityFrameworkCore;

namespace FirstClassFinance.Services;

public class ValidationService : IValidationService
{
    private readonly AppDBContext _context;
    
    public ValidationService(AppDBContext context)
    {
        _context = context;
    }

    private async Task<string> GetError(string messageCode)
    {
        return (await _context.ErrorMessages
            .FirstOrDefaultAsync(e => e.messageCode == messageCode)) 
            ?.message ?? messageCode;
    }

    public async Task<List<string>> ValidateJournalEntry(JournalModel journalEntry)
    {
        var errors = new List<string>();
        if (journalEntry.journalLines == null || journalEntry.journalLines.Count < 2)
        {
            errors.Add(await GetError("ERR_Minimum Line Count"));
            errors.Add("Journal entry must have at least two lines - one debit and one credit.");
        }

        if (!journalEntry.journalLines.Any(line => line.entryType == "Debit"))
        {
            errors.Add(await GetError("ERROR_Debit Entry Required"));
        }

        if (!journalEntry.journalLines.Any(line => line.entryType == "Credit"))
        {
            errors.Add(await GetError("ERROR_Credit Entry Required"));
        }

        if (journalEntry.journalLines.Any(line => line.amount <= 0))
        {
            errors.Add(await GetError("ERROR_Amount Must Be Greater Than Zero"));
        }
        
        var totalDebits = journalEntry.journalLines
            .Where(line => line.entryType == "Debit")
            .Sum(line => line.amount);
        
        var totalCredits = journalEntry.journalLines
            .Where(line => line.entryType == "Credit")
            .Sum(line => line.amount);
        
        // debits and credits must equal each other
        if (totalDebits != totalCredits)
        {
            errors.Add(await GetError("ERROR_Total Debits and Credits Must Equal"));
        }

        return errors;
    }
}