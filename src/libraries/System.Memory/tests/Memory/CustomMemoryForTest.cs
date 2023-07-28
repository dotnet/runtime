// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.MemoryTests
{
    public class CustomMemoryForTest<T> : MemoryManager<T>
    {
        private bool _disposed;
        private int _referenceCount;
        private int _noReferencesCalledCount;
        private T[] _array;
        private readonly int _offset;
        private readonly int _length;

        public CustomMemoryForTest(T[] array) : this(array, 0, array.Length)
        {
        }

        public CustomMemoryForTest(T[] array, int offset, int length)
        {
            _array = array;
            _offset = offset;
            _length = length;
        }

        public bool IsDisposed => _disposed;

        protected bool IsRetained => _referenceCount > 0;

        public override Span<T> GetSpan()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(CustomMemoryForTest<T>));
            return new Span<T>(_array, _offset, _length);
        }

        public override MemoryHandle Pin(int elementIndex = 0)
        {
            unsafe
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(CustomMemoryForTest<T>));
                Interlocked.Increment(ref _referenceCount);

                try
                {
                    if ((uint)elementIndex > (uint)(_array.Length - _offset))
                    {
                        throw new ArgumentOutOfRangeException(nameof(elementIndex));
                    }

                    var handle = GCHandle.Alloc(_array, GCHandleType.Pinned);
                    return new MemoryHandle(Unsafe.Add<T>((void*)handle.AddrOfPinnedObject(), _offset + elementIndex), handle, this);
                }
                catch
                {
                    Unpin();
                    throw;
                }
            }
        }

        protected override bool TryGetArray(out ArraySegment<T> segment)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(CustomMemoryForTest<T>));
            segment = new ArraySegment<T>(_array, _offset, _length);
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _array = null;
            }

            _disposed = true;

        }

        public override void Unpin()
        {
            int newRefCount = Interlocked.Decrement(ref _referenceCount);

            if (newRefCount < 0)
                throw new InvalidOperationException();

            if (newRefCount == 0)
            {
                _noReferencesCalledCount++;
            }
        }

        public Memory<T> CreateMemoryForTest(int length) => CreateMemory(length);

        public Memory<T> CreateMemoryForTest(int start, int length) => CreateMemory(start, length);
    }
}
