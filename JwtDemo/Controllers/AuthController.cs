using JwtDemo.Data;
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
        private readonly IConfiguration _config;
        private readonly AppDbContext _context;

        public AuthController(IConfiguration config, AppDbContext context)
        {
            _config = config;
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(UserRegistrationDto dto)
        {
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var user = new User
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                Username = dto.Username,
                PasswordHash = passwordHash
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(user);
        }

        [HttpPost("login")]
        public IActionResult Login(UserLoginDto dto)
        {
            var user = _context.Users.SingleOrDefault(x => x.Username == dto.Username);

            if (user == null)
                return BadRequest("Username or password.");

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return BadRequest("Username or password.");

            string token = GenerateToken(user);

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
                new Claim(ClaimTypes.Role, user.Role.ToString()),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.GetSection("AppSettings:Secret").Value!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);
            var token = new JwtSecurityToken(claims: claims, expires: DateTime.Now.AddHours(1), signingCredentials: creds);
            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return jwt;
        }
    }
}
