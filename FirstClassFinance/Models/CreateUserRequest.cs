using System;
using System.ComponentModel.DataAnnotations;

namespace FirstClassFinance.Models
{
    public class CreateUserRequest
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        public string LastName { get; set; }

        [Required]
        public DateTime DOB { get; set; }

        [Required]
        [StringLength(200)]
        public string Address { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string EmailAddress { get; set; }

        public bool IsApproved { get; set; } = false;

        public DateTime RequestSubmittedAt { get; set; } = DateTime.UtcNow;
    }
}