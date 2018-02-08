// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipelines.Tests
{
    public class BackpressureTests : IDisposable
    {
        public BackpressureTests()
        {
            _pool = new TestMemoryPool();
            _pipe = new Pipe(new PipeOptions(_pool, resumeWriterThreshold: 32, pauseWriterThreshold: 64));
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
        public void AdvanceThrowsIfFlushActiveAndNotConsumedPastThreshold()
        {
            PipeWriter writableBuffer = _pipe.Writer.WriteEmpty(64);
            PipeAwaiter<FlushResult> flushAsync = writableBuffer.FlushAsync();
            Assert.False(flushAsync.IsCompleted);

            ReadResult result = _pipe.Reader.ReadAsync().GetAwaiter().GetResult();
            SequencePosition consumed = result.Buffer.GetPosition(result.Buffer.Start, 31);
            Assert.Throws<InvalidOperationException>(() => _pipe.Reader.AdvanceTo(consumed, result.Buffer.End));

            _pipe.Reader.AdvanceTo(result.Buffer.End, result.Buffer.End);
        }

        [Fact]
        public void FlushAsyncAwaitableCompletesWhenReaderAdvancesUnderLow()
        {
            PipeWriter writableBuffer = _pipe.Writer.WriteEmpty(64);
            PipeAwaiter<FlushResult> flushAsync = writableBuffer.FlushAsync();

            Assert.False(flushAsync.IsCompleted);

            ReadResult result = _pipe.Reader.ReadAsync().GetAwaiter().GetResult();
            SequencePosition consumed = result.Buffer.GetPosition(result.Buffer.Start, 33);
            _pipe.Reader.AdvanceTo(consumed, consumed);

            Assert.True(flushAsync.IsCompleted);
            FlushResult flushResult = flushAsync.GetResult();
            Assert.False(flushResult.IsCompleted);
        }

        [Fact]
        public void FlushAsyncAwaitableDoesNotCompletesWhenReaderAdvancesUnderHight()
        {
            PipeWriter writableBuffer = _pipe.Writer.WriteEmpty(64);
            PipeAwaiter<FlushResult> flushAsync = writableBuffer.FlushAsync();

            Assert.False(flushAsync.IsCompleted);

            ReadResult result = _pipe.Reader.ReadAsync().GetAwaiter().GetResult();
            SequencePosition consumed = result.Buffer.GetPosition(result.Buffer.Start, 32);
            _pipe.Reader.AdvanceTo(consumed, consumed);

            Assert.False(flushAsync.IsCompleted);
        }

        [Fact]
        public void FlushAsyncAwaitableResetsOnCommit()
        {
            PipeWriter writableBuffer = _pipe.Writer.WriteEmpty(64);
            PipeAwaiter<FlushResult> flushAsync = writableBuffer.FlushAsync();

            Assert.False(flushAsync.IsCompleted);

            ReadResult result = _pipe.Reader.ReadAsync().GetAwaiter().GetResult();
            SequencePosition consumed = result.Buffer.GetPosition(result.Buffer.Start, 33);
            _pipe.Reader.AdvanceTo(consumed, consumed);

            Assert.True(flushAsync.IsCompleted);
            FlushResult flushResult = flushAsync.GetResult();
            Assert.False(flushResult.IsCompleted);

            writableBuffer = _pipe.Writer.WriteEmpty(64);
            flushAsync = writableBuffer.FlushAsync();

            Assert.False(flushAsync.IsCompleted);
        }

        [Fact]
        public void FlushAsyncReturnsCompletedIfReaderCompletes()
        {
            PipeWriter writableBuffer = _pipe.Writer.WriteEmpty(64);
            PipeAwaiter<FlushResult> flushAsync = writableBuffer.FlushAsync();

            Assert.False(flushAsync.IsCompleted);

            _pipe.Reader.Complete();

            Assert.True(flushAsync.IsCompleted);
            FlushResult flushResult = flushAsync.GetResult();
            Assert.True(flushResult.IsCompleted);
        }

        [Fact]
        public void FlushAsyncReturnsCompletedTaskWhenSizeLessThenLimit()
        {
            PipeWriter writableBuffer = _pipe.Writer.WriteEmpty(32);
            PipeAwaiter<FlushResult> flushAsync = writableBuffer.FlushAsync();
            Assert.True(flushAsync.IsCompleted);
            FlushResult flushResult = flushAsync.GetResult();
            Assert.False(flushResult.IsCompleted);
        }

        [Fact]
        public void FlushAsyncReturnsNonCompletedSizeWhenCommitOverTheLimit()
        {
            PipeWriter writableBuffer = _pipe.Writer.WriteEmpty(64);
            PipeAwaiter<FlushResult> flushAsync = writableBuffer.FlushAsync();
            Assert.False(flushAsync.IsCompleted);
        }

        [Fact]
        public async Task FlushAsyncThrowsIfReaderCompletedWithException()
        {
            _pipe.Reader.Complete(new InvalidOperationException("Reader failed"));

            PipeWriter writableBuffer = _pipe.Writer.WriteEmpty(64);
            InvalidOperationException invalidOperationException =
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await writableBuffer.FlushAsync());
            Assert.Equal("Reader failed", invalidOperationException.Message);
            invalidOperationException = await Assert.ThrowsAsync<InvalidOperationException>(async () => await writableBuffer.FlushAsync());
            Assert.Equal("Reader failed", invalidOperationException.Message);
        }
    }
}
