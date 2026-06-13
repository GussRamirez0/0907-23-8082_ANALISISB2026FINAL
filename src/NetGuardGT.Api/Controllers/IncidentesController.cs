using Microsoft.AspNetCore.Mvc;
using NetGuardGT.Api.DTOs;
using NetGuardGT.Api.Models.Enums;
using NetGuardGT.Api.Services;

namespace NetGuardGT.Api.Controllers;

/// <summary>
/// Endpoints de incidentes. El controlador es delgado: solo recibe la petición,
/// delega en el servicio (donde están las reglas de negocio) y devuelve el código HTTP.
/// </summary>
[ApiController]
[Route("api/incidentes")]
[Produces("application/json")]
public class IncidentesController : ControllerBase
{
    private readonly IIncidenteService _service;

    public IncidentesController(IIncidenteService service) => _service = service;

    /// <summary>Crea un incidente. Calcula la fecha límite del SLA automáticamente (Regla 1).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(IncidenteResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IncidenteResponseDto>> Crear([FromBody] CrearIncidenteDto dto)
    {
        var creado = await _service.CrearAsync(dto);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = creado.Id }, creado);
    }

    /// <summary>Lista incidentes con filtros opcionales por estado, severidad, técnico y sitio.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<IncidenteResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<IncidenteResponseDto>>> Listar(
        [FromQuery] EstadoIncidente? estado,
        [FromQuery] Severidad? severidad,
        [FromQuery] int? tecnicoId,
        [FromQuery] string? sitio)
    {
        var lista = await _service.ListarAsync(estado, severidad, tecnicoId, sitio);
        return Ok(lista);
    }

    /// <summary>Detalle de un incidente.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(IncidenteResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidenteResponseDto>> ObtenerPorId(int id)
    {
        var incidente = await _service.ObtenerPorIdAsync(id);
        return Ok(incidente);
    }

    /// <summary>Asigna un técnico al incidente (Reglas 2 y 6).</summary>
    [HttpPut("{id:int}/asignar")]
    [ProducesResponseType(typeof(IncidenteResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<IncidenteResponseDto>> Asignar(int id, [FromBody] AsignarTecnicoDto dto)
    {
        var incidente = await _service.AsignarAsync(id, dto);
        return Ok(incidente);
    }

    /// <summary>Avanza el estado del incidente respetando el flujo unidireccional (Regla 3).</summary>
    [HttpPut("{id:int}/estado")]
    [ProducesResponseType(typeof(IncidenteResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<IncidenteResponseDto>> CambiarEstado(int id, [FromBody] CambiarEstadoDto dto)
    {
        var incidente = await _service.CambiarEstadoAsync(id, dto);
        return Ok(incidente);
    }

    /// <summary>Reasigna a otro técnico o libera el incidente (Reglas 2, 6 y 7).</summary>
    [HttpPut("{id:int}/reasignar")]
    [ProducesResponseType(typeof(IncidenteResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<IncidenteResponseDto>> Reasignar(int id, [FromBody] ReasignarDto dto)
    {
        var incidente = await _service.ReasignarAsync(id, dto);
        return Ok(incidente);
    }

    /// <summary>Evalúa todos los incidentes y escala los Crítico/Urgente vencidos (Regla 5).</summary>
    [HttpPost("escalar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Escalar()
    {
        var escalados = await _service.EscalarVencidosAsync();
        return Ok(new
        {
            mensaje = escalados.Count == 0
                ? "No había incidentes que cumplieran las condiciones de escalamiento."
                : $"Se escalaron {escalados.Count} incidente(s).",
            totalEscalados = escalados.Count,
            incidentes = escalados
        });
    }

    /// <summary>Historial de cambios del incidente (Regla 7).</summary>
    [HttpGet("{id:int}/historial")]
    [ProducesResponseType(typeof(List<HistorialEstadoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<HistorialEstadoDto>>> Historial(int id)
    {
        var historial = await _service.ObtenerHistorialAsync(id);
        return Ok(historial);
    }
}
