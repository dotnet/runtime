// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
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
        if (_args.CommonConfig.Debugging)
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
                                               forwardConsole: _args.ForwardConsoleOutput ?? false,
                                               debugging: _args.CommonConfig.Debugging);
        runArgsJson.Save(Path.Combine(_args.CommonConfig.AppPath, "runArgs.json"));

        (ServerURLs serverURLs, IWebHost host) = await StartWebServerAsync(_args.CommonConfig.AppPath,
                                                                           _args.ForwardConsoleOutput ?? false,
                                                                           _args.CommonConfig.HostProperties.WebServerPort,
                                                                           token);

        string[] fullUrls = BuildUrls(serverURLs,
                                      _args.UseQueryStringToPassArguments ? _args.AppArgs : Array.Empty<string>());
        Console.WriteLine();
        foreach (string url in fullUrls)
            Console.WriteLine($"App url: {url}");

        await host.WaitForShutdownAsync(token);
    }

    private async Task<(ServerURLs, IWebHost)> StartWebServerAsync(string appPath, bool forwardConsole, int port, CancellationToken token)
    {
        WasmTestMessagesProcessor? logProcessor = null;
        if (forwardConsole)
        {
            logProcessor = new(_logger);
        }

        WebServerOptions options = new
        (
            OnConsoleConnected: forwardConsole
                                    ? socket => RunConsoleMessagesPump(socket, logProcessor!, token)
                                    : null,
            ContentRootPath: Path.GetFullPath(appPath),
            WebServerUseCors: true,
            WebServerUseCrossOriginPolicy: true,
            Port: port
        );

        (ServerURLs serverURLs, IWebHost host) = await WebServer.StartAsync(options, _logger, token);
        return (serverURLs, host);
    }

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
        string filename = Path.GetFileName(_args.HTMLPath!);
        string httpUrl = BuildUrl(serverURLs.Http, filename, query);

        return string.IsNullOrEmpty(serverURLs.Https)
            ? (new[] { httpUrl })
            : (new[]
                {
                    httpUrl,
                    BuildUrl(serverURLs.Https!, filename, query)
                });

        static string BuildUrl(string baseUrl, string htmlFileName, string query)
            => new UriBuilder(baseUrl)
            {
                Query = query,
                Path = htmlFileName
            }.ToString();
    }
}
