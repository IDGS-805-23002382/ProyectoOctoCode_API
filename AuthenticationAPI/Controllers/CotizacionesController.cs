using AuthenticationAPI.Data;
using AuthenticationAPI.DTO;
using AuthenticationAPI.Enums;
using AuthenticationAPI.interfaces;
using AuthenticationAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AuthenticationAPI.Controllers
{
    // Solicitud de cotizaciones de proyectos. Se apoya en IAcpanIntegrationService para
    // estimar el monto consultando la API de Acpan; si Acpan no responde, el administrador
    // puede capturar el monto manualmente después.
    [Route("api/[controller]")]
    [ApiController]
    public class CotizacionesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IAcpanIntegrationService _acpanService;

        public CotizacionesController(ApplicationDbContext db, IAcpanIntegrationService acpanService)
        {
            _db = db;
            _acpanService = acpanService;
        }

        // Público: cualquier visitante (aún no cliente) puede solicitar una cotización
        // desde la página de "Solicitud de Cotizaciones" del sitio.
        [AllowAnonymous]
        [HttpPost("publica")]
        public async Task<ActionResult<object>> SolicitarPublica(CotizacionRequestDto dto)
        {
            decimal monto = 0;
            string? detalle = null;
            try
            {
                var resultado = await _acpanService.ObtenerCotizacionAsync(dto.DescripcionProyecto);
                monto = resultado.MontoEstimado;
                detalle = resultado.Detalle;
            }
            catch (InvalidOperationException)
            {
                detalle = "No fue posible calcular un estimado automático en este momento; un administrador lo revisará y te lo hará llegar.";
            }

            return Ok(new { MontoEstimado = monto, Detalle = detalle });
        }

        // El cliente ya autenticado solicita una cotización formal, que queda registrada
        // y visible para el administrador.
        [Authorize(Roles = "cliente")]
        [HttpPost]
        public async Task<ActionResult<object>> Solicitar(CotizacionRequestDto dto)
        {
            var cliente = await ObtenerClienteActualAsync();
            if (cliente == null) return NotFound();

            decimal monto = 0;
            try
            {
                var resultado = await _acpanService.ObtenerCotizacionAsync(dto.DescripcionProyecto);
                monto = resultado.MontoEstimado;
            }
            catch (InvalidOperationException)
            {
                // Se registra en 0: el administrador la revisa y ajusta el monto manualmente.
            }

            var solicitud = new SolicitudCotizacion
            {
                ClienteId = cliente.Id,
                DescripcionProyecto = dto.DescripcionProyecto,
                MontoEstimado = monto,
                Estatus = EstatusCotizacion.Pendiente
            };
            _db.SolicitudesCotizacion.Add(solicitud);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = solicitud.Id }, MapDto(solicitud));
        }

        [Authorize(Roles = "cliente")]
        [HttpGet("mias")]
        public async Task<ActionResult<IEnumerable<object>>> Mias()
        {
            var cliente = await ObtenerClienteActualAsync();
            if (cliente == null) return NotFound();

            var solicitudes = await _db.SolicitudesCotizacion
                .Where(s => s.ClienteId == cliente.Id)
                .OrderByDescending(s => s.FechaSolicitud)
                .ToListAsync();

            return Ok(solicitudes.Select(MapDto));
        }

        [Authorize(Roles = "Administrador,admin")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetAll()
        {
            var solicitudes = await _db.SolicitudesCotizacion
                .Include(s => s.Cliente).ThenInclude(c => c!.Usuario)
                .OrderByDescending(s => s.FechaSolicitud)
                .ToListAsync();

            return Ok(solicitudes.Select(s => new
            {
                s.Id,
                Cliente = s.Cliente?.RazonSocial,
                s.DescripcionProyecto,
                s.MontoEstimado,
                Estatus = s.Estatus.ToString(),
                s.FechaSolicitud
            }));
        }

        [Authorize(Roles = "Administrador,admin")]
        [HttpPatch("{id:int}")]
        public async Task<IActionResult> Actualizar(int id, [FromQuery] decimal montoEstimado, [FromQuery] string estatus)
        {
            var solicitud = await _db.SolicitudesCotizacion.FindAsync(id);
            if (solicitud == null) return NotFound();
            if (!Enum.TryParse<EstatusCotizacion>(estatus, true, out var est))
                return BadRequest("Estatus inválido. Use: Pendiente, Enviada, Aceptada o Rechazada.");

            solicitud.MontoEstimado = montoEstimado;
            solicitud.Estatus = est;
            solicitud.FechaRespuesta = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [Authorize]
        [HttpGet("{id:int}")]
        public async Task<ActionResult<object>> GetById(int id)
        {
            var solicitud = await _db.SolicitudesCotizacion.FindAsync(id);
            return solicitud == null ? NotFound() : Ok(MapDto(solicitud));
        }

        private async Task<Cliente?> ObtenerClienteActualAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            return await _db.Clientes.FirstOrDefaultAsync(c => c.UserId == userId);
        }

        private static object MapDto(SolicitudCotizacion s) => new
        {
            s.Id,
            s.DescripcionProyecto,
            s.MontoEstimado,
            Estatus = s.Estatus.ToString(),
            s.FechaSolicitud,
            s.FechaRespuesta
        };
    }
}
