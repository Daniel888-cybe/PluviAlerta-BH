using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using PluviAlerta.Api.Auth;
using PluviAlerta.Api.Coleta;
using PluviAlerta.Api.Controllers;
using PluviAlerta.Infrastructure.Alertas;
using PluviAlerta.Infrastructure.Dados;
using PluviAlerta.Infrastructure.Relatos;
using PluviAlerta.Infrastructure.OpenMeteo;
using PluviAlerta.Infrastructure.Risco;
using PluviAlerta.Infrastructure.Simulacao;
using PluviAlerta.Infrastructure.Telegram;
using PluviAlerta.Api.Telegram;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("PluviAlerta")
    ?? throw new InvalidOperationException(
        "Connection string 'PluviAlerta' não configurada. Veja a seção 'Como rodar sem Docker' no README.");

// Versão fixa em vez de AutoDetect: assim gerar migration não exige o banco ligado.
builder.Services.AddDbContext<PluviAlertaDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 4))));

builder.Services.AddHttpClient<OpenMeteoClient>(c =>
{
    c.BaseAddress = new Uri(builder.Configuration["OpenMeteo:BaseUrl"] ?? "https://api.open-meteo.com/");
    c.Timeout = TimeSpan.FromSeconds(20);
});
builder.Services.AddScoped<AtualizadorDeRisco>();
builder.Services.AddScoped<ServicoDeAlertas>();
builder.Services.AddScoped<ServicoDeRelatos>();

// Com token: alertas pelo Telegram e bot respondendo /start e /sair. Sem token: alertas só no log.
var tokenTelegram = builder.Configuration["Telegram:BotToken"];
if (!string.IsNullOrWhiteSpace(tokenTelegram))
{
    builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(tokenTelegram));
    builder.Services.AddScoped<INotificadorAlerta, NotificadorTelegram>();
    builder.Services.AddScoped<ComandosBot>();
    builder.Services.AddSingleton<LimiteRelatosBot>();
    builder.Services.AddHostedService<BotTelegramWorker>();
}
else
{
    builder.Services.AddScoped<INotificadorAlerta, NotificadorSoLog>();
}
builder.Services.AddScoped<SimuladorDeTempestade>();
builder.Services.AddHostedService<ColetaWorker>();

// JWT só protege as rotas de administração (simulação). Falha ao subir se a chave não estiver configurada.
var jwt = ConfiguracaoJwt.Ler(builder.Configuration);
builder.Services.AddSingleton(jwt);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o => o.TokenValidationParameters = new()
    {
        ValidIssuer = jwt.Emissor,
        ValidAudience = jwt.Emissor,
        IssuerSigningKey = jwt.Chave,
    });
builder.Services.AddAuthorization();

// Enums saem como texto no JSON ("Laranja" em vez de 2).
// Configurado duas vezes: os controllers usam um, o gerador do OpenAPI lê o outro.
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi(o =>
{
    o.AddDocumentTransformer((doc, _, _) =>
    {
        doc.Info.Title = "PluviAlerta BH";
        doc.Info.Description = "Estimativa de um projeto acadêmico, não é alerta oficial. " +
                               "Em emergência, ligue 199 (Defesa Civil). Dados meteorológicos: Open-Meteo.";
        // Botão "Authorize" do Swagger: cola o token de POST /api/auth/token.
        doc.Components ??= new OpenApiComponents();
        doc.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
        };
        return Task.CompletedTask;
    });
    // Marca com cadeado só as rotas que têm [Authorize].
    o.AddOperationTransformer((op, ctx, _) =>
    {
        if (ctx.Description.ActionDescriptor.EndpointMetadata.OfType<IAuthorizeData>().Any())
        {
            var esquema = new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
            };
            op.Security = [new OpenApiSecurityRequirement { [esquema] = [] }];
        }
        return Task.CompletedTask;
    });
});

var origensFront = builder.Configuration.GetSection("Cors:Origens").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(origensFront).AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // POST /api/relatos: até 5 relatos a cada 10 min por IP, para dificultar relato falso em massa.
    o.AddPolicy(RelatosController.PoliticaLimite, http => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(10) }));

    // POST /api/auth/token: até 5 tentativas por minuto por IP, contra chute de senha.
    o.AddPolicy(AuthController.PoliticaLimite, http => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1) }));
});

builder.Services.AddHealthChecks();

var app = builder.Build();

// Cria/atualiza as tabelas ao subir, para o "docker compose up" já deixar o banco pronto.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<PluviAlertaDbContext>().Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // Só a tela do Swagger: o documento vem do AddOpenApi da Microsoft.
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "PluviAlerta BH v1"));
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}

// Sem UseHttpsRedirection: no Docker a API roda só em HTTP (porta 8080).
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
