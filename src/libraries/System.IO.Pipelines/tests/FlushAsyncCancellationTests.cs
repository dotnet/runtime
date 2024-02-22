// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipelines.Tests
{
    public class FlushAsyncCancellationTests : PipeTest
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedAndBlockingWait))]
        public void FlushAsyncCancellationDeadlock()
        {
            var cts = new CancellationTokenSource();
            var cts2 = new CancellationTokenSource();

            PipeWriter buffer = Pipe.Writer.WriteEmpty(MaximumSizeHigh);

            var e = new ManualResetEventSlim();

            ValueTaskAwaiter<FlushResult> awaiter = buffer.FlushAsync(cts.Token).GetAwaiter();
            awaiter.OnCompleted(
                () => {
                    // We are on cancellation thread and need to wait until another FlushAsync call
                    // takes pipe state lock
                    e.Wait();

                    // Make sure we had enough time to reach _cancellationTokenRegistration.Dispose
                    Thread.Sleep(100);

                    // Try to take pipe state lock
                    buffer.FlushAsync();
                });

            // Start a thread that would run cancellation callbacks
            Task cancellationTask = Task.Run(() => cts.Cancel());
            // Start a thread that would call FlushAsync with different token
            // and block on _cancellationTokenRegistration.Dispose
            Task blockingTask = Task.Run(
                () => {
                    e.Set();
                    buffer.FlushAsync(cts2.Token);
                });

            bool completed = Task.WhenAll(cancellationTask, blockingTask).Wait(TimeSpan.FromSeconds(30));
            Assert.True(completed, $"Flush tasks are not completed. CancellationTask: {cancellationTask.Status} BlockingTask: {blockingTask.Status}");
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

            ValueTask<FlushResult> flushAsync = writableBuffer.FlushAsync();

            Assert.True(flushAsync.IsCompleted);

            FlushResult flushResult = flushAsync.GetAwaiter().GetResult();

            Assert.True(flushResult.IsCanceled);

            flushAsync = writableBuffer.FlushAsync();

            Assert.True(flushAsync.IsCompleted);
        }

        [Fact]
        public void FlushAsyncNotCompletedAfterCancellation()
        {
            var onCompletedCalled = false;
            PipeWriter writableBuffer = Pipe.Writer.WriteEmpty(MaximumSizeHigh);

            ValueTaskAwaiter<FlushResult> awaitable = writableBuffer.FlushAsync().GetAwaiter();

            Assert.False(awaitable.IsCompleted);
            awaitable.OnCompleted(
                () => {
                    onCompletedCalled = true;
                    Assert.True(awaitable.IsCompleted);

                    FlushResult flushResult = awaitable.GetResult();

                    Assert.True(flushResult.IsCanceled);

                    awaitable = writableBuffer.FlushAsync().GetAwaiter();
                    Assert.False(awaitable.IsCompleted);
                });

            Pipe.Writer.CancelPendingFlush();
            Assert.True(onCompletedCalled);
        }

        [Fact]
        public void FlushAsyncNotCompletedAfterCancellationTokenCanceled()
        {
            var onCompletedCalled = false;
            var cts = new CancellationTokenSource();
            PipeWriter writableBuffer = Pipe.Writer.WriteEmpty(MaximumSizeHigh);

            ValueTaskAwaiter<FlushResult> awaitable = writableBuffer.FlushAsync(cts.Token).GetAwaiter();

            Assert.False(awaitable.IsCompleted);
            awaitable.OnCompleted(
                () => {
                    onCompletedCalled = true;
                    Assert.True(awaitable.IsCompleted);

                    Assert.Throws<OperationCanceledException>(() => awaitable.GetResult());

                    awaitable = writableBuffer.FlushAsync().GetAwaiter();
                    Assert.False(awaitable.IsCompleted);
                });

            cts.Cancel();
            Assert.True(onCompletedCalled);
        }

        [Fact]
        public void FlushAsyncReturnsCanceledIfCanceledBeforeFlush()
        {
            PipeWriter writableBuffer = Pipe.Writer.WriteEmpty(MaximumSizeHigh);

            Pipe.Writer.CancelPendingFlush();

            ValueTask<FlushResult> flushAsync = writableBuffer.FlushAsync();

            Assert.True(flushAsync.IsCompleted);
            FlushResult flushResult = flushAsync.GetAwaiter().GetResult();
            Assert.True(flushResult.IsCanceled);
        }

        [Fact]
        public void FlushAsyncReturnsCanceledIfFlushCanceled()
        {
            PipeWriter writableBuffer = Pipe.Writer.WriteEmpty(MaximumSizeHigh);
            ValueTask<FlushResult> flushAsync = writableBuffer.FlushAsync();

            Assert.False(flushAsync.IsCompleted);

            Pipe.Writer.CancelPendingFlush();

            Assert.True(flushAsync.IsCompleted);
            FlushResult flushResult = flushAsync.GetAwaiter().GetResult();
            Assert.True(flushResult.IsCanceled);
        }

        [Fact]
        public void FlushAsyncReturnsIsCancelOnCancelPendingFlushAfterGetResult()
        {
            PipeWriter writableBuffer = Pipe.Writer.WriteEmpty(MaximumSizeHigh);
            ValueTaskAwaiter<FlushResult> awaitable = writableBuffer.FlushAsync().GetAwaiter();

            Assert.False(awaitable.IsCompleted);
            awaitable.OnCompleted(() => { });

            Pipe.Writer.CancelPendingFlush();
            Pipe.Reader.AdvanceTo(Pipe.Reader.ReadAsync().GetAwaiter().GetResult().Buffer.End);

            Assert.True(awaitable.IsCompleted);

            FlushResult result = awaitable.GetResult();
            Assert.True(result.IsCanceled);
        }

        [Fact]
        public void FlushAsyncReturnsIsCancelOnCancelPendingFlushBeforeGetResult()
        {
            PipeWriter writableBuffer = Pipe.Writer.WriteEmpty(MaximumSizeHigh);
            ValueTaskAwaiter<FlushResult> awaitable = writableBuffer.FlushAsync().GetAwaiter();

            Assert.False(awaitable.IsCompleted);
            awaitable.OnCompleted(() => { });

            Pipe.Reader.AdvanceTo(Pipe.Reader.ReadAsync().GetAwaiter().GetResult().Buffer.End);
            Pipe.Writer.CancelPendingFlush();

            Assert.True(awaitable.IsCompleted);

            FlushResult result = awaitable.GetResult();
            Assert.True(result.IsCanceled);
        }

        [Fact]
        public async Task FlushAsyncThrowsIfPassedCanceledCancellationToken()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            ValueTask<FlushResult> task = Pipe.Writer.FlushAsync(cancellationTokenSource.Token);
            Assert.True(task.IsCanceled);
            await AssertExtensions.CanceledAsync(cancellationTokenSource.Token, async () => await task);
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
        public void GetResultThrowsIfFlushAsyncCanceledAfterOnCompleted()
        {
            var onCompletedCalled = false;
            var cancellationTokenSource = new CancellationTokenSource();
            PipeWriter buffer = Pipe.Writer.WriteEmpty(MaximumSizeHigh);

            ValueTaskAwaiter<FlushResult> awaiter = buffer.FlushAsync(cancellationTokenSource.Token).GetAwaiter();

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
        public void GetResultThrowsIfFlushAsyncCanceledBeforeOnCompleted()
        {
            var onCompletedCalled = false;
            var cancellationTokenSource = new CancellationTokenSource();
            PipeWriter buffer = Pipe.Writer.WriteEmpty(MaximumSizeHigh);

            ValueTaskAwaiter<FlushResult> awaiter = buffer.FlushAsync(cancellationTokenSource.Token).GetAwaiter();
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

            ValueTaskAwaiter<FlushResult> awaiter = buffer.FlushAsync(cancellationTokenSource.Token).GetAwaiter();
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

        [Fact]
        public async Task FlushAsyncThrowsIfPassedCanceledCancellationTokenAndPipeIsAbleToComplete()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // AsTask is important here, it validates that we are calling completion callback
            // and not only setting IsCompleted flag
            var task = Pipe.Reader.ReadAsync().AsTask();

            await AssertExtensions.CanceledAsync(cancellationTokenSource.Token, async () => await Pipe.Writer.FlushAsync(cancellationTokenSource.Token));

            Pipe.Writer.Complete();

            Assert.True(task.IsCompleted);
            Assert.True(task.Result.IsCompleted);
        }

        [Fact]
        public async Task ReadAsyncReturnsDataAfterItWasWrittenDuringCancelledRead()
        {
            ValueTask<ReadResult> readTask = Pipe.Reader.ReadAsync();
            ValueTaskAwaiter<ReadResult> awaiter = readTask.GetAwaiter();
            ReadResult result = default;
            awaiter.OnCompleted(
                () =>
                {
                    Pipe.Writer.WriteAsync(new byte[] { 1 }).AsTask().Wait();
                    result = awaiter.GetResult();
                });

            Pipe.Reader.CancelPendingRead();

            Assert.True(result.IsCanceled);

            result = await Pipe.Reader.ReadAsync();
            Assert.False(result.IsCanceled);
            Assert.Equal(new byte[] { 1 }, result.Buffer.ToArray());
        }
    }

    public static class TestWriterExtensions
    {
        public static PipeWriter WriteEmpty(this PipeWriter writer, int count)
        {
            writer.GetSpan(count).Slice(0, count).Clear();
            writer.Advance(count);
            return writer;
        }
    }
}
