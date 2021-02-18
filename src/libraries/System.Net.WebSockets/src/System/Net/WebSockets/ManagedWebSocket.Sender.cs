// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.WebSockets.Compression;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.WebSockets
{
    internal partial class ManagedWebSocket
    {
        private sealed class Sender : IDisposable
        {
            private readonly int _maskLength;
            private readonly WebSocketDeflater? _deflater;
            private readonly Stream _stream;

            private readonly Buffer _buffer = new();

            public Sender(Stream stream, WebSocketCreationOptions options)
            {
                _maskLength = options.IsServer ? 0 : MaskLength;
                _stream = stream;

                var deflate = options.DeflateOptions;

                if (deflate is not null)
                {
                    // If we are the server we must use the client options
                    _deflater = options.IsServer ?
                        new WebSocketDeflater(deflate.ClientMaxWindowBits, deflate.ClientContextTakeover) :
                        new WebSocketDeflater(deflate.ServerMaxWindowBits, deflate.ServerContextTakeover);
                }
            }

            public void Dispose() => _deflater?.Dispose();

            public ValueTask SendAsync(MessageOpcode opcode, bool endOfMessage, ReadOnlyMemory<byte> content, CancellationToken cancellationToken = default)
            {
                bool compressed = false;

                // Encoding is onlt supported for user messages
                if (_deflater is not null && opcode <= MessageOpcode.Binary)
                {
                    _buffer.EnsureFreeCapacity(MaxMessageHeaderLength + (int)(content.Length * 0.6));
                    _buffer.Advance(MaxMessageHeaderLength);

                    _deflater.Deflate(content.Span, _buffer, continuation: opcode == MessageOpcode.Continuation, endOfMessage);
                    compressed = true;
                }
                else if (content.Length > 0)
                {
                    _buffer.EnsureFreeCapacity(MaxMessageHeaderLength + content.Length);
                    _buffer.Advance(MaxMessageHeaderLength);

                    content.Span.CopyTo(_buffer.GetSpan(content.Length));
                    _buffer.Advance(content.Length);
                }

                var payload = _buffer.WrittenSpan.Slice(MaxMessageHeaderLength);
                var headerLength = CalculateHeaderLength(payload.Length);

                // Because we want the header to come just before to the payload
                // we will use a slice that offsets the unused part.
                var headerOffset = MaxMessageHeaderLength - headerLength;
                var header = _buffer.WrittenSpan.Slice(headerOffset, headerLength);

                // Write the message header data to the buffer.
                EncodeHeader(header, opcode, endOfMessage, payload.Length, compressed);

                // If we added a mask to the header, XOR the payload with the mask.
                if (payload.Length > 0 && _maskLength > 0)
                {
                    ApplyMask(payload, BitConverter.ToInt32(header.Slice(header.Length - MaskLength)), 0);
                }

                var resetBuffer = true;

                try
                {
                    var sendTask = _stream.WriteAsync(_buffer.WrittenMemory.Slice(headerOffset), cancellationToken);

                    if (sendTask.IsCompleted)
                        return sendTask;

                    resetBuffer = false;
                    return WaitAsync(sendTask);
                }
                finally
                {
                    if (resetBuffer)
                        _buffer.Reset();
                }
            }

            private async ValueTask WaitAsync(ValueTask sendTask)
            {
                try
                {
                    await sendTask.ConfigureAwait(false);
                }
                finally
                {
                    _buffer.Reset();
                }
            }

            private int CalculateHeaderLength(int payloadLength) => _maskLength + (payloadLength switch
            {
                <= 125 => 2,
                <= ushort.MaxValue => 4,
                _ => 10
            });

            private void EncodeHeader(Span<byte> header, MessageOpcode opcode, bool endOfMessage, int payloadLength, bool compressed)
            {
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

                if (compressed && opcode != MessageOpcode.Continuation)
                {
                    header[0] |= 0b0100_0000;
                }

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
                    RandomNumberGenerator.Fill(header.Slice(header.Length - MaskLength));
                }
            }

            /// <summary>
            /// Helper class which allows writing to a rent'ed byte array
            /// and auto-grow functionality.
            /// </summary>
            private sealed class Buffer : IBufferWriter<byte>
            {
                private readonly ArrayPool<byte> _arrayPool;

                private byte[]? _array;
                private int _index;

                public Buffer()
                {
                    _arrayPool = ArrayPool<byte>.Shared;
                }

                public Span<byte> WrittenSpan => new Span<byte>(_array, 0, _index);

                public ReadOnlyMemory<byte> WrittenMemory => new ReadOnlyMemory<byte>(_array, 0, _index);

                public void Advance(int count)
                {
                    Debug.Assert(_array is not null);
                    Debug.Assert(count >= 0);
                    Debug.Assert(_index + count <= _array.Length);

                    _index += count;
                }

                public Memory<byte> GetMemory(int sizeHint = 0)
                {
                    EnsureFreeCapacity(sizeHint);
                    return _array.AsMemory(_index);
                }

                public Span<byte> GetSpan(int sizeHint = 0)
                {
                    EnsureFreeCapacity(sizeHint);
                    return _array.AsSpan(_index);
                }

                public void Reset()
                {
                    if (_array is not null)
                    {
                        _arrayPool.Return(_array);
                        _array = null;
                        _index = 0;
                    }
                }

                [MemberNotNull(nameof(_array))]
                public void EnsureFreeCapacity(int sizeHint)
                {
                    if (sizeHint == 0)
                        sizeHint = 1;

                    if (_array is null)
                    {
                        _array = _arrayPool.Rent(sizeHint);
                        return;
                    }

                    if (sizeHint > (_array.Length - _index))
                    {
                        var newArray = _arrayPool.Rent(_array.Length + sizeHint);
                        _array.AsSpan().CopyTo(newArray);

                        _arrayPool.Return(_array);
                        _array = newArray;
                    }
                }
            }
        }
    }
}
