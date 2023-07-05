// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

#nullable enable

namespace Microsoft.WebAssembly.AppHost;

public class WebServer
{
    internal static async Task<(ServerURLs, IWebHost)> StartAsync(WebServerOptions options, ILogger logger, CancellationToken token)
    {
        TaskCompletionSource<ServerURLs> realUrlsAvailableTcs = new();

        IWebHostBuilder builder = new WebHostBuilder()
            .UseKestrel()
            .UseStartup<WebServerStartup>()
            .ConfigureLogging(logging =>
            {
                logging.AddConsole().AddFilter(null, LogLevel.Warning);
            })
            .ConfigureServices((ctx, services) =>
            {
                if (options.WebServerUseCors)
                {
                    services.AddCors(o => o.AddPolicy("AnyCors", builder =>
                        {
                            builder.AllowAnyOrigin()
                                .AllowAnyMethod()
                                .AllowAnyHeader()
                                .WithExposedHeaders("*");
                        }));
                }
                services.AddSingleton(logger);
                services.AddSingleton(Options.Create(options));
                services.AddSingleton(realUrlsAvailableTcs);
                services.AddRouting();
            })
            .UseUrls(options.Urls);

        if (options.ContentRootPath != null)
            builder.UseContentRoot(options.ContentRootPath);

        IWebHost? host = builder.Build();
        await host.StartAsync(token);

        if (token.CanBeCanceled)
            token.Register(async () => await host.StopAsync());

        ServerURLs serverUrls = await realUrlsAvailableTcs.Task;
        return (serverUrls, host);
    }

}

// FIXME: can be simplified to string[]
public record ServerURLs(string Http, string? Https);
