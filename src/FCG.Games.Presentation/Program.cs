using FCG.Games.Infrastructure;
using FCG.Games.Presentation.Configuration;
using FCG.Games.Presentation.Extensions;
using FCG.Games.Presentation.Middlewares;
using GraphQL.AspNet.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Adicionar servi�os ao cont�iner.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerDocumentation();

// Database Configuration with migrations assembly
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("FCG.Games.Presentation")));

// Identity Configuration
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// Authentication & Authorization
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

// Application Services
builder.Services.AddAppServices();

// Service Bus Services
builder.Services.AddServiceBusServices(builder.Configuration);

// Health Checks for Microservice
builder.Services.AddHealthChecks(builder.Configuration);

// Logging Configuration
builder.Host.ConfigureSerilog();

// CORS Configuration for Microservice Architecture
builder.Services.AddCors(options =>
{
    options.AddPolicy("MicroservicePolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// --- SE��O DE INICIALIZA��O DO BANCO DE DADOS ---
// Executa as tarefas de inicializa��o do banco de dados de forma ass�ncrona
// antes de configurar o pipeline de requisi��es.
await InitializeDatabaseAsync(app);

// Configurar o pipeline de requisi��es HTTP.
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FCG Games Microservice V1");
        c.RoutePrefix = "swagger";
    });
}

// CORS
app.UseCors("MicroservicePolicy");

app.UseHttpsRedirection();

// Custom Middlewares
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<SerilogRequestLoggingMiddleware>();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// GraphQL middleware
app.UseGraphQL();

// Map endpoints
app.MapControllers();
app.MapHealthChecks("/health");

// Log microservice startup information
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("FCG Games Microservice started successfully");
logger.LogInformation("Microservice is listening on the configured URLs");
logger.LogInformation("Health check endpoint: /health");
logger.LogInformation("Swagger UI: /swagger");
logger.LogInformation("GraphQL endpoint: /graphql");
logger.LogInformation("Service Bus processing enabled for sales queue");

// Inicia a aplica��o para escutar por requisi��es
await app.RunAsync();

// Fun��o auxiliar para encapsular a l�gica de inicializa��o de forma segura
static async Task InitializeDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Applying database migrations...");
        var dbContext = services.GetRequiredService<AppDbContext>();
        
        // Ensure database exists and apply migrations
        await dbContext.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully.");

        // Wait a moment for database to be ready
        await Task.Delay(2000);

        logger.LogInformation("Seeding database roles...");
        await IdentityConfiguration.SeedRolesAsync(services);
        logger.LogInformation("Database roles seeded successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during database initialization.");
        // For development, we'll log the error but not crash the application
        if (app.Environment.IsDevelopment())
        {
            logger.LogWarning("Continuing startup despite database initialization error in development mode.");
        }
        else
        {
            // In production, crash the application if database can't be initialized
            throw;
        }
    }
}