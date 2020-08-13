// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
        public void Configure(IApplicationBuilder app, IOptionsMonitor<ProxyOptions> optionsAccessor, IWebHostEnvironment env)
        {
            var options = optionsAccessor.CurrentValue;
            app.UseDeveloperExceptionPage()
                .UseWebSockets()
                .UseDebugProxy(options);
        }
    }

    static class DebugExtensions
    {
        public static Dictionary<string, string> MapValues(Dictionary<string, string> response, HttpContext context, Uri debuggerHost)
        {
            var filtered = new Dictionary<string, string>();
            var request = context.Request;

            foreach (var key in response.Keys)
            {
                switch (key)
                {
                    case "devtoolsFrontendUrl":
                        var front = response[key];
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
            var devToolsHost = options.DevToolsUrl;
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
                    var request = context.Request;
                    var requestPath = request.Path;
                    return $"{devToolsHost.Scheme}://{devToolsHost.Authority}{request.Path}{request.QueryString}";
                }

                async Task Copy(HttpContext context)
                {
                    using (var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) })
                    {
                        var response = await httpClient.GetAsync(GetEndpoint(context));
                        context.Response.ContentType = response.Content.Headers.ContentType.ToString();
                        if ((response.Content.Headers.ContentLength ?? 0) > 0)
                            context.Response.ContentLength = response.Content.Headers.ContentLength;
                        var bytes = await response.Content.ReadAsByteArrayAsync();
                        await context.Response.Body.WriteAsync(bytes);

                    }
                }

                async Task RewriteSingle(HttpContext context)
                {
                    var version = await ProxyGetJsonAsync<Dictionary<string, string>>(GetEndpoint(context));
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(
                        JsonSerializer.Serialize(mapFunc(version, context, devToolsHost)));
                }

                async Task RewriteArray(HttpContext context)
                {
                    var tabs = await ProxyGetJsonAsync<Dictionary<string, string>[]>(GetEndpoint(context));
                    var alteredTabs = tabs.Select(t => mapFunc(t, context, devToolsHost)).ToArray();
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(JsonSerializer.Serialize(alteredTabs));
                }

                async Task ConnectProxy(HttpContext context)
                {
                    if (!context.WebSockets.IsWebSocketRequest)
                    {
                        context.Response.StatusCode = 400;
                        return;
                    }

                    var endpoint = new Uri($"ws://{devToolsHost.Authority}{context.Request.Path.ToString()}");
                    try
                    {
                        using var loggerFactory = LoggerFactory.Create(
                            builder => builder.AddConsole().AddFilter(null, LogLevel.Information));
                        var proxy = new DebuggerProxy(loggerFactory);
                        var ideSocket = await context.WebSockets.AcceptWebSocketAsync();

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

        static async Task<T> ProxyGetJsonAsync<T>(string url)
        {
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(url);
                return await JsonSerializer.DeserializeAsync<T>(await response.Content.ReadAsStreamAsync());
            }
        }
    }
}
