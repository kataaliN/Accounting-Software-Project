using System;
using System.ComponentModel.DataAnnotations;

namespace FirstClassFinance.Models;
public class SourceDocumentsModel
{
    [Key]
    [Required]
    public int documentId { get; set; }
    
    [Required]
    public int journalEntryId { get; set; }
    
    [Required]
    public string documentType { get; set; } // is it a doc, PDF, jpeg etc or is it a receipt, sales form etc
    public string documentURL { get; set; }
    public DateTime uploadDate { get; set; } = DateTime.UtcNow;
}