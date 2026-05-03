using System;
using System.Linq;
using System.Threading.Tasks;
using FirstClassFinance.Models;
using FirstClassFinance.Data;
using FirstClassFinance.Filters;
using FirstClassFinance.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FirstClassFinance.Services;

public class JournalService
{
    private readonly AppDBContext _context;
    private readonly ILedgerService _ledgerService;
    private readonly IValidationService _validationService;
    private readonly INotificationService _notificationService;

    public JournalService(AppDBContext context, ILedgerService ledgerService, IValidationService validationService, INotificationService notificationService)
    {
        _context = context;
        _ledgerService = ledgerService;
        _validationService = validationService;
        _notificationService = notificationService;
    }

    public async Task<JournalModel> AddJournalEntry(JournalModel journalEntry)
    {
        // validate the journal entry
        var errors = await _validationService.ValidateJournalEntry(journalEntry);
        if (errors.Any())
        {
            throw new Exception(string.Join("; ", errors));
        }

        // Add the journal entry to the database
        _context.JournalEntries.Add(journalEntry);
        await _context.SaveChangesAsync();

        // After saving, journalEntry.journalId is populated, but we should ensure lines have it if EF didn't do it
        // (EF usually does it automatically if relationship is configured, but let's be safe if it's already added)
        
        // If the journal entry is already approved, post it to the ledger
        if (journalEntry.journalStatus == "Approved")
        {
            await _ledgerService.PostToLedger(journalEntry);
        }
        else if (journalEntry.journalStatus == "Pending")
        {
            try
            {
                await _notificationService.EmailManager("New Journal Entry Pending Approval", $"A new journal entry (ID: {journalEntry.journalId}) has been submitted and is pending approval.");
            }
            catch { /* Email notification is best-effort — do not fail the save */ }
        }

        return journalEntry;
    }

    public async Task<JournalModel?> GetJournalEntry(int id)
    {
        return await _context.JournalEntries
            .Include(j => j.journalLines)
            .FirstOrDefaultAsync(j => j.journalId == id);
    }

    public async Task<JournalModel> UpdateJournalStatus(int id, string status, int approverId, string approverName, string? comment = null)
    {
        var journal = await _context.JournalEntries
            .Include(j => j.journalLines)
            .FirstOrDefaultAsync(j => j.journalId == id);

        if (journal == null) throw new Exception("Journal entry not found.");

        journal.journalStatus = status;
        journal.approverId = approverId;
        journal.approverName = approverName;
        if (comment != null) journal.comment = comment;

        if (status == "Approved")
        {
            await _ledgerService.PostToLedger(journal);
            try
            {
                await _notificationService.EmailAccountant("Journal Entry Approved", $"Journal entry {id} has been approved by {approverName}.");
            }
            catch { /* Email notification is best-effort */ }
        }
        else if (status == "Rejected")
        {
            try
            {
                await _notificationService.EmailAccountant("Journal Entry Rejected", $"Journal entry {id} has been rejected by {approverName}.");
            }
            catch { /* Email notification is best-effort */ }
        }

        await _context.SaveChangesAsync();
        return journal;
    }
}