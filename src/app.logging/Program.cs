using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSingleton<IWeatherService, FakeWeatherService>();

var app = builder.Build();

app.MapGet("/weather/current", async (string city, IWeatherService weatherService) =>
{
    var result = await weatherService.GetCurrentWeatherAsync(city);
    return Results.Ok(result);
});

app.MapGet("/weather/forecast", async (string city, IWeatherService weatherService) =>
{
    var result = await weatherService.GetForecastAsync(city);
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
    private static readonly string[] Conditions =
        { "Sunny", "Cloudy", "Rainy", "Windy", "Snowy" };

    private readonly Random _random = new();

    public Task<WeatherInfo> GetCurrentWeatherAsync(string city)
    {
        if (_random.Next(0, 10) < 2) throw new Exception("External weather provider failed.");

        var weather = new WeatherInfo(
            city,
            Conditions[_random.Next(Conditions.Length)],
            _random.Next(-10, 35)
        );

        return Task.FromResult(weather);
    }

    public Task<IEnumerable<WeatherInfo>> GetForecastAsync(string city)
    {
        var forecast = Enumerable.Range(1, 5).Select(_ => new WeatherInfo(
            city,
            Conditions[_random.Next(Conditions.Length)],
            _random.Next(-10, 35)
        ));

        return Task.FromResult(forecast);
    }
}