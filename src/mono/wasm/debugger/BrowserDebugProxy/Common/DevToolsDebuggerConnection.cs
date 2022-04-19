// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

#nullable enable

namespace Microsoft.WebAssembly.Diagnostics;

internal class DevToolsDebuggerConnection : WasmDebuggerConnection
{
    public WebSocket WebSocket { get; init; }
    private readonly ILogger _logger;

    public DevToolsDebuggerConnection(WebSocket webSocket!!, string id, ILogger logger!!)
            : base(id)
    {
        WebSocket = webSocket;
        _logger = logger;
    }

    public override async Task<string?> ReadOne(TaskCompletionSource client_initiated_close, TaskCompletionSource<Exception> side_exception, CancellationToken token)
    {
        byte[] buff = new byte[4000];
        var mem = new MemoryStream();
        try
        {
            while (true)
            {
                if (WebSocket.State != WebSocketState.Open)
                {
                    _logger.LogError($"DevToolsProxy: Socket is no longer open.");
                    client_initiated_close.TrySetResult();
                    return null;
                }

                ArraySegment<byte> buffAsSeg = new(buff);
                WebSocketReceiveResult result = await WebSocket.ReceiveAsync(buffAsSeg, token);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    client_initiated_close.TrySetResult();
                    return null;
                }

                await mem.WriteAsync(new ReadOnlyMemory<byte>(buff, 0, result.Count), token);

                if (result.EndOfMessage)
                    return Encoding.UTF8.GetString(mem.GetBuffer(), 0, (int)mem.Length);
            }
        }
        catch (WebSocketException e)
        {
            if (e.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
            {
                client_initiated_close.TrySetResult();
                return null;
            }
        }
        return null;
    }

    public override Task SendAsync(byte[] bytes, CancellationToken token)
        => WebSocket.SendAsync(new ArraySegment<byte>(bytes),
                               WebSocketMessageType.Text,
                               true,
                               token);

    public override async Task ShutdownAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!cancellationToken.IsCancellationRequested && WebSocket.State == WebSocketState.Open)
                await WebSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancellationToken);
        }
        catch (Exception ex) when (ex is IOException || ex is WebSocketException || ex is OperationCanceledException)
        {
            _logger.LogDebug($"Shutdown: Close failed, but ignoring: {ex}");
        }
    }

    public override void Dispose()
    {
        WebSocket.Dispose();
        base.Dispose();
    }

    public override string ToString() => $"[ {Id} connection: state: {WebSocket?.State} ]";
}
