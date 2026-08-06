using AuthenticationAPI.Data;
using AuthenticationAPI.DTO;
using AuthenticationAPI.Enums;
using AuthenticationAPI.interfaces;
using AuthenticationAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationAPI.Controllers
{
    [Authorize(Roles = "Administrador,admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class ComprasController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ICosteoService _costeoService;

        public ComprasController(ApplicationDbContext db, ICosteoService costeoService)
        {
            _db = db;
            _costeoService = costeoService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CompraDto>>> GetAll()
        {
            var compras = await _db.Compras
                .Include(c => c.Proveedor)
                .Include(c => c.Detalles).ThenInclude(d => d.MateriaPrima)
                .OrderByDescending(c => c.FechaCompra)
                .ToListAsync();

            return Ok(compras.Select(MapDto));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<CompraDto>> GetById(int id)
        {
            var compra = await _db.Compras
                .Include(c => c.Proveedor)
                .Include(c => c.Detalles).ThenInclude(d => d.MateriaPrima)
                .FirstOrDefaultAsync(c => c.Id == id);

            return compra == null ? NotFound() : Ok(MapDto(compra));
        }

        [HttpPost]
        public async Task<ActionResult<CompraDto>> Create(CrearCompraDto dto)
        {
            var proveedor = await _db.Proveedores.FindAsync(dto.ProveedorId);
            if (proveedor == null) return BadRequest("Proveedor no encontrado.");
            if (!proveedor.Activo)
                return BadRequest($"El proveedor '{proveedor.Nombre}' está inactivo.");

            var compra = new Compra
            {
                ProveedorId = dto.ProveedorId,
                Folio = dto.Folio,
                Estatus = EstatusCompra.Recibida,
                FechaCompra = DateTime.UtcNow
            };

            foreach (var d in dto.Detalles)
            {
                MateriaPrima? materia;

                if (d.MateriaPrimaId.HasValue && d.MateriaPrimaId.Value > 0)
                {
                    materia = await _db.MateriasPrimas.FindAsync(d.MateriaPrimaId.Value);
                    if (materia == null) return BadRequest($"Materia prima no encontrada: {d.MateriaPrimaId}");
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(d.Nombre))
                        return BadRequest("Debes indicar el nombre del insumo.");

                    var nombreLimpio = d.Nombre.Trim();
                    materia = await _db.MateriasPrimas.FirstOrDefaultAsync(m =>
                        m.Nombre.ToLower() == nombreLimpio.ToLower() && m.ProveedorId == dto.ProveedorId);

                    if (materia == null)
                    {
                        materia = new MateriaPrima
                        {
                            Nombre = nombreLimpio,
                            UnidadMedida = string.IsNullOrWhiteSpace(d.UnidadMedida) ? "PZA" : d.UnidadMedida.Trim(),
                            MetodoCosteo = MetodoCosteo.PromedioPonderado,
                            Stock = 0,
                            StockMinimo = 0,
                            CostoUnitarioActual = 0,
                            ProveedorId = dto.ProveedorId,
                            Activo = true
                        };
                        _db.MateriasPrimas.Add(materia);
                        await _db.SaveChangesAsync();
                    }
                }

                compra.Detalles.Add(new CompraDetalle
                {
                    MateriaPrimaId = materia.Id,
                    Cantidad = d.Cantidad,
                    CostoUnitario = d.CostoUnitario
                });
            }

            compra.Total = compra.Detalles.Sum(d => d.Subtotal);

            _db.Compras.Add(compra);
            await _db.SaveChangesAsync();

            foreach (var detalle in compra.Detalles)
            {
                await _costeoService.RegistrarEntradaAsync(detalle.MateriaPrimaId, detalle.Cantidad, detalle.CostoUnitario, detalle.Id);
            }

            var creada = await _db.Compras
                .Include(c => c.Proveedor)
                .Include(c => c.Detalles).ThenInclude(d => d.MateriaPrima)
                .FirstAsync(c => c.Id == compra.Id);

            return CreatedAtAction(nameof(GetById), new { id = compra.Id }, MapDto(creada));
        }

        [HttpPatch("{id:int}/cancelar")]
        public async Task<IActionResult> Cancelar(int id)
        {
            var compra = await _db.Compras.FindAsync(id);
            if (compra == null) return NotFound();
            if (compra.Estatus == EstatusCompra.Recibida)
                return BadRequest("No se puede cancelar una compra ya recibida.");

            compra.Estatus = EstatusCompra.Cancelada;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        private static CompraDto MapDto(Compra c) => new()
        {
            Id = c.Id,
            Proveedor = c.Proveedor?.Nombre ?? string.Empty,
            Folio = c.Folio,
            FechaCompra = c.FechaCompra,
            Estatus = c.Estatus.ToString(),
            Total = c.Total,
            Detalles = c.Detalles.Select(d => new CompraDetalleDto
            {
                MateriaPrima = d.MateriaPrima?.Nombre ?? string.Empty,
                Cantidad = d.Cantidad,
                CostoUnitario = d.CostoUnitario,
                Subtotal = d.Subtotal
            }).ToList()
        };
    }
}