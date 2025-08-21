using Microsoft.EntityFrameworkCore;
using Serilog;
using TextGenerationWithAI.Data;
using TextGenerationWithAI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHttpClient<TextGenerationService>();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Memory Cache
builder.Services.AddMemoryCache();

//Add DbContext with SQLite
builder.Services.AddDbContext<AppDbCotext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register the TextGenerationService
builder.Services.AddHttpClient<TextGenerationService>();

//CORS policy to allow all origins, methods, and headers
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

//Serilog configuration to log files
builder.Host.UseSerilog((context, config) =>
{
    config.WriteTo.Console() // log to console
          .WriteTo.File("Logs/log.txt", rollingInterval: RollingInterval.Day); // log to file, one per day
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); 
    app.UseSwaggerUI(); 
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
