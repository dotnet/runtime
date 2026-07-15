// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

#nullable enable

namespace Microsoft.WebAssembly.AppHost.DevServer;

/// <summary>
/// In-process WasmAppHost dev server. It has two modes, selected by <see cref="DevServerRunOptions.ForwardToUrl"/>:
/// <list type="bullet">
/// <item><b>File serving</b> (<c>ForwardToUrl</c> is <c>null</c>): serves the app's static files from
/// <see cref="DevServerRunOptions.ContentRootPath"/>. Used for test-runner apps that are not built with the
/// WebAssembly SDK (no static web assets manifest), which the Blazor Gateway cannot serve.</item>
/// <item><b>Gateway fronting</b> (<c>ForwardToUrl</c> is set): reverse-proxies every request to the Blazor
/// Gateway (which serves the static web assets). Used so a same-origin dev/test endpoint can sit in front of the
/// Gateway without needing the Gateway itself to reverse-proxy (its preview build cannot).</item>
/// </list>
/// In both modes it hosts the test-only <c>POST /upload/{filename}</c> artifact sink when the
/// <c>DEVSERVER_UPLOAD_PATH</c>/<c>DEVSERVER_UPLOAD_PATTERN</c> environment variables are set. See
/// https://github.com/dotnet/aspnetcore/issues/67814.
/// </summary>
internal static class AppHostDevServer
{
    internal static bool IsUploadEndpointRequested =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DEVSERVER_UPLOAD_PATH")) &&
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DEVSERVER_UPLOAD_PATTERN"));

    internal static async Task<(ServerURLs, IHost)> StartAsync(DevServerRunOptions options, ILogger logger, CancellationToken token)
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls(options.Urls);
        // CreateSlimBuilder wires only Kestrel core; enable HTTPS so the https:// dev url can bind.
        builder.WebHost.UseKestrelHttpsConfiguration();

        if (options.ForwardToUrl is not null)
            builder.Services.AddHttpClient();

        // The Gateway host (when fronting) is already started; stop it when this front server shuts down.
        if (options.OwnedHost is not null)
            builder.Services.AddSingleton<IHostedService>(_ => new FrontedHostStopper(options.OwnedHost, logger));

        WebApplication app = builder.Build();

        if (options.UseCrossOriginPolicy)
        {
            // Browser multi-threaded runtime requires cross-origin isolation to enable SharedArrayBuffer. For apps
            // served by the Gateway these headers are baked into the endpoints manifest at build time; the file
            // serving mode adds them here for the (non-static-web-assets) apps that path serves.
            app.Use((context, next) =>
            {
                context.Response.Headers.Append("Cross-Origin-Embedder-Policy", "require-corp");
                context.Response.Headers.Append("Cross-Origin-Opener-Policy", "same-origin");
                return next();
            });
        }

        app.UseWebSockets();

        MapUploadEndpoint(app, logger);

        if (options.ForwardToUrl is not null)
            MapGatewayForwarder(app, options.ForwardToUrl);
        else
            MapStaticFileServing(app, options.ContentRootPath!);

        await app.StartAsync(token);

        string[] addresses = app.Urls.ToArray();
        string? http = addresses.FirstOrDefault(a => a.StartsWith("http:", StringComparison.OrdinalIgnoreCase));
        string? https = addresses.FirstOrDefault(a => a.StartsWith("https:", StringComparison.OrdinalIgnoreCase));

        if (http is null)
        {
            await app.DisposeAsync();
            throw new InvalidOperationException("The WasmAppHost dev server did not report an HTTP listening URL.");
        }

        return (new ServerURLs(new Uri(http).ToString(), https is null ? null : new Uri(https).ToString()), app);
    }

    private static void MapUploadEndpoint(WebApplication app, ILogger logger)
    {
        string? uploadPath = Environment.GetEnvironmentVariable("DEVSERVER_UPLOAD_PATH");
        string? uploadPattern = Environment.GetEnvironmentVariable("DEVSERVER_UPLOAD_PATTERN");
        if (string.IsNullOrEmpty(uploadPath) || string.IsNullOrEmpty(uploadPattern))
            return;

        Regex uploadFilter;
        try
        {
            uploadFilter = new Regex(uploadPattern, RegexOptions.Compiled | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"DEVSERVER_UPLOAD_PATTERN value '{uploadPattern}' is not a valid regular expression: {ex.Message}", ex);
        }

        // Artifact sink used by tests: validate the filename against DEVSERVER_UPLOAD_PATTERN, strip any path
        // components with Path.GetFileName (directory-traversal guard - IMPORTANT), and stream the body to disk.
        app.MapPost("/upload/{filename}", async (string filename, HttpContext context) =>
        {
            if (string.IsNullOrEmpty(filename) || !uploadFilter.IsMatch(filename))
                return Results.StatusCode(StatusCodes.Status403Forbidden);

            try
            {
                Directory.CreateDirectory(uploadPath);
                string safeName = Path.GetFileName(filename);
                if (string.IsNullOrEmpty(safeName))
                    return Results.StatusCode(StatusCodes.Status403Forbidden);

                string destination = Path.Combine(uploadPath, safeName);
                await using FileStream file = File.Create(destination);
                await context.Request.Body.CopyToAsync(file, context.RequestAborted);
                return Results.Text($"File saved to {destination}");
            }
            catch (Exception ex)
            {
                logger.LogError("Upload of '{FileName}' failed: {Message}", filename, ex.Message);
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        });
    }

    private static void MapStaticFileServing(WebApplication app, string contentRootPath)
    {
        var provider = new FileExtensionContentTypeProvider();
        provider.Mappings[".wasm"] = "application/wasm";
        provider.Mappings[".cjs"] = "text/javascript";
        provider.Mappings[".mjs"] = "text/javascript";
        foreach (string extn in new[] { ".dll", ".pdb", ".dat", ".webcil" })
            provider.Mappings[extn] = "application/octet-stream";

        var fileProvider = new PhysicalFileProvider(Path.GetFullPath(contentRootPath));
        app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = fileProvider });
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = fileProvider,
            ContentTypeProvider = provider,
            ServeUnknownFileTypes = true,
        });
    }

    private static void MapGatewayForwarder(WebApplication app, string forwardToUrl)
    {
        var target = new Uri(forwardToUrl);

        // Everything that is not one of the dev/test endpoints above is proxied to the Gateway. Only plain HTTP is
        // forwarded (the browser console WebSocket is no longer used), so a streaming HttpClient proxy is enough.
        app.MapFallback(async (HttpContext context, IHttpClientFactory httpClientFactory) =>
        {
            HttpClient client = httpClientFactory.CreateClient();
            var destination = new Uri(target, context.Request.Path + context.Request.QueryString);

            using var proxyRequest = new HttpRequestMessage(new HttpMethod(context.Request.Method), destination);
            if (context.Request.ContentLength > 0 || context.Request.Headers.ContainsKey("Transfer-Encoding"))
                proxyRequest.Content = new StreamContent(context.Request.Body);

            foreach (var header in context.Request.Headers)
            {
                if (string.Equals(header.Key, "Host", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!proxyRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()) && proxyRequest.Content is not null)
                    proxyRequest.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }

            using HttpResponseMessage proxyResponse = await client.SendAsync(proxyRequest, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);

            context.Response.StatusCode = (int)proxyResponse.StatusCode;
            foreach (var header in proxyResponse.Headers)
                context.Response.Headers[header.Key] = header.Value.ToArray();
            foreach (var header in proxyResponse.Content.Headers)
                context.Response.Headers[header.Key] = header.Value.ToArray();

            // Let Kestrel set the transfer framing.
            context.Response.Headers.Remove("transfer-encoding");

            await proxyResponse.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
        });
    }
}

internal sealed record DevServerRunOptions(
    string[] Urls,
    string? ForwardToUrl,
    string? ContentRootPath,
    bool UseCrossOriginPolicy,
    IHost? OwnedHost = null
);

/// <summary>
/// Stops (and disposes) the already-started Gateway host when the fronting dev server shuts down. Runs as a
/// hosted service so the stop is awaited asynchronously (avoiding sync-over-async during host shutdown).
/// </summary>
internal sealed class FrontedHostStopper : IHostedService
{
    private readonly IHost _owned;
    private readonly ILogger _logger;

    public FrontedHostStopper(IHost owned, ILogger logger)
    {
        _owned = owned;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _owned.StopAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to stop the fronted Gateway host: {Message}", ex.Message);
        }
        finally
        {
            _owned.Dispose();
        }
    }
}
