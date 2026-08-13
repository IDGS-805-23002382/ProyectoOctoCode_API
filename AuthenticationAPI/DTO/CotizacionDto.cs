namespace AuthenticationAPI.DTO
{
    public class CotizacionDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Empresa { get; set; } = string.Empty;
        public string DescripcionProyecto { get; set; } = string.Empty;
        public int SuperficieHectareas { get; set; }
        public int CaudalDiario { get; set; }
        public int CantidadDesviaciones { get; set; }
        public string TipoAgua { get; set; } = string.Empty;
        public string RangoPh { get; set; } = string.Empty;
        public string AlimentacionElectrica { get; set; } = string.Empty;
    }
}