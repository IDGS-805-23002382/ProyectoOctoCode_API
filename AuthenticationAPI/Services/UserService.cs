using AuthenticationAPI.DTO;
using AuthenticationAPI.interfaces;
using AuthenticationAPI.Models;
using AuthenticationAPI.Services; 
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;

    public UserService(UserManager<ApplicationUser> userManager, IEmailService emailService)
    {
        _userManager = userManager;
        _emailService = emailService;
    }

    public async Task<List<UserDto>> GetAllAsync()
    {
        var users = await _userManager.Users.ToListAsync();
        var result = new List<UserDto>();
        foreach (var user in users)
        {
            result.Add(new UserDto
            {
                Id = user.Id,
                NombreCompleto = user.NombreCompleto,
                Email = user.Email!,
                FechaRegistro = user.FechaRegistro,
                Roles = await _userManager.GetRolesAsync(user),
                Estatus = user.LockoutEnd == null || user.LockoutEnd <= DateTimeOffset.Now
            });
        }
        return result;
    }

    public async Task<UserDto?> GetByIdAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return null;
        return new UserDto
        {
            Id = user.Id,
            NombreCompleto = user.NombreCompleto,
            Email = user.Email!,
            FechaRegistro = user.FechaRegistro,
            Roles = await _userManager.GetRolesAsync(user),
            Estatus = user.LockoutEnd == null || user.LockoutEnd <= DateTimeOffset.Now  
        };
    }

    public async Task<bool> CreateAsync(RegisterRequestDto dto)
    {
        string tempPassword = "Acpan" + Guid.NewGuid().ToString().Substring(0, 6) + "!";

        var user = new ApplicationUser
        {
            UserName = dto.Correo,
            Email = dto.Correo,
            NombreCompleto = dto.Nombre
        };

        var result = await _userManager.CreateAsync(user, tempPassword);
        if (!result.Succeeded) return false;

        if (!string.IsNullOrEmpty(dto.Rol))
        {
            await _userManager.AddToRoleAsync(user, dto.Rol);
        }

        try
        {
            string subject = "Bienvenido a OCTO-CODE - Tus datos de acceso";
            string htmlMessage = $@"
                <!DOCTYPE html>
                <html lang='es'>
                <head>
                    <meta charset='UTF-8'>
                    <style>
                        body {{ font-family: Arial, sans-serif; background-color: #f4f4f7; color: #333333; margin: 0; padding: 0; }}
                        .container {{ max-width: 600px; margin: 40px auto; background: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05); }}
                        .header {{ background-color: #9B51E0; padding: 30px; text-align: center; color: #ffffff; }}
                        .header h1 {{ margin: 0; font-size: 24px; font-weight: 800; }}
                        .content {{ padding: 30px; }}
                        .credentials-box {{ background-color: #f9f5ff; border-left: 4px solid #9B51E0; padding: 15px 20px; margin: 20px 0; border-radius: 8px; }}
                        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #888888; background-color: #f4f4f7; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>OCTO-CODE</h1>
                            <p style='margin: 5px 0 0 0; font-size: 14px; opacity: 0.9;'>Monitoreo y Control</p>
                        </div>
                        <div class='content'>
                            <h2>¡Hola, {dto.Nombre}!</h2>
                            <p>Te damos la bienvenida a <strong>OCTO-CODE</strong>. Tu cuenta ha sido creada exitosamente por el administrador.</p>
                            <p>A continuación, tus credenciales de acceso temporal:</p>
                            <div class='credentials-box'>
                                <p><strong>Correo electrónico:</strong> {dto.Correo}</p>
                                <p><strong>Contraseña temporal:</strong> {tempPassword}</p>
                            </div>
                            <p>Te recomendamos iniciar sesión y cambiar tu contraseña por motivos de seguridad.</p>
                        </div>
                        <div class='footer'>
                            <p>© 2026 OCTO-CODE. Todos los derechos reservados.</p>
                        </div>
                    </div>
                </body>
                </html>";

            await _emailService.SendEmailAsync(dto.Correo, subject, htmlMessage);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al enviar el correo: {ex.Message}");
        }

        return true;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return false;
        var result = await _userManager.DeleteAsync(user);
        return result.Succeeded;
    }

    public async Task<bool> AssignRoleAsync(string id, string role)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return false;

        // 1. Conseguimos todos los roles que tiene actualmente el usuario
        var rolesActuales = await _userManager.GetRolesAsync(user);

        // 2. Si tiene roles asignados, los removemos todos de golpe
        if (rolesActuales.Any())
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesActuales);
            if (!removeResult.Succeeded) return false;
        }

        // 3. Le asignamos únicamente el nuevo rol seleccionado
        var addResult = await _userManager.AddToRoleAsync(user, role);
        return addResult.Succeeded;
    }

    public async Task<bool> ToggleStatusAsync(string id, string currentAdminId)
    {
        // 1. Evitar auto-desactivación
        if (id == currentAdminId)
        {
            return false; // O puedes lanzar una excepción controlada
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return false;

        if (user.LockoutEnd == null || user.LockoutEnd <= DateTimeOffset.Now)
        {
            user.LockoutEnd = DateTimeOffset.MaxValue;
        }
        else
        {
            user.LockoutEnd = null;
        }

        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded;
    }


    public async Task<bool> ResetAndResendPasswordAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return false;

        // Generar nueva contraseña temporal
        string tempPassword = "Acpan" + Guid.NewGuid().ToString().Substring(0, 6) + "!";

        // Eliminar contraseña anterior y establecer la nueva
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, tempPassword);

        if (!result.Succeeded) return false;

        // Enviar correo (puedes usar el mismo diseño HTML que tienes en el registro)
        try
        {
            string subject = "Restablecimiento de Contraseña - OCTO-CODE";
            string htmlMessage = $"<p>Hola <strong>{user.NombreCompleto}</strong>,</p><p>Tu contraseña ha sido restablecida por el administrador. Tus nuevos datos de acceso temporal son:</p><p><strong>Correo:</strong> {user.Email}</p><p><strong>Contraseña temporal:</strong> {tempPassword}</p>";

            await _emailService.SendEmailAsync(user.Email!, subject, htmlMessage);
        }
        catch (Exception)
        {
            return false;
        }

        return true;
    }
}