using System;
using FirstClassFinance.Data;
using FirstClassFinance.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using FirstClassFinance.Interfaces;
using Org.BouncyCastle.Crypto.Generators;

namespace FirstClassFinance.Services
{
    public class AuthenticationService
    {
        private readonly AppDBContext _context;
        private readonly IConfiguration _config;
        private readonly INotificationService _notificationService;

        public AuthenticationService(AppDBContext context, IConfiguration config, INotificationService notificationService)
        {
            _context = context;
            _config = config;
            _notificationService = notificationService;
        }

        public async Task<UserModel> Authenticate(string username, string password, string role)
        {
            var user = _context.Users.FirstOrDefault(u => u.EmployeeUsername == username);

            if (user == null)
                return null;

            if (!user.IsActive)
                return null;

            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                user.FailedLoginAttempts++;

                if (user.FailedLoginAttempts >= 3)
                {
                    user.IsActive = false; // temporary suspension
                    try
                    {
                        await _notificationService.EmailAdministrator("User Account Suspended",
                            $"User account '{username}' has been suspended due to too many failed login attempts.");
                    }
                    catch (Exception ex)
                    {
                        // Log the error but don't fail the login process
                        Console.WriteLine($"[ERROR] Failed to send suspension notification email: {ex.Message}");
                    }
                }

                await _context.SaveChangesAsync();
                return null;
            }
            
            // Role validation
            if (user.Role != role)
            {
                return null;
            }

            user.FailedLoginAttempts = 0;
            await _context.SaveChangesAsync();

            return user;
        }

        public string GenerateJwtToken(UserModel user)
        {
            var jwtSettings = _config.GetSection("Jwt");
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["Key"])
            );

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256
            );

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.EmployeeUsername),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(
                    Convert.ToDouble(jwtSettings["DurationInMinutes"])
                ),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        public async Task<UserModel> ForgotPassword(ForgotPasswordRequest request)
        {
            var user = _context.Users.FirstOrDefault(u => u.EmployeeUsername == request.Username && u.EmailAddress == request.EmailAddress);

            if (user == null)
                return null;

            // Generate a simple reset token (in production use a secure GUID or similar)
            var resetToken = Guid.NewGuid().ToString();
            user.ResetToken = resetToken;
            user.ResetTokenExpiry = DateTime.UtcNow.AddHours(24);
            await _context.SaveChangesAsync();

            try
            {
                await _notificationService.EmailUser(user.EmailAddress, "Password Reset Request",
                    $"Hello {user.EmployeeUsername},\n\n" +
                    $"You requested a password reset. Use this token to reset your password: {resetToken}\n\n" +
                    $"If you did not request this, please ignore this email.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to send forgot password email: {ex.Message}");
            }

            return user;
        }

        public async Task<bool> ResetPassword(ResetPasswordRequest request)
        {
            var user = _context.Users.FirstOrDefault(u => u.EmployeeUsername == request.Username && u.EmailAddress == request.EmailAddress);

            if (user == null)
                return false;

            if (!string.Equals(user.SecurityAnswer, request.SecurityAnswer, StringComparison.OrdinalIgnoreCase))
                return false;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.IsActive = true;
            user.FailedLoginAttempts = 0;

            await _context.SaveChangesAsync();

            try
            {
                await _notificationService.EmailUser(user.EmailAddress, "Password Reset Successful",
                    $"Hello {user.EmployeeUsername},\n\n" +
                    $"Your password has been successfully reset. If you did not perform this action, please contact an administrator immediately.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to send password reset success email: {ex.Message}");
            }

            return true;
        }
    }
}