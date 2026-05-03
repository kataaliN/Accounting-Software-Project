using System.ComponentModel.DataAnnotations;

namespace FirstClassFinance.Models
{
    public class ApproveUserRequest
    {
        [Required]
        public string Role { get; set; }

        [Required]
        public string Password { get; set; }
    }
}