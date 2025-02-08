using Polly;
using Polly.Retry;
using WeatherApi;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
//builder.Environment.EnvironmentName = "Development"; // Force Development Mode
// Add services to the container.
builder.Services.AddHttpClient<WeatherService>().AddStandardResilienceHandler();
builder.Services.AddSingleton<WeatherService>();

builder.Services.AddResiliencePipeline("default", x =>
{
    x.AddRetry(new RetryStrategyOptions
    {
        ShouldHandle = new PredicateBuilder().Handle<Exception>(),
        Delay = TimeSpan.FromSeconds(2),
        MaxRetryAttempts = 2,
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = true
    })
        .AddTimeout(TimeSpan.FromSeconds(30));
});

// Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Weather API", Version = "v1" });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Weather API v1");
        c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
    });
}

app.MapGet("/weather/{city}",
    async (string city, WeatherService weatherService) =>
    {
        var weather = await weatherService.GetCurrentWeatherAsync(city);
        return weather is null ? Results.NotFound() : Results.Ok(weather);
    });

app.Run();
