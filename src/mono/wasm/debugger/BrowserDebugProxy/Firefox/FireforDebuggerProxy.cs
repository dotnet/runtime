// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

#nullable enable

namespace Microsoft.WebAssembly.Diagnostics;

public class FirefoxDebuggerProxy : DebuggerProxyBase
{
    private static TcpListener? s_tcpListener;
    private static int s_nextId;
    internal FirefoxMonoProxy? FirefoxMonoProxy { get; private set; }

    [MemberNotNull(nameof(s_tcpListener))]
    public static void StartListener(int proxyPort, ILogger logger, int browserPort = -1)
    {
        if (s_tcpListener is null)
        {
            s_tcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), proxyPort);
            s_tcpListener.Start();
            Console.WriteLine($"Debug proxy for firefox now listening on tcp://{s_tcpListener.LocalEndpoint}." +
                                (browserPort >= 0 ? $" And expecting firefox at port {browserPort} ." : string.Empty));
        }
    }

    public static async Task RunServerLoopAsync(int browserPort, int proxyPort, ILoggerFactory loggerFactory, ILogger logger, CancellationToken token, ProxyOptions? options = null)
    {
        StartListener(proxyPort, logger, browserPort);
        while (!token.IsCancellationRequested)
        {
            TcpClient ideClient = await s_tcpListener.AcceptTcpClientAsync(token);
            _ = Task.Run(async () =>
                        {
                            CancellationTokenSource cts = new();
                            try
                            {
                                int id = Interlocked.Increment(ref s_nextId);
                                logger.LogInformation($"IDE connected to the proxy, id: {id}");
                                var monoProxy = new FirefoxMonoProxy(loggerFactory.CreateLogger($"{nameof(FirefoxMonoProxy)}-{id}"), id.ToString(), options);
                                await monoProxy.RunForFirefox(ideClient: ideClient, browserPort, cts);
                            }
                            catch (Exception ex)
                            {
                                logger.LogError($"{nameof(FirefoxMonoProxy)} crashed with {ex}");
                            }
                            finally
                            {
                                cts.Cancel();
                            }
                        }, token)
                        .ConfigureAwait(false);
        }
    }

    public async Task RunForTests(int browserPort, int proxyPort, string testId, ILoggerFactory loggerFactory, ILogger logger, CancellationTokenSource cts)
    {
        StartListener(proxyPort, logger, browserPort);

        TcpClient ideClient = await s_tcpListener.AcceptTcpClientAsync(cts.Token);
        FirefoxMonoProxy = new FirefoxMonoProxy(loggerFactory.CreateLogger($"FirefoxMonoProxy-{testId}"), testId);
        FirefoxMonoProxy.RunLoopStopped += (_, args) => ExitState = args;
        await FirefoxMonoProxy.RunForFirefox(ideClient: ideClient, browserPort, cts);
    }

    public override void Shutdown() => FirefoxMonoProxy?.Shutdown();
    public override void Fail(Exception ex) => FirefoxMonoProxy?.Fail(ex);
}
