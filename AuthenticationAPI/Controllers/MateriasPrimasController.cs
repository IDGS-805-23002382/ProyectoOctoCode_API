using AuthenticationAPI.Data;
using AuthenticationAPI.DTO;
using AuthenticationAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationAPI.Controllers
{
    [Authorize(Roles = "Administrador,admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class MateriasPrimasController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public MateriasPrimasController(ApplicationDbContext db) => _db = db;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MateriaPrimaDto>>> GetAll()
        {
            var materias = await _db.MateriasPrimas.Include(m => m.Proveedor).OrderBy(m => m.Nombre).ToListAsync();
            return Ok(materias.Select(MapDto));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<MateriaPrimaDto>> GetById(int id)
        {
            var materia = await _db.MateriasPrimas.Include(m => m.Proveedor).FirstOrDefaultAsync(m => m.Id == id);
            return materia == null ? NotFound() : Ok(MapDto(materia));
        }

        // Alta manual de un insumo NUEVO. Pide datos de catálogo (nombre, descripción,
        // método de costeo, stock mínimo de alerta y proveedor). Unidad, Stock y Costo
        // Unitario NO se piden aquí porque se generan solos al registrar la primera
        // Compra de este insumo (ver ComprasController).
        [HttpPost]
        public async Task<ActionResult<MateriaPrimaDto>> Create(CrearMateriaPrimaDto dto)
        {
            bool existe = await _db.MateriasPrimas.AnyAsync(m => m.Nombre.ToLower() == dto.Nombre.Trim().ToLower());
            if (existe)
                return BadRequest("Ya existe un insumo registrado con ese nombre. Si necesitas comprárselo a otro proveedor, regístralo directamente desde el módulo de Compras.");

            var proveedor = await _db.Proveedores.FindAsync(dto.ProveedorId);
            if (proveedor == null)
                return BadRequest("El proveedor seleccionado no existe.");
            if (!proveedor.Activo)
                return BadRequest("El proveedor seleccionado no está activo.");

            var materia = new MateriaPrima
            {
                Nombre = dto.Nombre.Trim(),
                Descripcion = dto.Descripcion,
                UnidadMedida = "PZA", // se corrige solo/con la primera Compra, o editable después en Editar
                MetodoCosteo = dto.MetodoCosteo,
                StockMinimo = dto.StockMinimo,
                Stock = 0,
                CostoUnitarioActual = 0,
                ProveedorId = dto.ProveedorId,
                Activo = true
            };

            _db.MateriasPrimas.Add(materia);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = materia.Id }, MapDto(materia));
        }

        // Edición de catálogo: permite corregir nombre/descripción/unidad/proveedor/método
        // de costeo/stock mínimo. Nunca permite tocar Stock ni CostoUnitarioActual: esos
        // solo se mueven vía Compras o Mermas para no romper el kardex (CapaCosto).
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, EditarMateriaPrimaDto dto)
        {
            var materia = await _db.MateriasPrimas.FindAsync(id);
            if (materia == null) return NotFound();

            if (dto.ProveedorId.HasValue)
            {
                var proveedor = await _db.Proveedores.FindAsync(dto.ProveedorId.Value);
                if (proveedor == null)
                    return BadRequest("El proveedor seleccionado no existe.");
            }

            materia.Nombre = dto.Nombre.Trim();
            materia.Descripcion = dto.Descripcion;
            materia.UnidadMedida = string.IsNullOrWhiteSpace(dto.UnidadMedida) ? materia.UnidadMedida : dto.UnidadMedida;
            materia.MetodoCosteo = dto.MetodoCosteo;
            materia.StockMinimo = dto.StockMinimo;
            materia.ProveedorId = dto.ProveedorId;
            materia.Activo = dto.Activo;

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                var detalle = ex.InnerException?.Message ?? ex.Message;
                return BadRequest($"No se pudo guardar el insumo: {detalle}");
            }

            return NoContent();
        }

        [HttpGet("{id:int}/kardex")]
        public async Task<ActionResult<IEnumerable<object>>> Kardex(int id)
        {
            var capas = await _db.CapasCosto
                .Where(c => c.MateriaPrimaId == id)
                .OrderBy(c => c.FechaEntrada)
                .Select(c => new
                {
                    c.FechaEntrada,
                    c.CantidadOriginal,
                    c.CantidadDisponible,
                    c.CostoUnitario,
                    ValorRestante = c.CantidadDisponible * c.CostoUnitario
                })
                .ToListAsync();

            return Ok(capas);
        }

        [HttpGet("bajo-stock")]
        public async Task<ActionResult<IEnumerable<MateriaPrimaDto>>> BajoStock()
        {
            var materias = await _db.MateriasPrimas.Include(m => m.Proveedor)
                .Where(m => m.Activo && m.Stock <= m.StockMinimo).ToListAsync();
            return Ok(materias.Select(MapDto));
        }

        [HttpPatch("{id:int}/estatus")]
        public async Task<IActionResult> CambiarEstatus(int id, [FromBody] bool activo)
        {
            var materia = await _db.MateriasPrimas.FindAsync(id);
            if (materia == null) return NotFound();

            materia.Activo = activo;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        private static MateriaPrimaDto MapDto(MateriaPrima m) => new()
        {
            Id = m.Id,
            Nombre = m.Nombre,
            Descripcion = m.Descripcion,
            UnidadMedida = m.UnidadMedida,
            MetodoCosteo = m.MetodoCosteo,
            Stock = m.Stock,
            CostoUnitarioActual = m.CostoUnitarioActual,
            StockMinimo = m.StockMinimo,
            Activo = m.Activo,
            ProveedorId = m.ProveedorId,
            Proveedor = m.Proveedor?.Nombre
        };
    }
}