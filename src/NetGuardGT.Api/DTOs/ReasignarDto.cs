using System.ComponentModel.DataAnnotations;

namespace NetGuardGT.Api.DTOs;

/// <summary>
/// Datos para reasignar o liberar un incidente (Regla 4).
/// Si <see cref="TecnicoId"/> es null, el incidente se LIBERA (queda sin técnico).
/// Si trae un valor, se reasigna a ese técnico (revalida Reglas 2 y 6).
/// </summary>
public class ReasignarDto
{
    /// <summary>Técnico destino. null = liberar (dejar sin técnico).</summary>
    public int? TecnicoId { get; set; }

    [MaxLength(120)]
    public string Responsable { get; set; } = "Supervisor";

    [MaxLength(500)]
    public string Motivo { get; set; } = string.Empty;
}
