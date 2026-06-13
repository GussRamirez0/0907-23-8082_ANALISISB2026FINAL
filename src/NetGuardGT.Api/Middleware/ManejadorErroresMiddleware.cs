using NetGuardGT.Api.Services;

namespace NetGuardGT.Api.Middleware;

/// <summary>
/// Middleware que captura las excepciones de la capa de servicios y las traduce
/// a códigos HTTP con un mensaje claro en español. Así los controladores quedan
/// limpios (sin try/catch repetidos) y los códigos son consistentes en toda la API.
/// </summary>
public class ManejadorErroresMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ManejadorErroresMiddleware> _logger;

    public ManejadorErroresMiddleware(RequestDelegate next, ILogger<ManejadorErroresMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (NoEncontradoException ex)
        {
            await EscribirRespuestaAsync(context, StatusCodes.Status404NotFound, ex.Message);
        }
        catch (ReglaNegocioException ex)
        {
            // 422 Unprocessable Entity: la petición es válida en forma, pero viola una regla de negocio.
            await EscribirRespuestaAsync(context, StatusCodes.Status422UnprocessableEntity, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error no controlado al procesar {Ruta}", context.Request.Path);
            await EscribirRespuestaAsync(context, StatusCodes.Status500InternalServerError,
                "Ocurrió un error interno en el servidor.");
        }
    }

    private static async Task EscribirRespuestaAsync(HttpContext context, int codigo, string mensaje)
    {
        context.Response.StatusCode = codigo;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { estado = codigo, mensaje });
    }
}
