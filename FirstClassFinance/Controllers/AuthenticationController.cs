using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using FirstClassFinance.Models;
using FirstClassFinance.Services;
using FirstClassFinance.Interfaces;
using Microsoft.Extensions.Configuration;

namespace FirstClassFinance.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly AuthenticationService _authService;
        private readonly IEmailService _emailService;

        public AuthenticationController(IConfiguration configuration, AuthenticationService authService, IEmailService emailService)
        {
            _configuration = configuration;
            _authService = authService;
            _emailService = emailService;
        }

        // POST: api/authentication/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Invalid request format." });

            var user = await _authService.Authenticate(request.Username, request.Password, request.Role);

            if (user == null)
            {
                return Unauthorized(new
                {
                    message = "Invalid credentials or account has been suspended."
                });
            }

            var token = _authService.GenerateJwtToken(user);

            return Ok(new LoginResponse
            {
                Token = token,
                UserId = user.Id,
                Username = user.EmployeeUsername,
                Role = user.Role,
                Message = "Login successful!"
            });
        }

        // POST: api/authentication/forgot-password
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Invalid request format." });

            var user = await _authService.ForgotPassword(request);

            if (user != null)
            {
                await _emailService.SendEmailAsync(
                    user.EmailAddress,
                    "Password Reset",
                    $"Your security question is: {user.SecurityQuestion}");
            }

            return Ok(new { message = "If the account exists, a security question has been sent. Check your email for next steps." });
        }

        // POST: api/authentication/reset-password
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Invalid request format." });

            var success = await _authService.ResetPassword(request);

            if (!success)
            {
                return BadRequest(new { message = "Invalid security answer or user details." });
            }

            return Ok(new { message = "Password has been reset successfully!" });
        }
    }
}