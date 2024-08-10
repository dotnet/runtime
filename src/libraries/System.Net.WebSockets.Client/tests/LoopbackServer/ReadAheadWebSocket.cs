// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.WebSockets.Client.Tests;

internal class ReadAheadWebSocket : WebSocket
{
    private const int ReadAheadBufferSize = 64 * 1024 * 1024;

    private record struct DataFrame(ValueWebSocketReceiveResult Metadata, Memory<byte> Memory, byte[] _rented);

    private Channel<DataFrame> _incomingFrames = Channel.CreateUnbounded<DataFrame>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });
    private DataFrame? _currentFrame;

    private SemaphoreSlim receiveMutex = new SemaphoreSlim(1, 1);
    private readonly WebSocket _innerWebSocket;

    public ReadAheadWebSocket(WebSocket innerWebSocket)
    {
        _innerWebSocket = innerWebSocket;
        _ = ProcessIncomingFrames();
    }

    private async Task ProcessIncomingFrames()
    {
        var buffer = new byte[ReadAheadBufferSize];
        while (true)
        {
            try
            {
                ValueWebSocketReceiveResult result = await _innerWebSocket.ReceiveAsync((Memory<byte>)buffer, default).ConfigureAwait(false);

                byte[] rented = result.Count > 0 ? ArrayPool<byte>.Shared.Rent(result.Count) : Array.Empty<byte>();
                Memory<byte> message = rented.AsMemory(0, result.Count);
                buffer.AsMemory(0, result.Count).CopyTo(message);

                await _incomingFrames.Writer.WriteAsync(new DataFrame(result, message, rented), default).ConfigureAwait(false);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _incomingFrames.Writer.Complete();
                    break;
                }
            }
            catch (Exception e)
            {
                _incomingFrames.Writer.Complete(e);
                break;
            }
        }
    }

    public override async ValueTask<ValueWebSocketReceiveResult> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        await receiveMutex.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            _currentFrame ??= await _incomingFrames.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);

            var (result, message, rented) = _currentFrame.Value;

            if (buffer.Length < result.Count)
            {
                message.Slice(0, buffer.Length).CopyTo(buffer);
                var remaining = message.Slice(buffer.Length);
                _currentFrame = _currentFrame.Value with { Metadata = new (remaining.Length, result.MessageType, result.EndOfMessage), Memory = remaining };

                return new (buffer.Length, result.MessageType, endOfMessage: false);
            }
            else
            {
                message.CopyTo(buffer);
                if (rented.Length > 0)
                {
                    ArrayPool<byte>.Shared.Return(rented);
                }
                _currentFrame = null;
                return result;
            }
        }
        finally
        {
            receiveMutex.Release();
        }
    }

    public override async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
    {
        ValueWebSocketReceiveResult valueResult = await ReceiveAsync((Memory<byte>)buffer, cancellationToken).ConfigureAwait(false);
        var result = new WebSocketReceiveResult(
            valueResult.Count,
            valueResult.MessageType,
            valueResult.EndOfMessage,
            valueResult.MessageType == WebSocketMessageType.Close ? CloseStatus : null,
            valueResult.MessageType == WebSocketMessageType.Close ? CloseStatusDescription : null);
        return result;
    }

    public override WebSocketCloseStatus? CloseStatus => _innerWebSocket.CloseStatus;
    public override string? CloseStatusDescription => _innerWebSocket.CloseStatusDescription;
    public override string? SubProtocol => _innerWebSocket.SubProtocol;
    public override WebSocketState State => _innerWebSocket.State;
    public override void Abort() => _innerWebSocket.Abort();
    public override void Dispose() => _innerWebSocket.Dispose();
    public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken) => _innerWebSocket.CloseAsync(closeStatus, statusDescription, cancellationToken);
    public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken) => _innerWebSocket.CloseOutputAsync(closeStatus, statusDescription, cancellationToken);
    public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken) => _innerWebSocket.SendAsync(buffer, messageType, endOfMessage, cancellationToken);
    public override ValueTask SendAsync(ReadOnlyMemory<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken) => _innerWebSocket.SendAsync(buffer, messageType, endOfMessage, cancellationToken);
    public override ValueTask SendAsync(ReadOnlyMemory<byte> buffer, WebSocketMessageType messageType, WebSocketMessageFlags messageFlags, CancellationToken cancellationToken) => _innerWebSocket.SendAsync(buffer, messageType, messageFlags, cancellationToken);
}
