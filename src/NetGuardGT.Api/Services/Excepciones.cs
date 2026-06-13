namespace NetGuardGT.Api.Services;

/// <summary>
/// Se lanza cuando se viola una regla de negocio. El middleware la traduce a HTTP 422
/// (Unprocessable Entity) con el mensaje en español.
/// </summary>
public class ReglaNegocioException : Exception
{
    public ReglaNegocioException(string mensaje) : base(mensaje) { }
}

/// <summary>
/// Se lanza cuando no se encuentra un recurso (incidente o técnico).
/// El middleware la traduce a HTTP 404 (Not Found).
/// </summary>
public class NoEncontradoException : Exception
{
    public NoEncontradoException(string mensaje) : base(mensaje) { }
}
