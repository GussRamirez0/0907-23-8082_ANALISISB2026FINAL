using Microsoft.AspNetCore.Mvc;
using NetGuardGT.Api.DTOs;
using NetGuardGT.Api.Services;

namespace NetGuardGT.Api.Controllers;

/// <summary>Endpoints de reportes gerenciales.</summary>
[ApiController]
[Route("api/reportes")]
[Produces("application/json")]
public class ReportesController : ControllerBase
{
    private readonly IReporteService _service;

    public ReportesController(IReporteService service) => _service = service;

    /// <summary>Reporte de cumplimiento de SLA: totales, cumplidos, incumplidos y % (Regla 8).</summary>
    [HttpGet("sla")]
    [ProducesResponseType(typeof(ReporteSlaDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReporteSlaDto>> Sla()
        => Ok(await _service.GenerarReporteSlaAsync());
}
