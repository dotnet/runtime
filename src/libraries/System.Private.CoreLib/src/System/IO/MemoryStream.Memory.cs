// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    public partial class MemoryStream
    {
        /// <summary>Initializes a new non-writable instance of the <see cref="MemoryStream"/> class based on the specified <see cref="ReadOnlyMemory{T}"/>.</summary>
        /// <param name="memory">The read-only memory from which to create the current stream.</param>
        public MemoryStream(ReadOnlyMemory<byte> memory)
        {
            _memoryData = new MemoryData(memory);
            _buffer = [];
            _length = _capacity = memory.Length;
            _isOpen = true;
        }

        /// <summary>Initializes a new writable instance of the <see cref="MemoryStream"/> class based on the specified <see cref="Memory{T}"/>.</summary>
        /// <param name="memory">The memory from which to create the current stream.</param>
        public MemoryStream(Memory<byte> memory)
            : this(memory, true)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="MemoryStream"/> class based on the specified <see cref="Memory{T}"/> with the <see cref="CanWrite"/> property set as specified.</summary>
        /// <param name="memory">The memory from which to create the current stream.</param>
        /// <param name="writable"><see langword="true"/> to enable writing; otherwise, <see langword="false"/>.</param>
        public MemoryStream(Memory<byte> memory, bool writable)
        {
            _memoryData = new MemoryData(memory, writable);
            _buffer = [];
            _length = _capacity = memory.Length;
            _writable = writable;
            _isOpen = true;
        }

        /// <summary>Holds only the <see cref="ReadOnlyMemory{T}"/> and <see cref="Memory{T}"/> references for Memory-backed streams.
        /// All other state (position, length, etc.) is stored in the enclosing <see cref="MemoryStream"/>.</summary>
        private sealed class MemoryData
        {
            internal readonly ReadOnlyMemory<byte> _memory;
            internal readonly Memory<byte> _writableMemory;

            internal MemoryData(ReadOnlyMemory<byte> memory)
            {
                _memory = memory;
            }

            internal MemoryData(Memory<byte> memory, bool writable)
            {
                _memory = memory;
                if (writable)
                {
                    _writableMemory = memory;
                }
            }

            internal int Read(MemoryStream outer, Span<byte> buffer)
            {
                int n = Math.Min(outer._length - outer._position, buffer.Length);
                if (n <= 0)
                    return 0;

                _memory.Span.Slice(outer._position, n).CopyTo(buffer);
                outer._position += n;
                return n;
            }

            internal int ReadByte(MemoryStream outer)
            {
                if (outer._position >= outer._length)
                    return -1;

                return _memory.Span[outer._position++];
            }

            internal void Write(MemoryStream outer, ReadOnlySpan<byte> buffer)
            {
                int i = outer._position + buffer.Length;
                if (i < 0)
                    throw new IOException(SR.IO_StreamTooLong);

                if (i > _memory.Length)
                    throw new NotSupportedException(SR.NotSupported_MemStreamNotExpandable);

                if (i > outer._length)
                {
                    if (outer._position > outer._length)
                    {
                        _writableMemory.Span.Slice(outer._length, outer._position - outer._length).Clear();
                    }
                    outer._length = i;
                }

                buffer.CopyTo(_writableMemory.Span.Slice(outer._position));
                outer._position = i;
            }

            internal void WriteByte(MemoryStream outer, byte value)
            {
                if (outer._position >= outer._length)
                {
                    int newLength = outer._position + 1;
                    if (newLength > _memory.Length)
                        throw new NotSupportedException(SR.NotSupported_MemStreamNotExpandable);

                    if (outer._position > outer._length)
                    {
                        _writableMemory.Span.Slice(outer._length, outer._position - outer._length).Clear();
                    }
                    outer._length = newLength;
                }
                _writableMemory.Span[outer._position++] = value;
            }

            internal void SetLength(MemoryStream outer, long value)
            {
                int newLength = (int)value;
                if (newLength > _memory.Length)
                    throw new NotSupportedException(SR.NotSupported_MemStreamNotExpandable);

                if (newLength > outer._length)
                    _writableMemory.Span.Slice(outer._length, newLength - outer._length).Clear();

                outer._length = newLength;
                if (outer._position > newLength)
                    outer._position = newLength;
            }

            internal byte[] ToArray(MemoryStream outer)
            {
                int count = outer._length;
                if (count == 0)
                    return [];
                byte[] copy = GC.AllocateUninitializedArray<byte>(count);
                _memory.Span.Slice(0, count).CopyTo(copy);
                return copy;
            }

            internal void WriteTo(MemoryStream outer, Stream stream)
            {
                stream.Write(_memory.Span.Slice(0, outer._length));
            }

            internal void CopyTo(MemoryStream outer, Stream destination)
            {
                int remaining = outer._length - outer._position;
                if (remaining > 0)
                {
                    destination.Write(_memory.Span.Slice(outer._position, remaining));
                    outer._position = outer._length;
                }
            }

            internal Task CopyToAsync(MemoryStream outer, Stream destination, CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                    return Task.FromCanceled(cancellationToken);

                int pos = outer._position;
                int n = outer._length - outer._position;
                outer._position = outer._length;

                if (n == 0)
                    return Task.CompletedTask;

                return destination.WriteAsync(_memory.Slice(pos, n), cancellationToken).AsTask();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal ReadOnlySpan<byte> InternalReadSpan(MemoryStream outer, int count)
            {
                int origPos = outer._position;
                int newPos = origPos + count;

                if ((uint)newPos > (uint)outer._length)
                {
                    outer._position = outer._length;
                    ThrowHelper.ThrowEndOfFileException();
                }

                var span = _memory.Span.Slice(origPos, count);
                outer._position = newPos;
                return span;
            }
        }
    }
}
