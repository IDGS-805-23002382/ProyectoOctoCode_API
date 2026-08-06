using AuthenticationAPI.DTO;
using AuthenticationAPI.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _service;
    public UsersController(IUserService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Roles = "Administrador,admin")]
    public async Task<IActionResult> GetAll()
    {
        var users = await _service.GetAllAsync();
        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var user = await _service.GetByIdAsync(id);
        if (user == null)
            return NotFound();
        return Ok(user);
    }

    [HttpPost]
    [Authorize(Roles = "Administrador,admin")]
    public async Task<IActionResult> Create([FromBody] RegisterRequestDto dto)
    {
        var result = await _service.CreateAsync(dto);
        if (!result)
            return BadRequest(new { message = "No se pudo registrar el usuario" });

        return Ok(new { success = true, message = "Usuario registrado correctamente" });
    }

    [HttpPost("{id}/role")]
    [Authorize(Roles = "Administrador,admin")]
    public async Task<IActionResult> AssignRole(string id, [FromBody] string role)
    {
        var result = await _service.AssignRoleAsync(id, role);
        if (!result)
            return BadRequest(new { message = "No se pudo asignar el rol" });
        return Ok(new { success = true, message = "Rol asignado correctamente" });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Administrador,admin")]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _service.DeleteAsync(id);
        if (!result)
            return NotFound(new { message = "Usuario no encontrado" });
        return Ok(new { success = true, message = "Usuario eliminado correctamente" });
    }

    [HttpPost("{id}/status")]
    [Authorize(Roles = "Administrador,admin")]
    public async Task<IActionResult> ToggleStatus(string id)
    {
        // Obtenemos el ID del administrador que está haciendo la petición desde el Token
        var currentAdminId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var result = await _service.ToggleStatusAsync(id, currentAdminId!);
        if (!result)
            return BadRequest(new { message = "No se pudo actualizar el estatus del usuario (o intentaste desactivarte a ti mismo)." });

        return Ok(new { success = true, message = "Estatus actualizado correctamente" });
    }

    [HttpPost("{id}/reset-password")]
    [Authorize(Roles = "Administrador,admin")]
    public async Task<IActionResult> ResetAndResendPassword(string id)
    {
        // Corregido de _userService a _service
        var result = await _service.ResetAndResendPasswordAsync(id);
        if (!result)
            return BadRequest(new { message = "No se pudo restablecer la contraseña." });

        return Ok(new { success = true, message = "Contraseña restablecida y correo enviado." });
    }
}