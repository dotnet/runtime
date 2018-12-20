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

        [Fact]
        public async Task AdvanceToEndReturnsAllBlocks()
        {
            var pool = new DisposeTrackingBufferPool();

            var writeSize = 512;

            var pipe = new Pipe(CreatePipeWithInlineSchedulers(pool));
            while (pool.CurrentlyRentedBlocks != 3)
            {
                PipeWriter writableBuffer = pipe.Writer.WriteEmpty(writeSize);
                await writableBuffer.FlushAsync();
            }

            ReadResult readResult = await pipe.Reader.ReadAsync();
            pipe.Reader.AdvanceTo(readResult.Buffer.End);

            Assert.Equal(0, pool.CurrentlyRentedBlocks);
            Assert.Equal(3, pool.DisposedBlocks);
        }

        [Fact]
        public async Task AdvanceToEndReturnsAllButOneBlockIfWritingBeforeAdvance()
        {
            var pool = new DisposeTrackingBufferPool();

            var writeSize = 512;

            var pipe = new Pipe(CreatePipeWithInlineSchedulers(pool));
            while (pool.CurrentlyRentedBlocks != 3)
            {
                PipeWriter writableBuffer = pipe.Writer.WriteEmpty(writeSize);
                await writableBuffer.FlushAsync();
            }

            ReadResult readResult = await pipe.Reader.ReadAsync();
            pipe.Writer.WriteEmpty(writeSize);
            pipe.Reader.AdvanceTo(readResult.Buffer.End);
            await pipe.Writer.FlushAsync();

            Assert.Equal(1, pool.CurrentlyRentedBlocks);
            Assert.Equal(2, pool.DisposedBlocks);
        }

        [Fact]
        public async Task CanWriteAfterReturningMultipleBlocks()
        {
            var pool = new DisposeTrackingBufferPool();

            var writeSize = 512;

            var pipe = new Pipe(CreatePipeWithInlineSchedulers(pool));

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

            Assert.Equal(1, pool.CurrentlyRentedBlocks);
            Assert.Equal(2, pool.DisposedBlocks);
        }

        [Fact]
        public async Task MultipleCompleteReaderWriterCauseDisposeOnlyOnce()
        {
            var pool = new DisposeTrackingBufferPool();

            var readerWriter = new Pipe(CreatePipeWithInlineSchedulers(pool));
            await readerWriter.Writer.WriteAsync(new byte[] { 1 });

            readerWriter.Writer.Complete();
            readerWriter.Reader.Complete();
            Assert.Equal(1, pool.DisposedBlocks);

            readerWriter.Writer.Complete();
            readerWriter.Reader.Complete();
            Assert.Equal(1, pool.DisposedBlocks);
        }

        [Fact]
        public async Task RentsMinimumSegmentSize()
        {
            var pool = new DisposeTrackingBufferPool();
            var writeSize = 512;

            var pipe = new Pipe(new PipeOptions(pool, minimumSegmentSize: 2020, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false));

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
            var pipe = new Pipe(CreatePipeWithInlineSchedulers(pool));
            pipe.Writer.GetMemory(512);

            pipe.Reader.Complete();
            pipe.Writer.Complete();
            Assert.Equal(0, pool.CurrentlyRentedBlocks);
            Assert.Equal(1, pool.DisposedBlocks);
        }

        [Fact]
        public void ReturnsWriteHeadWhenRequestingLargerBlock()
        {
            var pool = new DisposeTrackingBufferPool();
            var pipe = new Pipe(CreatePipeWithInlineSchedulers(pool));
            pipe.Writer.GetMemory(512);
            pipe.Writer.GetMemory(4096);

            pipe.Reader.Complete();
            pipe.Writer.Complete();
            Assert.Equal(0, pool.CurrentlyRentedBlocks);
            Assert.Equal(2, pool.DisposedBlocks);
        }

        [Fact]
        public async Task WriteDuringReadIsNotReturned()
        {
            var pool = new DisposeTrackingBufferPool();

            var writeSize = 512;

            var pipe = new Pipe(CreatePipeWithInlineSchedulers(pool));
            await pipe.Writer.WriteAsync(new byte[writeSize]);

            pipe.Writer.GetMemory(writeSize);
            ReadResult readResult = await pipe.Reader.ReadAsync();
            pipe.Reader.AdvanceTo(readResult.Buffer.End);
            pipe.Writer.Write(new byte[writeSize]);
            await pipe.Writer.FlushAsync();

            Assert.Equal(1, pool.CurrentlyRentedBlocks);
        }

        [Fact]
        public async Task OnWriterCompletedCalledAfterBlocksReturned()
        {
            var pool = new DisposeTrackingBufferPool();

            var pipe = new Pipe(CreatePipeWithInlineSchedulers(pool));
            await pipe.Writer.WriteAsync(new byte[1]);

            Assert.Equal(1, pool.CurrentlyRentedBlocks);

            pipe.Reader.OnWriterCompleted((exception, o) => Assert.Equal(0, pool.CurrentlyRentedBlocks), null);

            pipe.Reader.Complete();
            pipe.Writer.Complete();
        }

        [Fact]
        public async Task OnReaderCompletedCalledAfterBlocksReturned()
        {
            var pool = new DisposeTrackingBufferPool();

            var pipe = new Pipe(CreatePipeWithInlineSchedulers(pool));
            await pipe.Writer.WriteAsync(new byte[1]);

            Assert.Equal(1, pool.CurrentlyRentedBlocks);

            pipe.Writer.OnReaderCompleted((exception, o) => Assert.Equal(0, pool.CurrentlyRentedBlocks), null);

            pipe.Writer.Complete();
            pipe.Reader.Complete();
        }

        private static PipeOptions CreatePipeWithInlineSchedulers(DisposeTrackingBufferPool pool)
        {
            return new PipeOptions(pool, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false);
        }
    }
}
