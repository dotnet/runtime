// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Microsoft.WebAssembly.AppHost;

namespace Microsoft.WebAssembly.AppHost.DevServer;

internal sealed class DevServerStartup
{
    public DevServerStartup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddRouting();
    }

    public static void Configure(IApplicationBuilder app, IOptions<DevServerOptions> optionsContainer, TaskCompletionSource<ServerURLs> realUrlsAvailableTcs, ILogger logger, IHostApplicationLifetime applicationLifetime, IConfiguration configuration)
    {
        app.UseDeveloperExceptionPage();
        EnableConfiguredPathbase(app, configuration);

        app.UseWebAssemblyDebugging();

        DevServerOptions options = optionsContainer.Value;

        if (options.WebServerUseCrossOriginPolicy)
        {
            app.Use(async (ctx, next) =>
            {
                if (ctx.Request.Path.StartsWithSegments("/_framework") && !ctx.Request.Path.StartsWithSegments("/_framework/blazor.server.js") && !ctx.Request.Path.StartsWithSegments("/_framework/blazor.web.js"))
                {
                    string fileExtension = Path.GetExtension(ctx.Request.Path);
                    if (string.Equals(fileExtension, ".js") || string.Equals(fileExtension, ".mjs"))
                    {
                        // Browser multi-threaded runtime requires cross-origin policy headers to enable SharedArrayBuffer.
                        ApplyCrossOriginPolicyHeaders(ctx);
                    }
                }

                await next(ctx);
            });
        }

        //app.UseBlazorFrameworkFiles();
        app.UseRouting();

        app.UseStaticFiles(new StaticFileOptions
        {
            // In development, serve everything, as there's no other way to configure it.
            // In production, developers are responsible for configuring their own production server
            ServeUnknownFileTypes = true,
        });

        app.UseWebSockets();

        if (options.OnConsoleConnected is not null)
        {
            app.Use(async (ctx, next) =>
            {
                if (ctx.Request.Path.StartsWithSegments("/console"))
                {
                    if (!ctx.WebSockets.IsWebSocketRequest)
                    {
                        ctx.Response.StatusCode = 400;
                        return;
                    }

                    using WebSocket socket = await ctx.WebSockets.AcceptWebSocketAsync();
                    await options.OnConsoleConnected(socket);
                }
                else
                {
                    await next(ctx);
                }
            });
        }

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapStaticAssets(options.StaticWebAssetsEndpointsPath);

            endpoints.MapFallbackToFile("index.html", new StaticFileOptions
            {
                OnPrepareResponse = fileContext =>
                {
                    // Avoid caching index.html during development.
                    // When hot reload is enabled, a middleware injects a hot reload script into the response HTML.
                    // We don't want the browser to bypass this injection by using a cached response that doesn't
                    // contain the injected script. In the future, if script injection is removed in favor of a
                    // different mechanism, we can delete this comment and the line below it.
                    // See also: https://github.com/dotnet/aspnetcore/issues/45213
                    fileContext.Context.Response.Headers[HeaderNames.CacheControl] = "no-store";

                    if (options.WebServerUseCrossOriginPolicy)
                    {
                        // Browser multi-threaded runtime requires cross-origin policy headers to enable SharedArrayBuffer.
                        ApplyCrossOriginPolicyHeaders(fileContext.Context);
                    }
                }
            });

            // Add general-purpose file upload endpoint when DEVSERVER_UPLOAD_PATH is set
            string? fileUploadPath = Environment.GetEnvironmentVariable("DEVSERVER_UPLOAD_PATH");
            if (!string.IsNullOrEmpty(fileUploadPath))
            {
                // Ensure the upload directory exists
                if (!Directory.Exists(fileUploadPath))
                {
                    Directory.CreateDirectory(fileUploadPath!);
                }

                // Route with filename parameter
                endpoints.MapPost("/upload/{filename}", async context =>
                {
                    try
                    {
                        // Get the filename from the route
                        var routeValues = context.Request.RouteValues;
                        string? rawFileName = routeValues["filename"]?.ToString();

                        // Generate a unique name if none provided
                        if (string.IsNullOrEmpty(rawFileName))
                        {
                            rawFileName = $"upload_{Guid.NewGuid():N}";
                        }

                        // Sanitize filename - IMPORTANT: Only use GetFileName to strip any path components
                        // This prevents directory traversal attacks like "../../../etc/passwd"
                        string fileName = Path.GetFileName(rawFileName);

                        if (string.IsNullOrEmpty(fileName))
                        {
                            fileName = $"upload_{Guid.NewGuid():N}";
                        }

                        string filePath = Path.Combine(fileUploadPath!, fileName);

                        using (var outputStream = new FileStream(filePath, FileMode.Create))
                        {
                            await context.Request.Body.CopyToAsync(outputStream);
                        }

                        await context.Response.WriteAsync($"File saved to {filePath}");
                    }
                    catch (Exception ex)
                    {
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync($"Error processing upload: {ex.Message}");
                    }
                });
            }

        });

        ServerURLsProvider.ResolveServerUrlsOnApplicationStarted(app, logger, applicationLifetime, realUrlsAvailableTcs, "/_framework/debug");
    }

    private static void EnableConfiguredPathbase(IApplicationBuilder app, IConfiguration configuration)
    {
        var pathBase = configuration.GetValue<string>("pathbase");
        if (!string.IsNullOrEmpty(pathBase))
        {
            app.UsePathBase(pathBase);

            // To ensure consistency with a production environment, only handle requests
            // that match the specified pathbase.
            app.Use((context, next) =>
            {
                if (context.Request.PathBase == pathBase)
                {
                    return next(context);
                }
                else
                {
                    context.Response.StatusCode = 404;
                    return context.Response.WriteAsync($"The server is configured only to " +
                        $"handle request URIs within the PathBase '{pathBase}'.");
                }
            });
        }
    }

    private static void ApplyCrossOriginPolicyHeaders(HttpContext httpContext)
    {
        httpContext.Response.Headers["Cross-Origin-Embedder-Policy"] = "require-corp";
        httpContext.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
    }
}
