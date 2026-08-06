using AuthenticationAPI.Data;
using AuthenticationAPI.Enums;
using AuthenticationAPI.interfaces;
using AuthenticationAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationAPI.Services
{
    // Implementa el método de costeo de Materia Prima.
    // Cada compra crea una CapaCosto (lote a un costo dado); el consumo recorre las capas
    // en el orden que dicte el método (PEPS: más antigua primero; UEPS: más reciente primero;
    // Promedio Ponderado: se recalcula un costo único tras cada entrada y se consume a ese costo).
    public class CosteoService : ICosteoService
    {
        private readonly ApplicationDbContext _db;

        public CosteoService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task RegistrarEntradaAsync(int materiaPrimaId, decimal cantidad, decimal costoUnitario, int? compraDetalleId = null)
        {
            var materiaPrima = await _db.MateriasPrimas.FindAsync(materiaPrimaId)
                ?? throw new InvalidOperationException("Materia prima no encontrada.");

            _db.CapasCosto.Add(new CapaCosto
            {
                MateriaPrimaId = materiaPrimaId,
                CantidadOriginal = cantidad,
                CantidadDisponible = cantidad,
                CostoUnitario = costoUnitario,
                CompraDetalleId = compraDetalleId,
                FechaEntrada = DateTime.UtcNow
            });

            materiaPrima.Stock += cantidad;

            if (materiaPrima.MetodoCosteo == MetodoCosteo.PromedioPonderado)
            {
                // Costo promedio ponderado = (stock_anterior*costo_anterior + entrada*costo_entrada) / stock_nuevo
                var stockAnterior = materiaPrima.Stock - cantidad;
                var valorAnterior = stockAnterior * materiaPrima.CostoUnitarioActual;
                var valorEntrada = cantidad * costoUnitario;
                materiaPrima.CostoUnitarioActual = materiaPrima.Stock == 0
                    ? 0
                    : (valorAnterior + valorEntrada) / materiaPrima.Stock;
            }
            else
            {
                // Para PEPS/UEPS, el "costo actual" mostrado es el de la capa que se consumiría primero.
                materiaPrima.CostoUnitarioActual = costoUnitario;
            }

            await _db.SaveChangesAsync();
        }

        public async Task<decimal> ConsumirAsync(int materiaPrimaId, decimal cantidad)
        {
            var materiaPrima = await _db.MateriasPrimas.FindAsync(materiaPrimaId)
                ?? throw new InvalidOperationException("Materia prima no encontrada.");

            if (materiaPrima.Stock < cantidad)
                throw new InvalidOperationException($"Stock insuficiente de '{materiaPrima.Nombre}'. Disponible: {materiaPrima.Stock}, requerido: {cantidad}.");

            decimal costoTotal;

            if (materiaPrima.MetodoCosteo == MetodoCosteo.PromedioPonderado)
            {
                costoTotal = cantidad * materiaPrima.CostoUnitarioActual;

                // Se descuenta proporcionalmente de las capas disponibles (más antiguas primero) solo
                // para llevar el rastro de existencias por lote; el costo ya se fijó al promedio.
                await ConsumirDeCapasAsync(materiaPrimaId, cantidad, masAntiguaPrimero: true);
            }
            else
            {
                bool masAntiguaPrimero = materiaPrima.MetodoCosteo == MetodoCosteo.PEPS;
                costoTotal = await ConsumirDeCapasAsync(materiaPrimaId, cantidad, masAntiguaPrimero);

                // Tras consumir, el costo unitario "actual" pasa a ser el de la siguiente capa disponible.
                var siguienteCapa = masAntiguaPrimero
                    ? await _db.CapasCosto.Where(c => c.MateriaPrimaId == materiaPrimaId && c.CantidadDisponible > 0)
                        .OrderBy(c => c.FechaEntrada).FirstOrDefaultAsync()
                    : await _db.CapasCosto.Where(c => c.MateriaPrimaId == materiaPrimaId && c.CantidadDisponible > 0)
                        .OrderByDescending(c => c.FechaEntrada).FirstOrDefaultAsync();
                if (siguienteCapa != null)
                    materiaPrima.CostoUnitarioActual = siguienteCapa.CostoUnitario;
            }

            materiaPrima.Stock -= cantidad;
            await _db.SaveChangesAsync();

            return costoTotal;
        }

        private async Task<decimal> ConsumirDeCapasAsync(int materiaPrimaId, decimal cantidad, bool masAntiguaPrimero)
        {
            var query = _db.CapasCosto.Where(c => c.MateriaPrimaId == materiaPrimaId && c.CantidadDisponible > 0);
            var capas = masAntiguaPrimero
                ? await query.OrderBy(c => c.FechaEntrada).ToListAsync()
                : await query.OrderByDescending(c => c.FechaEntrada).ToListAsync();

            decimal restante = cantidad;
            decimal costoTotal = 0;

            foreach (var capa in capas)
            {
                if (restante <= 0) break;
                var tomar = Math.Min(capa.CantidadDisponible, restante);
                capa.CantidadDisponible -= tomar;
                costoTotal += tomar * capa.CostoUnitario;
                restante -= tomar;
            }

            if (restante > 0)
                throw new InvalidOperationException("No hay suficientes capas de costo para cubrir el consumo solicitado.");

            return costoTotal;
        }

        public async Task<decimal> CalcularCostoProductoAsync(int productoId)
        {
            var receta = await _db.RecetasBOM
                .Include(r => r.MateriaPrima)
                .Where(r => r.ProductoId == productoId)
                .ToListAsync();

            return receta.Sum(r => r.CantidadRequerida * (r.MateriaPrima?.CostoUnitarioActual ?? 0));
        }
    }
}
