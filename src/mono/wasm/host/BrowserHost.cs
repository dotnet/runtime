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
using System.Linq;

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

        string[] urls = envVars.TryGetValue("ASPNETCORE_URLS", out string? aspnetUrls)
                            ? aspnetUrls.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                            : new string[] { $"http://127.0.0.1:{_args.CommonConfig.HostProperties.WebServerPort}", "https://127.0.0.1:0" };

        (ServerURLs serverURLs, IWebHost host) = await StartWebServerAsync(_args.CommonConfig.AppPath,
                                                                           _args.ForwardConsoleOutput ?? false,
                                                                           urls,
                                                                           token);

        string[] fullUrls = BuildUrls(serverURLs,
                                    _args.AppArgs,
                                    _args.CommonConfig.RuntimeArguments,
                                    envVars,
                                    _args.ForwardConsoleOutput ?? false,
                                    _args.CommonConfig.Debugging);
        Console.WriteLine();
        foreach (string url in fullUrls)
            Console.WriteLine($"App url: {url}");

        await host.WaitForShutdownAsync(token);
    }

    private async Task<(ServerURLs, IWebHost)> StartWebServerAsync(string appPath, bool forwardConsole, string[] urls, CancellationToken token)
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
            Urls: urls
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

    private string[] BuildUrls(ServerURLs serverURLs,
                            IEnumerable<string> passThroughArguments,
                            string[] runtimeArguments,
                            IDictionary<string, string> environmentVariables,
                            bool forwardConsoleToWS=false,
                            bool debugging = false)
    {
        var sb = new StringBuilder();
        if (!forwardConsoleToWS)
            sb.Append($"arg=--no-forward-console");
        if (debugging)
        {
            if (sb.Length > 0)
            {
                sb.Append($"&arg=--debug");
            }
            else
            {
                sb.Append($"arg=--debug");
            }
        }
        if (sb.Length > 0 && runtimeArguments.Length > 0)
            sb.Append('&');
        sb.AppendJoin("&", runtimeArguments.Select(arg => ($"arg=--runtime-arg={HttpUtility.UrlEncode(arg)}")));

        if (sb.Length > 0 && environmentVariables.Count > 0)
            sb.Append('&');
        sb.AppendJoin("&", environmentVariables.Select(arg => $"arg=--setenv={HttpUtility.UrlEncode(arg.Key)}={HttpUtility.UrlEncode(arg.Value)}"));

        sb.Append(" -- ");
        sb.AppendJoin("&", passThroughArguments.Select(arg => $"arg={HttpUtility.UrlEncode(arg)}"));

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
