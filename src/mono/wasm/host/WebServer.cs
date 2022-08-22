// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

#nullable enable

namespace Microsoft.WebAssembly.AppHost;

public class WebServer
{
    internal static async Task<(ServerURLs, IWebHost)> StartAsync(WebServerOptions options, ILogger logger, CancellationToken token)
    {
        string[]? urls = new string[] { $"http://127.0.0.1:{options.Port}", "https://127.0.0.1:0" };

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
                services.AddRouting();
            })
            .UseUrls(urls);

        if (options.ContentRootPath != null)
            builder.UseContentRoot(options.ContentRootPath);

        IWebHost? host = builder.Build();
        await host.StartAsync(token);

        ICollection<string>? addresses = host.ServerFeatures
                            .Get<IServerAddressesFeature>()?
                            .Addresses;

        string? ipAddress =
                        addresses?
                        .Where(a => a.StartsWith("http:", StringComparison.InvariantCultureIgnoreCase))
                        .Select(a => new Uri(a))
                        .Select(uri => uri.ToString())
                        .FirstOrDefault();

        string? ipAddressSecure =
                        addresses?
                        .Where(a => a.StartsWith("https:", StringComparison.OrdinalIgnoreCase))
                        .Select(a => new Uri(a))
                        .Select(uri => uri.ToString())
                        .FirstOrDefault();

        return ipAddress == null || ipAddressSecure == null
            ? throw new InvalidOperationException("Failed to determine web server's IP address or port")
            : (new ServerURLs(ipAddress, ipAddressSecure), host);
    }

}

// FIXME: can be simplified to string[]
public record ServerURLs(string Http, string? Https);
