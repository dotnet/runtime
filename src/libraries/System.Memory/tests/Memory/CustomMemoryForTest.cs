// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Runtime.CompilerServices;
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
        private readonly int _offset;
        private readonly int _length;

        public CustomMemoryForTest(T[] array): this(array, 0, array.Length)
        {
        }

        public CustomMemoryForTest(T[] array, int offset, int length)
        {
            _array = array;
            _offset = offset;
            _length = length;
        }

        public int OnNoRefencesCalledCount => _noReferencesCalledCount;

        public override int Length => _length;

        public override bool IsDisposed => _disposed;

        protected override bool IsRetained => _referenceCount > 0;

        public override Span<T> Span
        {
            get
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(CustomMemoryForTest<T>));
                return new Span<T>(_array, _offset, _length);
            }
        }

        public override MemoryHandle Pin(int byteOffset = 0)
        {
            unsafe
            {
                Retain(); // this checks IsDisposed

                try
                {
                    if ((IntPtr.Size == 4 && (uint)byteOffset > (uint)_array.Length * (uint)Unsafe.SizeOf<T>())
                        || (IntPtr.Size != 4 && (ulong)byteOffset > (uint)_array.Length * (ulong)Unsafe.SizeOf<T>()))
                    {
                        throw new ArgumentOutOfRangeException(nameof(byteOffset));
                    }

                    var handle = GCHandle.Alloc(_array, GCHandleType.Pinned);
                    return new MemoryHandle(this, Unsafe.Add<byte>((void*)handle.AddrOfPinnedObject(), _offset + byteOffset), handle);
                }
                catch
                {
                    Release();
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

