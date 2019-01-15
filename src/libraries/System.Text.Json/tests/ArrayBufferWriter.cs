// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Runtime.CompilerServices;

namespace System.Text.Json.Tests
{
    internal class ArrayBufferWriter : IBufferWriter<byte>, IDisposable
    {
        private ResizableArray<byte> _buffer;

        public ArrayBufferWriter(int capacity)
        {
            _buffer = new ResizableArray<byte>(ArrayPool<byte>.Shared.Rent(capacity));
        }

        public int CommitedByteCount => _buffer.Count;

        public void Clear()
        {
            _buffer.Count = 0;
        }

        public ArraySegment<byte> Free => _buffer.Free;

        public ArraySegment<byte> Formatted => _buffer.Full;

        public Memory<byte> GetMemory(int minimumLength = 0)
        {
            if (minimumLength < 1)
            {
                minimumLength = 1;
            }

            if (minimumLength > _buffer.FreeCount)
            {
                int doubleCount = _buffer.FreeCount * 2;
                int newSize = minimumLength > doubleCount ? minimumLength : doubleCount;
                byte[] newArray = ArrayPool<byte>.Shared.Rent(newSize + _buffer.Count);
                byte[] oldArray = _buffer.Resize(newArray);
                ArrayPool<byte>.Shared.Return(oldArray);
            }

            return _buffer.FreeMemory;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> GetSpan(int minimumLength = 0)
        {
            if (minimumLength < 1)
            {
                minimumLength = 1;
            }

            if (minimumLength > _buffer.FreeCount)
            {
                int doubleCount = _buffer.FreeCount * 2;
                int newSize = minimumLength > doubleCount ? minimumLength : doubleCount;
                byte[] newArray = ArrayPool<byte>.Shared.Rent(newSize + _buffer.Count);
                byte[] oldArray = _buffer.Resize(newArray);
                ArrayPool<byte>.Shared.Return(oldArray);
            }

            return _buffer.FreeSpan;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Advance(int bytes)
        {
            _buffer.Count += bytes;
            if (_buffer.Count > _buffer.Capacity)
            {
                throw new InvalidOperationException("More bytes commited than returned from FreeBuffer");
            }
        }

        public void Dispose()
        {
            byte[] array = _buffer.Array;
            _buffer.Array = null;
            ArrayPool<byte>.Shared.Return(array);
        }
    }
}
