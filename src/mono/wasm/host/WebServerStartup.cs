// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

#nullable enable

namespace Microsoft.WebAssembly.AppHost;

internal sealed class WebServerStartup
{
    private readonly IWebHostEnvironment _hostingEnvironment;
    private ILogger? _logger;
    public WebServerStartup(IWebHostEnvironment hostingEnvironment) => _hostingEnvironment = hostingEnvironment;

    public void Configure(IApplicationBuilder app,
                          IOptions<WebServerOptions> optionsContainer,
                          TaskCompletionSource<ServerURLs> realUrlsAvailableTcs,
                          ILogger logger,
                          IHostApplicationLifetime applicationLifetime)
    {
        _logger = logger;
        var provider = new FileExtensionContentTypeProvider();
        provider.Mappings[".wasm"] = "application/wasm";
        provider.Mappings[".cjs"] = "text/javascript";
        provider.Mappings[".mjs"] = "text/javascript";

        foreach (string extn in new string[] { ".dll", ".pdb", ".dat", ".webcil" })
        {
            provider.Mappings[extn] = "application/octet-stream";
        }

        WebServerOptions options = optionsContainer.Value;
        if (options.WebServerUseCrossOriginPolicy)
        {
            app.Use((context, next) =>
            {
                context.Response.Headers.Append("Cross-Origin-Embedder-Policy", "require-corp");
                context.Response.Headers.Append("Cross-Origin-Opener-Policy", "same-origin");
                return next();
            });
        }

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(_hostingEnvironment.ContentRootPath),
            ContentTypeProvider = provider,
            ServeUnknownFileTypes = true
        });

        if (options.WebServerUseCors)
        {
            app.UseCors("AnyCors");
        }
        app.UseRouting();
        app.UseWebSockets();
        if (options.OnConsoleConnected is not null)
        {
            app.UseRouter(router =>
            {
                router.MapGet("/console", async context =>
                {
                    if (!context.WebSockets.IsWebSocketRequest)
                    {
                        context.Response.StatusCode = 400;
                        return;
                    }

                    using WebSocket socket = await context.WebSockets.AcceptWebSocketAsync();
                    await options.OnConsoleConnected(socket);
                });
            });
        }

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("/", context =>
            {
                context.Response.Redirect("index.html", permanent: false);
                return Task.CompletedTask;
            });
        });

        ServerURLsProvider.ResolveServerUrlsOnApplicationStarted(app, logger, applicationLifetime, realUrlsAvailableTcs);
    }
}
