using System;
using System.ComponentModel.DataAnnotations;

namespace FirstClassFinance.Models
{
    public class UserModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string EmployeeUsername { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        public string ResetToken { get; set; }

        public DateTime? ResetTokenExpiry { get; set; }

        [EmailAddress]
        public string? EmailAddress { get; set; }

        [Required]
        [StringLength(30)]
        public string Role { get; set; } // Administrator, Manager, Accountant

        public bool IsActive { get; set; } = false;

        public int FailedLoginAttempts { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(200)]
        public string? SecurityQuestion { get; set; }

        [StringLength(200)]
        public string? SecurityAnswer { get; set; }
    }
}