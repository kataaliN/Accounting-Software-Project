using System.ComponentModel.DataAnnotations;

namespace FirstClassFinance.Models
{
        public class LoginResponse
        {
            [Required]
            public string Token { get; set; }

            public int UserId { get; set; }

            [Required]
            [StringLength(50)]
            public string Username { get; set; }

            [Required]
            [StringLength(30)]
            public string Role { get; set; }

            [Url]
            public string? ProfilePictureURL { get; set; }

            [StringLength(200)]
            public string Message { get; set; }
        }
}