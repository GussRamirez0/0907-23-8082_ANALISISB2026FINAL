using NetGuardGT.Api.Models.Enums;

namespace NetGuardGT.Api.Services;

/// <summary>
/// REGLA 1 — Tabla de SLA centralizada (en un único lugar).
/// Define cuánto tiempo hay para atender un incidente según su severidad.
/// Si el negocio cambia un SLA, se modifica aquí y se refleja en toda la aplicación.
/// </summary>
public static class PoliticaSla
{
    /// <summary>Tiempo máximo de resolución permitido por severidad.</summary>
    public static readonly IReadOnlyDictionary<Severidad, TimeSpan> Tabla =
        new Dictionary<Severidad, TimeSpan>
        {
            [Severidad.Critico] = TimeSpan.FromHours(1),
            [Severidad.Urgente] = TimeSpan.FromHours(2),
            [Severidad.Alta]    = TimeSpan.FromHours(4),
            [Severidad.Media]   = TimeSpan.FromHours(8),
            [Severidad.Baja]    = TimeSpan.FromHours(24),
        };

    /// <summary>Devuelve el SLA (duración) correspondiente a una severidad.</summary>
    public static TimeSpan ObtenerSla(Severidad severidad) => Tabla[severidad];

    /// <summary>Calcula la fecha límite del SLA: FechaReporte + SLA de la severidad.</summary>
    public static DateTime CalcularFechaLimite(Severidad severidad, DateTime fechaReporte)
        => fechaReporte + Tabla[severidad];
}
