using NetGuardGT.Api.DTOs;

namespace NetGuardGT.Api.Services;

public interface IReporteService
{
    /// <summary>Genera el reporte de cumplimiento de SLA (Regla 8).</summary>
    Task<ReporteSlaDto> GenerarReporteSlaAsync();
}
