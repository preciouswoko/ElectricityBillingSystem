using ElectricityBillingSystem.Application.Abstraction;
using ElectricityBillingSystem.Application.Concrete;
using ElectricityBillingSystem.Application.DTOs;
using ElectricityBillingSystem.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ElectricityBillingSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
   
    public class UserController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IUserService _userService;
        private readonly ILogger<UserController> _logger;

        public UserController(IConfiguration config, IUserService userService, ILogger<UserController> logger)
        {
            _config = config;
            _userService = userService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
        {
            _logger.LogInformation("Login attempt for email: {Email}", loginRequest.Email);
            if (!ModelState.IsValid) {
                return Unauthorized("Invalid credentials");
            }
            var user = await _userService.AuthenticateUserAsync(loginRequest.Email, loginRequest.Password);
            if (user == null)
            {
                _logger.LogWarning("Unauthorized login attempt for email: {Email}", loginRequest.Email);
                return Unauthorized("Invalid credentials");
            }

            var token = _userService.GenerateJwtToken(user);

            await _userService.StoreJwtTokenInCacheAsync(user.Id, token);

            _logger.LogInformation("User ID {UserId} successfully logged in", user.Id);
            return Ok(new { Token = token });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Logout attempt failed - no valid user ID found");
                return Unauthorized("User not logged in");
            }

            await _userService.RemoveJwtTokenFromCacheAsync(userId);
            _logger.LogInformation("User ID {UserId} successfully logged out", userId);
            return Ok("Logout successful");
        }
    }

}
