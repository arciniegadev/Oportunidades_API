using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

const string IssuerPredeterminado = "OportunidadesApi";
const string AudiencePredeterminada = "OportunidadesApiClientes";
const int MinutosPredeterminados = 60;

MostrarAyudaSiCorresponde(args);

if (args.Length == 0)
{
    MostrarAyuda();
    return 1;
}

var rol = NormalizarRol(args[0]);

if (rol is null)
{
    Console.Error.WriteLine($"Rol no válido: '{args[0]}'. Debe ser Consulta o Editor.");
    return 1;
}

var minutos = MinutosPredeterminados;
var indice = 1;

while (indice < args.Length)
{
    if (args[indice] is "--minutes" or "-m")
    {
        if (indice + 1 >= args.Length || !int.TryParse(args[indice + 1], out minutos))
        {
            Console.Error.WriteLine("Falta un número entero válido después de --minutes.");
            return 1;
        }

        indice += 2;
        continue;
    }

    Console.Error.WriteLine($"Opción no reconocida: '{args[indice]}'.");
    return 1;
}

var configuration = new ConfigurationBuilder()
    .AddUserSecrets(typeof(Program).Assembly, optional: true)
    .AddEnvironmentVariables()
    .Build();

var issuer = configuration["Jwt:Issuer"] ?? IssuerPredeterminado;
var audience = configuration["Jwt:Audience"] ?? AudiencePredeterminada;
var signingKey = configuration["Jwt:SigningKey"];

if (string.IsNullOrWhiteSpace(signingKey) || Encoding.UTF8.GetByteCount(signingKey) < 32)
{
    Console.Error.WriteLine(
        "No se encontró una clave JWT válida (mínimo 32 caracteres).");
    Console.Error.WriteLine(
        "Configúrala con:");
    Console.Error.WriteLine(
        "  dotnet user-secrets set \"Jwt:SigningKey\" \"<clave-larga>\" --project src/Oportunidades.Api");
    Console.Error.WriteLine(
        "o define la variable de entorno Jwt__SigningKey.");
    return 1;
}

var ahora = DateTime.UtcNow;

var claims = new[]
{
    new Claim(JwtRegisteredClaimNames.Sub, rol.ToLowerInvariant()),
    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
    new Claim(JwtRegisteredClaimNames.Iat, EpochTime.GetIntDate(ahora).ToString(), ClaimValueTypes.Integer64),
    new Claim("role", rol)
};

var credenciales = new SigningCredentials(
    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
    SecurityAlgorithms.HmacSha256);

var token = new JwtSecurityToken(
    issuer: issuer,
    audience: audience,
    claims: claims,
    notBefore: ahora,
    expires: ahora.AddMinutes(minutos),
    signingCredentials: credenciales);

Console.WriteLine(new JwtSecurityTokenHandler().WriteToken(token));
return 0;

static void MostrarAyudaSiCorresponde(string[] argumentos)
{
    if (argumentos.Length > 0 && argumentos[0] is "-h" or "--help")
    {
        MostrarAyuda();
        Environment.Exit(0);
    }
}

static void MostrarAyuda()
{
    Console.WriteLine("""
        Genera un token JWT local para probar la API de oportunidades.

        Uso:
          dotnet run --project tools/Oportunidades.TokenGenerator -- <Consulta|Editor> [--minutes N]

        Ejemplos:
          dotnet run --project tools/Oportunidades.TokenGenerator -- Consulta
          dotnet run --project tools/Oportunidades.TokenGenerator -- Editor --minutes 120

        La clave se lee de Jwt:SigningKey en los user secrets (compartidos con la API)
        o de la variable de entorno Jwt__SigningKey.
        """);
}

static string? NormalizarRol(string valor) =>
    valor.Trim().ToLowerInvariant() switch
    {
        "consulta" => "Consulta",
        "editor" => "Editor",
        _ => null
    };
