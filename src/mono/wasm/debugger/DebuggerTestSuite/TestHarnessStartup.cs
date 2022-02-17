// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
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
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Microsoft.WebAssembly.Diagnostics
{
    public class TestHarnessStartup
    {
        static Regex parseConnection = new Regex(@"listening on (ws?s://[^\s]*)");
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

        public async Task LaunchAndServe(ProcessStartInfo psi, HttpContext context, Func<string, ILogger<TestHarnessProxy>, Task<string>> extract_conn_url, Uri devToolsUrl)
        {

            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = 400;
                return;
            }

            var tcs = new TaskCompletionSource<string>();

            var proc = Process.Start(psi);
            try
            {
                proc.ErrorDataReceived += (sender, e) =>
                {
                    var str = e.Data;
                    Logger.LogTrace($"browser-stderr: {str}");

                    if (tcs.Task.IsCompleted)
                        return;
                    if (str != null)
                    {
                        var match = parseConnection.Match(str);
                        if (match.Success)
                        {
                            tcs.TrySetResult(match.Groups[1].Captures[0].Value);
                        }
                    }
                };

                proc.OutputDataReceived += (sender, e) =>
                {
                    Logger.LogTrace($"browser-stdout: {e.Data}");
                };

                proc.BeginErrorReadLine();
                proc.BeginOutputReadLine();
                string line;
                if (await Task.WhenAny(tcs.Task, Task.Delay(5000)) != tcs.Task)
                {
                    if (devToolsUrl.Port != 0)
                    {
                        tcs.TrySetResult(devToolsUrl.ToString());
                    }
                    else
                    {
                        Logger.LogError("Didnt get the con string after 20s.");
                        throw new Exception("node.js timedout");
                    }
                }
                line = await tcs.Task;
                var con_str = extract_conn_url != null ? await extract_conn_url(line, Logger) : line;

                Logger.LogInformation($"launching proxy for {con_str}");
#if RUN_IN_CHROME
                var proxy = new DebuggerProxy(_loggerFactory, null);
                var browserUri = new Uri(con_str);
                var ideSocket = await context.WebSockets.AcceptWebSocketAsync();
                await proxy.Run(browserUri, ideSocket);
#else
                var ideSocket = await context.WebSockets.AcceptWebSocketAsync();
                var proxyFirefox = new FirefoxProxyServer(_loggerFactory, 6000);
                await proxyFirefox.RunForTests(9500, ideSocket);
#endif
                Logger.LogInformation("Proxy done");
            }
            catch (Exception e)
            {
                Logger.LogError("got exception {0}", e);
            }
            finally
            {
                proc.CancelErrorRead();
                proc.CancelOutputRead();
                proc.Kill();
                proc.WaitForExit();
                proc.Close();
            }
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
                router.MapGet("launch-browser-and-connect", async context =>
                {
                    Logger.LogInformation("New test request");
                    try
                    {
                        var psi = new ProcessStartInfo();

                        psi.Arguments = $"{options.BrowserParms}{devToolsUrl.Port} http://{TestHarnessProxy.Endpoint.Authority}/{options.PagePath}";
                        psi.UseShellExecute = false;
                        psi.FileName = options.BrowserPath;
                        psi.RedirectStandardError = true;
                        psi.RedirectStandardOutput = true;
                        await LaunchAndServe(psi, context, options.ExtractConnUrl, devToolsUrl);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"launch-browser-and-connect failed with {ex.ToString()}");
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
                        await LaunchAndServe(psi, context, null, null);
                    });
                });
            }
        }
    }
}
