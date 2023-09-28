using JwtDemo.Dtos;
using JwtDemo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace JwtDemo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private static User _user = new User();
        private readonly IConfiguration _config;

        public AuthController(IConfiguration config)
        {
            _config = config;
        }

        [HttpPost("register")]
        public IActionResult Register(UserRegistrationDto dto)
        {
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            _user.Username = dto.Username;
            _user.PasswordHash = passwordHash;
            _user.Email = dto.Email;

            return Ok(_user);
        }

        [HttpPost("login")]
        public IActionResult Login(UserLoginDto dto)
        {
            if (dto.Username != _user.Username)
            {
                return BadRequest("User not found.");
            }

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, _user.PasswordHash))
            {
                return BadRequest("Wrong password.");
            }

            string token = GenerateToken(_user);

            return Ok(token);
        }

        [HttpGet, Authorize]
        public IActionResult GetUser()
        {
            var username = User?.Identity?.Name;
            var id = User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = User?.FindFirstValue(ClaimTypes.Email);

            var rolesClaims = User?.FindAll(ClaimTypes.Role);
            var roles = rolesClaims?.Select(x => x.Value).ToList();

            return Ok(new { id, username, email, roles });
        }

        private string GenerateToken(User user)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Username),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, "Admin"),
                //new Claim(ClaimTypes.Role, "User"),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.GetSection("AppSettings:Secret").Value!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);
            var token = new JwtSecurityToken(claims: claims, expires: DateTime.Now.AddHours(1), signingCredentials: creds);
            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return jwt;
        }
    }
}
