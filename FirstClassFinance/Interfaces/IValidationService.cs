using System.Collections.Generic;
using System.Threading.Tasks;
using FirstClassFinance.Models;

namespace FirstClassFinance.Interfaces;

public interface IValidationService
{
    Task<List<string>> ValidateJournalEntry(JournalModel journalEntry);
}