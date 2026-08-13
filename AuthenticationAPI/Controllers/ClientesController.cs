using AuthenticationAPI.Services;
using AuthenticationAPI.interfaces;
using AuthenticationAPI.Data;
using AuthenticationAPI.DTO;
using AuthenticationAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AuthenticationAPI.Controllers
{
    // Seccion de clientes: un administrador registra al cliente y se le envian sus
    // credenciales por correo; el propio cliente gestiona su perfil, ve la documentacion
    // de sus productos y su historial de compras/resenas.
    [Route("api/[controller]")]
    [ApiController]
    public class ClientesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;

        public ClientesController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IEmailService emailService)
        {
            _db = db;
            _userManager = userManager;
            _emailService = emailService;
        }

        // ---------- Administracion (solo Administrador) ----------

        [Authorize(Roles = "Administrador,admin")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ClienteDto>>> GetAll()
        {
            var clientes = await _db.Clientes.Include(c => c.Usuario).ToListAsync();
            return Ok(clientes.Select(MapDto));
        }

        [Authorize(Roles = "Administrador,admin")]
        [HttpPost]
        public async Task<ActionResult<ClienteDto>> Registrar(RegistrarClienteDto dto)
        {
            if (await _userManager.FindByEmailAsync(dto.Email) != null)
                return Conflict("Ya existe un usuario con ese correo.");

            var passwordTemporal = GenerarPasswordTemporal();

            var usuario = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                NombreCompleto = dto.NombreCompleto
            };

            var resultado = await _userManager.CreateAsync(usuario, passwordTemporal);
            if (!resultado.Succeeded)
                return BadRequest(resultado.Errors.Select(e => e.Description));

            await _userManager.AddToRoleAsync(usuario, "cliente");

            var cliente = new Cliente
            {
                UserId = usuario.Id,
                RazonSocial = dto.RazonSocial,
                RFC = dto.RFC,
                Telefono = dto.Telefono,
                Direccion = dto.Direccion
            };
            _db.Clientes.Add(cliente);
            await _db.SaveChangesAsync();

            try
            {
                var asunto = "Bienvenido a OCTO-CODE - Tus datos de acceso";
                var html = $@"
                    <p>Hola <strong>{dto.NombreCompleto}</strong>,</p>
                    <p>Se te ha dado de alta como cliente en <strong>OCTO-CODE</strong>. Estos son tus datos de acceso temporales:</p>
                    <p><strong>Correo:</strong> {dto.Email}</p>
                    <p><strong>Contraseña temporal:</strong> {passwordTemporal}</p>
                    <p>Te recomendamos iniciar sesión y cambiarla desde tu perfil.</p>";
                await _emailService.SendEmailAsync(dto.Email, asunto, html);
            }
            catch (Exception ex)
            {
                // No se bloquea el alta del cliente si falla el envío del correo,
                // pero queda registrado para revisarlo.
                Console.WriteLine($"Error al enviar credenciales al cliente: {ex.Message}");
            }

            cliente.Usuario = usuario;
            return CreatedAtAction(nameof(GetAll), MapDto(cliente));
        }




        // Solo afecta el estatus de negocio del cliente (si sigue activo como cliente).
        // Para bloquear su acceso de login usa /api/users/{id}/status.
        [Authorize(Roles = "Administrador,admin")]
        [HttpPatch("{id:int}/estatus")]
        public async Task<IActionResult> CambiarEstatus(int id, [FromQuery] bool activo)
        {
            var cliente = await _db.Clientes.FindAsync(id);
            if (cliente == null) return NotFound();

            cliente.Activo = activo;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // ---------- Autoservicio del cliente ----------

        [Authorize(Roles = "cliente")]
        [HttpGet("mi-perfil")]
        public async Task<ActionResult<ClienteDto>> MiPerfil()
        {
            var cliente = await ObtenerClienteActualAsync();
            if (cliente == null) return NotFound();
            return Ok(MapDto(cliente));
        }
        [Authorize(Roles = "cliente")]
        [HttpPut("mi-perfil")]
        public async Task<IActionResult> ActualizarPerfil(ActualizarPerfilClienteDto dto)
        {
            var cliente = await ObtenerClienteActualAsync();
            if (cliente == null) return NotFound();

            // Actualizamos todas las propiedades correspondientes
            cliente.RazonSocial = dto.RazonSocial;
            cliente.RFC = dto.RFC;                
            cliente.Telefono = dto.Telefono;
            cliente.Direccion = dto.Direccion;

            if (cliente.Usuario != null)
            {
                cliente.Usuario.NombreCompleto = dto.NombreCompleto;
            }

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // Documentacion (manuales/guias) de los productos que el cliente ha adquirido.
        [Authorize(Roles = "cliente")]
        [HttpGet("mis-productos/documentos")]
        public async Task<ActionResult<IEnumerable<DocumentoProductoConProductoDto>>> MisDocumentos()
        {
            var cliente = await ObtenerClienteActualAsync();
            if (cliente == null) return NotFound();

            var productoIds = await _db.Ventas.Where(v => v.ClienteId == cliente.Id).Select(v => v.ProductoId).Distinct().ToListAsync();

            var documentos = await _db.DocumentosProducto
                .Include(d => d.Producto)
                .Where(d => productoIds.Contains(d.ProductoId))
                .Select(d => new DocumentoProductoConProductoDto
                {
                    Id = d.Id,
                    ProductoId = d.ProductoId,
                    Producto = d.Producto!.Nombre,
                    NombreArchivo = d.NombreArchivo,
                    RutaArchivo = d.RutaArchivo,
                    Tipo = d.Tipo.ToString()
                })
                .ToListAsync();

            return Ok(documentos);
        }

        // Listado de compras realizadas por el cliente.
        [Authorize(Roles = "cliente")]
        [HttpGet("mis-compras")]
        public async Task<ActionResult<IEnumerable<object>>> MisCompras()
        {
            var cliente = await ObtenerClienteActualAsync();
            if (cliente == null) return NotFound();

            var compras = await _db.Ventas
                .Include(v => v.Producto)
                .Where(v => v.ClienteId == cliente.Id)
                .Select(v => new
                {
                    v.Id,
                    Producto = v.Producto!.Nombre,
                    v.Fecha,
                    v.PrecioPagado,
                    v.Cantidad,
                    YaResenado = _db.Resenas.Any(r => r.VentaId == v.Id)
                })
                .ToListAsync();

            return Ok(compras);
        }

        private async Task<Cliente?> ObtenerClienteActualAsync()
        {
            // Buscamos en todas las posibles variantes de claims que usan los JWT
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User.FindFirstValue("sub")
                      ?? User.FindFirstValue("id")
                      ?? User.FindFirstValue("uid");

            if (string.IsNullOrEmpty(userId)) return null;

            return await _db.Clientes.Include(c => c.Usuario).FirstOrDefaultAsync(c => c.UserId == userId);
        }

        private static ClienteDto MapDto(Cliente c) => new()
        {
            Id = c.Id,
            NombreCompleto = c.Usuario?.NombreCompleto ?? string.Empty,
            Email = c.Usuario?.Email ?? string.Empty,
            RazonSocial = c.RazonSocial,
            RFC = c.RFC,
            Telefono = c.Telefono,
            Direccion = c.Direccion,
            Activo = c.Activo
        };

        private static string GenerarPasswordTemporal()
        {
            // Cumple los requisitos de Identity (mayuscula, minuscula, digito, no alfanumerico, 8+).
            var raiz = Guid.NewGuid().ToString("N")[..8];
            return $"Oc{raiz}!1";
        }
    }
}
