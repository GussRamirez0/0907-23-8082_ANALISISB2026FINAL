using Microsoft.EntityFrameworkCore;
using NetGuardGT.Api.Data;
using NetGuardGT.Api.DTOs;
using NetGuardGT.Api.Models.Enums;

namespace NetGuardGT.Api.Services;

/// <summary>
/// REGLA 8 — Reporte de cumplimiento de SLA.
/// Clasifica cada incidente y calcula totales y porcentaje de cumplimiento.
/// </summary>
public class ReporteService : IReporteService
{
    private readonly AppDbContext _db;
    private readonly IRelojSistema _reloj;

    public ReporteService(AppDbContext db, IRelojSistema reloj)
    {
        _db = db;
        _reloj = reloj;
    }

    public async Task<ReporteSlaDto> GenerarReporteSlaAsync()
    {
        var ahora = _reloj.Ahora;
        var incidentes = await _db.Incidentes.AsNoTracking().ToListAsync();

        int cumplidos = 0;
        int incumplidosResueltos = 0;
        int vencidosAbiertos = 0;
        int totalResueltos = 0;

        var detalle = new List<IncidenteSlaDto>();

        foreach (var i in incidentes)
        {
            bool estaAbierto = i.Estado != EstadoIncidente.Resuelto && i.Estado != EstadoIncidente.Cerrado;
            string clasificacion;

            if (i.FechaResolucion != null)
            {
                // Incidente resuelto: cumple si se resolvió dentro del plazo.
                totalResueltos++;
                if (i.FechaResolucion.Value <= i.FechaLimite)
                {
                    cumplidos++;
                    clasificacion = "Cumplido";
                }
                else
                {
                    incumplidosResueltos++;
                    clasificacion = "IncumplidoResuelto";
                }
            }
            else if (estaAbierto && ahora > i.FechaLimite)
            {
                // Abierto y con la fecha límite ya pasada: vencido (incumplimiento).
                vencidosAbiertos++;
                clasificacion = "VencidoAbierto";
            }
            else
            {
                // Abierto pero todavía dentro de plazo: resultado SLA aún indeterminado.
                clasificacion = "EnPlazo";
            }

            detalle.Add(new IncidenteSlaDto
            {
                Id = i.Id,
                SitioRed = i.SitioRed,
                Severidad = i.Severidad,
                Estado = i.Estado,
                FechaLimite = i.FechaLimite,
                FechaResolucion = i.FechaResolucion,
                Clasificacion = clasificacion
            });
        }

        int totalIncumplidos = incumplidosResueltos + vencidosAbiertos;
        int baseCalculo = cumplidos + totalIncumplidos; // los "EnPlazo" no se cuentan aún

        // Si no hay nada con resultado definitivo, reportamos 100% (no hay incumplimientos).
        double porcentaje = baseCalculo == 0
            ? 100.0
            : Math.Round(100.0 * cumplidos / baseCalculo, 2);

        return new ReporteSlaDto
        {
            TotalIncidentes = incidentes.Count,
            TotalResueltos = totalResueltos,
            Cumplidos = cumplidos,
            IncumplidosResueltos = incumplidosResueltos,
            VencidosAbiertos = vencidosAbiertos,
            TotalIncumplidos = totalIncumplidos,
            PorcentajeCumplimiento = porcentaje,
            Detalle = detalle
        };
    }
}
