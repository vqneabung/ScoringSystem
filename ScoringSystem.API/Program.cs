using ScoringSystem.API.Extensions;
using ScoringSystem.API.Test;
using ScoringSystem.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddScoped<FileHelper>();
builder.Services.AddScoped<ProcessHelper>();
builder.Services.AddScoped<InteractWebsite>();
builder.Services.AddScoped<ScoringService>();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
