using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using app.logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// TODO naming?
builder.ConfigureLogging();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSingleton<IWeatherService, FakeWeatherService>();

var app = builder.Build();

// TODO correct placement for all below?
// TODO naming?
app.UseCorrelationId();
app.UseLoggingContextEnrichment();
app.UseRequestLogging();
app.UseExceptionHandler();

app.MapGet("/weather/current", async (string city, IWeatherService weatherService, [FromServices] ILogger<Program> logger, [FromServices] IHttpContextAccessor contextAccessor) =>
{
    if (Random.Shared.Next(10) < 3)
        throw new Exception("Unhandled Exception");

    try
    {
        var result = await weatherService.GetCurrentWeatherAsync(city);
        logger.LogInformation("Current Weather successfully fetched for {City}", city);
        return Results.Ok(result);
    }
    catch (Exception e)
    {
        logger.LogError(e, "Failed to retrieve weather data");
        return Results.InternalServerError("Failed to retrieve weather data");
    }
});

app.MapGet("/weather/forecast", async (string city, IWeatherService weatherService, [FromServices] ILogger<Program> logger) =>
{
    var result = await weatherService.GetForecastAsync(city);
    if (result.Count() < 5)
        logger.LogWarning("Only partial forecast data could be retrieved");
    logger.LogInformation("Weather forecast for {ForecastDays} days has been successfully fetched for {City}",
        result.Count(),
        city);
    return Results.Ok(result);
});

app.Run();

public record WeatherInfo(string City, string Condition, double TemperatureCelsius);

public interface IWeatherService
{
    Task<WeatherInfo> GetCurrentWeatherAsync(string city);
    Task<IEnumerable<WeatherInfo>> GetForecastAsync(string city);
}

public class FakeWeatherService : IWeatherService
{
    private static readonly string[] Conditions = ["Sunny", "Cloudy", "Rainy", "Windy", "Snowy"];

    private readonly Random _random = new();

    public Task<WeatherInfo> GetCurrentWeatherAsync(string city)
    {
        if (_random.Next(0, 10) < 2)
            throw new Exception("External weather provider failed.");

        var weather = new WeatherInfo(
            city,
            Conditions[_random.Next(Conditions.Length)],
            _random.Next(-10, 35)
        );

        return Task.FromResult(weather);
    }

    public Task<IEnumerable<WeatherInfo>> GetForecastAsync(string city)
    {
        var range = _random.Next(0, 3) == 0 ? 2 : 5;
        var forecast = Enumerable.Range(1, range).Select(_ => new WeatherInfo(
            city,
            Conditions[_random.Next(Conditions.Length)],
            _random.Next(-10, 35)
        ));

        return Task.FromResult(forecast);
    }
}