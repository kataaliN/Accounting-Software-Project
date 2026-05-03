using System.Threading.Tasks;
using System;
using FirstClassFinance.Models;
using FirstClassFinance.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FirstClassFinance.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserService _userService;
        public UsersController(UserService userService)
        {
            _userService = userService;
        }

        // POST api/users
        [HttpPost]
        public async Task<IActionResult> SubmitRequest([FromBody] CreateUserRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Invalid registration request." });

            try
            {
                var createdUser = await _userService.SubmitRegistrationRequest(request);

                return Ok(new
                {
                    message = "Registration request submitted for approval.",
                    request = createdUser
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET api/users/pending
        [HttpGet("pending")]
        [Authorize(Roles = "Administrator")]
        public IActionResult GetPendingRequests()
        {
            try
            {
                var pendingRequests = _userService.GetPendingRequests();

                return Ok(pendingRequests);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT api/users/approve/{id}
        [HttpPut("approve/{id}")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> ApproveRegistrationRequest(int id, [FromBody] ApproveUserRequest request)
        {
            try
            {
                var user = await _userService.ApproveRegistration(id, request.Role, request.Password);

                return Ok(new
                {
                    message = "User request approved.",
                    user
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        // GET api/users
        [HttpGet]
        [Authorize(Roles = "Administrator")]
        public IActionResult GetAllUsers()
        {
            try
            {
                var users = _userService.GetAllUsers();
                return Ok(users);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        // DELETE api/users/reject/{id}
        [HttpDelete("reject/{id}")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> RejectRegistrationRequest(int id)
        {
            try
            {
                await _userService.RejectRegistration(id);
                return Ok(new { message = "Registration request rejected and removed." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT api/users/toggle-status/{id}
        [HttpPut("toggle-status/{id}")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> ToggleUserStatus(int id)
        {
            try
            {
                var user = await _userService.ToggleUserStatus(id);
                return Ok(new { message = "User status toggled.", user });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}