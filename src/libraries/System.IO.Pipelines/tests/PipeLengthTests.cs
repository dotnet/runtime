// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipelines.Tests
{
    public class PipeLengthTests : IDisposable
    {
        public PipeLengthTests()
        {
            _pool = new TestMemoryPool();
            _pipe = new Pipe(new PipeOptions(_pool, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, pauseWriterThreshold: 0, resumeWriterThreshold: 0, useSynchronizationContext: false));
        }

        public void Dispose()
        {
            _pipe.Writer.Complete();
            _pipe.Reader.Complete();
            _pool?.Dispose();
        }

        private readonly TestMemoryPool _pool;

        private readonly Pipe _pipe;

        [Fact]
        public async Task ByteByByteTest()
        {
            for (var i = 1; i <= 1024 * 1024; i++)
            {
                _pipe.Writer.GetMemory(100);
                _pipe.Writer.Advance(1);
                await _pipe.Writer.FlushAsync();

                Assert.Equal(i, _pipe.Length);
            }

            await _pipe.Writer.FlushAsync();

            for (int i = 1024 * 1024 - 1; i >= 0; i--)
            {
                ReadResult result = await _pipe.Reader.ReadAsync();
                SequencePosition consumed = result.Buffer.Slice(1).Start;

                Assert.Equal(i + 1, result.Buffer.Length);

                _pipe.Reader.AdvanceTo(consumed, consumed);

                Assert.Equal(i, _pipe.Length);
            }
        }

        [Fact]
        public async Task LengthCorrectAfterAlloc0AdvanceFlush()
        {
            _pipe.Writer.GetMemory(0);
            _pipe.Writer.WriteEmpty(10);
            await _pipe.Writer.FlushAsync();

            Assert.Equal(10, _pipe.Length);
        }

        [Fact]
        public async Task LengthCorrectAfterAllocAdvanceFlush()
        {
            PipeWriter writableBuffer = _pipe.Writer.WriteEmpty(10);
            await writableBuffer.FlushAsync();

            Assert.Equal(10, _pipe.Length);
        }

        [Fact]
        public async Task LengthDecreasedAfterReadAdvanceExamined()
        {
            _pipe.Writer.GetMemory(100);
            _pipe.Writer.Advance(10);
            await _pipe.Writer.FlushAsync();

            ReadResult result = await _pipe.Reader.ReadAsync();
            SequencePosition consumed = result.Buffer.Slice(5).Start;
            _pipe.Reader.AdvanceTo(consumed, consumed);

            Assert.Equal(5, _pipe.Length);
        }

        [Fact]
        public async Task LengthDoesNotChangeIfExamineDoesNotChange()
        {
            PipeWriter writableBuffer = _pipe.Writer.WriteEmpty(10);
            await writableBuffer.FlushAsync();

            ReadResult result = await _pipe.Reader.ReadAsync();
            _pipe.Reader.AdvanceTo(result.Buffer.Start);

            Assert.Equal(10, _pipe.Length);
        }

        [Fact]
        public async Task LengthChangesIfExaminedChanges()
        {
            PipeWriter writableBuffer = _pipe.Writer.WriteEmpty(10);
            await writableBuffer.FlushAsync();

            ReadResult result = await _pipe.Reader.ReadAsync();
            _pipe.Reader.AdvanceTo(result.Buffer.Start, result.Buffer.End);

            Assert.Equal(0, _pipe.Length);
        }

        [Fact]
        public async Task LengthIsBasedOnPreviouslyExamined()
        {
            for (int i = 0; i < 5; i++)
            {
                PipeWriter writableBuffer = _pipe.Writer.WriteEmpty(10);
                await writableBuffer.FlushAsync();

                ReadResult result = await _pipe.Reader.ReadAsync();
                _pipe.Reader.AdvanceTo(result.Buffer.Start, result.Buffer.End);

                Assert.Equal(0, _pipe.Length);
            }
        }

        [Fact]
        public async Task PooledSegmentsDontAffectLastExaminedSegment()
        {
            _pipe.Writer.WriteEmpty(_pool.MaxBufferSize);
            _pipe.Writer.WriteEmpty(_pool.MaxBufferSize);
            await _pipe.Writer.FlushAsync();

            ReadResult result = await _pipe.Reader.ReadAsync();
            // This gets the end of the first block
            SequencePosition position = result.Buffer.Slice(result.Buffer.Start, _pool.MaxBufferSize).End;

            // This should return the first segment
            _pipe.Reader.AdvanceTo(position);

            // One block remaining
            Assert.Equal(4096, _pipe.Length);

            // This should use the segment that was returned
            _pipe.Writer.WriteEmpty(_pool.MaxBufferSize);
            await _pipe.Writer.FlushAsync();

            result = await _pipe.Reader.ReadAsync();
            _pipe.Reader.AdvanceTo(result.Buffer.Start);

            Assert.Equal(8192, _pipe.Length);

            result = await _pipe.Reader.ReadAsync();
            _pipe.Reader.AdvanceTo(result.Buffer.End);

            Assert.Equal(0, _pipe.Length);
        }

        [Fact]
        public async Task PooledSegmentsDontAffectLastExaminedSegmentEmptyGapWithDifferentBlocks()
        {
            _pipe.Writer.WriteEmpty(_pool.MaxBufferSize);
            _pipe.Writer.WriteEmpty(_pool.MaxBufferSize);
            await _pipe.Writer.FlushAsync();

            ReadResult result = await _pipe.Reader.ReadAsync();
            // This gets the end of the first block
            SequencePosition endOfFirstBlock = result.Buffer.Slice(result.Buffer.Start, _pool.MaxBufferSize).End;
            // Start of the next block
            SequencePosition startOfSecondBlock = result.Buffer.GetPosition(_pool.MaxBufferSize);

            Assert.NotSame(endOfFirstBlock.GetObject(), startOfSecondBlock.GetObject());

            // This should return the first segment
            _pipe.Reader.AdvanceTo(startOfSecondBlock, endOfFirstBlock);

            // One block remaining
            Assert.Equal(4096, _pipe.Length);

            // This should use the segment that was returned
            _pipe.Writer.WriteEmpty(_pool.MaxBufferSize);
            await _pipe.Writer.FlushAsync();

            result = await _pipe.Reader.ReadAsync();
            _pipe.Reader.AdvanceTo(result.Buffer.Start);

            Assert.Equal(8192, _pipe.Length);

            result = await _pipe.Reader.ReadAsync();
            _pipe.Reader.AdvanceTo(result.Buffer.End);

            Assert.Equal(0, _pipe.Length);
        }

        [Fact]
        public async Task ExaminedAtSecondLastBlockWorks()
        {
            _pipe.Writer.WriteEmpty(_pool.MaxBufferSize);
            _pipe.Writer.WriteEmpty(_pool.MaxBufferSize);
            _pipe.Writer.WriteEmpty(_pool.MaxBufferSize);
            await _pipe.Writer.FlushAsync();

            ReadResult result = await _pipe.Reader.ReadAsync();
            // This gets the end of the first block
            SequencePosition position = result.Buffer.Slice(result.Buffer.Start, _pool.MaxBufferSize).End;

            // This should return the first segment
            _pipe.Reader.AdvanceTo(position, result.Buffer.GetPosition(_pool.MaxBufferSize * 2));

            // One block remaining
            Assert.Equal(4096, _pipe.Length);

            // This should use the segment that was returned
            _pipe.Writer.WriteEmpty(_pool.MaxBufferSize);
            await _pipe.Writer.FlushAsync();

            result = await _pipe.Reader.ReadAsync();
            _pipe.Reader.AdvanceTo(result.Buffer.End);

            Assert.Equal(0, _pipe.Length);
        }

        [Fact]
        public async Task ExaminedLessThanBeforeThrows()
        {
            _pipe.Writer.WriteEmpty(10);
            await _pipe.Writer.FlushAsync();

            ReadResult result = await _pipe.Reader.ReadAsync();
            _pipe.Reader.AdvanceTo(result.Buffer.Start, result.Buffer.End);

            Assert.Equal(0, _pipe.Length);

            _pipe.Writer.WriteEmpty(10);
            await _pipe.Writer.FlushAsync();

            result = await _pipe.Reader.ReadAsync();
            Assert.Throws<InvalidOperationException>(() => _pipe.Reader.AdvanceTo(result.Buffer.Start, result.Buffer.Start));
        }

        [Fact]
        public async Task ConsumedGreaterThanExaminedThrows()
        {
            _pipe.Writer.WriteEmpty(10);
            await _pipe.Writer.FlushAsync();

            ReadResult result = await _pipe.Reader.ReadAsync();
            Assert.Throws<InvalidOperationException>(() => _pipe.Reader.AdvanceTo(result.Buffer.End, result.Buffer.Start));
        }

        [Fact]
        public async Task NullConsumedOrExaminedNoops()
        {
            _pipe.Writer.WriteEmpty(10);
            await _pipe.Writer.FlushAsync();

            ReadResult result = await _pipe.Reader.ReadAsync();
            _pipe.Reader.AdvanceTo(default, result.Buffer.End);
        }

        [Fact]
        public async Task NullExaminedNoops()
        {
            _pipe.Writer.WriteEmpty(10);
            await _pipe.Writer.FlushAsync();

            ReadResult result = await _pipe.Reader.ReadAsync();
            _pipe.Reader.AdvanceTo(result.Buffer.Start, default);
        }

        [Fact]
        public async Task NullExaminedAndConsumedNoops()
        {
            _pipe.Writer.WriteEmpty(10);
            await _pipe.Writer.FlushAsync();

            ReadResult result = await _pipe.Reader.ReadAsync();
            _pipe.Reader.AdvanceTo(default, default);
        }

        [Fact]
        public async Task AdvanceFollowedByWriteAsyncTest()
        {
            Memory<byte> buffer = new byte[26];
            Pipe pipe = new(new PipeOptions(minimumSegmentSize: 1));

            var mem = pipe.Writer.GetMemory(14)[..14];
            buffer[..14].CopyTo(mem);
            pipe.Writer.Advance(14);
            await pipe.Writer.WriteAsync(buffer[14..]);
            ReadResult res = await pipe.Reader.ReadAsync();
            Assert.Equal(res.Buffer.Length, buffer.Length);
        }
    }
}
