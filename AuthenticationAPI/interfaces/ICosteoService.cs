namespace AuthenticationAPI.interfaces
{
    public interface ICosteoService
    {
        // Registra una entrada de inventario (capa de costo) generada por una compra
        // y recalcula Stock/CostoUnitarioActual de la materia prima según su método de costeo.
        Task RegistrarEntradaAsync(int materiaPrimaId, decimal cantidad, decimal costoUnitario, int? compraDetalleId = null);

        // Consume cantidad de inventario (p.ej. al fabricar un producto) siguiendo PEPS/UEPS/Promedio,
        // devolviendo el costo total consumido. Lanza excepción si no hay stock suficiente.
        Task<decimal> ConsumirAsync(int materiaPrimaId, decimal cantidad);

        // Calcula el costo unitario estimado de un producto sumando su receta (BOM) a costo actual.
        Task<decimal> CalcularCostoProductoAsync(int productoId);
    }
}
