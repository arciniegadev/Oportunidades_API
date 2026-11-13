using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oportunidades.Api.Data;
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
                Status = StatusCodes.Status400BadRequest
            });
        };
    });

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("OportunidadesDb");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Falta la cadena de conexión 'OportunidadesDb'. Configúrala en appsettings.json " +
        "o mediante la variable de entorno ConnectionStrings__OportunidadesDb.");
}

builder.Services.AddDbContext<OportunidadesDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<IOportunidadService, OportunidadService>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    utc = DateTime.UtcNow
}));

app.Run();
