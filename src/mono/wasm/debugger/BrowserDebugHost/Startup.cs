// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.WebAssembly.Diagnostics
{
    internal class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services) =>
            services.AddRouting()
            .Configure<ProxyOptions>(Configuration);

        public Startup(IConfiguration configuration) =>
            Configuration = configuration;

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IOptionsMonitor<ProxyOptions> optionsAccessor, IWebHostEnvironment env, IHostApplicationLifetime applicationLifetime)
        {
            ProxyOptions options = optionsAccessor.CurrentValue;

            if (options.OwnerPid.HasValue)
            {
                Process ownerProcess = Process.GetProcessById(options.OwnerPid.Value);
                if (ownerProcess != null)
                {
                    ownerProcess.EnableRaisingEvents = true;
                    ownerProcess.Exited += (sender, eventArgs) =>
                    {
                        applicationLifetime.StopApplication();
                    };
                }
            }

            app.UseDeveloperExceptionPage()
                .UseWebSockets()
                .UseDebugProxy(options);
        }
    }

    internal static class DebugExtensions
    {

        public static IApplicationBuilder UseDebugProxy(
            this IApplicationBuilder app,
            ProxyOptions options)
        {
            Uri devToolsHost = options.DevToolsUrl;
            app.UseRouter(router =>
            {
                router.MapGet("/", ReturnPageLinksToDebug);
                router.MapGet("/favicon.ico", Copy);
                router.MapGet("json", Copy);
                router.MapGet("json/list", Copy);
                router.MapGet("json/version", Copy);
                router.MapGet("json/new", Copy);
                router.MapGet("devtools/page/{pageId}", ConnectProxy);
                router.MapGet("devtools/browser/{pageId}", ConnectProxy);

                string GetEndpoint(HttpContext context)
                {
                    HttpRequest request = context.Request;
                    PathString requestPath = request.Path;
                    return $"{devToolsHost.Scheme}://{devToolsHost.Authority}{request.Path}{request.QueryString}";
                }

                async Task ReturnPageLinksToDebug(HttpContext context)
                {
                    HttpRequest request = context.Request;
                    Dictionary<string, string>[] tabs = await ProxyGetJsonAsync<Dictionary<string, string>[]>(GetEndpoint(context) + "json/list");
                    context.Response.ContentType = "text/html";
                    string urlsToInspect = "<title>Inspectable pages</title><h3>Inspectable pages</h3><hr>";
                    if (tabs.Length == 0)
                        urlsToInspect += "No inspectable pages available";
                    foreach (var tab in tabs)
                    {
                        string aHref = tab["devtoolsFrontendUrl"];
                        aHref = aHref.Replace($"ws={devToolsHost.Authority}", $"ws={request.Host}");
                        urlsToInspect += $"<a href=\"http://{devToolsHost.Authority}{aHref}\">{tab["title"]}<br><span>{tab["url"]}</a><br><br>";
                    }
                    await context.Response.Body.WriteAsync(Encoding.ASCII.GetBytes(urlsToInspect));
                }

                async Task Copy(HttpContext context)
                {
                    using (var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) })
                    {
                        HttpResponseMessage response = await httpClient.GetAsync(GetEndpoint(context));
                        context.Response.ContentType = response.Content.Headers.ContentType.ToString();
                        if ((response.Content.Headers.ContentLength ?? 0) > 0)
                            context.Response.ContentLength = response.Content.Headers.ContentLength;
                        byte[] bytes = await response.Content.ReadAsByteArrayAsync();
                        await context.Response.Body.WriteAsync(bytes);

                    }
                }

                async Task ConnectProxy(HttpContext context)
                {
                    if (!context.WebSockets.IsWebSocketRequest)
                    {
                        context.Response.StatusCode = 400;
                        return;
                    }

                    var endpoint = new Uri($"ws://{devToolsHost.Authority}{context.Request.Path}");
                    int runtimeId = 0;
                    if (context.Request.Query.TryGetValue("RuntimeId", out StringValues runtimeIdValue) &&
                                            int.TryParse(runtimeIdValue.FirstOrDefault(), out int parsedId))
                    {
                        runtimeId = parsedId;
                    }
                    try
                    {
                        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
                            builder.AddSimpleConsole(options =>
                                    {
                                        options.SingleLine = true;
                                        options.TimestampFormat = "[HH:mm:ss] ";
                                    })
                                   .AddFilter(null, LogLevel.Information)
                        );

                        context.Request.Query.TryGetValue("urlSymbolServer", out StringValues urlSymbolServerList);
                        var proxy = new DebuggerProxy(loggerFactory, urlSymbolServerList.ToList(), runtimeId);

                        System.Net.WebSockets.WebSocket ideSocket = await context.WebSockets.AcceptWebSocketAsync();

                        await proxy.Run(endpoint, ideSocket);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("got exception {0}", e);
                    }
                }
            });
            return app;
        }

        private static async Task<T> ProxyGetJsonAsync<T>(string url)
        {
            using (var httpClient = new HttpClient())
            {
                HttpResponseMessage response = await httpClient.GetAsync(url);
                return await JsonSerializer.DeserializeAsync<T>(await response.Content.ReadAsStreamAsync());
            }
        }
    }
}
