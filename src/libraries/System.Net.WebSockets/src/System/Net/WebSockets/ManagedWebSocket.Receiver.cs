// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.WebSockets
{
    internal partial class ManagedWebSocket
    {
        private const int ReceivedConnectionClose = -1;
        private const int ReceivedControlMessage = -2;
        private const int ReceivedHeaderError = -3;

        private sealed class Receiver : IDisposable
        {
            private readonly bool _isServer;
            private readonly Stream _stream;
            private readonly Decoder? _decoder;

            /// <summary>
            /// If we have a decoder we cannot use the buffer provided from clients because
            /// we cannot guarantee that the decoding can happen in place. This buffer is rent'ed
            /// and returned when consumed.
            /// </summary>
            private byte[]? _decoderBuffer;

            /// <summary>
            /// The next index that needs to be consumed from the decoder's buffer.
            /// </summary>
            private int _decoderBufferPosition;

            /// <summary>
            /// The number of usable bytes in the decoder's buffer.
            /// </summary>
            private int _decoderBufferCount;

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

            public Receiver(Stream stream, WebSocketCreationOptions options)
            {
                _stream = stream;
                _isServer = options.IsServer;

                // Create a buffer just large enough to handle received packet headers (at most 14 bytes) and
                // control payloads (at most 125 bytes). Message payloads are read directly into the buffer
                // supplied to ReceiveAsync.
                _readBuffer = new(MaxControlPayloadLength + MaxMessageHeaderLength);

                var deflate = options.DeflateOptions;

                if (deflate is not null)
                {
                    // Important note here is that we must use negative window bits
                    // which will instruct the underlying implementation to not expect deflate headers
                    if (options.IsServer)
                    {
                        _decoder = deflate.ServerContextTakeover ?
                            new PersistedInflater(-deflate.ServerMaxWindowBits) :
                            new Inflater(-deflate.ServerMaxWindowBits);
                    }
                    else
                    {
                        _decoder = deflate.ClientContextTakeover ?
                            new PersistedInflater(-deflate.ClientMaxWindowBits) :
                            new Inflater(-deflate.ClientMaxWindowBits);
                    }
                }
            }

            public void Dispose()
            {
                _decoder?.Dispose();

                if (_decoderBuffer is not null)
                {
                    ArrayPool<byte>.Shared.Return(_decoderBuffer);
                    _decoderBuffer = null;
                }
            }

            public MessageHeader GetLastHeader() => _lastHeader;

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
                    return new ControlMessage(_lastHeader.Opcode, ReadOnlyMemory<byte>.Empty);

                _readBuffer.DiscardConsumed();

                while (_lastHeader.PayloadLength > _readBuffer.AvailableLength)
                {
                    var byteCount = await _stream.ReadAsync(_readBuffer.FreeMemory, cancellationToken).ConfigureAwait(false);
                    if (byteCount <= 0)
                        return null;

                    ApplyMask(_readBuffer.FreeMemory.Span.Slice(0, (int)Math.Min(_lastHeader.PayloadLength, byteCount)));
                    _readBuffer.Commit(byteCount);
                }

                // Update the payload length in the header to indicate
                // that we've received everything we need.
                var payload = _readBuffer.Consume((int)_lastHeader.PayloadLength);
                _lastHeader.PayloadLength = 0;

                return new ControlMessage(_lastHeader.Opcode, payload);
            }

            public async ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
            {
                _readBuffer.DiscardConsumed();

                // When there's nothing left over to receive, start a new
                if (_lastHeader.PayloadLength == 0)
                {
                    var success = await ReceiveHeaderAsync(cancellationToken).ConfigureAwait(false);

                    if (!success)
                        return ReceivedConnectionClose;

                    if (_lastHeader.Error is not null)
                        return ReceivedHeaderError;

                    if (_lastHeader.Opcode > MessageOpcode.Binary)
                    {
                        // The received message is a control message and it's up
                        // to the websocket how to handle it.
                        return ReceivedControlMessage;
                    }
                }

                if (buffer.IsEmpty)
                    return 0;

                // The number of bytes that are copied onto the provided buffer
                var resultByteCount = 0;

                if (_readBuffer.AvailableLength > 0)
                {
                    int consumed, written;
                    int available = (int)Math.Min(_readBuffer.AvailableLength, _lastHeader.PayloadLength);

                    if (_decoder is not null && _decoder.IsNeeded(_lastHeader))
                    {
                        _decoder.Decode(input: _readBuffer.AvailableSpan.Slice(0, available),
                                        output: buffer.Span, out consumed, out written);
                    }
                    else
                    {
                        written = Math.Min(available, buffer.Length);
                        consumed = written;
                        _readBuffer.AvailableSpan.Slice(0, written).CopyTo(buffer.Span);
                    }

                    _readBuffer.Consume(consumed);
                    _lastHeader.PayloadLength -= consumed;

                    if (_lastHeader.PayloadLength == 0 || _readBuffer.AvailableLength > 0)
                    {
                        // If the payload length is 0 it means that we have consumed everything.
                        // Otherwise if available length is still non zero, than it means that the
                        // decoder needs more memory and the operation cannot continue.
                        return written;
                    }

                    resultByteCount += written;
                    buffer = buffer.Slice(written);
                }

                // At this point we should have consumed everything from the buffer
                // and should start issuing reads on the stream.
                Debug.Assert(_readBuffer.AvailableLength == 0 && _lastHeader.PayloadLength > 0);

                if (_decoder is null || !_decoder.IsNeeded(_lastHeader))
                {
                    if (buffer.Length > _lastHeader.PayloadLength)
                    {
                        // We don't want to receive more that we need
                        buffer = buffer.Slice(0, (int)_lastHeader.PayloadLength);
                    }

                    var bytesRead = await _stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                    if (bytesRead <= 0)
                        return ReceivedConnectionClose;

                    resultByteCount += bytesRead;
                    ApplyMask(buffer.Span.Slice(0, bytesRead));
                }
                else
                {
                    if (_decoderBuffer is null)
                    {
                        // Rent a buffer but restrict it's max size to 1MB
                        _decoderBuffer = ArrayPool<byte>.Shared.Rent((int)Math.Min(_lastHeader.PayloadLength, 1_000_000));
                        _decoderBufferCount = await _stream.ReadAsync(_decoderBuffer, cancellationToken).ConfigureAwait(false);
                        if (_decoderBufferCount <= 0)
                        {
                            ArrayPool<byte>.Shared.Return(_decoderBuffer);
                            return ReceivedConnectionClose;
                        }

                        ApplyMask(_decoderBuffer.AsSpan(_decoderBufferPosition, _decoderBufferCount));
                    }

                    // There is lefover data that we need to decode
                    _decoder.Decode(input: _decoderBuffer.AsSpan(_decoderBufferPosition, _decoderBufferCount),
                                     output: buffer.Span, out var consumed, out var written);

                    resultByteCount += written;
                    _decoderBufferPosition += consumed;
                    _decoderBufferCount -= consumed;
                    _lastHeader.PayloadLength -= consumed;

                    if (_decoderBufferCount == 0)
                    {
                        ArrayPool<byte>.Shared.Return(_decoderBuffer);
                        _decoderBuffer = null;
                        _decoderBufferPosition = 0;
                    }
                }

                return resultByteCount;
            }

            private async ValueTask<bool> ReceiveHeaderAsync(CancellationToken cancellationToken)
            {
                Debug.Assert(_lastHeader.PayloadLength == 0);

                _receivedMaskOffset = 0;

                while (true)
                {
                    if (TryParseMessageHeader(_readBuffer.AvailableSpan, _lastHeader, _isServer, out var header, out var consumedBytes))
                    {
                        // If this is a continuation, replace the opcode with the one of the message it's continuing
                        if (header.Opcode == MessageOpcode.Continuation)
                        {
                            header.Opcode = _lastHeader.Opcode;
                            header.Compressed = _lastHeader.Compressed;
                        }
                        else
                        {
                            _decoder?.Reset();
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

                    // More data is neeed to parse the header
                    var byteCount = await _stream.ReadAsync(_readBuffer.FreeMemory, cancellationToken).ConfigureAwait(false);
                    if (byteCount <= 0)
                        return false;

                    _readBuffer.Commit(byteCount);
                }

                return true;
            }

            private void ApplyMask(Span<byte> input)
            {
                if (_isServer)
                {
                    _receivedMaskOffset = ManagedWebSocket.ApplyMask(input, _lastHeader.Mask, _receivedMaskOffset);
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
                    _bytes = new byte[capacity];
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

                public void Commit(int count)
                {
                    _position += count;
                }

                public Memory<byte> Consume(int count)
                {
                    var memory = new Memory<byte>(_bytes, _consumed, count);
                    _consumed += count;

                    return memory;
                }

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

            private abstract class Decoder : IDisposable
            {
                public abstract bool IsNeeded(MessageHeader header);

                public abstract void Dispose();

                /// <summary>
                /// Resets the decoder state after fully processing a message.
                /// </summary>
                public abstract void Reset();

                public abstract void Decode(ReadOnlySpan<byte> input, Span<byte> output, out int consumed, out int written);
            }

            private class Inflater : Decoder
            {
                private readonly int _windowBits;

                // Although the inflater isn't persisted accross messages, a single message
                // might have been split into multiple frames.
                private IO.Compression.Inflater? _inflater;

                public Inflater(int windowBits) => _windowBits = windowBits;

                public override bool IsNeeded(MessageHeader header) => header.Compressed;

                public override void Dispose() => _inflater?.Dispose();

                public override void Reset()
                {
                    _inflater?.Dispose();
                    _inflater = null;
                }

                public override void Decode(ReadOnlySpan<byte> input, Span<byte> output, out int consumed, out int written)
                {
                    _inflater ??= new IO.Compression.Inflater(_windowBits);
                    _inflater.Inflate(input, output, out consumed, out written);
                }
            }

            private sealed class PersistedInflater : Decoder
            {
                private readonly IO.Compression.Inflater _inflater;

                public PersistedInflater(int windowBits) => _inflater = new(windowBits);

                public override bool IsNeeded(MessageHeader header) => header.Compressed;

                public override void Dispose() => _inflater.Dispose();

                public override void Reset() { }

                public override void Decode(ReadOnlySpan<byte> input, Span<byte> output, out int consumed, out int written)
                {
                    _inflater.Inflate(input, output, out consumed, out written);
                }
            }
        }
    }
}
