using System.ComponentModel.DataAnnotations;

namespace FirstClassFinance.Models
{
        public class ForgotPasswordRequest
        {
            [Required]
            [StringLength(50)]
            public string Username { get; set; }

            [Required]
            [EmailAddress]
            [StringLength(100)]
            public string EmailAddress { get; set; }
        }
}