using AuthenticationAPI.Data;
using AuthenticationAPI.DTO;
using AuthenticationAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VentasController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public VentasController(ApplicationDbContext db)
        {
            _db = db;
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> RegistrarVenta([FromBody] RegistrarVentaDto dto)
        {
            if (dto == null || dto.Detalles == null || !dto.Detalles.Any())
            {
                return BadRequest("El carrito de compras está vacío.");
            }

            foreach (var item in dto.Detalles)
            {
                var nuevaVenta = new Venta
                {
                    ClienteId = dto.ClienteId,
                    ProductoId = item.ProductoId,
                    Cantidad = (int)item.Cantidad,
                    PrecioPagado = item.PrecioUnitario,
                    Fecha = DateTime.UtcNow,
                    Estatus = "Pendiente"
                };

                _db.Ventas.Add(nuevaVenta);
            }

            await _db.SaveChangesAsync();

            return Ok(new { mensaje = "Venta(s) registrada(s) con éxito" });
        }

        [Authorize(Roles = "Administrador,admin,Empleado,empleado")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VentaDto>>> GetVentas()
        {
            var ventas = await _db.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Producto)
                .Select(v => new VentaDto
                {
                    Id = v.Id,
                    ClienteNombre = v.Cliente != null ? v.Cliente.RazonSocial : "Cliente General",
                    Fecha = v.Fecha,
                    Total = v.PrecioPagado * v.Cantidad,
                    Estatus = v.Estatus
                })
                .ToListAsync();

            return Ok(ventas);
        }

        [Authorize(Roles = "Administrador,admin,Empleado,empleado")]
        [HttpPut("{id:int}/estatus")]
        public async Task<IActionResult> ActualizarEstatus(int id, [FromBody] ActualizarEstatusDto dto)
        {
            var venta = await _db.Ventas.FindAsync(id);
            if (venta == null) return NotFound("Venta no encontrada.");

            venta.Estatus = dto.Estatus;
            await _db.SaveChangesAsync();

            return Ok(new { mensaje = "Estatus actualizado correctamente." });
        }
    }
}