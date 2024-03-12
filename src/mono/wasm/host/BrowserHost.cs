// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.WebAssembly.AppHost.DevServer;
using Microsoft.WebAssembly.Diagnostics;

#nullable enable

namespace Microsoft.WebAssembly.AppHost;

internal sealed class BrowserHost
{
    private readonly ILogger _logger;
    private readonly BrowserArguments _args;

    public BrowserHost(BrowserArguments args, ILogger logger)
    {
        _logger = logger;
        _args = args;
    }

    public static async Task<int> InvokeAsync(CommonConfiguration commonArgs,
                                              ILoggerFactory loggerFactory,
                                              ILogger logger,
                                              CancellationToken token)
    {
        var args = new BrowserArguments(commonArgs);
        args.Validate();
        var host = new BrowserHost(args, logger);
        await host.RunAsync(loggerFactory, token);

        return 0;
    }

    private async Task RunAsync(ILoggerFactory loggerFactory, CancellationToken token)
    {
        if (_args.CommonConfig.Debugging && !_args.CommonConfig.UseStaticWebAssets)
        {
            ProxyOptions options = _args.CommonConfig.ToProxyOptions();
            _ = Task.Run(() => DebugProxyHost.RunDebugProxyAsync(options, Array.Empty<string>(), loggerFactory, token), token)
                    .ConfigureAwait(false);
        }

        Dictionary<string, string> envVars = new();
        if (_args.CommonConfig.HostProperties.EnvironmentVariables is not null)
        {
            foreach (KeyValuePair<string, string> kvp in _args.CommonConfig.HostProperties.EnvironmentVariables)
                envVars[kvp.Key] = kvp.Value;
        }

        foreach (DictionaryEntry de in Environment.GetEnvironmentVariables())
        {
            if (de.Key is not null && de.Value is not null)
                envVars[(string)de.Key] = (string)de.Value;
        }

        var runArgsJson = new RunArgumentsJson(applicationArguments: _args.AppArgs,
                                               runtimeArguments: _args.CommonConfig.RuntimeArguments,
                                               environmentVariables: envVars,
                                               forwardConsoleToWS: _args.ForwardConsoleOutput ?? false,
                                               debugging: _args.CommonConfig.Debugging);
        runArgsJson.Save(Path.Combine(_args.CommonConfig.AppPath, "runArgs.json"));

        string[] urls = (envVars.TryGetValue("ASPNETCORE_URLS", out string? aspnetUrls) && aspnetUrls.Length > 0)
                            ? aspnetUrls.Split(';', StringSplitOptions.RemoveEmptyEntries)
                            : new string[] { $"http://127.0.0.1:{_args.CommonConfig.HostProperties.WebServerPort}", "https://127.0.0.1:0" };

        (ServerURLs serverURLs, IWebHost host) = await StartWebServerAsync(_args,
                                                                           urls,
                                                                           token);

        string[] fullUrls = BuildUrls(serverURLs, _args.AppArgs);
        Console.WriteLine();
        foreach (string url in fullUrls)
            Console.WriteLine($"App url: {url}");

        if (serverURLs.DebugPath != null)
        {
            Console.WriteLine($"Debug at url: {BuildUrl(serverURLs.Http, serverURLs.DebugPath, string.Empty)}");

            if (serverURLs.Https != null)
                Console.WriteLine($"Debug at url: {BuildUrl(serverURLs.Https, serverURLs.DebugPath, string.Empty)}");
        }

        await host.WaitForShutdownAsync(token);
    }

    private async Task<(ServerURLs, IWebHost)> StartWebServerAsync(BrowserArguments args, string[] urls, CancellationToken token)
    {
        Func<WebSocket, Task>? onConsoleConnected = null;
        if (args.ForwardConsoleOutput ?? false)
        {
            WasmTestMessagesProcessor logProcessor = new(_logger);
            onConsoleConnected = socket => RunConsoleMessagesPump(socket, logProcessor!, token);
        }

        // If we are using new browser template, use dev server
        if (args.CommonConfig.UseStaticWebAssets)
        {
            DevServerOptions devServerOptions = CreateDevServerOptions(args, urls, onConsoleConnected);
            return await DevServer.DevServer.StartAsync(devServerOptions, _logger, token);
        }

        // Otherwise for old template, use web server
        WebServerOptions webServerOptions = CreateWebServerOptions(urls, args.CommonConfig.AppPath, onConsoleConnected);
        return await WebServer.StartAsync(webServerOptions, _logger, token);
    }

    private static WebServerOptions CreateWebServerOptions(string[] urls, string appPath, Func<WebSocket, Task>? onConsoleConnected) => new
    (
        OnConsoleConnected: onConsoleConnected,
        ContentRootPath: Path.GetFullPath(appPath),
        WebServerUseCors: true,
        WebServerUseCrossOriginPolicy: true,
        Urls: urls
    );

    private static DevServerOptions CreateDevServerOptions(BrowserArguments args, string[] urls, Func<WebSocket, Task>? onConsoleConnected)
    {
        const string staticWebAssetsV1Extension = ".StaticWebAssets.xml";
        const string staticWebAssetsV2Extension = ".staticwebassets.runtime.json";

        DevServerOptions? devServerOptions = null;

        string appPath = args.CommonConfig.AppPath;
        if (args.CommonConfig.HostProperties.MainAssembly != null)
        {
            // If we have main assembly name, try to find static web assets manifest by precise name.

            var mainAssemblyPath = Path.Combine(appPath, args.CommonConfig.HostProperties.MainAssembly);
            var staticWebAssetsPath = Path.ChangeExtension(mainAssemblyPath, staticWebAssetsV2Extension);
            if (File.Exists(staticWebAssetsPath))
            {
                devServerOptions = CreateDevServerOptions(urls, staticWebAssetsPath, onConsoleConnected);
            }
            else
            {
                staticWebAssetsPath = Path.ChangeExtension(mainAssemblyPath, staticWebAssetsV1Extension);
                if (File.Exists(staticWebAssetsPath))
                    devServerOptions = CreateDevServerOptions(urls, staticWebAssetsPath, onConsoleConnected);
            }

            if (devServerOptions == null)
                devServerOptions = CreateDevServerOptions(urls, mainAssemblyPath, onConsoleConnected);
        }
        else
        {
            // If we don't have main assembly name, try to find static web assets manifest by search in the directory.

            var staticWebAssetsPath = FindFirstFileWithExtension(appPath, staticWebAssetsV2Extension)
                ?? FindFirstFileWithExtension(appPath, staticWebAssetsV1Extension);

            if (staticWebAssetsPath != null)
                devServerOptions = CreateDevServerOptions(urls, staticWebAssetsPath, onConsoleConnected);

            if (devServerOptions == null)
                throw new CommandLineException($"Please, provide mainAssembly in hostProperties of runtimeconfig. Alternatively leave the static web assets manifest ('*{staticWebAssetsV2Extension}') in the build output directory '{appPath}' .");
        }

        return devServerOptions;
    }

    private static DevServerOptions CreateDevServerOptions(string[] urls, string staticWebAssetsPath, Func<WebSocket, Task>? onConsoleConnected) => new
    (
        OnConsoleConnected: onConsoleConnected,
        StaticWebAssetsPath: staticWebAssetsPath,
        WebServerUseCors: true,
        WebServerUseCrossOriginPolicy: true,
        Urls: urls
    );

    private static string? FindFirstFileWithExtension(string directory, string extension)
        => Directory.EnumerateFiles(directory, "*" + extension).FirstOrDefault();

    private async Task RunConsoleMessagesPump(WebSocket socket, WasmTestMessagesProcessor messagesProcessor, CancellationToken token)
    {
        byte[] buff = new byte[4000];
        var mem = new MemoryStream();
        try
        {
            while (!token.IsCancellationRequested)
            {
                if (socket.State != WebSocketState.Open)
                {
                    _logger.LogError($"Console websocket is no longer open");
                    break;
                }

                WebSocketReceiveResult result = await socket.ReceiveAsync(new ArraySegment<byte>(buff), token).ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    // tcs.SetResult(false);
                    return;
                }

                mem.Write(buff, 0, result.Count);

                if (result.EndOfMessage)
                {
                    string? line = Encoding.UTF8.GetString(mem.GetBuffer(), 0, (int)mem.Length);
                    line += Environment.NewLine;

                    messagesProcessor.Invoke(line);
                    mem.SetLength(0);
                    mem.Seek(0, SeekOrigin.Begin);
                }
            }
        }
        catch (OperationCanceledException oce)
        {
            if (!token.IsCancellationRequested)
                _logger.LogDebug($"RunConsoleMessagesPump cancelled: {oce}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Console pump failed: {ex}");
            throw;
        }
    }

    private string[] BuildUrls(ServerURLs serverURLs, IEnumerable<string> passThroughArguments)
    {
        var sb = new StringBuilder();
        foreach (string arg in passThroughArguments)
        {
            if (sb.Length > 0)
                sb.Append('&');

            sb.Append($"arg={HttpUtility.UrlEncode(arg)}");
        }

        string query = sb.ToString();
        string? filename = _args.HTMLPath != null ? Path.GetFileName(_args.HTMLPath) : null;
        string httpUrl = BuildUrl(serverURLs.Http, filename, query);

        return string.IsNullOrEmpty(serverURLs.Https)
            ? (new[] { httpUrl })
            : (new[]
                {
                    httpUrl,
                    BuildUrl(serverURLs.Https!, filename, query)
                });
    }

    private static string BuildUrl(string baseUrl, string? htmlFileName, string query)
    {
        var uriBuilder = new UriBuilder(baseUrl)
        {
            Query = query
        };

        if (htmlFileName != null)
            uriBuilder.Path = htmlFileName;

        return uriBuilder.ToString();
    }
}
