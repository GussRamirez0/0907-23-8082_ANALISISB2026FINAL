namespace NetGuardGT.Api.Models.Enums;

/// <summary>
/// Especialidad técnica. Un incidente solo puede asignarse a un técnico cuya
/// especialidad coincida con la requerida por el incidente (Regla 6).
/// </summary>
public enum Especialidad
{
    FibraOptica,
    Microondas,
    SistemasElectricos
}
