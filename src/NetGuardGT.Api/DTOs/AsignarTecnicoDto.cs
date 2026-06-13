using System.ComponentModel.DataAnnotations;

namespace NetGuardGT.Api.DTOs;

/// <summary>Datos para asignar un técnico a un incidente (Reglas 2 y 6).</summary>
public class AsignarTecnicoDto
{
    [Required(ErrorMessage = "El TecnicoId es obligatorio.")]
    public int TecnicoId { get; set; }

    /// <summary>Quién realiza la asignación (para el historial). Opcional.</summary>
    [MaxLength(120)]
    public string Responsable { get; set; } = "Supervisor";
}
