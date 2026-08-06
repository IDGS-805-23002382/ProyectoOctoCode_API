using AuthenticationAPI.Data;
using AuthenticationAPI.DTO;
using AuthenticationAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AuthenticationAPI.Controllers
{
    // El cliente deja una opinion sobre un producto que adquirio.
    [Route("api/[controller]")]
    [ApiController]
    public class ResenasController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public ResenasController(ApplicationDbContext db) => _db = db;

        [Authorize(Roles = "cliente")]
        [HttpPost]
        public async Task<ActionResult<ResenaDto>> Crear(CrearResenaDto dto)
        {
            var cliente = await ObtenerClienteActualAsync();
            if (cliente == null) return NotFound();

            var venta = await _db.Ventas.Include(v => v.Producto).FirstOrDefaultAsync(v => v.Id == dto.VentaId && v.ClienteId == cliente.Id);
            if (venta == null) return BadRequest("Esa compra no pertenece a este cliente o no existe.");

            if (await _db.Resenas.AnyAsync(r => r.VentaId == dto.VentaId))
                return Conflict("Ya se dejó una opinión para esta compra.");

            var resena = new ResenaProducto
            {
                ClienteId = cliente.Id,
                ProductoId = venta.ProductoId,
                VentaId = venta.Id,
                Calificacion = dto.Calificacion,
                Comentario = dto.Comentario
            };
            _db.Resenas.Add(resena);
            await _db.SaveChangesAsync();

            return Ok(new ResenaDto { Id = resena.Id, Producto = venta.Producto!.Nombre, Calificacion = resena.Calificacion, Comentario = resena.Comentario, Fecha = resena.Fecha });
        }

        // Publico: resenas visibles de un producto (util para el catalogo).
        [HttpGet("producto/{productoId:int}")]
        public async Task<ActionResult<IEnumerable<ResenaDto>>> PorProducto(int productoId)
        {
            var resenas = await _db.Resenas.Include(r => r.Producto)
                .Where(r => r.ProductoId == productoId)
                .OrderByDescending(r => r.Fecha)
                .Select(r => new ResenaDto { Id = r.Id, Producto = r.Producto!.Nombre, Calificacion = r.Calificacion, Comentario = r.Comentario, Fecha = r.Fecha })
                .ToListAsync();

            return Ok(resenas);
        }

        private async Task<Cliente?> ObtenerClienteActualAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            return await _db.Clientes.FirstOrDefaultAsync(c => c.UserId == userId);
        }
    }
}
