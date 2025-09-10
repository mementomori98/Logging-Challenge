using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Serilog;
using Serilog.Context;
using Serilog.Formatting.Compact;
using Serilog.Templates;

namespace app.logging;

// TODO naming of this class?
public static class AppExtensions
{
    private const string CorrelationId = "CorrelationId";
    
    public static void UseCorrelationId(this WebApplication app)
    {
        app.Use((httpContext, next) =>
        {
            httpContext.Items[CorrelationId] = Guid.NewGuid().ToString();
            httpContext.Response.Headers.Add(new KeyValuePair<string, StringValues>(CorrelationId, httpContext.Items[CorrelationId]!.ToString()));
            return next(httpContext);
        });
    }

    // TODO naming of this method?
    // TODO signature? (WebApplication -> void)
    public static void UseLoggingContextEnrichment(this WebApplication app)
    {
        app.Use(async (httpContext, next) =>
        {
            using var scope = app.Logger.BeginScope("request-scope");
            using var props = new DisposableCollection();

            // TODO add Timestamp, LogLevel, ApiName?
            props.Add(LogContext.PushProperty("Environment", app.Environment.EnvironmentName));
            props.Add(LogContext.PushProperty("AssemblyVersion", typeof(Program).Assembly.GetName().Version));

            httpContext.Items.TryGetValue(CorrelationId, out var correlationId);
            props.Add(LogContext.PushProperty(CorrelationId, correlationId ?? "MISSING_CORRELATION_ID"));
            httpContext.Request.Query.TryGetValue("city", out var city);
            props.Add(LogContext.PushProperty("City", string.Join(",", city.AsEnumerable())));

            await next(httpContext);
        });
    }

    public static void UseRequestLogging(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            context.Items.TryGetValue(CorrelationId, out var correlationId);
            app.Logger.LogInformation("Executing request {CorrelationId} {HttpMethod} {RequestPath} {QueryParameters}",
                correlationId,
                context.Request.Method,
                context.Request.Path,
                string.Join(",", context.Request.Query.Select(item => $"{item.Key}={item.Value}")));

            // TODO use dateProvider
            var startTime = DateTimeOffset.UtcNow;
            await next(context);
            var executionTime = DateTimeOffset.UtcNow - startTime;

            app.Logger.LogInformation("Completed request {CorrelationId} with status code {StatusCode} in {ExecutionTime}ms",
                correlationId,
                context.Response.StatusCode,
                executionTime.TotalMilliseconds.ToString("#,##0.0"));

            var threshold = TimeSpan.FromMilliseconds(1);
            if (executionTime > threshold)
                app.Logger.LogWarning("Request {CorrelationId} took too long to complete ({ExecutionTime}ms)", correlationId, executionTime);
        });
    }

    public static void UseExceptionHandler(this WebApplication app)
    {
        app.Use(async (httpContext, next) =>
        {
            try
            {
                await next(httpContext);
            }
            catch (Exception e)
            {
                app.Logger.LogCritical(e, "Unhandled error: {Message}", e.Message);
                httpContext.Items.TryGetValue(CorrelationId, out var correlationId);
                httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                await httpContext.Response.WriteAsJsonAsync(new
                {
                    Message = "An internal server error occurred. Contact the developers of the application",
                    CorrelationId = correlationId ?? "MISSING_CORRELATION_ID"
                });
            }
        });
    }

    public static void UseConfiguredSerilog(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, config) => config
            .ReadFrom.Configuration(new ConfigurationBuilder()
                .AddUserSecrets<Program>(optional: true)
                .Build())
            .WriteTo.Console(
                formatter: new ExpressionTemplate("[{UtcDateTime(@t):o} {@l:u3} {SourceContext}] {@m:lj}\n{@x}"))
            .WriteTo.File(
                formatter: new ExpressionTemplate("[{UtcDateTime(@t):o} {@l:u3} {SourceContext}] {@m:lj}\n{@p}\n{@x}"),
                path: "logs/logs.txt",
                rollingInterval: RollingInterval.Day)
            .WriteTo.File(
                formatter: new CompactJsonFormatter(),
                path: "logs/logs.json",
                rollingInterval: RollingInterval.Day)
            .Enrich.FromLogContext());
    }

    private class DisposableCollection : IDisposable
    {
        private readonly List<IDisposable> _disposables = [];

        public void Add(IDisposable? disposable)
        {
            if (disposable == null)
                return;

            _disposables.Add(disposable);
        }

        public void Dispose()
        {
            foreach (var disposable in _disposables)
                disposable.Dispose();
        }
    }
}