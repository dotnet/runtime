// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.WebSockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

#nullable enable

namespace Microsoft.WebAssembly.AppHost;

internal sealed class WebServerStartup
{
    private readonly IWebHostEnvironment _hostingEnvironment;

    public WebServerStartup(IWebHostEnvironment hostingEnvironment) => _hostingEnvironment = hostingEnvironment;

    public void Configure(IApplicationBuilder app, IOptions<WebServerOptions> optionsContainer)
    {
        var provider = new FileExtensionContentTypeProvider();
        provider.Mappings[".wasm"] = "application/wasm";
        provider.Mappings[".cjs"] = "text/javascript";
        provider.Mappings[".mjs"] = "text/javascript";

        foreach (string extn in new string[] { ".dll", ".pdb", ".dat", ".blat" })
        {
            provider.Mappings[extn] = "application/octet-stream";
        }

        WebServerOptions options = optionsContainer.Value;
        if (options.WebServerUseCrossOriginPolicy)
        {
            app.Use((context, next) =>
            {
                context.Response.Headers.Add("Cross-Origin-Embedder-Policy", "require-corp");
                context.Response.Headers.Add("Cross-Origin-Opener-Policy", "same-origin");
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

        // app.UseEndpoints(endpoints =>
        // {
        //     endpoints.MapFallbackToFile(options.DefaultFileName);
        // });
    }
}
