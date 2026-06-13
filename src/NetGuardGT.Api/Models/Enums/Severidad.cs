namespace NetGuardGT.Api.Models.Enums;

/// <summary>
/// Severidad del incidente. El orden refleja la criticidad (Critico es lo más grave).
/// La severidad determina el SLA aplicable (ver <c>PoliticaSla</c>).
/// </summary>
public enum Severidad
{
    Critico,
    Urgente,
    Alta,
    Media,
    Baja
}
