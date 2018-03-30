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

        public override IMemoryOwner<byte> Rent(int minBufferSize = -1)
        {
            CheckDisposed();
            return new PooledMemory((MemoryManager<byte>)_pool.Rent(minBufferSize), this);
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

        private class PooledMemory : MemoryManager<byte>
        {
            private MemoryManager<byte> _manager;

            private readonly TestMemoryPool _pool;

            private int _referenceCount;

            private bool _returned;

            private string _leaser;

            public PooledMemory(MemoryManager<byte> manager, TestMemoryPool pool)
            {
                _manager = manager;
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
            }

            public override MemoryHandle Pin(int elementIndex = 0)
            {
                _pool.CheckDisposed();
                Interlocked.Increment(ref _referenceCount);
                return _manager.Pin(elementIndex);
            }

            public override void Unpin()
            {
                _pool.CheckDisposed();
                _manager.Unpin();

                int newRefCount = Interlocked.Decrement(ref _referenceCount);

                if (newRefCount < 0)
                    throw new InvalidOperationException();

                if (newRefCount == 0)
                {
                    _returned = true;
                }
            }

            protected override bool TryGetArray(out ArraySegment<byte> segment)
            {
                _pool.CheckDisposed();
                return MemoryMarshal.TryGetArray(_manager.Memory, out segment);
            }

            public override int Length
            {
                get
                {
                    _pool.CheckDisposed();
                    return _manager.Length;
                }
            }

            public override Span<byte> GetSpan()
            {
                _pool.CheckDisposed();
                return _manager.GetSpan();
            }
        }
    }
}
