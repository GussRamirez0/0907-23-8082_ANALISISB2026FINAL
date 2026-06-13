namespace NetGuardGT.Api.Services;

/// <summary>
/// Implementación real del reloj. Usa UTC para evitar problemas de zona horaria
/// (importante al desplegar en Render.com, cuyos servidores corren en UTC).
/// </summary>
public class RelojSistema : IRelojSistema
{
    public DateTime Ahora => DateTime.UtcNow;
}
