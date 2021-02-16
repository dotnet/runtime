// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.WebSockets
{
    internal partial class ManagedWebSocket
    {
        private sealed class Sender : IDisposable
        {
            private const byte PerMessageDeflateBit = 0b0100_0000;

            private readonly int _maskLength;
            private readonly Encoder? _encoder;
            private readonly Stream _stream;

            public Sender(Stream stream, WebSocketCreationOptions options)
            {
                _maskLength = options.IsServer ? 0 : MaskLength;
                _stream = stream;

                var deflate = options.DeflateOptions;

                if (deflate is not null)
                {
                    // Important note here is that we must use negative window bits
                    // which will instruct the underlying implementation to not emit deflate headers
                    if (options.IsServer)
                    {
                        _encoder = deflate.ServerContextTakeover ?
                            new PersistedDeflater(-deflate.ServerMaxWindowBits) :
                            new Deflater(-deflate.ServerMaxWindowBits);
                    }
                    else
                    {
                        _encoder = deflate.ClientContextTakeover ?
                            new PersistedDeflater(-deflate.ClientMaxWindowBits) :
                            new Deflater(-deflate.ClientMaxWindowBits);
                    }
                }
            }

            public void Dispose() => _encoder?.Dispose();

            public ValueTask SendAsync(MessageOpcode opcode, bool endOfMessage, ReadOnlyMemory<byte> content, CancellationToken cancellationToken = default)
            {
                var buffer = new Buffer(content.Length + MaxMessageHeaderLength);
                byte reservedBits = 0;

                // Reserve space for the frame header
                buffer.Advance(MaxMessageHeaderLength);

                // Encoding is onlt supported for user messages
                if (_encoder is not null && opcode <= MessageOpcode.Continuation)
                {
                    _encoder.Encode(content.Span, ref buffer, continuation: opcode == MessageOpcode.Continuation, endOfMessage, out reservedBits);
                }
                else if (content.Length > 0)
                {
                    content.Span.CopyTo(buffer.GetSpan(content.Length));
                    buffer.Advance(content.Length);
                }

                var payload = buffer.WrittenSpan.Slice(MaxMessageHeaderLength);
                var headerLength = CalculateHeaderLength(payload.Length);

                // Because we want the header to come just before to the payload
                // we will use a slice that offsets the unused part.
                var headerOffset = MaxMessageHeaderLength - headerLength;
                var header = buffer.WrittenSpan.Slice(headerOffset, headerLength);

                // Write the message header data to the buffer.
                EncodeHeader(header, opcode, endOfMessage, payload.Length, reservedBits);

                // If we added a mask to the header, XOR the payload with the mask.
                if (payload.Length > 0 && _maskLength > 0)
                {
                    ApplyMask(payload, BitConverter.ToInt32(header.Slice(header.Length - MaskLength)), 0);
                }

                var releaseArray = true;

                try
                {
                    var sendTask = _stream.WriteAsync(new ReadOnlyMemory<byte>(buffer.Array, headerOffset, headerLength + payload.Length), cancellationToken);

                    if (sendTask.IsCompleted)
                        return sendTask;

                    releaseArray = false;
                    return WaitAsync(sendTask.AsTask(), buffer.Array);
                }
                finally
                {
                    if (releaseArray)
                        ArrayPool<byte>.Shared.Return(buffer.Array);
                }
            }

            private static async ValueTask WaitAsync(Task sendTask, byte[] buffer)
            {
                try
                {
                    await sendTask.ConfigureAwait(false);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }

            private int CalculateHeaderLength(int payloadLength) => payloadLength switch
            {
                <= 125 => 2,
                <= ushort.MaxValue => 4,
                _ => 10
            } + _maskLength;

            private void EncodeHeader(Span<byte> header, MessageOpcode opcode, bool endOfMessage, int payloadLength, byte reservedBits)
            {
                // The current implementation only supports per message deflate extension. In the future
                // if more extensions are implemented or we allow third party extensions this assert must be changed.
                Debug.Assert((reservedBits | 0b0100_0000) == 0b0100_0000);

                // Client header format:
                // 1 bit - FIN - 1 if this is the final fragment in the message (it could be the only fragment), otherwise 0
                // 1 bit - RSV1 - Per-Message Deflate Compress
                // 1 bit - RSV2 - Reserved - 0
                // 1 bit - RSV3 - Reserved - 0
                // 4 bits - Opcode - How to interpret the payload
                //     - 0x0 - continuation
                //     - 0x1 - text
                //     - 0x2 - binary
                //     - 0x8 - connection close
                //     - 0x9 - ping
                //     - 0xA - pong
                //     - (0x3 to 0x7, 0xB-0xF - reserved)
                // 1 bit - Masked - 1 if the payload is masked, 0 if it's not.  Must be 1 for the client
                // 7 bits, 7+16 bits, or 7+64 bits - Payload length
                //     - For length 0 through 125, 7 bits storing the length
                //     - For lengths 126 through 2^16, 7 bits storing the value 126, followed by 16 bits storing the length
                //     - For lengths 2^16+1 through 2^64, 7 bits storing the value 127, followed by 64 bytes storing the length
                // 0 or 4 bytes - Mask, if Masked is 1 - random value XOR'd with each 4 bytes of the payload, round-robin
                // Length bytes - Payload data
                header[0] = (byte)opcode; // 4 bits for the opcode
                header[0] |= reservedBits;

                if (endOfMessage)
                {
                    header[0] |= 0b1000_0000; // 1 bit for FIN
                }

                // Store the payload length.
                if (payloadLength <= 125)
                {
                    header[1] = (byte)payloadLength;
                }
                else if (payloadLength <= ushort.MaxValue)
                {
                    header[1] = 126;
                    header[2] = (byte)(payloadLength / 256);
                    header[3] = unchecked((byte)payloadLength);
                }
                else
                {
                    header[1] = 127;
                    for (int i = 9; i >= 2; i--)
                    {
                        header[i] = unchecked((byte)payloadLength);
                        payloadLength = payloadLength / 256;
                    }
                }

                if (_maskLength > 0)
                {
                    // Generate the mask.
                    header[1] |= 0x80;
                    RandomNumberGenerator.Fill(header.Slice(header.Length - _maskLength));
                }
            }

            /// <summary>
            /// Helper class which allows writing to a rent'ed byte array
            /// and auto-grow functionality.
            /// </summary>
            internal ref struct Buffer
            {
                private byte[] _array;
                private int _index;

                public Buffer(int capacity)
                {
                    _array = ArrayPool<byte>.Shared.Rent(capacity);
                    _index = 0;
                }

                public Span<byte> WrittenSpan => new Span<byte>(_array, 0, _index);

                public byte[] Array => _array;

                public int FreeCapacity => _array.Length - _index;

                public void Advance(int count)
                {
                    _index += count;

                    Debug.Assert(_index >= 0 || _index < _array.Length);
                }

                public Span<byte> GetSpan(int sizeHint = 0)
                {
                    if (sizeHint == 0)
                        sizeHint = 1;

                    if (sizeHint > FreeCapacity)
                    {
                        var newArray = ArrayPool<byte>.Shared.Rent(_array.Length + sizeHint);
                        _array.AsSpan().CopyTo(newArray);

                        ArrayPool<byte>.Shared.Return(_array);
                        _array = newArray;
                    }

                    return _array.AsSpan(_index);
                }
            }

            private abstract class Encoder : IDisposable
            {
                public abstract void Dispose();

                internal abstract void Encode(ReadOnlySpan<byte> payload, ref Buffer buffer, bool continuation, bool endOfMessage, out byte reservedBits);
            }

            /// <summary>
            /// Deflate encoder which doesn't persist the deflator accross messages.
            /// </summary>
            private class Deflater : Encoder
            {
                private readonly int _windowBits;

                // Although the inflater isn't persisted accross messages, a single message
                // might be split into multiple frames.
                private IO.Compression.Deflater? _deflater;

                public Deflater(int windowBits) => _windowBits = windowBits;

                public override void Dispose() => _deflater?.Dispose();

                internal override void Encode(ReadOnlySpan<byte> payload, ref Buffer buffer, bool continuation, bool endOfMessage, out byte reservedBits)
                {
                    Debug.Assert((continuation && _deflater is not null) || (!continuation && _deflater is null),
                        "Invalid state. The deflater was expected to be null if not continuation and not null otherwise.");

                    _deflater ??= new IO.Compression.Deflater(_windowBits);

                    Encode(payload, ref buffer, _deflater);
                    reservedBits = continuation ? 0 : PerMessageDeflateBit;

                    if (endOfMessage)
                    {
                        _deflater.Dispose();
                        _deflater = null;
                    }
                }

                public static void Encode(ReadOnlySpan<byte> payload, ref Buffer buffer, IO.Compression.Deflater deflater)
                {
                    while (payload.Length > 0)
                    {
                        deflater.Deflate(payload, buffer.GetSpan(payload.Length), out var consumed, out var written);
                        buffer.Advance(written);

                        payload = payload.Slice(consumed);
                    }

                    while (true)
                    {
                        var bytesWritten = deflater.Finish(buffer.GetSpan(), out var completed);
                        buffer.Advance(bytesWritten);

                        if (completed)
                            break;
                    }

                    // The deflated block always ends with 0x00 0x00 0xFF 0xFF but the websocket protocol doesn't want it.
                    buffer.Advance(-4);
                }
            }

            /// <summary>
            /// Deflate encoder which persists the deflator state accross messages.
            /// </summary>
            private sealed class PersistedDeflater : Encoder
            {
                private readonly IO.Compression.Deflater _deflater;

                public PersistedDeflater(int windowBits) => _deflater = new(windowBits);

                public override void Dispose() => _deflater.Dispose();

                internal override void Encode(ReadOnlySpan<byte> payload, ref Buffer buffer, bool continuation, bool endOfMessage, out byte reservedBits)
                {
                    Deflater.Encode(payload, ref buffer, _deflater);
                    reservedBits = continuation ? 0 : PerMessageDeflateBit;
                }
            }
        }
    }
}
