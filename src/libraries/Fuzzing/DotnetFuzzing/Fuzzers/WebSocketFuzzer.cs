// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;

namespace DotnetFuzzing.Fuzzers;

internal sealed class WebSocketFuzzer : IFuzzer
{
    public string[] TargetAssemblies => ["System.Net.WebSockets"];
    public string[] TargetCoreLibPrefixes => [];

    private readonly byte[] _inputBuffer = new byte[4096]; // Default max fuzzer input length
    private readonly ReadOnlyMemory<byte>[] _segments = new ReadOnlyMemory<byte>[4096];
    private readonly byte[] _receiveBuffer = new byte[ushort.MaxValue + 1];
    private readonly byte[] _clientOperationsBuffer = new byte[ushort.MaxValue + 1];

    [Flags]
    private enum Options : byte
    {
        None = 0,
        IsServer = 1 << 0,
        EnableCompression = 1 << 1,
    }

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 5)
        {
            return;
        }

        Options options = (Options)bytes[0];
        bytes = bytes.Slice(1);

        int receiveBufferLength = 1 + BinaryPrimitives.ReadUInt16BigEndian(bytes);
        bytes = bytes.Slice(2);

        int clientOperationsBufferLength = BinaryPrimitives.ReadUInt16BigEndian(bytes);
        bytes = bytes.Slice(2);

        if (bytes.Length < clientOperationsBufferLength)
        {
            return;
        }

        bytes.Slice(0, clientOperationsBufferLength).CopyTo(_clientOperationsBuffer);
        bytes = bytes.Slice(clientOperationsBufferLength);
        ReadOnlyMemory<byte> clientOperations = _clientOperationsBuffer.AsMemory(0, clientOperationsBufferLength);

        if (!TryCreateSegments(bytes, out Memory<ReadOnlyMemory<byte>> segments))
        {
            return;
        }

        var stream = new TrickleStream(segments);

        using WebSocket webSocket = WebSocket.CreateFromStream(stream, new WebSocketCreationOptions
        {
            KeepAliveInterval = Timeout.InfiniteTimeSpan,
            KeepAliveTimeout = Timeout.InfiniteTimeSpan,
            IsServer = options.HasFlag(Options.IsServer),
            DangerousDeflateOptions = options.HasFlag(Options.EnableCompression) ? new WebSocketDeflateOptions() : null
        });

        TestWebSocket(webSocket, stream, _receiveBuffer.AsMemory(0, receiveBufferLength), clientOperations).GetAwaiter().GetResult();
    }

    private async Task TestWebSocket(WebSocket webSocket, TrickleStream stream, Memory<byte> receiveBuffer, ReadOnlyMemory<byte> clientOperations)
    {
        using CancellationTokenSource cts = new();
        cts.CancelAfter(TimeSpan.FromSeconds(30));

        try
        {
            while (true) // We should always exit from reading EOF
            {
                ValueWebSocketReceiveResult result = await webSocket.ReceiveAsync(ReadBool() ? Memory<byte>.Empty : receiveBuffer, cts.Token);

                if (ReadBool())
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, ReadBool() ? "Close description" : null, GetCancellationToken());
                }

                if (ReadBool())
                {
                    await webSocket.CloseOutputAsync(WebSocketCloseStatus.PolicyViolation, ReadBool() ? "Close output description" : null, GetCancellationToken());
                }

                if (ReadBool())
                {
                    await webSocket.SendAsync(
                        buffer: receiveBuffer.Slice(0, Math.Min(receiveBuffer.Length, ReadByte())),
                        messageType: ReadBool() ? WebSocketMessageType.Text : WebSocketMessageType.Binary,
                        endOfMessage: ReadBool(),
                        cancellationToken: GetCancellationToken());
                }

                if (ReadBool())
                {
                    webSocket.Dispose();
                }
            }
        }
        catch (WebSocketException)
        {
            // Expected
        }

        CancellationToken GetCancellationToken() => ReadBool() ? cts.Token : CancellationToken.None;

        bool ReadBool() => ReadByte() != 0;

        byte ReadByte()
        {
            if (clientOperations.IsEmpty) return 0;
            byte value = clientOperations.Span[0];
            clientOperations = clientOperations.Slice(1);
            return value;
        }
    }

    private bool TryCreateSegments(ReadOnlySpan<byte> bytes, out Memory<ReadOnlyMemory<byte>> segments)
    {
        bytes.CopyTo(_inputBuffer);
        ReadOnlyMemory<byte> buffer = _inputBuffer.AsMemory(0, bytes.Length);

        int segmentCount = 0;
        while (!buffer.IsEmpty)
        {
            int length = buffer.Span[0];
            buffer = buffer.Slice(1);

            if (length > buffer.Length)
            {
                segments = default;
                return false;
            }

            _segments[segmentCount++] = buffer.Slice(0, length);
            buffer = buffer.Slice(length);
        }

        segments = _segments.AsMemory(0, segmentCount);
        return true;
    }

    // Allows controlling how the read data is chunked between Read calls.
    // All writes are ignored.
    private sealed class TrickleStream : Stream
    {
        private Memory<ReadOnlyMemory<byte>> _segments;

        public TrickleStream(Memory<ReadOnlyMemory<byte>> segments)
        {
            _segments = segments;
        }

        public override bool CanRead => true;
        public override bool CanWrite => true;
        public override bool CanSeek => false;

        public override int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException("Expected only async reads");

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (buffer.IsEmpty || _segments.IsEmpty)
            {
                return new ValueTask<int>(0);
            }

            ReadOnlyMemory<byte> segment = _segments.Span[0];
            int toCopy = Math.Min(buffer.Length, segment.Length);

            segment.Span.Slice(0, toCopy).CopyTo(buffer.Span);

            if (toCopy == segment.Length)
            {
                _segments = _segments.Slice(1);
            }
            else
            {
                _segments.Span[0] = segment.Slice(toCopy);
            }

            return new ValueTask<int>(toCopy);
        }

        public override void Flush() { }
        public override void Write(byte[] buffer, int offset, int count) { }
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => Task.CompletedTask;
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

        public override long Length => throw new NotImplementedException();
        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override void SetLength(long value) => throw new NotImplementedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();
    }
}
