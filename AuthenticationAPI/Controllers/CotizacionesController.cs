using AuthenticationAPI.Data;
using AuthenticationAPI.DTO;
using AuthenticationAPI.Enums;
using AuthenticationAPI.interfaces;
using AuthenticationAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Net;
using System.Net.Mail;
using AuthenticationAPI.DTO;

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
        public async Task<ActionResult<object>> SolicitarPublica([FromBody] CotizacionDto dto, [FromServices] IConfiguration configuration)
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
                monto = (dto.SuperficieHectareas * 100) + (dto.CaudalDiario * 0.1m);
                detalle = "No fue posible calcular un estimado automático en este momento; se ha generado un cálculo preliminar.";
            }

            // 👈 AQUÍ USAMOS EL PUERTO 7227 (QUE ES DONDE CORRE TU API PRINCIPAL)
            string baseUrl = "https://192.168.X.X:7227/api/cotizaciones";
            string urlConfirmacion = $"{baseUrl}/confirmar?correo={Uri.EscapeDataString(dto.Correo)}&nombre={Uri.EscapeDataString(dto.Nombre)}&empresa={Uri.EscapeDataString(dto.Empresa)}";

            var bodyHtml = $@"
        <div style='font-family: Arial, sans-serif; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 8px;'>    
            <h2 style='color: #4A154B; margin-top: 0;'>OCTO-CODE - Cotización</h2>    
            <p>Hola <b>{dto.Nombre}</b>,</p>    
            <p>Hemos recibido tu solicitud de cotización para la empresa <b>{dto.Empresa}</b>.</p>                
            <div style='background-color: #f8f9fa; padding: 15px; border-radius: 6px; margin: 20px 0;'>        
                <p style='margin: 5px 0;'><b>Monto estimado:</b> <span style='color: #28a745; font-size: 18px;'>${monto:N2}</span></p>        
                <p style='margin: 5px 0;'><b>Detalles:</b> {detalle}</p>    
            </div>    
            <p style='text-align: center; font-weight: bold; margin-top: 30px;'>¿Deseas confirmar este producto o servicio?</p>                
            <div style='text-align: center; margin: 25px 0;'>        
                <a href='{urlConfirmacion}' style='background-color: #28a745; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; font-weight: bold; display: inline-block;'>           
                    Confirmar Producto        
                </a>    
            </div>    
            <hr style='border: none; border-top: 1px solid #e0e0e0; margin: 30px 0;'>    
            <p style='font-size: 12px; color: #777; text-align: center;'>Este es un mensaje automático de OctoCode Soporte. Si no deseas adquirirlo, simplemente ignora este correo.</p>
        </div>";

            try
            {
                var smtpServer = configuration["EmailSettings:Server"];
                var smtpPort = int.Parse(configuration["EmailSettings:Port"] ?? "587");
                var senderName = configuration["EmailSettings:SenderName"] ?? "OctoCode Soporte";
                var senderEmail = configuration["EmailSettings:SenderEmail"];
                var senderPassword = configuration["EmailSettings:Password"];

                using var smtpClient = new SmtpClient(smtpServer, smtpPort)
                {
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail!, senderName),
                    Subject = "Cotización de Sistema de pH y Control de Agua",
                    Body = bodyHtml,
                    IsBodyHtml = true,
                };

                mailMessage.To.Add(dto.Correo);
                await smtpClient.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al enviar correo: {ex.Message}");
            }

            return Ok(new { MontoEstimado = monto, Detalle = detalle });
        }

        // Endpoint para recibir la respuesta cuando el cliente hace clic en el enlace
        [HttpGet("confirmar")]
        [AllowAnonymous]
        public async Task<IActionResult> Confirmar([FromQuery] string correo, [FromQuery] string nombre, [FromQuery] string empresa)
        {
            try
            {
                // 1. LÓGICA DE ALTA EN TU BASE DE DATOS
                // var nuevoUsuario = new Usuario { Email = correo, Nombre = nombre, Empresa = empresa, Activo = true };
                // _context.Usuarios.Add(nuevoUsuario);
                // await _context.SaveChangesAsync();

                return Content(@"
            <html>        
                <body style='font-family: Arial; text-align: center; padding: 50px;'>            
                    <h2 style='color: #28a745;'>¡Felicidades!</h2>            
                    <p>Tu cuenta ha sido creada y activada correctamente para <b>" + empresa + @"</b>.</p>            
                    <p>Ya puedes iniciar sesión en nuestra plataforma.</p>        
                </body>    
            </html>", "html");
            }
            catch (Exception)
            {
                return Content("<h2>Error</h2><p>Hubo un problema al activar tu cuenta. Intenta nuevamente o contacta a soporte.</p>", "html");
            }
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
