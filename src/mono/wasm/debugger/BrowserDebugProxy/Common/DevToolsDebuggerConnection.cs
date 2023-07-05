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

internal sealed class DevToolsDebuggerConnection : WasmDebuggerConnection
{
    public WebSocket WebSocket { get; init; }
    private readonly ILogger _logger;

    public DevToolsDebuggerConnection(WebSocket webSocket, string id, ILogger logger)
            : base(id)
    {
        ArgumentNullException.ThrowIfNull(webSocket);
        ArgumentNullException.ThrowIfNull(logger);
        WebSocket = webSocket;
        _logger = logger;
    }

    public override bool IsConnected => WebSocket.State == WebSocketState.Open;

    public override async Task<string?> ReadOneAsync(CancellationToken token)
    {
        byte[] buff = new byte[4000];
        var mem = new MemoryStream();

        while (true)
        {
            if (WebSocket.State != WebSocketState.Open)
                throw new Exception($"WebSocket is no longer open, state: {WebSocket.State}");

            ArraySegment<byte> buffAsSeg = new(buff);
            WebSocketReceiveResult result = await WebSocket.ReceiveAsync(buffAsSeg, token);
            if (result.MessageType == WebSocketMessageType.Close)
                throw new Exception($"WebSocket close message received, state: {WebSocket.State}");

            await mem.WriteAsync(new ReadOnlyMemory<byte>(buff, 0, result.Count), token);

            if (result.EndOfMessage)
                return Encoding.UTF8.GetString(mem.GetBuffer(), 0, (int)mem.Length);
        }
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
