// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
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
    internal sealed class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services) =>
            services.AddRouting()
            .Configure<ProxyOptions>(Configuration);

        public Startup(IConfiguration configuration) =>
            Configuration = configuration;

        public IConfiguration Configuration { get; }

#pragma warning disable CA1822
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
#pragma warning restore CA1822
    }

    internal static class DebugExtensions
    {
        public static Dictionary<string, string> MapValues(Dictionary<string, string> response, HttpContext context, Uri debuggerHost)
        {
            var filtered = new Dictionary<string, string>();
            HttpRequest request = context.Request;

            foreach (string key in response.Keys)
            {
                switch (key)
                {
                    case "devtoolsFrontendUrl":
                        string front = response[key];
                        filtered[key] = $"{debuggerHost.Scheme}://{debuggerHost.Authority}{front.Replace($"ws={debuggerHost.Authority}", $"ws={request.Host}")}";
                        break;
                    case "webSocketDebuggerUrl":
                        var page = new Uri(response[key]);
                        filtered[key] = $"{page.Scheme}://{request.Host}{page.PathAndQuery}";
                        break;
                    default:
                        filtered[key] = response[key];
                        break;
                }
            }
            return filtered;
        }

        public static IApplicationBuilder UseDebugProxy(this IApplicationBuilder app, ProxyOptions options) =>
            UseDebugProxy(app, options, MapValues);

        public static IApplicationBuilder UseDebugProxy(
            this IApplicationBuilder app,
            ProxyOptions options,
            Func<Dictionary<string, string>, HttpContext, Uri, Dictionary<string, string>> mapFunc)
        {
            Uri devToolsHost = options.DevToolsUrl;
            app.UseRouter(router =>
            {
                router.MapGet("/", Copy);
                router.MapGet("/favicon.ico", Copy);
                router.MapGet("json", RewriteArray);
                router.MapGet("json/list", RewriteArray);
                router.MapGet("json/version", RewriteSingle);
                router.MapGet("json/new", RewriteSingle);
                router.MapGet("devtools/page/{pageId}", ConnectProxy);
                router.MapGet("devtools/browser/{pageId}", ConnectProxy);

                string GetEndpoint(HttpContext context)
                {
                    HttpRequest request = context.Request;
                    PathString requestPath = request.Path;
                    return $"{devToolsHost.Scheme}://{devToolsHost.Authority}{request.Path}{request.QueryString}";
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

                async Task RewriteSingle(HttpContext context)
                {
                    Dictionary<string, string> version = await ProxyGetJsonAsync<Dictionary<string, string>>(GetEndpoint(context));
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(
                        JsonSerializer.Serialize(mapFunc(version, context, devToolsHost)));
                }

                async Task RewriteArray(HttpContext context)
                {
                    Dictionary<string, string>[] tabs = await ProxyGetJsonAsync<Dictionary<string, string>[]>(GetEndpoint(context));
                    Dictionary<string, string>[] alteredTabs = tabs.Select(t => mapFunc(t, context, devToolsHost)).ToArray();
                    context.Response.ContentType = "application/json";
                    string text = JsonSerializer.Serialize(alteredTabs);
                    context.Response.ContentLength = text.Length;
                    await context.Response.WriteAsync(text);
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
