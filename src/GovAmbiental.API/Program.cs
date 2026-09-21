using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using GovAmbiental.API.Common;
using GovAmbiental.API.Data;
using GovAmbiental.API.Middleware;
using GovAmbiental.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ---- Configurações tipadas ----
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<AuthCredentials>(builder.Configuration.GetSection("AuthCredentials"));
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()!;

// ---- Banco de dados (SQL Server + migrations) ----
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null)));

// ---- Controllers + validação (FluentValidation) ----
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
})
.AddJsonOptions(options =>
{
    // Enums trafegam como texto (ex.: "Agua", "Alta") em requests e responses.
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// ---- Serviços de domínio ----
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<INormaService, NormaService>();
builder.Services.AddScoped<IAuditoriaService, AuditoriaService>();
builder.Services.AddScoped<IConformidadeService, ConformidadeService>();

// ---- Autenticação / Autorização (JWT Bearer) ----
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ---- Health checks (liveness / readiness) ----
// Consumidos pelo HEALTHCHECK do Docker, pelo docker-compose e pelas probes do
// Kubernetes. "self" responde sempre que o processo está de pé (liveness);
// "database" só fica Healthy quando o banco aceita conexão (readiness).
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("API em execução."), tags: new[] { "live" })
    .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "ready" });

// ---- Swagger / OpenAPI com suporte a JWT ----
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "API de Governança e Compliance Ambiental",
        Version = "v1",
        Description = "Registro automático de conformidade com normas ambientais e auditorias internas (ESG)."
    });

    var jwtScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Informe o token JWT obtido em /api/auth/login.",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    c.AddSecurityDefinition("Bearer", jwtScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { { jwtScheme, Array.Empty<string>() } });

    var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.xml");
    if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// ---- Pipeline ----
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "GovAmbiental API v1"));

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ---- Endpoints de health ----
static Task EscreverHealth(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json; charset=utf-8";

    var payload = new
    {
        status = report.Status.ToString(),
        ambiente = context.RequestServices.GetRequiredService<IHostEnvironment>().EnvironmentName,
        versao = Environment.GetEnvironmentVariable("APP_VERSION") ?? "local",
        duracaoMs = Math.Round(report.TotalDuration.TotalMilliseconds, 2),
        checagens = report.Entries.Select(e => new
        {
            nome = e.Key,
            status = e.Value.Status.ToString(),
            descricao = e.Value.Description
        })
    };

    return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
}

// Liveness: o container está vivo (não toca no banco).
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = registro => registro.Tags.Contains("live"),
    ResponseWriter = EscreverHealth
});

// Readiness: a aplicação está apta a receber tráfego (banco acessível).
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    ResponseWriter = EscreverHealth
});

// ---- Migrations + seed automáticos (exceto no ambiente de teste, que usa InMemory) ----
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    // Em container o banco pode subir alguns segundos depois da API: tenta
    // aplicar as migrations com backoff antes de derrubar o processo.
    const int tentativasMaximas = 10;
    for (var tentativa = 1; tentativa <= tentativasMaximas; tentativa++)
    {
        try
        {
            await context.Database.MigrateAsync();
            await DbSeeder.SeedAsync(context);
            logger.LogInformation("Migrations aplicadas e seed concluído (tentativa {Tentativa}).", tentativa);
            break;
        }
        catch (Exception ex) when (tentativa < tentativasMaximas)
        {
            logger.LogWarning(ex, "Banco indisponível (tentativa {Tentativa}/{Total}). Nova tentativa em 5s.",
                tentativa, tentativasMaximas);
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }
}

app.Run();

// Expõe a classe Program para os testes de integração (WebApplicationFactory<Program>).
public partial class Program { }
