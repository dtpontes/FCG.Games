using FCG.Games.Infrastructure;
using FCG.Games.Presentation.Configuration;
using FCG.Games.Presentation.Extensions;
using FCG.Games.Presentation.Middlewares;
using GraphQL.AspNet.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Adicionar serviços ao contêiner.
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

// Configurar o pipeline de requisições HTTP.
if (app.Environment.IsDevelopment())
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

// Controllers and GraphQL
app.MapControllers();
app.UseGraphQL();

// Health Check Endpoint
app.MapHealthChecks("/health");

// --- SEÇÃO CORRIGIDA ---
// Executa as tarefas de inicialização do banco de dados de forma assíncrona
// antes de iniciar o servidor web.
await InitializeDatabaseAsync(app);

// Log microservice startup information
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("FCG Games Microservice started successfully");
logger.LogInformation("Microservice is listening on the configured URLs");
logger.LogInformation("Health check endpoint: /health");
logger.LogInformation("Swagger UI: /swagger");

// Inicia a aplicação para escutar por requisições
await app.RunAsync();

// Função auxiliar para encapsular a lógica de inicialização de forma segura
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