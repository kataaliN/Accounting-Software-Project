using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FirstClassFinance.Models;

public class JournalModel
{
    [Key]
    [Required]
    public int journalId { get; set; }
    
    [Required]
    [StringLength(100)]
    public string? journalName { get; set; }
    
    public DateTime entryDate { get; set; } = DateTime.UtcNow;
    
    public string journalStatus { get; set; } // is it pending, approved, rejected
    
    [Required]
    public int creatorId { get; set; }
    public string? creatorName { get; set; }
    
    public int? approverId { get; set; }
    public string? approverName { get; set; }
    
    public string? comment { get; set; }

    public List<JournalLine> journalLines { get; set; } = new();
    public List<SourceDocumentsModel> sourceDocument { get; set; } = new();
}