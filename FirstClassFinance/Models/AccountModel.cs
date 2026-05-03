using System;
using System.ComponentModel.DataAnnotations;

namespace FirstClassFinance.Models
{
    public class AccountModel
    {
        public int Id { get; set; } //PK in our table

        [Required]
        [StringLength(100)]
        public string AccountName { get; set; }

        [Required]
        public int AccountNumber { get; set; }

        [StringLength(200)]
        public string AccountDescription { get; set; }

        [Required]
        public string NormalSide { get; set; } // Debit or Credit

        [Required]
        public string Category { get; set; } // is it Asset, Liability, Equity etc.

        public string Subcategory { get; set; }

        [Range(0, double.MaxValue)]
        public decimal InitialBalance { get; set; }

        public decimal Debit { get; set; }

        public decimal Credit { get; set; }

        public decimal Balance { get; set; }

        public DateTime DateAdded { get; set; } = DateTime.UtcNow;

        public int UserId { get; set; } //who added this account

        public int Order { get; set; }

        public string Statement { get; set; } // is it BS, IS etc

        public string Comment { get; set; }

        public bool IsActive { get; set; } = true;
    }
}