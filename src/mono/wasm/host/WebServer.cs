// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
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
public record ServerURLs(string Http, string? Https, string? DebugPath = null);

public static class ServerURLsProvider
{
    public static void ResolveServerUrlsOnApplicationStarted(IApplicationBuilder app, ILogger logger, IHostApplicationLifetime applicationLifetime, TaskCompletionSource<ServerURLs> realUrlsAvailableTcs, string? debugPath = null)
    {
        applicationLifetime.ApplicationStarted.Register(() =>
        {
            TaskCompletionSource<ServerURLs> tcs = realUrlsAvailableTcs;
            try
            {
                ICollection<string>? addresses = app.ServerFeatures.Get<IServerAddressesFeature>()?.Addresses;

                string? ipAddress = null;
                string? ipAddressSecure = null;
                if (addresses is not null)
                {
                    ipAddress = GetHttpServerAddress(addresses, secure: false);
                    ipAddressSecure = GetHttpServerAddress(addresses, secure: true);
                }

                if (ipAddress == null)
                    tcs.SetException(new InvalidOperationException("Failed to determine web server's IP address or port"));
                else
                    tcs.SetResult(new ServerURLs(ipAddress, ipAddressSecure, debugPath));
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to get urls for the webserver: {ex}");
                tcs.TrySetException(ex);
                throw;
            }

            static string? GetHttpServerAddress(ICollection<string> addresses, bool secure) => addresses?
                .Where(a => a.StartsWith(secure ? "https:" : "http:", StringComparison.InvariantCultureIgnoreCase))
                .Select(a => new Uri(a))
                .Select(uri => uri.ToString())
                .FirstOrDefault();
        });
    }
}
