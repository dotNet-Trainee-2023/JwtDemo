﻿using JwtDemo.Data;
using JwtDemo.Dtos;
using JwtDemo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
                    PasswordHash = passwordHash,
                    Role = dto.Role
                };

                //var findUser = _context.Users.SingleOrDefault(x => x.Username == dto.Username);
                //if (findUser != null)
                //{
                //    return BadRequest("Username Exists");
                //}
                //findUser = _context.Users.SingleOrDefault(x => x.Email == dto.Email);
                //if (findUser != null)
                //{
                //    return BadRequest("Email Exists");
                //}

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return Ok(user);
        }

        [HttpPost("login")]
        public IActionResult Login(UserLoginDto dto)
        {
            var user = _context.Users.SingleOrDefault(x => x.Username == dto.Username);

            if (user == null)
                return BadRequest("Username or password incorrect.");

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return BadRequest("Username or password incorrect.");

            string token = GenerateToken(user);

            return Ok(new { token });
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
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.GetSection("AppSettings:Secret").Value!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);
            var expiretime = DateTime.Now.AddHours(1);
            var token = new JwtSecurityToken(claims: claims, expires: expiretime, signingCredentials: creds);
            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            CookieOptions cookieOptions = new CookieOptions()
            {
                Expires = expiretime,
                Secure = true
            };
            Response.Cookies.Append("jwt-token", jwt, cookieOptions);

            return jwt;
        }
    }
}
