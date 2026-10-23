using WeSharkia.Infrastructure;
using WeSharkia.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add Clean Architecture Infrastructure (EF Core with SQL Server)
builder.Services.AddInfrastructure(builder.Configuration);

// Add Controllers and JSON settings
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// OpenAPI / Swagger
builder.Services.AddOpenApi();

// CORS configuration for Frontend (React Vite & Remote)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowViteClient", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Seed initial database and sample data
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await DbInitializer.SeedAsync(db);
        logger.LogInformation("Database seeded successfully with WE Sharkia default data.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowViteClient");

app.UseAuthorization();

app.MapControllers();

app.Run();
