namespace NetGuardGT.Api.Services;

/// <summary>
/// Constantes de negocio centralizadas para no repetir "números mágicos"
/// en distintos servicios.
/// </summary>
public static class ReglasNegocio
{
    /// <summary>REGLA 2 — Máximo de incidentes activos (estado != Cerrado) por técnico.</summary>
    public const int MaxIncidentesActivos = 3;

    /// <summary>REGLA 5 — Antigüedad a partir de la cual un Crítico/Urgente en Registrado se escala.</summary>
    public static readonly TimeSpan UmbralEscalamiento = TimeSpan.FromHours(2);
}
