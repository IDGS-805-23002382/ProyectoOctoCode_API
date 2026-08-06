using AuthenticationAPI.Data;
using AuthenticationAPI.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationAPI.Controllers
{
    // Panel general del administrador: indicadores de clientes, inventario, compras,
    // comentarios pendientes y cotizaciones, para tener un vistazo rápido del negocio.
    [Authorize(Roles = "Administrador,admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public DashboardController(ApplicationDbContext db) => _db = db;

        [HttpGet("resumen")]
        public async Task<ActionResult<object>> Resumen()
        {
            var hoy = DateTime.UtcNow.Date;
            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);

            var totalClientes = await _db.Clientes.CountAsync();
            var clientesActivos = await _db.Clientes.CountAsync(c => c.Activo);

            var totalProductos = await _db.Productos.CountAsync(p => p.Activo);

            var materiasPrimas = await _db.MateriasPrimas.Where(m => m.Activo).ToListAsync();
            var valorInventario = materiasPrimas.Sum(m => m.Stock * m.CostoUnitarioActual);
            var insumosBajoStock = materiasPrimas.Count(m => m.Stock <= m.StockMinimo);

            var comprasDelMes = await _db.Compras
                .Where(c => c.Estatus == EstatusCompra.Recibida && c.FechaCompra >= inicioMes)
                .SumAsync(c => (decimal?)c.Total) ?? 0;

            var comentariosPendientes = await _db.Comentarios
                .CountAsync(c => c.Estatus == EstatusComentario.Nuevo || c.Estatus == EstatusComentario.EnRevision);

            var cotizacionesPendientes = await _db.SolicitudesCotizacion
                .CountAsync(s => s.Estatus == EstatusCotizacion.Pendiente);

            var mermasDelMes = await _db.MermasMateriaPrima
                .Where(m => m.FechaRegistro >= inicioMes && !m.Restaurada)
                .SumAsync(m => (decimal?)(m.Cantidad * m.CostoUnitario)) ?? 0;

            var calificacionPromedio = await _db.Resenas.AnyAsync()
                ? Math.Round(await _db.Resenas.AverageAsync(r => (double)r.Calificacion), 2)
                : (double?)null;

            return Ok(new
            {
                Clientes = new { Total = totalClientes, Activos = clientesActivos },
                Productos = new { TotalActivos = totalProductos },
                Inventario = new { ValorTotal = valorInventario, InsumosBajoStock = insumosBajoStock },
                ComprasDelMes = comprasDelMes,
                MermasDelMes = mermasDelMes,
                ComentariosPendientes = comentariosPendientes,
                CotizacionesPendientes = cotizacionesPendientes,
                CalificacionPromedioProductos = calificacionPromedio
            });
        }

        // Últimos movimientos relevantes para mostrar como "actividad reciente" en el panel.
        [HttpGet("actividad-reciente")]
        public async Task<ActionResult<object>> ActividadReciente()
        {
            var ultimasCompras = await _db.Compras.Include(c => c.Proveedor)
                .OrderByDescending(c => c.FechaCompra).Take(5)
                .Select(c => new { c.Id, Proveedor = c.Proveedor!.Nombre, c.FechaCompra, c.Total })
                .ToListAsync();

            var ultimosComentarios = await _db.Comentarios.Include(c => c.Cliente)
                .OrderByDescending(c => c.FechaCreacion).Take(5)
                .Select(c => new { c.Id, Cliente = c.Cliente!.RazonSocial, c.Asunto, Estatus = c.Estatus.ToString(), c.FechaCreacion })
                .ToListAsync();

            var ultimasMermas = await _db.MermasMateriaPrima.Include(m => m.MateriaPrima)
                .OrderByDescending(m => m.FechaRegistro).Take(5)
                .Select(m => new { m.Id, MateriaPrima = m.MateriaPrima!.Nombre, m.Cantidad, m.TipoMerma, m.FechaRegistro })
                .ToListAsync();

            return Ok(new { UltimasCompras = ultimasCompras, UltimosComentarios = ultimosComentarios, UltimasMermas = ultimasMermas });
        }
    }
}
