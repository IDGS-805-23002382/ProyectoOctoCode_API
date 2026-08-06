using AuthenticationAPI.DTO;
using AuthenticationAPI.interfaces;
using AuthenticationAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AuthenticationAPI.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _correoService; // Inyectamos tu servicio de correo corporativo

        public AuthService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,
            IEmailService correoService) // Agregado al constructor
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _correoService = correoService;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto)
        {
            var userExists = await _userManager.FindByEmailAsync(dto.Correo);
            if (userExists != null)
                return new AuthResponseDto { Success = false, Message = "El correo ya está registrado." };

            string tempPassword = GenerateRandomPassword();

            var newUser = new ApplicationUser
            {
                UserName = dto.Correo,
                Email = dto.Correo
            };

            var result = await _userManager.CreateAsync(newUser, tempPassword);

            if (!result.Succeeded)
                return new AuthResponseDto { Success = false, Message = "Error al crear el usuario." };

            if (await _roleManager.RoleExistsAsync(dto.Rol))
            {
                await _userManager.AddToRoleAsync(newUser, dto.Rol);
            }

            return new AuthResponseDto
            {
                Success = true,
                Message = $"Usuario registrado exitosamente. Contraseña temporal: {tempPassword}",
                Token = null,
                RefreshToken = null
            };
        }

        // Fusión de Login con flujo obligatorio de 2FA
        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Correo);
            if (user == null)
            {
                return new AuthResponseDto { Success = false, Message = "Cuenta o contraseña inválida, inténtalo de nuevo." };
            }

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!isPasswordValid)
            {
                return new AuthResponseDto { Success = false, Message = "Cuenta o contraseña inválida, inténtalo de nuevo." };
            }

            // Validar Estatus de Cuenta Activa / Inactiva
            if (user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.Now)
            {
                return new AuthResponseDto { Success = false, Message = "Tu cuenta se encuentra inactiva. Contacta al administrador." };
            }

            // CANDADO SEGURO: Evitar ráfagas masivas y spam de correos
            if (user.TwoFactorExpiry.HasValue)
            {
                var tiempoRestante = user.TwoFactorExpiry.Value - DateTime.UtcNow;

                // Si el token expira en 5 min, y restan más de 4 min, significa que se creó hace menos de 60 segundos
                if (tiempoRestante.TotalMinutes > 4.0)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Ya se ha generado un código recientemente. Por favor, revisa tu bandeja o espera 60 segundos antes de solicitar otro."
                    };
                }
            }

            // 1. Generar código numérico OTP de 6 dígitos
            var random = new Random();
            var codigoOtp = random.Next(100000, 999999).ToString();

            // 2. Almacenar código temporal con vigencia de 5 minutos
            user.TwoFactorCode = codigoOtp;
            user.TwoFactorExpiry = DateTime.UtcNow.AddMinutes(5);
            await _userManager.UpdateAsync(user);

            // 3. Despachar correo de seguridad firmado por la empresa
            var mensajeHtml = $@"
            <div style='font-family: sans-serif; padding: 20px; max-width: 500px; border: 1px solid #e2e8f0; border-radius: 12px;'>
                <h2 style='color: #4c1d95; font-weight: 900;'>OCTO-CODE</h2>
                <p style='color: #64748b; font-size: 14px;'>Tu código de verificación de seguridad para el acceso al panel ACPAN es:</p>
                <div style='background-color: #f5f3ff; border: 1px dashed #7c3aed; padding: 15px; text-align: center; border-radius: 8px; margin: 20px 0;'>
                    <span style='font-size: 24px; font-weight: 900; color: #7c3aed; letter-spacing: 4px;'>{codigoOtp}</span>
                </div>
                <p style='color: #94a3b8; font-size: 11px;'>Este código expirará en 5 minutos. Si no solicitaste este acceso, por favor ignora este correo.</p>
            </div>";

            await _correoService.SendEmailAsync(user.Email!, "Código de Verificación - OCTO-CODE", mensajeHtml);

            // 4. Retornar bandera para que Angular active la vista de OTP
            return new AuthResponseDto
            {
                Success = true,
                Requiere2FA = true,
                Email = user.Email,
                Message = "Código de seguridad enviado al correo electrónico."
            };
        }

        // Validación y expedición final del Token JWT
        public async Task<AuthResponseDto> Verify2FaAsync(Verify2FaDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return new AuthResponseDto { Success = false, Message = "Usuario no encontrado." };

            // Comprobar coincidencia y tiempo de expiración
            if (user.TwoFactorCode != dto.Codigo || user.TwoFactorExpiry < DateTime.UtcNow)
            {
                return new AuthResponseDto { Success = false, Message = "El código de verificación es incorrecto o ya expiró." };
            }

            // Limpiar credenciales temporales usadas
            user.TwoFactorCode = null;
            user.TwoFactorExpiry = null;
            await _userManager.UpdateAsync(user);

            // Generar claims y construir el Token JWT final
            var userRoles = await _userManager.GetRolesAsync(user);

            // 👤 Extraemos el nombre real desde tu propiedad personalizada NombreCompleto
            string nombreLimpio = !string.IsNullOrEmpty(user.NombreCompleto)
                ? user.NombreCompleto
                : (user.UserName!.Contains("@") ? user.UserName.Split('@')[0] : user.UserName);

            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email!),
                
                // Asignamos el nombre real para que Angular lo pinte en la bienvenida y el header
                new Claim(ClaimTypes.Name, nombreLimpio),

                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            foreach (var userRole in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, userRole));
            }

            var token = CreateToken(authClaims);
            var expiration = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["JwtSettings:ExpirationInMinutes"]));

            return new AuthResponseDto
            {
                Success = true,
                Message = "Login exitoso",
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Expiration = expiration,
                RefreshToken = GenerateRefreshToken(),
                Requiere2FA = false
            };
        }

        public async Task LogoutAsync(string userId)
        {
            await Task.CompletedTask;
        }

        private JwtSecurityToken CreateToken(List<Claim> authClaims)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"]!));

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["JwtSettings:ExpirationInMinutes"])),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return token;
        }

        public async Task<AuthResponseDto> ChangePasswordAsync(string userId, ChangePasswordDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return new AuthResponseDto { Success = false, Message = "Usuario no encontrado." };
            }

            // 🔐 El método nativo de Identity verifica la contraseña actual y genera el hash de la nueva
            var result = await _userManager.ChangePasswordAsync(user, dto.PasswordActual, dto.NuevaPassword);

            if (!result.Succeeded)
            {
                // Agarra el primer error que devuelva Identity (ej: contraseña incorrecta o no cumple con mayúsculas/números)
                var errorPrincipal = result.Errors.FirstOrDefault()?.Description ?? "Error al actualizar la contraseña.";
                return new AuthResponseDto { Success = false, Message = errorPrincipal };
            }

            return new AuthResponseDto { Success = true, Message = "Contraseña actualizada con éxito." };
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        private string GenerateRandomPassword()
        {
            var random = new Random();
            int randomNumber = random.Next(1000, 9999);
            return $"Octo{randomNumber}!Plus";
        }

    public async Task<AuthResponseDto> ResetUserPasswordAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return new AuthResponseDto { Success = false, Message = "Usuario no encontrado." };
            }

            // 1. Generar nueva contraseña temporal aleatoria
            string tempPassword = GenerateRandomPassword();

            // 2. Remover la contraseña actual del usuario
            var removeResult = await _userManager.RemovePasswordAsync(user);
            if (!removeResult.Succeeded)
            {
                return new AuthResponseDto { Success = false, Message = "Error al remover la contraseña anterior." };
            }

            // 3. Asignar la nueva contraseña temporal
            var addResult = await _userManager.AddPasswordAsync(user, tempPassword);
            if (!addResult.Succeeded)
            {
                return new AuthResponseDto { Success = false, Message = "Error al asignar la contraseña temporal." };
            }

            // 4. Mandar el correo con el formato corporativo de la plataforma
            var mensajeHtml = $@"
    <div style='font-family: sans-serif; padding: 20px; max-width: 500px; border: 1px solid #e2e8f0; border-radius: 12px;'>
        <h2 style='color: #4c1d95; font-weight: 900;'>OCTO-CODE</h2>
        <p style='color: #64748b; font-size: 14px;'>Un administrador ha restablecido tus credenciales de acceso al panel ACPAN. Tu nueva contraseña temporal es:</p>
        <div style='background-color: #f5f3ff; border: 1px dashed #7c3aed; padding: 15px; text-align: center; border-radius: 8px; margin: 20px 0;'>
            <span style='font-size: 18px; font-weight: 900; color: #7c3aed; letter-spacing: 1px;'>{tempPassword}</span>
        </div>
        <p style='color: #94a3b8; font-size: 11px;'>Por motivos de seguridad, el sistema te solicitará cambiar esta clave temporal la próxima vez que intentes ingresar al panel.</p>
    </div>";

            await _correoService.SendEmailAsync(user.Email!, "Restablecimiento de Contraseña - OCTO-CODE", mensajeHtml);

            return new AuthResponseDto { Success = true, Message = "Contraseña temporal enviada con éxito." };
        }
    }
}