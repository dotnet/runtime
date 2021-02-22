// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets.Compression;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.WebSockets
{
    internal partial class ManagedWebSocket
    {
        private enum ReceiveResultType
        {
            Message,
            ConnectionClose,
            ControlMessage,
            HeaderError
        }

        [StructLayout(LayoutKind.Auto)]
        private readonly struct ReceiveResult
        {
            public int Count { get; init; }
            public bool EndOfMessage { get; init; }
            public ReceiveResultType ResultType { get; init; }
            public WebSocketMessageType MessageType { get; init; }
        }

        private sealed class Receiver : IDisposable
        {
            private readonly bool _isServer;
            private readonly Stream _stream;
            private readonly WebSocketInflater? _inflater;

            /// <summary>
            /// Because a message might be split into multiple fragments we need to
            /// keep in mind that even if we've processed all of them, we need to call
            /// finish on the decoder to flush any left over data.
            /// </summary>
            private bool _inflateFinished = true;

            /// <summary>
            /// If we have compression we cannot use the buffer provided from clients because
            /// we cannot guarantee that the decoding can happen in place. This buffer is rent'ed
            /// and returned when consumed.
            /// </summary>
            private Memory<byte> _inflateBuffer;

            /// <summary>
            /// The last header received in a ReceiveAsync. If ReceiveAsync got a header but then
            /// returned fewer bytes than was indicated in the header, subsequent ReceiveAsync calls
            /// will use the data from the header to construct the subsequent receive results, and
            /// the payload length in this header will be decremented to indicate the number of bytes
            /// remaining to be received for that header.  As a result, between fragments, the payload
            /// length in this header should be 0.
            /// </summary>
            private MessageHeader _lastHeader = new() { Opcode = MessageOpcode.Text, Fin = true };

            /// <summary>
            /// Buffer used for reading data from the network.
            /// Not readonly here because the buffer is mutable and is a struct.
            /// </summary>
            private Buffer _readBuffer;

            /// <summary>
            /// When dealing with partially read fragments of binary/text messages, a mask previously received may still
            /// apply, and the first new byte received may not correspond to the 0th position in the mask.  This value is
            /// the next offset into the mask that should be applied.
            /// </summary>
            private int _receivedMaskOffset;

            /// <summary>
            /// When parsing message header if an error occurs the websocket is notified and this
            /// will contain the error message.
            /// </summary>
            private string? _headerError;

            public Receiver(Stream stream, WebSocketCreationOptions options)
            {
                _stream = stream;
                _isServer = options.IsServer;

                // Create a buffer just large enough to handle received packet headers (at most 14 bytes) and
                // control payloads (at most 125 bytes). Message payloads are read directly into the buffer
                // supplied to ReceiveAsync.
                _readBuffer = new Buffer(MaxControlPayloadLength + MaxMessageHeaderLength);

                var deflate = options.DeflateOptions;

                if (deflate is not null)
                {
                    // Important note here is that we must use negative window bits
                    // which will instruct the underlying implementation to not expect deflate headers
                    _inflater = options.IsServer ?
                        new WebSocketInflater(deflate.ServerMaxWindowBits, deflate.ServerContextTakeover) :
                        new WebSocketInflater(deflate.ClientMaxWindowBits, deflate.ClientContextTakeover);
                }
            }

            public void Dispose()
            {
                _inflater?.Dispose();
                ReturnInflateBuffer();
            }

            public string? GetHeaderError() => _headerError;

            /// <summary>Issues a read on the stream to wait for EOF.</summary>
            public async ValueTask WaitForServerToCloseConnectionAsync(CancellationToken cancellationToken)
            {
                if (_readBuffer.FreeLength == 0)
                {
                    // Because we are going to need only 1 byte buffer, do a discard
                    // only when necessary (avoiding needless copying).
                    _readBuffer.DiscardConsumed();
                }
                // Per RFC 6455 7.1.1, try to let the server close the connection.  We give it up to a second.
                // We simply issue a read and don't care what we get back; we could validate that we don't get
                // additional data, but at this point we're about to close the connection and we're just stalling
                // to try to get the server to close first.
                ValueTask<int> finalReadTask = _stream.ReadAsync(_readBuffer.FreeMemory.Slice(start: 0, length: 1), cancellationToken);

                if (!finalReadTask.IsCompletedSuccessfully)
                {
                    // Wait an arbitrary amount of time to give the server (same as netfx, 1 second)
                    using var cts = finalReadTask.IsCompleted ? null : new CancellationTokenSource(TimeSpan.FromSeconds(1));
                    using var cancellation = cts is not null ? cts.Token.UnsafeRegister(static s => ((Stream)s!).Dispose(), _stream) : default;

                    // TODO: Once this is merged https://github.com/dotnet/runtime/issues/47525
                    // use configure await with the option to suppress exceptions and remove the try catch
                    try
                    {
                        await finalReadTask.ConfigureAwait(false);
                    }
                    catch
                    {
                        // Eat any resulting exceptions. We were going to close the connection, anyway.
                    }
                }
            }

            public async ValueTask<ControlMessage?> ReceiveControlMessageAsync(CancellationToken cancellationToken)
            {
                Debug.Assert(_lastHeader.Opcode > MessageOpcode.Binary);

                if (_lastHeader.PayloadLength == 0)
                {
                    return new ControlMessage(_lastHeader.Opcode, ReadOnlyMemory<byte>.Empty);
                }
                _readBuffer.DiscardConsumed();

                while (_lastHeader.PayloadLength > _readBuffer.AvailableLength)
                {
                    int byteCount = await _stream.ReadAsync(_readBuffer.FreeMemory, cancellationToken).ConfigureAwait(false);
                    if (byteCount <= 0)
                    {
                        return null;
                    }
                    ApplyMask(_readBuffer.FreeMemory.Span.Slice(0, (int)Math.Min(_lastHeader.PayloadLength, byteCount)));
                    _readBuffer.Commit(byteCount);
                }

                // Update the payload length in the header to indicate
                // that we've received everything we need.
                ReadOnlyMemory<byte> payload = _readBuffer.AvailableMemory.Slice(0, (int)_lastHeader.PayloadLength);

                _readBuffer.Consume(payload.Length);
                _lastHeader.PayloadLength = 0;

                return new ControlMessage(_lastHeader.Opcode, payload);
            }

            public async ValueTask<ReceiveResult> ReceiveAsync(Memory<byte> output, CancellationToken cancellationToken)
            {
                // When there's nothing left over to receive, start a new
                if (_lastHeader.PayloadLength == 0)
                {
                    if (!_inflateFinished)
                    {
                        Debug.Assert(_inflater is not null);
                        _inflateFinished = _inflater.Finish(output.Span, out int written);

                        return Result(written);
                    }

                    _readBuffer.DiscardConsumed();

                    if (!await ReceiveHeaderAsync(cancellationToken).ConfigureAwait(false))
                    {
                        return Result(_headerError is not null ? ReceiveResultType.HeaderError : ReceiveResultType.ConnectionClose);
                    }
                    if (_lastHeader.Opcode > MessageOpcode.Binary)
                    {
                        // The received message is a control message and it's up
                        // to the websocket how to handle it.
                        return Result(ReceiveResultType.ControlMessage);
                    }
                }

                if (output.IsEmpty)
                {
                    return Result(count: 0);
                }
                // The number of bytes that are written to the output buffer
                int outputByteCount = 0;

                if (_readBuffer.AvailableLength > 0)
                {
                    if (!ConsumeReadBuffer(output.Span, out int written))
                    {
                        return Result(written);
                    }
                    outputByteCount += written;
                    output = output[written..];
                }

                // At this point we should have consumed everything from the read buffer
                // and should start issuing reads on the stream.
                Debug.Assert(_readBuffer.AvailableLength == 0 && _lastHeader.PayloadLength > 0);

                int receivedByteCount = _lastHeader.Compressed ?
                    await ReceiveCompressedAsync(output, cancellationToken).ConfigureAwait(false) :
                    await ReceiveUncompressedAsync(output, cancellationToken).ConfigureAwait(false);

                if (receivedByteCount == 0)
                {
                    return Result(ReceiveResultType.ConnectionClose);
                }

                return Result(outputByteCount + receivedByteCount);
            }

            private async ValueTask<int> ReceiveUncompressedAsync(Memory<byte> output, CancellationToken cancellationToken)
            {
                Debug.Assert(!_lastHeader.Compressed);

                if (output.Length > _lastHeader.PayloadLength)
                {
                    // We don't want to receive more than we need
                    output = output.Slice(0, (int)_lastHeader.PayloadLength);
                }

                int bytesRead = await _stream.ReadAsync(output, cancellationToken).ConfigureAwait(false);
                if (bytesRead > 0)
                {
                    _lastHeader.PayloadLength -= bytesRead;
                    ApplyMask(output.Span.Slice(0, bytesRead));
                }

                return bytesRead;
            }

            private async ValueTask<int> ReceiveCompressedAsync(Memory<byte> output, CancellationToken cancellationToken)
            {
                Debug.Assert(_lastHeader.Compressed);
                Debug.Assert(_inflater is not null);

                if (_inflateBuffer.IsEmpty)
                {
                    if (!await LoadInflateBufferAsync(cancellationToken).ConfigureAwait(false))
                    {
                        return 0;
                    }
                }

                _inflater.Inflate(_inflateBuffer.Span, output.Span, out int consumed, out int outputByteCount);
                _lastHeader.PayloadLength -= consumed;
                _inflateBuffer = _inflateBuffer.Slice(consumed);

                if (_inflateBuffer.IsEmpty)
                {
                    ReturnInflateBuffer();

                    if (_lastHeader.PayloadLength == 0 && _lastHeader.Fin)
                    {
                        _inflateFinished = _inflater.Finish(output.Span.Slice(outputByteCount), out var written);
                        outputByteCount += written;
                    }
                }

                return outputByteCount;
            }

            private async ValueTask<bool> ReceiveHeaderAsync(CancellationToken cancellationToken)
            {
                Debug.Assert(_lastHeader.PayloadLength == 0);

                _receivedMaskOffset = 0;

                while (true)
                {
                    if (TryParseMessageHeader(_readBuffer.AvailableSpan, _lastHeader, _isServer,
                        out MessageHeader header, out string? error, out int consumedBytes))
                    {
                        if (header.Compressed && _inflater is null)
                        {
                            _headerError = SR.net_Websockets_PerMessageCompressedFlagWhenNotEnabled;
                            return false;
                        }

                        // If this is a continuation, replace the opcode with the one of the message it's continuing
                        if (header.Opcode == MessageOpcode.Continuation)
                        {
                            header.Opcode = _lastHeader.Opcode;
                            header.Compressed = _lastHeader.Compressed;
                        }

                        _lastHeader = header;
                        _readBuffer.Consume(consumedBytes);

                        if (_isServer)
                        {
                            // Unmask any payload that we've received
                            if (header.PayloadLength > 0 && _readBuffer.AvailableLength > 0)
                            {
                                ApplyMask(_readBuffer.AvailableSpan.Slice(0, (int)Math.Min(_readBuffer.AvailableLength, header.PayloadLength)));
                            }
                        }

                        break;
                    }
                    else if (error is not null)
                    {
                        _headerError = error;
                        return false;
                    }

                    // More data is neeed to parse the header
                    int byteCount = await _stream.ReadAsync(_readBuffer.FreeMemory, cancellationToken).ConfigureAwait(false);
                    if (byteCount <= 0)
                    {
                        return false;
                    }
                    _readBuffer.Commit(byteCount);
                }

                return true;
            }

            private ReceiveResult Result(int count) => new ReceiveResult
            {
                Count = count,
                ResultType = ReceiveResultType.Message,
                MessageType = _lastHeader.Opcode == MessageOpcode.Text ? WebSocketMessageType.Text : WebSocketMessageType.Binary,
                EndOfMessage = _lastHeader.Fin && _lastHeader.PayloadLength == 0 && _inflateFinished
            };

            private ReceiveResult Result(ReceiveResultType resultType) => new ReceiveResult
            {
                ResultType = resultType
            };

            private void ApplyMask(Span<byte> input)
            {
                if (_isServer)
                {
                    _receivedMaskOffset = ManagedWebSocket.ApplyMask(input, _lastHeader.Mask, _receivedMaskOffset);
                }
            }

            /// <summary>
            /// Tries to consume anything remaining in _readBuffer for the current message.s
            /// </summary>
            /// <returns>
            /// True when the read buffer is consumed and there's more to be processed,
            /// and the output buffer is not full.
            /// </returns>
            private bool ConsumeReadBuffer(Span<byte> output, out int outputByteCount)
            {
                Debug.Assert(_readBuffer.AvailableLength > 0);

                int consumed, written;
                int available = (int)Math.Min(_readBuffer.AvailableLength, _lastHeader.PayloadLength);

                if (_lastHeader.Compressed)
                {
                    Debug.Assert(_inflater is not null);
                    _inflater.Inflate(input: _readBuffer.AvailableSpan.Slice(0, available),
                                      output, out consumed, out written);
                }
                else
                {
                    // We can copy directly to output
                    written = Math.Min(available, output.Length);
                    consumed = written;
                    _readBuffer.AvailableSpan.Slice(0, written).CopyTo(output);
                }

                _readBuffer.Consume(consumed);
                _lastHeader.PayloadLength -= consumed;

                outputByteCount = written;

                if (_lastHeader.PayloadLength == 0 || output.Length == written)
                {
                    // We have either consumed everything or the output is full.
                    // In this case we try to finish inflating if needed and return.
                    if (_inflater is not null && _lastHeader.Compressed && _lastHeader.PayloadLength == 0 && _lastHeader.Fin)
                    {
                        _inflateFinished = _inflater.Finish(output.Slice(written), out written);
                        outputByteCount += written;
                    }

                    return false;
                }

                return true;
            }

            private async ValueTask<bool> LoadInflateBufferAsync(CancellationToken cancellationToken)
            {
                // Rent a buffer but restrict it's max size to 1MB
                int decoderBufferLength = (int)Math.Min(_lastHeader.PayloadLength, 1_000_000);

                _inflateBuffer = ArrayPool<byte>.Shared.Rent(decoderBufferLength);
                int byteCount = await _stream.ReadAsync(_inflateBuffer, cancellationToken).ConfigureAwait(false);

                if (byteCount <= 0)
                {
                    ReturnInflateBuffer();
                    return false;
                }

                _inflateBuffer = _inflateBuffer.Slice(0, byteCount);
                ApplyMask(_inflateBuffer.Span);

                return true;
            }

            private void ReturnInflateBuffer()
            {
                if (MemoryMarshal.TryGetArray(_inflateBuffer, out ArraySegment<byte> arraySegment)
                    && arraySegment.Array!.Length > 0)
                {
                    Debug.Assert(arraySegment.Array is not null);
                    ArrayPool<byte>.Shared.Return(arraySegment.Array);
                    _inflateBuffer = null;
                }
            }

            [StructLayout(LayoutKind.Auto)]
            private struct Buffer
            {
                private readonly byte[] _bytes;
                private int _position;
                private int _consumed;

                public Buffer(int capacity)
                {
                    _bytes = GC.AllocateUninitializedArray<byte>(capacity, pinned: true);
                    _position = 0;
                    _consumed = 0;
                }

                public int AvailableLength => _position - _consumed;

                public Span<byte> AvailableSpan =>
                    new Span<byte>(_bytes, start: _consumed, length: _position - _consumed);

                public Memory<byte> AvailableMemory =>
                    new Memory<byte>(_bytes, start: _consumed, length: _position - _consumed);

                public Memory<byte> FreeMemory => _bytes.AsMemory(_position);

                public int FreeLength => _bytes.Length - _position;

                public void Commit(int count) => _position += count;

                public void Consume(int count) => _consumed += count;

                public void DiscardConsumed()
                {
                    if (AvailableLength > 0)
                    {
                        AvailableMemory.CopyTo(_bytes);
                    }

                    _position -= _consumed;
                    _consumed = 0;
                }
            }
        }
    }
}
