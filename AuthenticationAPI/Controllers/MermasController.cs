using AuthenticationAPI.Data;
using AuthenticationAPI.DTO;
using AuthenticationAPI.interfaces;
using AuthenticationAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AuthenticationAPI.Controllers
{
    // Modulo dedicado de Merma: baja de materia prima por daño/caducidad/perdida/error,
    // con bitacora de quien la registro y cuando. Se puede corregir (editar motivo/detalle)
    // o revertir por completo (restaurar a inventario) solo dentro de los primeros 15 dias;
    // despues queda bloqueada para no perder la trazabilidad del costeo.
    [Authorize(Roles = "Administrador,admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class MermasController : ControllerBase
    {
        private const int DiasLimiteEdicion = 15;

        private readonly ApplicationDbContext _db;
        private readonly ICosteoService _costeoService;
        private readonly UserManager<ApplicationUser> _userManager;

        public MermasController(ApplicationDbContext db, ICosteoService costeoService, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _costeoService = costeoService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MermaDto>>> GetAll()
        {
            var mermas = await _db.MermasMateriaPrima
                .Include(m => m.MateriaPrima)
                .Include(m => m.UsuarioRegistro)
                .OrderByDescending(m => m.FechaRegistro)
                .ToListAsync();

            return Ok(mermas.Select(MapDto));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<MermaDto>> GetById(int id)
        {
            var merma = await _db.MermasMateriaPrima
                .Include(m => m.MateriaPrima)
                .Include(m => m.UsuarioRegistro)
                .FirstOrDefaultAsync(m => m.Id == id);

            return merma == null ? NotFound() : Ok(MapDto(merma));
        }

        // El usuario que registra la merma NO se captura en el formulario: se toma
        // automaticamente del token JWT de la sesion.
        [HttpPost]
        public async Task<ActionResult<MermaDto>> Registrar(RegistrarMermaDto dto)
        {
            var materia = await _db.MateriasPrimas.FindAsync(dto.MateriaPrimaId);
            if (materia == null) return BadRequest("Materia prima no encontrada.");

            if (dto.Cantidad > materia.Stock)
                return BadRequest($"No puedes dar de baja {dto.Cantidad} {materia.UnidadMedida}: solo hay {materia.Stock} en stock.");

            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? string.Empty;

            // Se consume igual que cualquier otra salida de inventario, respetando PEPS/UEPS/Promedio,
            // para que el kardex y el costo unitario de la materia prima se mantengan consistentes.
            decimal costoTotal;
            try
            {
                costoTotal = await _costeoService.ConsumirAsync(dto.MateriaPrimaId, dto.Cantidad);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }

            var merma = new MermaMateriaPrima
            {
                MateriaPrimaId = dto.MateriaPrimaId,
                Cantidad = dto.Cantidad,
                CostoUnitario = dto.Cantidad == 0 ? 0 : costoTotal / dto.Cantidad,
                TipoMerma = dto.TipoMerma,
                Detalle = dto.Detalle,
                UsuarioRegistroId = usuarioId,
                FechaRegistro = DateTime.UtcNow
            };

            _db.MermasMateriaPrima.Add(merma);
            await _db.SaveChangesAsync();

            var creada = await _db.MermasMateriaPrima
                .Include(m => m.MateriaPrima)
                .Include(m => m.UsuarioRegistro)
                .FirstAsync(m => m.Id == merma.Id);

            return CreatedAtAction(nameof(GetById), new { id = merma.Id }, MapDto(creada));
        }

        // Corrige tipo/detalle de una merma ya registrada (no cambia cantidades ni stock).
        // Solo disponible dentro de los primeros 15 dias y si no ha sido restaurada.
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Editar(int id, EditarMermaDto dto)
        {
            var merma = await _db.MermasMateriaPrima.FindAsync(id);
            if (merma == null) return NotFound();

            if (!PuedeModificarse(merma))
                return BadRequest("Esta merma ya no se puede editar (pasaron más de 15 días o ya fue restaurada).");

            merma.TipoMerma = dto.TipoMerma;
            merma.Detalle = dto.Detalle;
            merma.FechaUltimaEdicion = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // El usuario se equivoco al registrarla: regresa la cantidad completa a inventario
        // (nueva capa de costo al mismo costo unitario con el que se dio de baja) y la marca
        // como restaurada, dejando la merma en la bitacora como referencia historica.
        [HttpPost("{id:int}/restaurar")]
        public async Task<IActionResult> Restaurar(int id)
        {
            var merma = await _db.MermasMateriaPrima.FindAsync(id);
            if (merma == null) return NotFound();

            if (!PuedeModificarse(merma))
                return BadRequest("Esta merma ya no se puede restaurar (pasaron más de 15 días o ya fue restaurada).");

            await _costeoService.RegistrarEntradaAsync(merma.MateriaPrimaId, merma.Cantidad, merma.CostoUnitario);

            merma.Restaurada = true;
            merma.FechaRestauracion = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        private static bool PuedeModificarse(MermaMateriaPrima m) =>
            !m.Restaurada && DateTime.UtcNow <= m.FechaRegistro.AddDays(DiasLimiteEdicion);

        private static MermaDto MapDto(MermaMateriaPrima m) => new()
        {
            Id = m.Id,
            MateriaPrimaId = m.MateriaPrimaId,
            MateriaPrima = m.MateriaPrima?.Nombre ?? string.Empty,
            UnidadMedida = m.MateriaPrima?.UnidadMedida ?? string.Empty,
            Cantidad = m.Cantidad,
            CostoUnitario = m.CostoUnitario,
            CostoTotal = m.Cantidad * m.CostoUnitario,
            TipoMerma = m.TipoMerma,
            Detalle = m.Detalle,
            UsuarioRegistro = m.UsuarioRegistro?.NombreCompleto ?? string.Empty,
            FechaRegistro = m.FechaRegistro,
            FechaUltimaEdicion = m.FechaUltimaEdicion,
            Restaurada = m.Restaurada,
            FechaRestauracion = m.FechaRestauracion,
            PuedeModificarse = PuedeModificarse(m)
        };
    }
}
