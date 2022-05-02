// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.WebAssembly.Diagnostics;
using Newtonsoft.Json.Linq;

namespace DebuggerTests
{
    public class TestHarnessStartup
    {
        public TestHarnessStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; set; }
        public ILogger<TestHarnessProxy> Logger { get; private set; }

        private ILoggerFactory _loggerFactory;

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting()
                .Configure<TestHarnessOptions>(Configuration);
        }

        async Task SendNodeVersion(HttpContext context)
        {
            Logger.LogTrace("hello chrome! json/version");
            var resp_obj = new JObject();
            resp_obj["Browser"] = "node.js/v9.11.1";
            resp_obj["Protocol-Version"] = "1.1";

            var response = resp_obj.ToString();
            await context.Response.WriteAsync(response, new CancellationTokenSource().Token);
        }

        async Task SendNodeList(HttpContext context)
        {
            Logger.LogTrace("webserver: hello chrome! json/list");
            try
            {
                var response = new JArray(JObject.FromObject(new
                {
                    description = "node.js instance",
                    devtoolsFrontendUrl = "chrome-devtools://devtools/bundled/inspector.html?experiments=true&v8only=true&ws=localhost:9300/91d87807-8a81-4f49-878c-a5604103b0a4",
                    faviconUrl = "https://nodejs.org/static/favicon.ico",
                    id = "91d87807-8a81-4f49-878c-a5604103b0a4",
                    title = "foo.js",
                    type = "node",
                    webSocketDebuggerUrl = "ws://localhost:9300/91d87807-8a81-4f49-878c-a5604103b0a4"
                })).ToString();

                Logger.LogTrace($"webserver: sending: {response}");
                await context.Response.WriteAsync(response, new CancellationTokenSource().Token);
            }
            catch (Exception e) { Logger.LogError(e, "webserver: SendNodeList failed"); }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IOptionsMonitor<TestHarnessOptions> optionsAccessor, IWebHostEnvironment env, ILogger<TestHarnessProxy> logger, ILoggerFactory loggerFactory)
        {
            this.Logger = logger;
            this._loggerFactory = loggerFactory;

            app.UseWebSockets();
            app.UseStaticFiles();

            TestHarnessOptions options = optionsAccessor.CurrentValue;

            var provider = new FileExtensionContentTypeProvider();
            provider.Mappings[".wasm"] = "application/wasm";

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(options.AppPath),
                ServeUnknownFileTypes = true, //Cuz .wasm is not a known file type :cry:
                RequestPath = "",
                ContentTypeProvider = provider
            });

            var devToolsUrl = options.DevToolsUrl;
            app.UseRouter(router =>
            {
                router.MapGet("launch-host-and-connect", async context =>
                {
                    string test_id;
                    if (context.Request.Query.TryGetValue("test_id", out var value) && value.Count == 1)
                        test_id = value[0];
                    else
                        test_id = "unknown";

                    WasmHost host = WasmHost.Chrome;
                    if (context.Request.Query.TryGetValue("host", out value) && value.Count == 1)
                    {
                        if (!Enum.TryParse(value[0], true, out host))
                            throw new ArgumentException($"Unknown wasm host - {value[0]}");
                    }

                    int firefox_proxy_port = 6002;
                    if (context.Request.Query.TryGetValue("firefox-proxy-port", out value) && value.Count == 1 &&
                        int.TryParse(value[0], out int port))
                    {
                        firefox_proxy_port = port;
                    }

                    string message_prefix = $"[testId: {test_id}]";
                    Logger.LogInformation($"{message_prefix} New test request for test id {test_id}");
                    CancellationTokenSource cts = new();
                    try
                    {
                        int browserPort;
                        if (host == WasmHost.Chrome)
                        {
                            using var provider = new ChromeProvider(test_id, Logger);
                            browserPort = options.DevToolsUrl.Port;
                            await provider.StartBrowserAndProxyAsync(context,
                                                $"http://{TestHarnessProxy.Endpoint.Authority}/{options.PagePath}",
                                                browserPort,
                                                message_prefix,
                                                _loggerFactory,
                                                cts).ConfigureAwait(false);
                        }
                        else if (host == WasmHost.Firefox)
                        {
                            using var provider = new FirefoxProvider(test_id, Logger);
                            browserPort = 6500 + int.Parse(test_id);
                            await provider.StartBrowserAndProxyAsync(context,
                                                $"http://{TestHarnessProxy.Endpoint.Authority}/{options.PagePath}",
                                                browserPort,
                                                firefox_proxy_port,
                                                message_prefix,
                                                _loggerFactory,
                                                cts).ConfigureAwait(false);
                        }
                        Logger.LogDebug($"{message_prefix} TestHarnessStartup done");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"{message_prefix} launch-host-and-connect failed with {ex}");
                        TestHarnessProxy.RegisterProxyExitState(test_id, new(RunLoopStopReason.Exception, ex));
                    }
                    finally
                    {
                        Logger.LogDebug($"TestHarnessStartup: closing for {test_id}");
                        cts.Cancel();
                    }
                });
            });

            if (options.NodeApp != null)
            {
                Logger.LogTrace($"Doing the nodejs: {options.NodeApp}");
                var nodeFullPath = Path.GetFullPath(options.NodeApp);
                Logger.LogTrace(nodeFullPath);
                var psi = new ProcessStartInfo();

                psi.UseShellExecute = false;
                psi.RedirectStandardError = true;
                psi.RedirectStandardOutput = true;

                psi.Arguments = $"--inspect-brk=localhost:0 {nodeFullPath}";
                psi.FileName = "node";

                app.UseRouter(router =>
                {
                    //Inspector API for using chrome devtools directly
                    router.MapGet("json", SendNodeList);
                    router.MapGet("json/list", SendNodeList);
                    router.MapGet("json/version", SendNodeVersion);
                    router.MapGet("launch-done-and-connect", async context =>
                    {
                        await Task.CompletedTask;
                        // await LaunchAndServe(psi, context, null, null, null, null);
                    });
                });
            }
        }
    }
}
