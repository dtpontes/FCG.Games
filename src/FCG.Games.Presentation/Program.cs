using FCG.Games.Infrastructure;
using FCG.Games.Presentation.Configuration;
using FCG.Games.Presentation.Middlewares;
using GraphQL.AspNet.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Adicionar servi�os ao cont�iner.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerDocumentation();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services.AddAppServices();
builder.Host.ConfigureSerilog();

var app = builder.Build();

// Configurar o pipeline de requisi��es HTTP.
// Esta se��o agora � executada imediatamente, sem bloqueios.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<SerilogRequestLoggingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.UseGraphQL();

// --- SE��O CORRIGIDA ---
// Executa as tarefas de inicializa��o do banco de dados de forma ass�ncrona
// antes de iniciar o servidor web.
await InitializeDatabaseAsync(app);

// Inicia a aplica��o para escutar por requisi��es
await app.RunAsync();

// Fun��o auxiliar para encapsular a l�gica de inicializa��o de forma segura
static async Task InitializeDatabaseAsync(IHost app)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Applying database migrations...");
        var dbContext = services.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync(); // Usa a vers�o ass�ncrona
        logger.LogInformation("Database migrations applied successfully.");

        logger.LogInformation("Seeding database roles...");
        await IdentityConfiguration.SeedRolesAsync(services);
        logger.LogInformation("Database roles seeded successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during database initialization.");
        // Lan�ar a exce��o aqui far� com que a aplica��o pare se o banco de dados
        // n�o puder ser inicializado, o que � um comportamento desej�vel.
        throw;
    }
}