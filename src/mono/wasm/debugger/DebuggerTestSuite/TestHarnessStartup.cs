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

        public async Task LaunchAndServe(ProcessStartInfo psi,
                                         HttpContext context,
                                         Func<string, Task<string>> extract_conn_url,
                                         string test_id,
                                         string message_prefix,
                                         int get_con_url_timeout_ms=20000)
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
                    Logger.LogTrace($"{message_prefix} browser-stderr: {str}");

                    if (tcs.Task.IsCompleted)
                        return;

                    if (!string.IsNullOrEmpty(str))
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
                    Logger.LogTrace($"{message_prefix} browser-stdout: {e.Data}");
                };

                proc.BeginErrorReadLine();
                proc.BeginOutputReadLine();

                if (await Task.WhenAny(tcs.Task, Task.Delay(get_con_url_timeout_ms)) != tcs.Task)
                {
                    Logger.LogError($"{message_prefix} Timed out after {get_con_url_timeout_ms/1000}s waiting for a connection string from {psi.FileName}");
                    return;
                }
                var line = await tcs.Task;
                var con_str = extract_conn_url != null ? await extract_conn_url(line) : line;

                Logger.LogInformation($"{message_prefix} launching proxy for {con_str}");

                string logFilePath = Path.Combine(DebuggerTests.DebuggerTestBase.TestLogPath, $"{test_id}-proxy.log");
                File.Delete(logFilePath);

                var proxyLoggerFactory = LoggerFactory.Create(
                    builder => builder
                        .AddFile(logFilePath, minimumLevel: LogLevel.Debug)
                        .AddFilter(null, LogLevel.Trace));

                var proxy = new DebuggerProxy(proxyLoggerFactory, null, loggerId: test_id);
                var browserUri = new Uri(con_str);
                var ideSocket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);

                await proxy.Run(browserUri, ideSocket).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.LogError($"{message_prefix} got exception {e}");
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
                router.MapGet("launch-chrome-and-connect", async context =>
                {
                    string test_id;
                    if (context.Request.Query.TryGetValue("test_id", out var value) && value.Count == 1)
                        test_id = value[0];
                    else
                        test_id = "unknown";

                    string message_prefix = $"[testId: {test_id}]";
                    Logger.LogInformation($"{message_prefix} New test request for test id {test_id}");
                    try
                    {
                        var client = new HttpClient();
                        var psi = new ProcessStartInfo();

                        psi.Arguments = $"--headless --disable-gpu --lang=en-US --incognito --remote-debugging-port={devToolsUrl.Port} http://{TestHarnessProxy.Endpoint.Authority}/{options.PagePath}";
                        if (File.Exists("/.dockerenv"))
                        {
                            Logger.LogInformation("Detected a container, disabling sandboxing for debugger tests.");
                            psi.Arguments = "--no-sandbox " + psi.Arguments;
                        }
                        psi.UseShellExecute = false;
                        psi.FileName = options.ChromePath;
                        psi.RedirectStandardError = true;
                        psi.RedirectStandardOutput = true;

                        await LaunchAndServe(psi, context, async (str) =>
                        {
                            var start = DateTime.Now;
                            JArray obj = null;

                            while (true)
                            {
                                // Unfortunately it does look like we have to wait
                                // for a bit after getting the response but before
                                // making the list request.  We get an empty result
                                // if we make the request too soon.
                                await Task.Delay(100);

                                var res = await client.GetStringAsync(new Uri(new Uri(str), "/json/list"));
                                Logger.LogTrace($"{message_prefix}res is {res}");

                                if (!String.IsNullOrEmpty(res))
                                {
                                    // Sometimes we seem to get an empty array `[ ]`
                                    obj = JArray.Parse(res);
                                    if (obj != null && obj.Count >= 1)
                                        break;
                                }

                                var elapsed = DateTime.Now - start;
                                if (elapsed.Milliseconds > 5000)
                                {
                                    Logger.LogError($"{message_prefix} Unable to get DevTools /json/list response in {elapsed.Seconds} seconds, stopping");
                                    return null;
                                }
                            }

                            var wsURl = obj[0]?["webSocketDebuggerUrl"]?.Value<string>();
                            Logger.LogTrace($"{message_prefix} >>> {wsURl}");

                            return wsURl;
                        }, test_id, message_prefix).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"{message_prefix} launch-chrome-and-connect failed with {ex.ToString()}");
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
                        await LaunchAndServe(psi, context, null, null, null);
                    });
                });
            }
        }
    }
}
