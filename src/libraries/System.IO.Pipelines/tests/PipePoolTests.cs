// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipelines.Tests
{
    public class PipePoolTests
    {
        private class DisposeTrackingBufferPool : TestMemoryPool
        {
            public int ReturnedBlocks { get; set; }
            public int CurrentlyRentedBlocks { get; set; }

            public override OwnedMemory<byte> Rent(int size)
            {
                return new DisposeTrackingOwnedMemory(new byte[size], this);
            }

            protected override void Dispose(bool disposing)
            {
            }

            private class DisposeTrackingOwnedMemory : OwnedMemory<byte>
            {
                private readonly byte[] _array;

                private readonly DisposeTrackingBufferPool _bufferPool;

                public DisposeTrackingOwnedMemory(byte[] array, DisposeTrackingBufferPool bufferPool)
                {
                    _array = array;
                    _bufferPool = bufferPool;
                    _bufferPool.CurrentlyRentedBlocks++;
                }

                public override int Length => _array.Length;

                public override Span<byte> Span
                {
                    get
                    {
                        if (IsDisposed)
                            throw new ObjectDisposedException(nameof(DisposeTrackingBufferPool));
                        return _array;
                    }
                }

                public override bool IsDisposed { get; }

                protected override bool IsRetained => true;

                public override MemoryHandle Pin(int byteOffset = 0)
                {
                    throw new NotImplementedException();
                }

                protected override bool TryGetArray(out ArraySegment<byte> arraySegment)
                {
                    if (IsDisposed)
                        throw new ObjectDisposedException(nameof(DisposeTrackingBufferPool));
                    arraySegment = new ArraySegment<byte>(_array);
                    return true;
                }

                protected override void Dispose(bool disposing)
                {
                    throw new NotImplementedException();
                }

                public override bool Release()
                {
                    _bufferPool.ReturnedBlocks++;
                    _bufferPool.CurrentlyRentedBlocks--;
                    return IsRetained;
                }

                public override void Retain()
                {
                }
            }
        }

        [Fact]
        public async Task AdvanceToEndReturnsAllBlocks()
        {
            var pool = new DisposeTrackingBufferPool();

            var writeSize = 512;

            var pipe = new Pipe(new PipeOptions(pool));
            while (pool.CurrentlyRentedBlocks != 3)
            {
                PipeWriter writableBuffer = pipe.Writer.WriteEmpty(writeSize);
                await writableBuffer.FlushAsync();
            }

            ReadResult readResult = await pipe.Reader.ReadAsync();
            pipe.Reader.AdvanceTo(readResult.Buffer.End);

            Assert.Equal(0, pool.CurrentlyRentedBlocks);
        }

        [Fact]
        public async Task CanWriteAfterReturningMultipleBlocks()
        {
            var pool = new DisposeTrackingBufferPool();

            var writeSize = 512;

            var pipe = new Pipe(new PipeOptions(pool));

            // Write two blocks
            Memory<byte> buffer = pipe.Writer.GetMemory(writeSize);
            pipe.Writer.Advance(buffer.Length);
            pipe.Writer.GetMemory(buffer.Length);
            pipe.Writer.Advance(writeSize);
            await pipe.Writer.FlushAsync();

            Assert.Equal(2, pool.CurrentlyRentedBlocks);

            // Read everything
            ReadResult readResult = await pipe.Reader.ReadAsync();
            pipe.Reader.AdvanceTo(readResult.Buffer.End);

            // Try writing more
            await pipe.Writer.WriteAsync(new byte[writeSize]);
        }

        [Fact]
        public async Task MultipleCompleteReaderWriterCauseDisposeOnlyOnce()
        {
            var pool = new DisposeTrackingBufferPool();

            var readerWriter = new Pipe(new PipeOptions(pool));
            await readerWriter.Writer.WriteAsync(new byte[] { 1 });

            readerWriter.Writer.Complete();
            readerWriter.Reader.Complete();
            Assert.Equal(1, pool.ReturnedBlocks);

            readerWriter.Writer.Complete();
            readerWriter.Reader.Complete();
            Assert.Equal(1, pool.ReturnedBlocks);
        }

        [Fact]
        public async Task RentsMinimumSegmentSize()
        {
            var pool = new DisposeTrackingBufferPool();
            var writeSize = 512;

            var pipe = new Pipe(new PipeOptions(pool, minimumSegmentSize: 2020));

            Memory<byte> buffer = pipe.Writer.GetMemory(writeSize);
            int allocatedSize = buffer.Length;
            pipe.Writer.Advance(buffer.Length);
            buffer = pipe.Writer.GetMemory(1);
            int ensuredSize = buffer.Length;
            await pipe.Writer.FlushAsync();

            pipe.Reader.Complete();
            pipe.Writer.Complete();

            Assert.Equal(2020, ensuredSize);
            Assert.Equal(2020, allocatedSize);
        }

        [Fact]
        public void ReturnsWriteHeadOnComplete()
        {
            var pool = new DisposeTrackingBufferPool();
            var pipe = new Pipe(new PipeOptions(pool));
            var memory = pipe.Writer.GetMemory(512);

            pipe.Reader.Complete();
            pipe.Writer.Complete();
            Assert.Equal(0, pool.CurrentlyRentedBlocks);
        }

        [Fact]
        public void ReturnsWriteHeadWhenRequestingLargerBlock()
        {
            var pool = new DisposeTrackingBufferPool();
            var pipe = new Pipe(new PipeOptions(pool));
            var memory = pipe.Writer.GetMemory(512);
            pipe.Writer.GetMemory(4096);

            pipe.Reader.Complete();
            pipe.Writer.Complete();
            Assert.Equal(0, pool.CurrentlyRentedBlocks);
        }

        [Fact]
        public async Task WriteDuringReadIsNotReturned()
        {
            var pool = new DisposeTrackingBufferPool();

            var writeSize = 512;

            var pipe = new Pipe(new PipeOptions(pool));
            await pipe.Writer.WriteAsync(new byte[writeSize]);

            pipe.Writer.GetMemory(writeSize);
            ReadResult readResult = await pipe.Reader.ReadAsync();
            pipe.Reader.AdvanceTo(readResult.Buffer.End);
            pipe.Writer.Write(new byte[writeSize]);
            pipe.Writer.Commit();

            Assert.Equal(1, pool.CurrentlyRentedBlocks);
        }
    }
}
