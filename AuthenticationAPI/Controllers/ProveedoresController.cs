using AuthenticationAPI.Data;
using AuthenticationAPI.DTO;
using AuthenticationAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationAPI.Controllers
{
    // Administracion de proveedores.
    [Authorize(Roles = "Administrador,admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class ProveedoresController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public ProveedoresController(ApplicationDbContext db) => _db = db;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProveedorDto>>> GetAll([FromQuery] bool soloActivos = false)
        {
            var query = _db.Proveedores.AsQueryable();
            if (soloActivos) query = query.Where(p => p.Activo);
            var proveedores = await query.OrderBy(p => p.Nombre).ToListAsync();
            return Ok(proveedores.Select(MapDto));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProveedorDto>> GetById(int id)
        {
            var proveedor = await _db.Proveedores.FindAsync(id);
            return proveedor == null ? NotFound() : Ok(MapDto(proveedor));
        }

        [HttpPost]
        public async Task<ActionResult<ProveedorDto>> Create(ProveedorDto dto)
        {
            bool existe = await _db.Proveedores.AnyAsync(p => p.Nombre.ToLower() == dto.Nombre.Trim().ToLower());
            if (existe)
                return BadRequest("Ya existe un proveedor registrado con ese nombre.");

            var proveedor = new Proveedor
            {
                Nombre = dto.Nombre.Trim(),
                RFC = dto.RFC,
                Contacto = dto.Contacto,
                Telefono = dto.Telefono,
                Email = dto.Email,
                Direccion = dto.Direccion,
                Activo = true
            };
            _db.Proveedores.Add(proveedor);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = proveedor.Id }, MapDto(proveedor));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, ProveedorDto dto)
        {
            var proveedor = await _db.Proveedores.FindAsync(id);
            if (proveedor == null) return NotFound();

            bool existeOtro = await _db.Proveedores.AnyAsync(p => p.Id != id && p.Nombre.ToLower() == dto.Nombre.Trim().ToLower());
            if (existeOtro)
                return BadRequest("Ya existe otro proveedor registrado con ese nombre.");

            proveedor.Nombre = dto.Nombre.Trim();
            proveedor.RFC = dto.RFC;
            proveedor.Contacto = dto.Contacto;
            proveedor.Telefono = dto.Telefono;
            proveedor.Email = dto.Email;
            proveedor.Direccion = dto.Direccion;
            proveedor.Activo = dto.Activo;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // Baja logica: un proveedor con historial de compras no debe eliminarse fisicamente.
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Deactivate(int id)
        {
            var proveedor = await _db.Proveedores.FindAsync(id);
            if (proveedor == null) return NotFound();
            proveedor.Activo = false;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        private static ProveedorDto MapDto(Proveedor p) => new()
        {
            Id = p.Id,
            Nombre = p.Nombre,
            RFC = p.RFC,
            Contacto = p.Contacto,
            Telefono = p.Telefono,
            Email = p.Email,
            Direccion = p.Direccion,
            Activo = p.Activo
        };
    }
}
