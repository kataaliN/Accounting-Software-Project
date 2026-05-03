using System;
using System.Collections.Generic;
using System.Linq;
using FirstClassFinance.Data;
using FirstClassFinance.Models;
using System.Threading.Tasks;
using FirstClassFinance.Interfaces;
using Org.BouncyCastle.Crypto.Generators;

namespace FirstClassFinance.Services
{
    public class UserService
    {
        private readonly AppDBContext _context;
        private readonly IEmailService _emailService;
        private readonly INotificationService _notificationService;

        public UserService(AppDBContext context, IEmailService emailService, INotificationService notificationService)
        {
            _context = context;
            _emailService = emailService;
            _notificationService = notificationService;
        }

        private bool IsPasswordValid(string password)
        {
            if (string.IsNullOrWhiteSpace(password)) return false;
            if (password.Length < 8) return false;
            if (!password.Any(char.IsUpper)) return false;
            if (!password.Any(char.IsLower)) return false;
            if (!password.Any(char.IsDigit)) return false;
            if (!password.Any(ch => !char.IsLetterOrDigit(ch))) return false;

            return true;
        }

        private string GenerateUsername(string firstName, string lastName)
        {
            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                throw new ArgumentException("Invalid name provided.");

            var firstInitial = firstName.Substring(0, 1).ToUpper();
            var formattedLastName = lastName.Replace(" ", "");
            var datePart = DateTime.UtcNow.ToString("MMyy");

            var baseUsername = $"{firstInitial}{formattedLastName}{datePart}";

            var username = baseUsername;
            int counter = 1;

            while (_context.Users.Any(u => u.EmployeeUsername == username))
            {
                username = $"{baseUsername}{counter}";
                counter++;
            }

            return username;
        }

        public async Task<UserModel> ApproveRegistration(int requestId, string role, string password)
        {
            var request = _context.RegistrationRequests.Find(requestId);

            if (request == null)
                throw new InvalidOperationException("Registration request not found.");

            if (!IsPasswordValid(password))
                throw new ArgumentException("Password does not meet complexity requirements.");

            var validRoles = new[] { "Administrator", "Manager", "Accountant" };

            if (!Enumerable.Contains(validRoles, role))
                throw new ArgumentException("Invalid role specified.");

            var userName = GenerateUsername(request.FirstName, request.LastName);

            var newUser = new UserModel
            {
                EmployeeUsername = userName,
                EmailAddress = request.EmailAddress,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);

            request.IsApproved = true;

            await _context.SaveChangesAsync();
            
            // Actual email sending
            try
            {
                await _emailService.SendEmailAsync(request.EmailAddress, "Account Approved",
                    $"Your account has been approved. Username: {userName}. Login at: /index.html");
            }
            catch (Exception ex)
            {
                // Log the error but don't fail the approval
                Console.WriteLine($"[ERROR] Failed to send approval email: {ex.Message}");
            }

            return newUser;
        }

        public async Task<CreateUserRequest> SubmitRegistrationRequest(CreateUserRequest request)
        {
            _context.RegistrationRequests.Add(request);
            await _context.SaveChangesAsync();

            // Actual email to administrator
            try
            {
                await _notificationService.EmailAdministrator("New Registration Request",
                    $"A new registration request has been submitted by {request.FirstName} {request.LastName} ({request.EmailAddress}).");
            }
            catch (Exception ex)
            {
                // Log the error but don't fail the registration request submission
                Console.WriteLine($"[ERROR] Failed to send notification email: {ex.Message}");
            }

            return request;
        }

        public List<UserModel> GetAllUsers()
        {
            return _context.Users.ToList();
        }

        public async Task<UserModel> ToggleUserStatus(int userId)
        {
            var user = _context.Users.Find(userId);
            if (user == null)
                throw new InvalidOperationException("User not found.");

            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();
            return user;
        }

        public List<CreateUserRequest> GetPendingRequests()
        {
            return _context.RegistrationRequests.Where(r => !r.IsApproved).ToList();
        }

        public async Task RejectRegistration(int requestId)
        {
            var request = _context.RegistrationRequests.Find(requestId);
            if (request == null)
                throw new InvalidOperationException("Registration request not found.");
            _context.RegistrationRequests.Remove(request);
            await _context.SaveChangesAsync();
        }
    }
}