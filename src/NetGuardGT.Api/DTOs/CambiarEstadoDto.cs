using System.ComponentModel.DataAnnotations;
using NetGuardGT.Api.Models.Enums;

namespace NetGuardGT.Api.DTOs;

/// <summary>Datos para avanzar el estado de un incidente (Regla 3).</summary>
public class CambiarEstadoDto
{
    [Required(ErrorMessage = "El nuevo estado es obligatorio.")]
    public EstadoIncidente NuevoEstado { get; set; }

    [MaxLength(120)]
    public string Responsable { get; set; } = "Supervisor";

    [MaxLength(500)]
    public string Motivo { get; set; } = string.Empty;
}
