namespace AuthenticationAPI.Enums
{
    // Métodos de costeo de inventario soportados para Materia Prima.
    public enum MetodoCosteo
    {
        PEPS = 0,           // Primeras Entradas, Primeras Salidas (FIFO)
        UEPS = 1,           // Últimas Entradas, Primeras Salidas (LIFO)
        PromedioPonderado = 2
    }

    public enum EstatusComentario
    {
        Nuevo = 0,
        EnRevision = 1,
        Resuelto = 2,
        Cerrado = 3
    }

    public enum EstatusCotizacion { Pendiente = 0, Enviada = 1, Aceptada = 2, Rechazada = 3 }

    public enum EstatusCompra
    {
        Pendiente = 0,
        Recibida = 1,
        Cancelada = 2
    }

    public enum TipoDocumento { Manual = 0, Guia = 1, FichaTecnica = 2, Otro = 3 }
}
