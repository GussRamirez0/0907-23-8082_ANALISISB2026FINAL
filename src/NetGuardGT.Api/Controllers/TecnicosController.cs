using Microsoft.AspNetCore.Mvc;
using NetGuardGT.Api.DTOs;
using NetGuardGT.Api.Services;

namespace NetGuardGT.Api.Controllers;

/// <summary>Endpoints de consulta de técnicos.</summary>
[ApiController]
[Route("api/tecnicos")]
[Produces("application/json")]
public class TecnicosController : ControllerBase
{
    private readonly ITecnicoService _service;

    public TecnicosController(ITecnicoService service) => _service = service;

    /// <summary>Lista todos los técnicos.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<TecnicoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TecnicoDto>>> Listar()
        => Ok(await _service.ListarAsync());

    /// <summary>Carga de trabajo de cada técnico (incidentes activos y disponibilidad, Regla 2).</summary>
    [HttpGet("carga")]
    [ProducesResponseType(typeof(List<CargaTrabajoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CargaTrabajoDto>>> Carga()
        => Ok(await _service.ObtenerCargaAsync());
}
