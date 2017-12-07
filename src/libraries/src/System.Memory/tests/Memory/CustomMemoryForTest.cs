// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.MemoryTests
{
    public class CustomMemoryForTest<T> : OwnedMemory<T>
    {
        private bool _disposed;
        private int _referenceCount;
        private int _noReferencesCalledCount;
        private T[] _array;

        public CustomMemoryForTest(T[] array)
        {
            _array = array;
        }

        public int OnNoRefencesCalledCount => _noReferencesCalledCount;

        public override int Length => _array.Length;

        public override bool IsDisposed => _disposed;

        protected override bool IsRetained => _referenceCount > 0;

        public override Span<T> Span
        {
            get
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(CustomMemoryForTest<T>));
                return new Span<T>(_array, 0, _array.Length);
            }
        }

        public override MemoryHandle Pin()
        {
            unsafe
            {
                Retain();
                var handle = GCHandle.Alloc(_array, GCHandleType.Pinned);
                return new MemoryHandle(this, (void*)handle.AddrOfPinnedObject(), handle);
            }
        }

        protected override bool TryGetArray(out ArraySegment<T> arraySegment)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(CustomMemoryForTest<T>));
            arraySegment = new ArraySegment<T>(_array);
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

        public override void Retain()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(CustomMemoryForTest<T>));
            Interlocked.Increment(ref _referenceCount);
        }

        public override bool Release()
        {
            int newRefCount = Interlocked.Decrement(ref _referenceCount);

            if (newRefCount < 0)
                throw new InvalidOperationException();

            if (newRefCount == 0)
            {
                _noReferencesCalledCount++;
                return false;
            }
            return true;
        }
    }
}

