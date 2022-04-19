// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

#nullable enable

namespace Microsoft.WebAssembly.Diagnostics;

public class FirefoxProxyServer
{
    private static TcpListener? s_tcpListener;
    private FirefoxMonoProxy? _firefoxMonoProxy;

    [MemberNotNull(nameof(s_tcpListener))]
    public static void StartListener(int proxyPort, ILogger logger)
    {
        if (s_tcpListener is null)
        {
            s_tcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), proxyPort);
            logger.LogInformation($"Now listening on {s_tcpListener.LocalEndpoint} for firefox debugging");
            s_tcpListener.Start();
        }
    }

    public static async Task Run(int browserPort, int proxyPort, ILoggerFactory loggerFactory, ILogger logger)
    {
        StartListener(proxyPort, logger);
        logger.LogInformation($"Expecting firefox to be listening on {browserPort}");
        while (true)
        {
            TcpClient ideClient = await s_tcpListener.AcceptTcpClientAsync();
            _ = Task.Run(() =>
                        {
                            logger.LogInformation($"IDE connected to the proxy");
                            var monoProxy = new FirefoxMonoProxy(loggerFactory);
                            return monoProxy.RunForFirefox(ideClient: ideClient, browserPort);
                        })
                        .ContinueWith(t =>
                        {
                            logger.LogError($"{nameof(FirefoxMonoProxy)} crashed with {t.Exception}");
                        }, TaskScheduler.Default)
                        .ConfigureAwait(false);
        }
    }

    public async Task RunForTests(int browserPort, int proxyPort, string testId, ILoggerFactory loggerFactory, ILogger logger)
    {
        StartListener(proxyPort, logger);

        TcpClient ideClient = await s_tcpListener.AcceptTcpClientAsync();
        _firefoxMonoProxy = new FirefoxMonoProxy(loggerFactory, testId);
        await _firefoxMonoProxy.RunForFirefox(ideClient: ideClient, browserPort);
    }

    public void Shutdown() => _firefoxMonoProxy?.Shutdown();
}
