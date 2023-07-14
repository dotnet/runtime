// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.WebAssembly.AppHost;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

internal static class DevServer
{
    internal static async Task<(ServerURLs, IWebHost)> StartAsync(WebServerOptions options, ILogger logger, CancellationToken token)
    {
        TaskCompletionSource<ServerURLs> realUrlsAvailableTcs = new();

        IWebHostBuilder builder = new WebHostBuilder()
            .UseConfiguration(ConfigureHostConfiguration(options))
            .UseKestrel()
            .UseStaticWebAssets()
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
            });


        IWebHost? host = builder.Build();
        await host.StartAsync(token);

        if (token.CanBeCanceled)
            token.Register(async () => await host.StopAsync());

        ServerURLs serverUrls = await realUrlsAvailableTcs.Task;
        return (serverUrls, host);
    }

    private static IConfiguration ConfigureHostConfiguration(WebServerOptions options)
    {
        var config = new ConfigurationBuilder();

        var applicationPath = options.ApplicationPath;
        var applicationDirectory = Path.GetDirectoryName(applicationPath)!;
        var name = Path.ChangeExtension(applicationPath, ".staticwebassets.runtime.json");
        name = !File.Exists(name) ? Path.ChangeExtension(applicationPath, ".StaticWebAssets.xml") : name;

        var inMemoryConfiguration = new Dictionary<string, string?>
        {
            [WebHostDefaults.EnvironmentKey] = "Development",
            ["Logging:LogLevel:Microsoft"] = "Warning",
            ["Logging:LogLevel:Microsoft.Hosting.Lifetime"] = "Information",
            [WebHostDefaults.StaticWebAssetsKey] = name,
            ["ApplyCopHeaders"] = options.WebServerUseCrossOriginPolicy.ToString()
        };

        config.AddInMemoryCollection(inMemoryConfiguration);
        config.AddJsonFile(Path.Combine(applicationDirectory, "dotnet-devserversettings.json"), optional: true, reloadOnChange: true);

        return config.Build();
    }
}
