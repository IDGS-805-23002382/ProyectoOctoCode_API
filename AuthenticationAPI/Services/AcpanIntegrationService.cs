using AuthenticationAPI.Configuration;
using AuthenticationAPI.interfaces;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace AuthenticationAPI.Services
{
    // Cliente HTTP hacia la API de Acpan (proyecto independiente de IoT / tratamiento de agua).
    // Este backend NUNCA accede directamente a la base de datos de Acpan: solo consume su API
    // pública, manteniendo el aislamiento entre ambos sistemas.
    public class AcpanIntegrationService : IAcpanIntegrationService
    {
        private readonly HttpClient _http;
        private readonly ILogger<AcpanIntegrationService> _logger;

        public AcpanIntegrationService(HttpClient http, IOptions<AcpanApiSettings> settings, ILogger<AcpanIntegrationService> logger)
        {
            _http = http;
            if (!string.IsNullOrWhiteSpace(settings.Value.BaseUrl))
                _http.BaseAddress = new Uri(settings.Value.BaseUrl);
            if (!string.IsNullOrWhiteSpace(settings.Value.ApiKey))
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.Value.ApiKey);
            _logger = logger;
        }

        public async Task<AcpanCotizacionResult> ObtenerCotizacionAsync(string descripcionProyecto)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("api/cotizaciones", new { descripcion = descripcionProyecto });
                response.EnsureSuccessStatusCode();
                var resultado = await response.Content.ReadFromJsonAsync<AcpanCotizacionResult>();
                return resultado ?? new AcpanCotizacionResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al solicitar cotización a la API de Acpan.");
                throw new InvalidOperationException("No fue posible obtener la cotización desde Acpan en este momento.", ex);
            }
        }

        public async Task<List<AcpanInventarioItem>> ObtenerInventarioAsync()
        {
            try
            {
                var resultado = await _http.GetFromJsonAsync<List<AcpanInventarioItem>>("api/inventario");
                return resultado ?? new List<AcpanInventarioItem>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar el inventario de la API de Acpan.");
                throw new InvalidOperationException("No fue posible obtener el inventario desde Acpan en este momento.", ex);
            }
        }
    }
}
