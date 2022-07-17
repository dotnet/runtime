// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.WebAssembly.Diagnostics;

namespace DebuggerTests
{
    internal class DevToolsClient : IDisposable
    {
        protected readonly ILogger logger;
        protected RunLoop _runLoop;

        public event EventHandler<RunLoopExitState> RunLoopStopped;

        public DevToolsClient(ILogger logger)
        {
            this.logger = logger;
        }

        ~DevToolsClient()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            _runLoop?.Dispose();
            _runLoop = null;
        }

        public Task ShutdownAsync(CancellationToken cancellationToken)
            => _runLoop is null
                        ? Task.CompletedTask
                        : _runLoop.ShutdownAsync(cancellationToken);

        public void Fail(Exception exception) => _runLoop?.Fail(exception);
        protected void Send(byte[] bytes, CancellationToken token) => _runLoop?.Send(bytes, token);

        protected async Task<ClientWebSocket> ConnectToWebServer(Uri uri, CancellationToken token)
        {
            // connects to the webserver to start the proxy
            ClientWebSocket clientSocket = new ();
            clientSocket.Options.KeepAliveInterval = Timeout.InfiniteTimeSpan;
            logger.LogDebug("Client connecting to {0}", uri);
            await clientSocket.ConnectAsync(uri, token);
            return clientSocket;
        }

        protected virtual Task<WasmDebuggerConnection> SetupConnection(Uri webserverUri, CancellationToken token)
            => throw new NotImplementedException();

        protected async Task ConnectAndStartRunLoopAsync(
            Uri uri,
            Func<string, CancellationToken, Task> receive,
            CancellationTokenSource cts)
        {
            WasmDebuggerConnection conn = await SetupConnection(uri, cts.Token);
            conn.OnReadAsync = receive;
            _runLoop = new(new[] { new DevToolsQueue(conn) }, logger);
            _runLoop.RunLoopStopped += (sender, args) => RunLoopStopped?.Invoke(sender, args);
            _ = _runLoop.RunAsync(cts);
        }
    }
}
