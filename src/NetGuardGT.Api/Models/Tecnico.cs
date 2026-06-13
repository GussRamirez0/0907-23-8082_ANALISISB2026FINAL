using NetGuardGT.Api.Models.Enums;

namespace NetGuardGT.Api.Models;

/// <summary>
/// Técnico especializado de NetGuard GT. Solo puede atender incidentes de su
/// misma especialidad y, como máximo, 3 incidentes activos a la vez (Regla 2).
/// </summary>
public class Tecnico
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public Especialidad Especialidad { get; set; }

    /// <summary>Incidentes asignados a este técnico (relación 1:N).</summary>
    public ICollection<Incidente> Incidentes { get; set; } = new List<Incidente>();
}
