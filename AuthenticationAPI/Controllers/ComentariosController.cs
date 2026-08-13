using AuthenticationAPI.Data;
using AuthenticationAPI.DTO;
using AuthenticationAPI.Enums;
using AuthenticationAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AuthenticationAPI.Controllers
{
    // Comentarios/dudas/quejas de los clientes, con seguimiento por parte de un administrador.
    [Route("api/[controller]")]
    [ApiController]
    public class ComentariosController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public ComentariosController(ApplicationDbContext db) => _db = db;

        // El cliente crea un comentario/duda/queja.
        [Authorize(Roles = "cliente")]
        [HttpPost]
        public async Task<ActionResult<ComentarioDto>> Crear(CrearComentarioDto dto)
        {
            var cliente = await ObtenerClienteActualAsync();
            if (cliente == null) return NotFound();

            var comentario = new Comentario
            {
                ClienteId = cliente.Id,
                Asunto = dto.Asunto,
                Mensaje = dto.Mensaje,
                Estatus = EstatusComentario.Nuevo
            };
            _db.Comentarios.Add(comentario);
            await _db.SaveChangesAsync();

            comentario.Cliente = cliente;
            return CreatedAtAction(nameof(GetById), new { id = comentario.Id }, MapDto(comentario));
        }

        [Authorize(Roles = "cliente")]
        [HttpGet("mios")]
        public async Task<ActionResult<IEnumerable<ComentarioDto>>> Mios()
        {
            var cliente = await ObtenerClienteActualAsync();
            if (cliente == null) return NotFound();

            var comentarios = await _db.Comentarios.Include(c => c.Cliente).ThenInclude(cl => cl!.Usuario)
                .Where(c => c.ClienteId == cliente.Id).OrderByDescending(c => c.FechaCreacion).ToListAsync();
            return Ok(comentarios.Select(MapDto));
        }

        // El administrador da seguimiento: ve todos, filtra por estatus y responde.
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ComentarioDto>>> GetAll([FromQuery] string? estatus)
        {
            var query = _db.Comentarios.Include(c => c.Cliente).ThenInclude(cl => cl!.Usuario).AsQueryable();
            if (!string.IsNullOrWhiteSpace(estatus) && Enum.TryParse<EstatusComentario>(estatus, true, out var est))
                query = query.Where(c => c.Estatus == est);

            var comentarios = await query.OrderByDescending(c => c.FechaCreacion).ToListAsync();
            return Ok(comentarios.Select(MapDto));
        }

        [Authorize]
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ComentarioDto>> GetById(int id)
        {
            var comentario = await _db.Comentarios.Include(c => c.Cliente).ThenInclude(cl => cl!.Usuario)
                .FirstOrDefaultAsync(c => c.Id == id);
            return comentario == null ? NotFound() : Ok(MapDto(comentario));
        }

        [Authorize(Roles = "Administrador,admin")]
        [HttpPatch("{id:int}/responder")]
        public async Task<IActionResult> Responder(int id, ResponderComentarioDto dto)
        {
            var comentario = await _db.Comentarios.FindAsync(id);
            if (comentario == null) return NotFound();

            if (!Enum.TryParse<EstatusComentario>(dto.Estatus, true, out var estatus))
                return BadRequest("Estatus inválido. Use: Nuevo, EnRevision, Resuelto o Cerrado.");

            comentario.RespuestaAdmin = dto.Respuesta;
            comentario.Estatus = estatus;
            comentario.FechaRespuesta = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        private async Task<Cliente?> ObtenerClienteActualAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            return await _db.Clientes.FirstOrDefaultAsync(c => c.UserId == userId);
        }

        private static ComentarioDto MapDto(Comentario c) => new()
        {
            Id = c.Id,
            Cliente = c.Cliente?.RazonSocial ?? string.Empty,
            Asunto = c.Asunto,
            Mensaje = c.Mensaje,
            Estatus = c.Estatus.ToString(),
            RespuestaAdmin = c.RespuestaAdmin,
            FechaCreacion = c.FechaCreacion
        };
    }
}
