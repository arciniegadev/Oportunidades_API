namespace Oportunidades.Api.Security;

/// <summary>
/// Roles admitidos en los tokens JWT.
/// </summary>
public static class Roles
{
    public const string Consulta = "Consulta";
    public const string Editor = "Editor";
    public const string ConsultaOEditor = Consulta + "," + Editor;
}
