// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipelines.Tests
{
    public class FlushAsyncCancellationTests : PipeTest
    {
        [Fact]
        public void FlushAsyncCancellationDeadlock()
        {
            var cts = new CancellationTokenSource();
            var cts2 = new CancellationTokenSource();

            PipeWriter buffer = Pipe.Writer.WriteEmpty(MaximumSizeHigh);

            var e = new ManualResetEventSlim();

            PipeAwaiter<FlushResult> awaiter = buffer.FlushAsync(cts.Token);
            awaiter.OnCompleted(
                () => {
                    // We are on cancellation thread and need to wait untill another FlushAsync call
                    // takes pipe state lock
                    e.Wait();

                    // Make sure we had enough time to reach _cancellationTokenRegistration.Dispose
                    Thread.Sleep(100);

                    // Try to take pipe state lock
                    buffer.FlushAsync();
                });

            // Start a thread that would run cancellation calbacks
            Task cancellationTask = Task.Run(() => cts.Cancel());
            // Start a thread that would call FlushAsync with different token
            // and block on _cancellationTokenRegistration.Dispose
            Task blockingTask = Task.Run(
                () => {
                    e.Set();
                    buffer.FlushAsync(cts2.Token);
                });

            bool completed = Task.WhenAll(cancellationTask, blockingTask).Wait(TimeSpan.FromSeconds(10));
            Assert.True(completed);
        }

        [Fact]
        public async Task FlushAsyncCancellationE2E()
        {
            var cts = new CancellationTokenSource();
            var cancelled = false;

            Func<Task> taskFunc = async () => {
                try
                {
                    Pipe.Writer.WriteEmpty(MaximumSizeHigh);
                    await Pipe.Writer.FlushAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    cancelled = true;
                    await Pipe.Writer.FlushAsync();
                }
            };

            Task task = taskFunc();

            cts.Cancel();

            ReadResult result = await Pipe.Reader.ReadAsync();
            Assert.Equal(new byte[MaximumSizeHigh], result.Buffer.ToArray());
            Pipe.Reader.AdvanceTo(result.Buffer.End);
            await task;
            Assert.True(cancelled);
        }

        [Fact]
        public void FlushAsyncCompletedAfterPreCancellation()
        {
            PipeWriter writableBuffer = Pipe.Writer.WriteEmpty(1);

            Pipe.Writer.CancelPendingFlush();

            PipeAwaiter<FlushResult> flushAsync = writableBuffer.FlushAsync();

            Assert.True(flushAsync.IsCompleted);

            FlushResult flushResult = flushAsync.GetResult();

            Assert.True(flushResult.IsCanceled);

            flushAsync = writableBuffer.FlushAsync();

            Assert.True(flushAsync.IsCompleted);
        }

        [Fact]
        public void FlushAsyncNotCompletedAfterCancellation()
        {
            var onCompletedCalled = false;
            PipeWriter writableBuffer = Pipe.Writer.WriteEmpty(MaximumSizeHigh);

            PipeAwaiter<FlushResult> awaitable = writableBuffer.FlushAsync();

            Assert.False(awaitable.IsCompleted);
            awaitable.OnCompleted(
                () => {
                    onCompletedCalled = true;
                    Assert.True(awaitable.IsCompleted);

                    FlushResult flushResult = awaitable.GetResult();

                    Assert.True(flushResult.IsCanceled);

                    awaitable = writableBuffer.FlushAsync();
                    Assert.False(awaitable.IsCompleted);
                });

            Pipe.Writer.CancelPendingFlush();
            Assert.True(onCompletedCalled);
        }

        [Fact]
        public void FlushAsyncNotCompletedAfterCancellationTokenCancelled()
        {
            var onCompletedCalled = false;
            var cts = new CancellationTokenSource();
            PipeWriter writableBuffer = Pipe.Writer.WriteEmpty(MaximumSizeHigh);

            PipeAwaiter<FlushResult> awaitable = writableBuffer.FlushAsync(cts.Token);

            Assert.False(awaitable.IsCompleted);
            awaitable.OnCompleted(
                () => {
                    onCompletedCalled = true;
                    Assert.True(awaitable.IsCompleted);

                    Assert.Throws<OperationCanceledException>(() => awaitable.GetResult());

                    awaitable = writableBuffer.FlushAsync();
                    Assert.False(awaitable.IsCompleted);
                });

            cts.Cancel();
            Assert.True(onCompletedCalled);
        }

        [Fact]
        public void FlushAsyncReturnsCanceledIfCancelledBeforeFlush()
        {
            PipeWriter writableBuffer = Pipe.Writer.WriteEmpty(MaximumSizeHigh);

            Pipe.Writer.CancelPendingFlush();

            PipeAwaiter<FlushResult> flushAsync = writableBuffer.FlushAsync();

            Assert.True(flushAsync.IsCompleted);
            FlushResult flushResult = flushAsync.GetResult();
            Assert.True(flushResult.IsCanceled);
        }

        [Fact]
        public void FlushAsyncReturnsCanceledIfFlushCancelled()
        {
            PipeWriter writableBuffer = Pipe.Writer.WriteEmpty(MaximumSizeHigh);
            PipeAwaiter<FlushResult> flushAsync = writableBuffer.FlushAsync();

            Assert.False(flushAsync.IsCompleted);

            Pipe.Writer.CancelPendingFlush();

            Assert.True(flushAsync.IsCompleted);
            FlushResult flushResult = flushAsync.GetResult();
            Assert.True(flushResult.IsCanceled);
        }

        [Fact]
        public void FlushAsyncReturnsIsCancelOnCancelPendingFlushAfterGetResult()
        {
            PipeWriter writableBuffer = Pipe.Writer.WriteEmpty(MaximumSizeHigh);
            PipeAwaiter<FlushResult> awaitable = writableBuffer.FlushAsync();

            Assert.False(awaitable.IsCompleted);
            awaitable.OnCompleted(() => { });

            Pipe.Writer.CancelPendingFlush();
            Pipe.Reader.AdvanceTo(Pipe.Reader.ReadAsync().GetResult().Buffer.End);

            Assert.True(awaitable.IsCompleted);

            FlushResult result = awaitable.GetResult();
            Assert.True(result.IsCanceled);
        }

        [Fact]
        public void FlushAsyncReturnsIsCancelOnCancelPendingFlushBeforeGetResult()
        {
            PipeWriter writableBuffer = Pipe.Writer.WriteEmpty(MaximumSizeHigh);
            PipeAwaiter<FlushResult> awaitable = writableBuffer.FlushAsync();

            Assert.False(awaitable.IsCompleted);
            awaitable.OnCompleted(() => { });

            Pipe.Reader.AdvanceTo(Pipe.Reader.ReadAsync().GetResult().Buffer.End);
            Pipe.Writer.CancelPendingFlush();

            Assert.True(awaitable.IsCompleted);

            FlushResult result = awaitable.GetResult();
            Assert.True(result.IsCanceled);
        }

        [Fact]
        public void FlushAsyncThrowsIfPassedCancelledCancellationToken()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            Assert.Throws<OperationCanceledException>(() => Pipe.Writer.FlushAsync(cancellationTokenSource.Token));
        }

        [Fact]
        public async Task FlushAsyncWithNewCancellationTokenNotAffectedByPrevious()
        {
            var cancellationTokenSource1 = new CancellationTokenSource();
            PipeWriter buffer = Pipe.Writer.WriteEmpty(10);
            await buffer.FlushAsync(cancellationTokenSource1.Token);

            cancellationTokenSource1.Cancel();

            var cancellationTokenSource2 = new CancellationTokenSource();
            buffer = Pipe.Writer.WriteEmpty(10);
            // Verifying that ReadAsync does not throw
            await buffer.FlushAsync(cancellationTokenSource2.Token);
        }

        [Fact]
        public void GetResultThrowsIfFlushAsyncCancelledAfterOnCompleted()
        {
            var onCompletedCalled = false;
            var cancellationTokenSource = new CancellationTokenSource();
            PipeWriter buffer = Pipe.Writer.WriteEmpty(MaximumSizeHigh);

            PipeAwaiter<FlushResult> awaiter = buffer.FlushAsync(cancellationTokenSource.Token);

            awaiter.OnCompleted(
                () => {
                    onCompletedCalled = true;
                    Assert.Throws<OperationCanceledException>(() => awaiter.GetResult());
                });

            bool awaiterIsCompleted = awaiter.IsCompleted;

            cancellationTokenSource.Cancel();

            Assert.False(awaiterIsCompleted);
            Assert.True(onCompletedCalled);
        }

        [Fact]
        public void GetResultThrowsIfFlushAsyncCancelledBeforeOnCompleted()
        {
            var onCompletedCalled = false;
            var cancellationTokenSource = new CancellationTokenSource();
            PipeWriter buffer = Pipe.Writer.WriteEmpty(MaximumSizeHigh);

            PipeAwaiter<FlushResult> awaiter = buffer.FlushAsync(cancellationTokenSource.Token);
            bool awaiterIsCompleted = awaiter.IsCompleted;

            cancellationTokenSource.Cancel();

            awaiter.OnCompleted(
                () => {
                    onCompletedCalled = true;
                    Assert.Throws<OperationCanceledException>(() => awaiter.GetResult());
                });

            Assert.False(awaiterIsCompleted);
            Assert.True(onCompletedCalled);
        }

        [Fact]
        public void GetResultThrowsIfFlushAsyncTokenFiredAfterCancelPending()
        {
            var onCompletedCalled = false;
            var cancellationTokenSource = new CancellationTokenSource();
            PipeWriter buffer = Pipe.Writer.WriteEmpty(MaximumSizeHigh);

            PipeAwaiter<FlushResult> awaiter = buffer.FlushAsync(cancellationTokenSource.Token);
            bool awaiterIsCompleted = awaiter.IsCompleted;

            Pipe.Writer.CancelPendingFlush();
            cancellationTokenSource.Cancel();

            awaiter.OnCompleted(
                () => {
                    onCompletedCalled = true;
                    Assert.Throws<OperationCanceledException>(() => awaiter.GetResult());
                });

            Assert.False(awaiterIsCompleted);
            Assert.True(onCompletedCalled);
        }
    }

    public static class TestWriterExtensions
    {
        public static PipeWriter WriteEmpty(this PipeWriter writer, int count)
        {
            writer.GetMemory(count);
            writer.Advance(count);
            return writer;
        }
    }
}
