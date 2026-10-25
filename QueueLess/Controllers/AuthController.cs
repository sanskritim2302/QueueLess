using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using QueueLess.DTOs;
using QueueLess.Settings;

namespace QueueLess.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly JwtSettings _jwtSettings;
        private readonly PasswordHasher<string> _passwordHasher = new();

        public AuthController(
            IConfiguration configuration,
            IOptions<JwtSettings> jwtOptions)
        {
            _configuration = configuration;
            _jwtSettings = jwtOptions.Value;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            var username =
                _configuration["StaffCredentials:Username"];

            var configuredPassword =
                _configuration["StaffCredentials:Password"];

            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(configuredPassword))
            {
                return StatusCode(500, new
                {
                    message = "Staff credentials are not configured."
                });
            }

            // Hash the configured development password, then verify
            // the supplied password against that hash.
            var passwordHash = _passwordHasher.HashPassword(
                username, configuredPassword);

            var verification = _passwordHasher.VerifyHashedPassword(
                username, passwordHash, request.Password ?? string.Empty);

            if (!string.Equals(
                    request.Username, username,
                    StringComparison.Ordinal) ||
                verification == PasswordVerificationResult.Failed)
            {
                return Unauthorized(new
                {
                    message = "Invalid username or password."
                });
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, username),
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, "Staff")
            };

            var signingKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));

            var credentials = new SigningCredentials(
                signingKey, SecurityAlgorithms.HmacSha256);

            var expiresAt = DateTime.UtcNow.AddMinutes(
                _jwtSettings.ExpiryMinutes);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials);

            return Ok(new
            {
                accessToken = new JwtSecurityTokenHandler()
                    .WriteToken(token),
                tokenType = "Bearer",
                expiresAt
            });
        }
    }
}
