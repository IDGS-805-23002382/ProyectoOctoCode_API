using AuthenticationAPI.DTO;
using AuthenticationAPI.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthenticationAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _service;

        public AuthController(IAuthService service)
        {
            _service = service;
        }

        [Authorize(Roles = "Administrador,admin")]
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequestDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _service.RegisterAsync(dto);
            if (!result.Success) return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // 🤖 Aquí puedes meter luego tu validación de Captcha de forma limpia si lo deseas
            var result = await _service.LoginAsync(model);

            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("verify-2fa")]
        public async Task<IActionResult> Verify2FA([FromBody] Verify2FaDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _service.Verify2FaAsync(model);
            if (!result.Success) return BadRequest(result);

            return Ok(result);
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult Me()
        {
            return Ok(new
            {
                Id = User.FindFirst("sub")?.Value,
                Email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                           ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email)?.Value,
                Nombre = User.Identity?.Name,
                Roles = User.Claims
                    .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                    .Select(c => c.Value)
            });
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                         ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            // 🚀 AHORA SÍ LLAMAMOS A LA BASE DE DATOS
            var result = await _service.ChangePasswordAsync(userId, dto);

            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(new { message = result.Message });
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var id = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(id)) return Unauthorized();

            await _service.LogoutAsync(id);
            return Ok("Sesión cerrada");
        }


        [Authorize(Roles = "Administrador,admin")]
        [HttpPost("reset-user-password/{userId}")]
        public async Task<IActionResult> ResetUserPassword(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return BadRequest("El ID de usuario es obligatorio.");

            // 🚀 Llamamos a la lógica real del servicio
            var result = await _service.ResetUserPasswordAsync(userId);

            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(new { message = result.Message });
        }
    }
}