// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

#nullable enable

namespace Microsoft.WebAssembly.Diagnostics;

public class FirefoxProxyServer
{
    private readonly int portBrowser;
    private readonly ILoggerFactory loggerFactory;

    public FirefoxProxyServer(ILoggerFactory loggerFactory, int portBrowser)
    {
        this.portBrowser = portBrowser;
        this.loggerFactory = loggerFactory;
    }

    public async void Run()
    {
        var _server = new TcpListener(IPAddress.Parse("127.0.0.1"), 0);
        _server.Start();
        var port = ((IPEndPoint)_server.LocalEndpoint).Port;
        Console.WriteLine($"Now listening on: 127.0.0.1:{port} for Firefox debugging");
        while (true)
        {
            TcpClient ideClient = await _server.AcceptTcpClientAsync();
            Console.WriteLine ($"IDE connected to the proxy");
            var monoProxy = new FirefoxMonoProxy(loggerFactory, portBrowser);
            await monoProxy.Run(ideClient: ideClient);
        }
    }

    public async Task RunForTests(int proxyPort, string testId, ILogger logger)
    {
        try
        {
        var _server = new TcpListener(IPAddress.Parse("127.0.0.1"), proxyPort);
        logger.LogInformation($"[{testId}] RunForTests: listening on port {proxyPort}");
        _server.Start();
        TcpClient ideClient = await _server.AcceptTcpClientAsync();
        _server.Stop();

        logger.LogDebug($"[{testId}] RunForTests: client connected");
        var monoProxy = new FirefoxMonoProxy(loggerFactory, portBrowser, testId);
        await monoProxy.Run(ideClient: ideClient);
        }
        catch (Exception ex)
        {
            Console.WriteLine ($"[{testId}] RunForTests: {ex}");
            throw;
        }
        finally
        {
            Console.WriteLine($"[{testId}] RunForTests: finally");
        }
    }
}
