using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

using Oportunidades.Api.Data;
using Oportunidades.Api.Infrastructure;
using Oportunidades.Api.Repositories;
using Oportunidades.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        // Los estados viajan como texto: "Nueva", "EnSeguimiento", "Ganada", "Perdida".
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(allowIntegerValues: false));
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errores = context.ModelState
                .Where(entrada => entrada.Value?.Errors.Count > 0)
                .ToDictionary(
                    entrada => JsonNamingPolicy.CamelCase.ConvertName(entrada.Key),
                    entrada => entrada.Value!.Errors
                        .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                            ? "Valor inválido."
                            : error.ErrorMessage)
                        .Distinct()
                        .ToArray());

            return new BadRequestObjectResult(new ValidationProblemDetails(errores)
            {
                Title = "La solicitud contiene errores de validación.",
                Status = StatusCodes.Status400BadRequest,
                Instance = context.HttpContext.Request.Path
            })
            {
                ContentTypes = { "application/problem+json" }
            };
        };
    });

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "API de Oportunidades Comerciales";
        document.Info.Version = "v1";

        var components = document.Components ??= new OpenApiComponents();
        components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header
        };

        // Todos los endpoints de oportunidades exigen el esquema Bearer.
        document.Security ??= new List<OpenApiSecurityRequirement>();
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
        });

        return Task.CompletedTask;
    });
});

var connectionString = builder.Configuration.GetConnectionString("OportunidadesDb");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Falta la cadena de conexión 'OportunidadesDb'. Configúrala en appsettings.json " +
        "o mediante la variable de entorno ConnectionStrings__OportunidadesDb.");
}

builder.Services.AddSingleton<IDbConnectionFactory>(new SqlConnectionFactory(connectionString));
builder.Services.AddScoped<IOportunidadRepository, OportunidadRepository>();
builder.Services.AddScoped<IOportunidadService, OportunidadService>();

var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
var jwtSigningKey = builder.Configuration["Jwt:SigningKey"];

if (string.IsNullOrWhiteSpace(jwtIssuer) || string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException(
        "Faltan 'Jwt:Issuer' o 'Jwt:Audience' en la configuración.");
}

if (string.IsNullOrWhiteSpace(jwtSigningKey) || Encoding.UTF8.GetByteCount(jwtSigningKey) < 32)
{
    throw new InvalidOperationException(
        "La clave 'Jwt:SigningKey' no está configurada o tiene menos de 32 caracteres. " +
        "Configúrala fuera del repositorio con: " +
        "dotnet user-secrets set \"Jwt:SigningKey\" \"<clave-larga>\" --project src/Oportunidades.Api " +
        "o mediante la variable de entorno Jwt__SigningKey.");
}

var jwtSigningKeyBytes = Encoding.UTF8.GetBytes(jwtSigningKey);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.Zero,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(jwtSigningKeyBytes),
            ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
            RoleClaimType = "role",
            NameClaimType = "sub"
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();

                if (!context.Response.HasStarted)
                {
                    await Results.Problem(
                            statusCode: StatusCodes.Status401Unauthorized,
                            title: "No autorizado",
                            detail: "Se requiere un token JWT válido para acceder a este recurso.")
                        .ExecuteAsync(context.HttpContext);
                }
            },
            OnForbidden = async context =>
            {
                if (!context.Response.HasStarted)
                {
                    await Results.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Acceso denegado",
                            detail: "El rol del token no tiene permiso para esta operación.")
                        .ExecuteAsync(context.HttpContext);
                }
            }
        };
    });

builder.Services.AddAuthorization();


var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();    // 401/403/404 sin cuerpo también salen como ProblemDetails


app.MapOpenApi();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    utc = DateTime.UtcNow
}));

app.Run();
