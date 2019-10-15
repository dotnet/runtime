// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;

namespace System.IO.Pipelines.Tests
{
    public class DisposeTrackingBufferPool : TestMemoryPool
    {
        public int DisposedBlocks { get; set; }
        public int CurrentlyRentedBlocks { get; set; }

        public override IMemoryOwner<byte> Rent(int size)
        {
            return new DisposeTrackingMemoryManager(new byte[size], this);
        }

        protected override void Dispose(bool disposing)
        {
        }

        private class DisposeTrackingMemoryManager : MemoryManager<byte>
        {
            private byte[] _array;

            private readonly DisposeTrackingBufferPool _bufferPool;

            public DisposeTrackingMemoryManager(byte[] array, DisposeTrackingBufferPool bufferPool)
            {
                _array = array;
                _bufferPool = bufferPool;
                _bufferPool.CurrentlyRentedBlocks++;
            }

            public override Memory<byte> Memory => CreateMemory(_array.Length);

            public bool IsDisposed => _array == null;

            public override MemoryHandle Pin(int elementIndex = 0)
            {
                throw new NotImplementedException();
            }

            public override void Unpin()
            {
                throw new NotImplementedException();
            }

            protected override bool TryGetArray(out ArraySegment<byte> segment)
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(DisposeTrackingBufferPool));
                segment = new ArraySegment<byte>(_array);
                return true;
            }

            protected override void Dispose(bool disposing)
            {
                _bufferPool.DisposedBlocks++;
                _bufferPool.CurrentlyRentedBlocks--;

                _array = null;
            }

            public override Span<byte> GetSpan()
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(DisposeTrackingBufferPool));
                return _array;
            }
        }
    }
}
