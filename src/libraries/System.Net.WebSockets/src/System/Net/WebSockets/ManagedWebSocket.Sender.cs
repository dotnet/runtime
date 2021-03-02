// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace System.Net.WebSockets
{
    internal partial class ManagedWebSocket
    {
        private sealed class Sender : IDisposable, IValueTaskSource
        {
            private const int MaxSegmentCapacity = 1024 * 1024; // 1MB
            private const int MaskQueueCapacity = 8;

            private readonly Stream _stream;

            /// <summary>
            /// The first buffer segment.
            /// </summary>
            private BufferSegment _firstSegment;

            /// <summary>
            /// The buffer segment which is being written to.
            /// </summary>
            private BufferSegment _currentSegment;

            private int _headOffset;

            /// <summary>
            /// The encoded payload length that is about to be sent.
            /// </summary>
            private int _payloadLength;

            private readonly bool _applyMask;

            /// <summary>
            /// Calling into RandomNumberGenerator for just one mask is relatively expensive.
            /// This is why we'll batch generate `MaskQueueCapacity` masks at a time.
            /// </summary>
            private readonly Queue<int>? _maskQueue;
            private int _mask;

            /// <summary>
            /// We apply mask automatically each time Advance() is called and so we can
            /// potentionally have multiple blocks. This value is the next offset into the mask that should be applied.
            /// </summary>
            private int _maskIndex;

            private ManualResetValueTaskSourceCore<bool> _valueTaskSource;
            private ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter _awaiter;
            private readonly Action _onCompleted;

            public Sender(Stream stream, bool isServer)
            {
                _applyMask = !isServer;
                _stream = stream;
                _onCompleted = OnCompleted;
                _firstSegment = _currentSegment = new BufferSegment();

                if (_applyMask)
                {
                    _maskQueue = new Queue<int>(MaskQueueCapacity);
                }
            }

            public void Dispose()
            {
                Reset();
            }

            public ValueTask SendAsync(MessageOpcode opcode, bool endOfMessage, ReadOnlySpan<byte> payload, CancellationToken cancellationToken)
            {
                Initialize(payload.Length);
                EncodeUncompressed(payload);
                Finalize(opcode, endOfMessage);
                bool reset = true;

                try
                {
                    ValueTask sendTask = _firstSegment == _currentSegment ?
                        _stream.WriteAsync(_firstSegment.WrittenMemory.Slice(_headOffset), cancellationToken) :
                        WriteSegmentsAsync(cancellationToken);

                    if (sendTask.IsCompleted)
                    {
                        return sendTask;
                    }

                    reset = false;
                    _valueTaskSource.Reset();
                    _awaiter = sendTask.ConfigureAwait(false).GetAwaiter();
                    _awaiter.UnsafeOnCompleted(_onCompleted);

                    return new ValueTask(this, _valueTaskSource.Version);
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
                BufferSegment segment = _firstSegment;
                await _stream.WriteAsync(segment.WrittenMemory.Slice(_headOffset), cancellationToken).ConfigureAwait(false);

                // Release the memory used by the segment as soon as we've sent it
                segment.Reset();

                while (segment != _currentSegment)
                {
                    segment = segment.Next!;
                    await _stream.WriteAsync(segment.WrittenMemory, cancellationToken).ConfigureAwait(false);

                    // Release the memory used by the segment as soon as we've sent it
                    segment.Reset();
                }
            }

            private void Initialize(int payloadSizeHint)
            {
                Debug.Assert(_firstSegment == _currentSegment);

                if (_applyMask)
                {
                    Debug.Assert(_maskQueue is not null);
                    if (!_maskQueue.TryDequeue(out _mask))
                    {
                        Span<int> masks = stackalloc int[1 + MaskQueueCapacity];
                        RandomNumberGenerator.Fill(MemoryMarshal.AsBytes(masks));
                        _mask = masks[MaskQueueCapacity];

                        for (var i = 0; i < MaskQueueCapacity; ++i)
                        {
                            _maskQueue.Enqueue(masks[i]);
                        }
                    }
                }

                _firstSegment.Initialize(Math.Min(MaxSegmentCapacity, payloadSizeHint + MaxMessageHeaderLength));
                _firstSegment.Advance(MaxMessageHeaderLength);
            }

            private void EncodeUncompressed(ReadOnlySpan<byte> payload)
            {
                if (payload.IsEmpty)
                {
                    return;
                }
                if (payload.Length <= MaxSegmentCapacity - MaxMessageHeaderLength)
                {
                    // The payload can be placed on a sinle segment
                    payload.CopyTo(_firstSegment.AvailableSpan);
                    Advance(payload.Length);
                    return;
                }

                while (!payload.IsEmpty)
                {
                    var span = GetSpan(sizeHint: 1);
                    var length = Math.Min(payload.Length, span.Length);

                    payload[0..length].CopyTo(span);
                    payload = payload[length..];

                    Advance(length);
                }
            }

            private void Finalize(MessageOpcode opcode, bool endOfMessage)
            {
                // Calculate the header's length
                int payloadLength = _payloadLength;
                var headerLength = payloadLength switch
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
                _headOffset = MaxMessageHeaderLength - headerLength;

                // Write the message header data to the buffer.
                var buffer = _firstSegment.Span.Slice(_headOffset, headerLength);

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
                buffer[0] = (byte)opcode; // 4 bits for the opcode

                if (endOfMessage)
                {
                    buffer[0] |= 0b1000_0000; // 1 bit for FIN
                }

                // Store the payload length.
                if (payloadLength <= 125)
                {
                    buffer[1] = (byte)payloadLength;
                }
                else if (payloadLength <= ushort.MaxValue)
                {
                    buffer[3] = unchecked((byte)payloadLength);
                    buffer[2] = (byte)(payloadLength / 256);
                    buffer[1] = 126;
                }
                else
                {
                    buffer[1] = 127;
                    for (int i = 9; i >= 2; i--)
                    {
                        buffer[i] = unchecked((byte)payloadLength);
                        payloadLength = payloadLength / 256;
                    }
                }

                if (_applyMask)
                {
                    buffer[1] |= 0x80;
                    BitConverter.TryWriteBytes(buffer.Slice(buffer.Length - MaskLength), _mask);
                }
            }

            private void Reset()
            {
                BufferSegment? segment = _currentSegment;

                while (segment is not null)
                {
                    segment.Reset();
                    segment = segment.Previous;
                }

                _currentSegment = _firstSegment;
                _payloadLength = 0;
                _maskIndex = 0;
            }

            internal void Advance(int count)
            {
                if (_applyMask)
                {
                    _maskIndex = ApplyMask(_currentSegment.AvailableSpan[0..count], _mask, _maskIndex);
                }

                _currentSegment.Advance(count);
                _payloadLength += count;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Span<byte> GetSpan(int sizeHint)
            {
                Debug.Assert(sizeHint > 0);
                var span = _currentSegment.AvailableSpan;

                if (span.Length < sizeHint)
                {
                    span = AllocateBufferSegment(sizeHint);
                }

                return span;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private Span<byte> AllocateBufferSegment(int sizeHint)
            {
                var newSegment = _currentSegment.Next ?? new BufferSegment();
                newSegment.Previous = _currentSegment;

                // When initializing a new segment, try to keep the capacity the same as the previous segment
                newSegment.Initialize(Math.Max(_currentSegment.Capacity, sizeHint));

                _currentSegment.Next = newSegment;
                _currentSegment = newSegment;

                return newSegment.AvailableSpan;
            }

            void IValueTaskSource.GetResult(short token) => _valueTaskSource.GetResult(token);

            ValueTaskSourceStatus IValueTaskSource.GetStatus(short token) => _valueTaskSource.GetStatus(token);

            void IValueTaskSource.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
                => _valueTaskSource.OnCompleted(continuation, state, token, flags);

            private void OnCompleted()
            {
                try
                {
                    try
                    {
                        _awaiter.GetResult();
                    }
                    finally
                    {
                        _awaiter = default;
                    }
                }
                catch (Exception ex)
                {
                    Reset();
                    _valueTaskSource.SetException(ex);
                    return;
                }

                Reset();
                _valueTaskSource.SetResult(true);
            }

            private sealed class BufferSegment
            {
                private byte[]? _array;
                private int _index;

                public BufferSegment? Previous { get; set; }

                public BufferSegment? Next { get; set; }

                public int Capacity => _array is null ? 0 : _array.Length;

                public Span<byte> AvailableSpan => _array.AsSpan(_index);

                public Span<byte> Span => _array.AsSpan();

                public ReadOnlyMemory<byte> WrittenMemory => new ReadOnlyMemory<byte>(_array, 0, _index);

                public void Initialize(int capacity)
                {
                    Debug.Assert(_array is null);

                    _array = ArrayPool<byte>.Shared.Rent(capacity);
                }

                public void Reset()
                {
                    if (_array is not null)
                    {
                        ArrayPool<byte>.Shared.Return(_array);
                        _array = null;
                        _index = 0;
                    }
                }

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
