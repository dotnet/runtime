// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.WebSockets
{
    internal partial class ManagedWebSocket
    {
        private sealed class Sender : IDisposable
        {
            public const int MaxSegmentCapacity = 1024 * 1024; // 1MB

            private readonly Stream _stream;

            /// <summary>
            /// Available buffers to use when encoding messages. We use array because
            /// the segment is a struct and we want amortized zero allocations if possible.
            /// </summary>
            private BufferSegment[] _segments = new BufferSegment[] { BufferSegment.Empty };

            /// <summary>
            /// The current segment index.
            /// </summary>
            private int _segmentIndex;

            /// <summary>
            /// The encoded payload length that is about to be sent.
            /// </summary>
            private int _payloadLength;

            private readonly bool _applyMask;
            private int _mask;

            /// <summary>
            /// We apply mask automatically each time Advance() is called and so we can
            /// potentionally have multiple blocks. This value is the next offset into the mask that should be applied.
            /// </summary>
            private int _maskIndex;

            public Sender(Stream stream, bool isServer)
            {
                _applyMask = !isServer;
                _stream = stream;
            }

            public void Dispose()
            {
                Reset();
            }

            public ValueTask SendAsync(MessageOpcode opcode, bool endOfMessage, ReadOnlySpan<byte> payload, CancellationToken cancellationToken)
            {
                Initialize(payload.Length);

                while (!payload.IsEmpty)
                {
                    var span = GetSpan(sizeHint: 1);
                    var length = Math.Min(payload.Length, span.Length);

                    payload[0..length].CopyTo(span);
                    payload = payload[length..];

                    Advance(length);
                }

                Finalize(opcode, endOfMessage);
                bool reset = true;

                try
                {
                    ValueTask sendTask = _segmentIndex == 0 ?
                        _stream.WriteAsync(_segments[0].WrittenMemory, cancellationToken) :
                        WriteSegmentsAsync(cancellationToken);

                    if (sendTask.IsCompleted)
                    {
                        return sendTask;
                    }
                    reset = false;
                    return WaitAsync(sendTask);
                }
                finally
                {
                    if (reset)
                    {
                        Reset();
                    }
                }
            }

            private async ValueTask WriteSegmentsAsync(CancellationToken cancellationToken)
            {
                for (var index = 0; index <= _segmentIndex; ++index)
                {
                    await _stream.WriteAsync(_segments[index].WrittenMemory, cancellationToken).ConfigureAwait(false);

                    // Release the memory used by the segment as soon as we've sent it
                    _segments[index].Dispose();
                }
            }

            private void Initialize(int payloadSizeHint)
            {
                Debug.Assert(_segmentIndex == 0);

                if (_applyMask)
                {
                    Span<int> mask = stackalloc int[1];
                    RandomNumberGenerator.Fill(MemoryMarshal.AsBytes(mask));
                    _mask = mask[0];
                    _maskIndex = 0;
                }

                ref BufferSegment segment = ref _segments[0];
                segment = new BufferSegment(Math.Min(MaxSegmentCapacity, payloadSizeHint + MaxMessageHeaderLength));
                segment.Advance(MaxMessageHeaderLength);

                _payloadLength = 0;
            }

            private void Finalize(MessageOpcode opcode, bool endOfMessage)
            {
                ref BufferSegment segment = ref _segments[0];

                // Calculate the header's length
                var headerLength = _payloadLength switch
                {
                    <= 125 => 2,
                    <= ushort.MaxValue => 4,
                    _ => 10
                };
                if (_applyMask)
                {
                    headerLength += MaskLength;
                }

                // Because we want the header to come just before to the payload
                // we will use a slice that offsets the unused part.
                segment.Offset = MaxMessageHeaderLength - headerLength;

                // Write the message header data to the buffer.
                EncodeHeader(segment.WrittenSpan[0..headerLength], opcode, endOfMessage);
            }

            private void EncodeHeader(Span<byte> header, MessageOpcode opcode, bool endOfMessage)
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

                if (endOfMessage)
                {
                    header[0] |= 0b1000_0000; // 1 bit for FIN
                }

                // Store the payload length.
                int payloadLength = _payloadLength;
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

                if (_applyMask)
                {
                    header[1] |= 0x80;
                    BitConverter.TryWriteBytes(header.Slice(header.Length - MaskLength), _mask);
                }
            }

            private void Reset()
            {
                while (_segmentIndex > 0)
                {
                    _segments[_segmentIndex].Dispose();
                    _segmentIndex -= 1;
                }

                _segments[0].Dispose();
                _payloadLength = 0;
                _maskIndex = 0;
                _mask = 0;
            }

            private async ValueTask WaitAsync(ValueTask sendTask)
            {
                try
                {
                    await sendTask.ConfigureAwait(false);
                }
                finally
                {
                    Reset();
                }
            }

            internal void Advance(int count)
            {
                ref BufferSegment segment = ref _segments[_segmentIndex];

                if (_applyMask)
                {
                    _maskIndex = ApplyMask(segment.AvailableSpan[0..count], _mask, _maskIndex);
                }

                segment.Advance(count);
                _payloadLength += count;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Span<byte> GetSpan(int sizeHint)
            {
                Debug.Assert(sizeHint > 0);
                var span = _segments[_segmentIndex].AvailableSpan;

                if (span.Length < sizeHint)
                {
                    span = AllocateBufferSegment(sizeHint);
                }

                return span;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private Span<byte> AllocateBufferSegment(int sizeHint)
            {
                // When allocating new segment, try to keep the capacity the same as the previous segment
                var newSegment = new BufferSegment(Math.Max(_segments[_segmentIndex].Capacity, sizeHint));

                if (++_segmentIndex == _segments.Length)
                {
                    Array.Resize(ref _segments, _segments.Length + 1);
                }

                _segments[_segmentIndex] = newSegment;
                return newSegment.AvailableSpan;
            }

            [StructLayout(LayoutKind.Auto)]
            private struct BufferSegment
            {
                public static readonly BufferSegment Empty = new BufferSegment
                {
                    _array = Array.Empty<byte>()
                };

                private byte[] _array;
                private int _index;

                public BufferSegment(int capacity)
                {
                    _array = ArrayPool<byte>.Shared.Rent(capacity);
                    _index = 0;

                    Offset = 0;
                }

                public int Capacity => _array.Length;

                public int Offset { get; set; }

                public Span<byte> AvailableSpan
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get => _array.AsSpan(_index);
                }

                public Span<byte> WrittenSpan => _array.AsSpan(Offset, _index - Offset);

                public ReadOnlyMemory<byte> WrittenMemory => new ReadOnlyMemory<byte>(_array, Offset, _index - Offset);

                public void Dispose()
                {
                    if (_array.Length > 0)
                    {
                        ArrayPool<byte>.Shared.Return(_array);
                        _array = Array.Empty<byte>();
                    }
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public void Advance(int count)
                {
                    Debug.Assert(_array is not null);
                    Debug.Assert(count >= 0);
                    Debug.Assert(_index + count <= _array.Length);

                    _index += count;
                }
            }
        }
    }
}
