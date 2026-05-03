using Microsoft.EntityFrameworkCore;
using FirstClassFinance.Models;

namespace FirstClassFinance.Data
{
    public class AppDBContext : DbContext
    {
        public AppDBContext(DbContextOptions<AppDBContext> options)
            : base(options)
        {
        }

        public DbSet<UserModel> Users { get; set; } // individual users within the DB
        public DbSet<CreateUserRequest> RegistrationRequests { get; set; } // user requests table within DB
        public DbSet<AccountModel> Accounts { get; set; } // chart of accounts table within DB
        public DbSet<EventLogModel> EventLogs { get; set; } // event log table within the DB
        public DbSet<JournalModel> JournalEntries { get; set; }
        public DbSet<JournalLine> JournalLines { get; set; }
        public DbSet<LedgerEntryModel> LedgerEntries { get; set; }
        public DbSet<ErrorMessageModel> ErrorMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User table configuration
            modelBuilder.Entity<UserModel>(entity =>
            {
                entity.HasKey(u => u.Id);

                entity.Property(u => u.EmployeeUsername)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(u => u.EmailAddress)
                    .HasMaxLength(100);

                entity.Property(u => u.PasswordHash)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(u => u.Role)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(u => u.SecurityQuestion)
                    .HasMaxLength(200);

                entity.Property(u => u.SecurityAnswer)
                    .HasMaxLength(200);

                entity.HasIndex(u => u.EmployeeUsername)
                    .IsUnique();
            });

            // Registration request configuration
            modelBuilder.Entity<CreateUserRequest>(entity =>
            {
                entity.HasKey(r => r.Id);

                entity.Property(r => r.FirstName)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(r => r.LastName)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(r => r.EmailAddress)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(r => r.Address)
                    .HasMaxLength(200);
            });

            // account creation configuration
            modelBuilder.Entity<AccountModel>(entity =>
            {
                entity.HasKey(a => a.Id);

                entity.Property(a => a.AccountName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(a => a.AccountNumber)
                    .IsRequired();

                entity.Property(a => a.AccountDescription)
                    .HasMaxLength(200);

                entity.Property(a => a.Category)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(a => a.Subcategory)
                    .HasMaxLength(50);

                entity.Property(a => a.NormalSide)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(a => a.Statement)
                    .HasMaxLength(10);

                entity.Property(a => a.Comment)
                    .HasMaxLength(250);

                entity.HasIndex(a => a.AccountNumber)
                    .IsUnique();
            });

            // event log configuration
            modelBuilder.Entity<EventLogModel>(entity =>
            {
                entity.HasKey(a => a.EventLogID);
                
                entity.Property(a => a.EntityID)
                    .IsRequired()
                    .HasMaxLength(10);
                
                entity.Property(a => a.EntityName)
                    .IsRequired()
                    .HasMaxLength(50);
                
                entity.Property(a => a.ActionType)
                    .IsRequired();

                entity.Property(a => a.UserID)
                    .IsRequired();

                entity.Property(a => a.EntityBeforeState);
                
                entity.Property(a => a.EntityAfterState);
                
                entity.Property(a => a.ChangeTimeStamp)
                    .IsRequired();
            });

            // Journal Entry relationship
            modelBuilder.Entity<JournalModel>(entity =>
            {
                entity.HasKey(j => j.journalId);
                entity.HasMany(j => j.journalLines)
                    .WithOne()
                    .HasForeignKey(l => l.journalId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
    }
    }
}