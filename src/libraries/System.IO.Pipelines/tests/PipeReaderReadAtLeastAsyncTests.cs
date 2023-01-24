// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipelines.Tests
{
    public class ReadAtLeastAsyncTests
    {
        private static readonly PipeOptions s_testOptions = new PipeOptions(readerScheduler: PipeScheduler.Inline, useSynchronizationContext: false);

        protected Pipe Pipe { get; set; }
        protected virtual PipeReader PipeReader => Pipe.Reader;

        public ReadAtLeastAsyncTests()
        {
            Pipe = new Pipe(s_testOptions);
        }

        protected virtual void SetPipeReaderOptions(MemoryPool<byte>? pool = null, int bufferSize = -1)
        {
            PipeOptions options = new PipeOptions(pool, readerScheduler: PipeScheduler.Inline, useSynchronizationContext: false , minimumSegmentSize: bufferSize);
            Pipe = new Pipe(options);
        }

        [Fact]
        public async Task CanWriteAndReadAtLeast()
        {
            byte[] bytes = "Hello World"u8.ToArray();

            await Pipe.Writer.WriteAsync(bytes);
            ReadResult result = await PipeReader.ReadAtLeastAsync(11);
            ReadOnlySequence<byte> buffer = result.Buffer;

            Assert.Equal(11, buffer.Length);
            Assert.Equal("Hello World", Encoding.ASCII.GetString(buffer.ToArray()));

            PipeReader.AdvanceTo(buffer.End);
        }

        [Fact]
        public async Task ReadAtLeastShouldNotCompleteIfWriterWroteLessThanMinimum()
        {
            byte[] bytes = "Hello World"u8.ToArray();

            await Pipe.Writer.WriteAsync(bytes.AsMemory(0, 5));
            ValueTask<ReadResult> task = PipeReader.ReadAtLeastAsync(11);

            Assert.False(task.IsCompleted);

            await Pipe.Writer.WriteAsync(bytes.AsMemory(5));

            ReadResult result = await task;

            ReadOnlySequence<byte> buffer = result.Buffer;

            Assert.Equal(11, buffer.Length);
            Assert.Equal("Hello World", Encoding.ASCII.GetString(buffer.ToArray()));

            PipeReader.AdvanceTo(buffer.End);
        }

        [Fact]
        public async Task CanAlternateReadAtLeastAndRead()
        {
            byte[] bytes = "Hello World"u8.ToArray();

            await Pipe.Writer.WriteAsync(bytes.AsMemory(0, 5));
            ReadResult result = await PipeReader.ReadAtLeastAsync(3);
            ReadOnlySequence<byte> buffer = result.Buffer;

            Assert.Equal(5, buffer.Length);
            Assert.Equal("Hello", Encoding.ASCII.GetString(buffer.ToArray()));

            PipeReader.AdvanceTo(buffer.End);

            await Pipe.Writer.WriteAsync(bytes.AsMemory(5));
            result = await PipeReader.ReadAsync();
            buffer = result.Buffer;

            Assert.Equal(6, buffer.Length);
            Assert.Equal(" World", Encoding.ASCII.GetString(buffer.ToArray()));

            PipeReader.AdvanceTo(buffer.End);
        }

        [Fact]
        public async Task ReadAtLeastReturnsIfCompleted()
        {
            Pipe.Writer.Complete();

            // Make sure we get the same results (state transitions are working)
            for (int i = 0; i < 3; i++)
            {
                ReadResult result = await PipeReader.ReadAtLeastAsync(100);

                Assert.True(result.IsCompleted);

                PipeReader.AdvanceTo(result.Buffer.End);
            }
        }

        [Theory]
        [InlineData(-1, false)]
        [InlineData(-1, true)]
        [InlineData(5, false)]
        [InlineData(5, true)]
        public async Task CanReadAtLeast(int bufferSize, bool bufferedRead)
        {
            SetPipeReaderOptions(bufferSize: bufferSize);
            await Pipe.Writer.WriteAsync("Hello Pipelines World"u8.ToArray());

            if (bufferedRead)
            {
                ReadResult bufferedReadResult = await PipeReader.ReadAsync();
                Assert.NotEqual(0, bufferedReadResult.Buffer.Length);
                PipeReader.AdvanceTo(bufferedReadResult.Buffer.Start);
            }

            ReadResult readResult = await PipeReader.ReadAtLeastAsync(20);
            ReadOnlySequence<byte> buffer = readResult.Buffer;

            Assert.Equal(21, buffer.Length);

            var isSingleSegment = bufferSize == -1;
            // Optimization in StreamPipeReader.ReadAtLeastAsync()
            if (PipeReader is StreamPipeReader) isSingleSegment |= !bufferedRead; 
            Assert.Equal(isSingleSegment, buffer.IsSingleSegment);

            Assert.Equal("Hello Pipelines World", Encoding.ASCII.GetString(buffer.ToArray()));

            PipeReader.AdvanceTo(buffer.End);
            PipeReader.Complete();
        }

        [Fact]
        public Task ReadAtLeastAsyncThrowsIfPassedCanceledCancellationToken()
        {
            ValueTask<ReadResult> task = PipeReader.ReadAtLeastAsync(0, new CancellationToken(canceled: true));
            return Assert.ThrowsAsync<TaskCanceledException>(async () => await task);
        }

        [Fact]
        public async Task WriteAndCancellingPendingReadBeforeReadAtLeastAsync()
        {
            byte[] bytes = "Hello World"u8.ToArray();
            PipeWriter output = Pipe.Writer;
            output.Write(bytes);
            await output.FlushAsync();

            PipeReader.CancelPendingRead();

            ReadResult result = await PipeReader.ReadAtLeastAsync(1000);
            ReadOnlySequence<byte> buffer = result.Buffer;

            Assert.False(result.IsCompleted);
            Assert.True(result.IsCanceled);
            PipeReader.AdvanceTo(buffer.End);
        }

        [Fact]
        public Task ReadAtLeastAsyncCancelableWhenWaitingForMoreData()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            ValueTask<ReadResult> task = PipeReader.ReadAtLeastAsync(1, cts.Token);
            cts.Cancel();
            return Assert.ThrowsAsync<OperationCanceledException>(async () => await task);
        }

        [Fact]
        public async Task ReadAtLeastAsyncCancelableAfterReadingSome()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            await Pipe.WriteAsync(new byte[10], default);
            ValueTask<ReadResult> task = PipeReader.ReadAtLeastAsync(11, cts.Token);
            cts.Cancel();
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await task);
        }

        [Fact]
        public async Task ReadAtLeastAsyncCancelableAfterReadingSomeAndWritingAfterStartingRead()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            await Pipe.WriteAsync(new byte[10], default);
            ValueTask<ReadResult> task = PipeReader.ReadAtLeastAsync(12, cts.Token);
            // Write, but not enough to unblock ReadAtLeastAsync
            await Pipe.WriteAsync(new byte[1], default);
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await task);
        }
    }
}
