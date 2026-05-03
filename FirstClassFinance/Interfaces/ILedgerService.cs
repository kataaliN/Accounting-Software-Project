using System.Collections.Generic;
using System.Threading.Tasks;
using FirstClassFinance.Models;

namespace FirstClassFinance.Interfaces;

public interface ILedgerService
{
    Task PostToLedger(JournalModel journalEntry);
    Task<List<LedgerEntryModel>> GetLedgerEntries(int? accountId = null);
}