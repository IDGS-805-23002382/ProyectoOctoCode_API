using AuthenticationAPI.Configuration;
using AuthenticationAPI.interfaces;
using AuthenticationAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AuthenticationAPI.Services
{
   
        public class TokenService : ITokenService
        {
            private readonly JwtSettings _jwt;
            private readonly UserManager<ApplicationUser> _userManager;
            public TokenService(
                IOptions<JwtSettings> jwt,
                UserManager<ApplicationUser> userManager)
            {
                _jwt = jwt.Value;
                _userManager = userManager;
            }
            public async Task<string> CreateTokenAsync(ApplicationUser user)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub,user.Id),
                new(JwtRegisteredClaimNames.Email,user.Email!),
                new(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString()),
                new(ClaimTypes.Name,user.NombreCompleto)
            };
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
                var key = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(_jwt.SecretKey));
                var credentials =
                    new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                var token = new JwtSecurityToken(
                    issuer: _jwt.Issuer,
                    audience: _jwt.Audience,
                    claims: claims,
                    expires: DateTime.UtcNow.AddMinutes(_jwt.ExpirationInMinutes),
                    signingCredentials: credentials);
                return new JwtSecurityTokenHandler().WriteToken(token);
            }
            public string GenerateRefreshToken()
            {
                var random = new byte[64];
                using var rng = RandomNumberGenerator.Create();
                rng.GetBytes(random);
                return Convert.ToBase64String(random);
            }
        }
    }
