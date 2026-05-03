using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FirstClassFinance.Models
{
    [Table("JournalLineEntries")]
    public class JournalLine
    {
        [Key]
        [Column("entryId")]
        public int entryId { get; set; }

        public int journalId { get; set; }

        [Required]
        public int accountId { get; set; }

        [Required]
        public string entryType { get; set; } // "Debit" or "Credit"

        [Required]
        public decimal amount { get; set; }
    }
}