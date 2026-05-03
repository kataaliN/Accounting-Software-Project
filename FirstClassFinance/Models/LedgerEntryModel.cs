using System;
using System.ComponentModel.DataAnnotations;

namespace FirstClassFinance.Models;

public class LedgerEntryModel
{
    [Key]
    [Required]
    public int ledgerId { get; set; }
    
    public int journalEntryId { get; set; }
    public int accountId { get; set; }

    public DateTime date { get; set; } = DateTime.UtcNow;
    
    [Required]
    public decimal debits { get; set; }
    [Required]
    public decimal credits { get; set; }
    
    [Required]
    public decimal balance { get; set; }
}