using System.ComponentModel.DataAnnotations;

namespace FirstClassFinance.Models
{
        public class ResetPasswordRequest
        {
            [Required]
            [StringLength(50)]
            public string Username { get; set; }

            [Required]
            [EmailAddress]
            [StringLength(100)]
            public string EmailAddress { get; set; }

            [Required]
            public string Token {get; protected set;}

            [Required]
            [StringLength(200)]
            public string SecurityAnswer { get; set; }

            [Required]
            [StringLength(100, MinimumLength = 8)]
            public string NewPassword { get; set; }
        }
}