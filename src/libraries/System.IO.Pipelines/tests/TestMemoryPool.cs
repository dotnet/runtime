// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.IO.Pipelines
{
    public class TestMemoryPool: MemoryPool<byte>
    {
        private MemoryPool<byte> _pool = Shared;

        private bool _disposed;

        public override OwnedMemory<byte> Rent(int minBufferSize = -1)
        {
            CheckDisposed();
            return new PooledMemory(_pool.Rent(minBufferSize), this);
        }

        protected override void Dispose(bool disposing)
        {
            _disposed = true;
        }

        public override int MaxBufferSize => 4096;

        internal void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(TestMemoryPool));
            }
        }

        private class PooledMemory : OwnedMemory<byte>
        {
            private OwnedMemory<byte> _ownedMemory;

            private readonly TestMemoryPool _pool;

            private int _referenceCount;

            private bool _returned;

            private string _leaser;

            public PooledMemory(OwnedMemory<byte> ownedMemory, TestMemoryPool pool)
            {
                _ownedMemory = ownedMemory;
                _pool = pool;
                _leaser = Environment.StackTrace;
                _referenceCount = 1;
            }

            ~PooledMemory()
            {
                Debug.Assert(_returned, "Block being garbage collected instead of returned to pool" + Environment.NewLine + _leaser);
            }

            protected override void Dispose(bool disposing)
            {
                _pool.CheckDisposed();
                _ownedMemory.Dispose();
            }

            public override MemoryHandle Pin(int byteOffset = 0)
            {
                _pool.CheckDisposed();
                return _ownedMemory.Pin(byteOffset);
            }

            public override void Retain()
            {
                _pool.CheckDisposed();
                _ownedMemory.Retain();
                Interlocked.Increment(ref _referenceCount);
            }

            public override bool Release()
            {
                _pool.CheckDisposed();
                _ownedMemory.Release();

                int newRefCount = Interlocked.Decrement(ref _referenceCount);

                if (newRefCount < 0)
                    throw new InvalidOperationException();

                if (newRefCount == 0)
                {
                    _returned = true;
                    return false;
                }
                return true;
            }

            protected override bool TryGetArray(out ArraySegment<byte> segment)
            {
                _pool.CheckDisposed();
                return MemoryMarshal.TryGetArray(_ownedMemory.Memory, out segment);
            }

            public override bool IsDisposed
            {
                get
                {
                    _pool.CheckDisposed();
                    return _ownedMemory.IsDisposed;
                }
            }

            protected override bool IsRetained
            {
                get
                {
                    _pool.CheckDisposed();
                    return !_returned;
                }
            }

            public override int Length
            {
                get
                {
                    _pool.CheckDisposed();
                    return _ownedMemory.Length;
                }
            }

            public override Span<byte> Span
            {
                get
                {
                    _pool.CheckDisposed();
                    return _ownedMemory.Span;
                }
            }
        }
    }
}
