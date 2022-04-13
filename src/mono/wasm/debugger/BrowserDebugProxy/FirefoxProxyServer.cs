// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

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
            TcpClient newClient = await _server.AcceptTcpClientAsync();
            var monoProxy = new FirefoxMonoProxy(loggerFactory, portBrowser);
            await monoProxy.Run(newClient);
        }
    }

    public async Task RunForTests(int port, WebSocket socketForDebuggerTests)
    {
        var _server = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
        _server.Start();
        port = ((IPEndPoint)_server.LocalEndpoint).Port;
        Console.WriteLine($"Now listening on: 127.0.0.1:{port} for Firefox debugging");
        TcpClient newClient = await _server.AcceptTcpClientAsync();
        try {
            var monoProxy = new FirefoxMonoProxy(loggerFactory, portBrowser);
            await monoProxy.Run(newClient, socketForDebuggerTests);
        }
        catch (Exception)
        {
            _server.Stop();
            newClient.Dispose();
            throw;
        }
        _server.Stop();
    }
}
