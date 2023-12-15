// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Web;
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
    private static readonly object LaunchLock = new object();
    private static string LaunchedDebugProxyUrl = "";
    private ILogger? _logger;
    public WebServerStartup(IWebHostEnvironment hostingEnvironment) => _hostingEnvironment = hostingEnvironment;

    public static int StartDebugProxy(string devToolsHost)
    {
        //we need to start another process, otherwise it will be running the BrowserDebugProxy in the same process that will be debugged, so pausing in a breakpoint
        //on managed code will freeze because it will not be able to continue executing the BrowserDebugProxy to get the locals value
        var executablePath = Path.Combine(System.AppContext.BaseDirectory, "BrowserDebugHost.dll");
        var ownerPid = Environment.ProcessId;
        // generate a random port in a given range, skipping the ports blocked by browsers: https://chromestatus.com/feature/5064283639513088
        var generateRandomPort = GetNextRandomExcept(5000..5300,
            5060, // SIP
            5061 // SIPS
        );
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "dotnet" + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : ""),
            Arguments = $"exec \"{executablePath}\" --OwnerPid {ownerPid} --DevToolsUrl {devToolsHost} --DevToolsProxyPort {generateRandomPort}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
        };
        var debugProxyProcess = Process.Start(processStartInfo);
        if (debugProxyProcess is null)
        {
            throw new InvalidOperationException("Unable to start debug proxy process.");
        }
        return generateRandomPort;

        static int GetNextRandomExcept(Range range, params int[] except)
        {
            int current;
            do
            {
                current = Random.Shared.Next(range.Start.Value, range.End.Value);
            } while (Array.IndexOf(except, current) > -1);

            return current;
        }
    }

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

        app.Map("/debug", app =>
        {
            app.Run(async (context) =>
            {
                //debug from VS
                var queryParams = HttpUtility.ParseQueryString(context.Request.QueryString.Value!);
                var browserParam = queryParams.Get("browser");
                Uri? browserUrl = null;
                var devToolsHost = "http://localhost:9222";
                if (browserParam != null)
                {
                    browserUrl = new Uri(browserParam);
                    devToolsHost = $"http://{browserUrl.Host}:{browserUrl.Port}";
                }
                lock (LaunchLock)
                {
                    if (LaunchedDebugProxyUrl == "")
                    {
                        LaunchedDebugProxyUrl = $"http://localhost:{StartDebugProxy(devToolsHost)}";
                    }
                }
                var requestPath = context.Request.Path.ToString();
                if (requestPath == string.Empty)
                {
                    requestPath = "/";
                }
                context.Response.Redirect($"{LaunchedDebugProxyUrl}{browserUrl!.PathAndQuery}");
                await Task.FromResult(0);
            });
        });
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
