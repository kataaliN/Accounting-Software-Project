using System.ComponentModel.DataAnnotations;

namespace FirstClassFinance.Models
{
    public class ErrorMessageModel
    {
        [Key]
        [Required]
        public int messageId { get; set; }
    
        public string messageCode { get; set; }
    
        public string message { get; set; }
    }
}