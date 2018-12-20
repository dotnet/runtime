// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipelines.Tests
{
    public class ReadAsyncCancellationTests : PipeTest
    {
        [Fact]
        public async Task AdvanceShouldResetStateIfReadCanceled()
        {
            Pipe.Reader.CancelPendingRead();

            ReadResult result = await Pipe.Reader.ReadAsync();
            ReadOnlySequence<byte> buffer = result.Buffer;
            Pipe.Reader.AdvanceTo(buffer.End);

            Assert.False(result.IsCompleted);
            Assert.True(result.IsCanceled);
            Assert.True(buffer.IsEmpty);

            ValueTask<ReadResult> awaitable = Pipe.Reader.ReadAsync();
            Assert.False(awaitable.IsCompleted);
        }

        [Fact]
        public async Task CancellingBeforeAdvance()
        {
            byte[] bytes = Encoding.ASCII.GetBytes("Hello World");
            PipeWriter output = Pipe.Writer;
            output.Write(bytes);
            await output.FlushAsync();

            ReadResult result = await Pipe.Reader.ReadAsync();
            ReadOnlySequence<byte> buffer = result.Buffer;

            Assert.Equal(11, buffer.Length);
            Assert.False(result.IsCanceled);
            Assert.True(buffer.IsSingleSegment);
            var array = new byte[11];
            buffer.First.Span.CopyTo(array);
            Assert.Equal("Hello World", Encoding.ASCII.GetString(array));

            Pipe.Reader.CancelPendingRead();

            Pipe.Reader.AdvanceTo(buffer.End);

            ValueTask<ReadResult> awaitable = Pipe.Reader.ReadAsync();

            Assert.True(awaitable.IsCompleted);

            result = await awaitable;

            Assert.True(result.IsCanceled);

            Pipe.Reader.AdvanceTo(result.Buffer.Start, result.Buffer.Start);
        }

        [Fact]
        public async Task CancellingPendingAfterReadAsync()
        {
            byte[] bytes = Encoding.ASCII.GetBytes("Hello World");
            PipeWriter output = Pipe.Writer;
            output.Write(bytes);

            Func<Task> taskFunc = async () => {
                ReadResult result = await Pipe.Reader.ReadAsync();
                ReadOnlySequence<byte> buffer = result.Buffer;
                Pipe.Reader.AdvanceTo(buffer.End);

                Assert.False(result.IsCompleted);
                Assert.True(result.IsCanceled);
                Assert.True(buffer.IsEmpty);

                await output.FlushAsync();

                result = await Pipe.Reader.ReadAsync();
                buffer = result.Buffer;

                Assert.Equal(11, buffer.Length);
                Assert.True(buffer.IsSingleSegment);
                Assert.False(result.IsCanceled);
                var array = new byte[11];
                buffer.First.Span.CopyTo(array);
                Assert.Equal("Hello World", Encoding.ASCII.GetString(array));
                Pipe.Reader.AdvanceTo(result.Buffer.End, result.Buffer.End);

                Pipe.Reader.Complete();
            };

            Task task = taskFunc();

            Pipe.Reader.CancelPendingRead();

            await task;

            Pipe.Writer.Complete();
        }

        [Fact]
        public async Task CancellingPendingReadBeforeReadAsync()
        {
            Pipe.Reader.CancelPendingRead();

            ReadResult result = await Pipe.Reader.ReadAsync();
            ReadOnlySequence<byte> buffer = result.Buffer;
            Pipe.Reader.AdvanceTo(buffer.End);

            Assert.False(result.IsCompleted);
            Assert.True(result.IsCanceled);
            Assert.True(buffer.IsEmpty);

            byte[] bytes = Encoding.ASCII.GetBytes("Hello World");
            PipeWriter output = Pipe.Writer;
            output.Write(bytes);
            await output.FlushAsync();

            result = await Pipe.Reader.ReadAsync();
            buffer = result.Buffer;

            Assert.Equal(11, buffer.Length);
            Assert.False(result.IsCanceled);
            Assert.True(buffer.IsSingleSegment);
            var array = new byte[11];
            buffer.First.Span.CopyTo(array);
            Assert.Equal("Hello World", Encoding.ASCII.GetString(array));

            Pipe.Reader.AdvanceTo(buffer.Start, buffer.Start);
        }

        [Fact]
        public void FlushAsyncCancellationDeadlock()
        {
            var cts = new CancellationTokenSource();
            var cts2 = new CancellationTokenSource();
            var e = new ManualResetEventSlim();

            ValueTaskAwaiter<ReadResult> awaiter = Pipe.Reader.ReadAsync(cts.Token).GetAwaiter();
            awaiter.OnCompleted(
                () => {
                    // We are on cancellation thread and need to wait untill another ReadAsync call
                    // takes pipe state lock
                    e.Wait();
                    // Make sure we had enough time to reach _cancellationTokenRegistration.Dispose
                    Thread.Sleep(100);
                    // Try to take pipe state lock
                    Pipe.Reader.ReadAsync();
                });

            // Start a thread that would run cancellation calbacks
            Task cancellationTask = Task.Run(() => cts.Cancel());
            // Start a thread that would call ReadAsync with different token
            // and block on _cancellationTokenRegistration.Dispose
            Task blockingTask = Task.Run(
                () => {
                    e.Set();
                    Pipe.Reader.ReadAsync(cts2.Token);
                });

            bool completed = Task.WhenAll(cancellationTask, blockingTask).Wait(TimeSpan.FromSeconds(10));
            Assert.True(completed);
        }

        [Fact]
        public void GetResultThrowsIfFlushAsyncTokenFiredAfterCancelPending()
        {
            var onCompletedCalled = false;
            var cancellationTokenSource = new CancellationTokenSource();

            ValueTaskAwaiter<ReadResult> awaiter = Pipe.Reader.ReadAsync(cancellationTokenSource.Token).GetAwaiter();
            bool awaiterIsCompleted = awaiter.IsCompleted;

            cancellationTokenSource.Cancel();
            Pipe.Reader.CancelPendingRead();

            awaiter.OnCompleted(
                () => {
                    onCompletedCalled = true;
                    Assert.Throws<OperationCanceledException>(() => awaiter.GetResult());
                });

            Assert.False(awaiterIsCompleted);
            Assert.True(onCompletedCalled);
        }

        [Fact]
        public void GetResultThrowsIfReadAsyncCanceledAfterOnCompleted()
        {
            var onCompletedCalled = false;
            var cancellationTokenSource = new CancellationTokenSource();

            ValueTaskAwaiter<ReadResult> awaiter = Pipe.Reader.ReadAsync(cancellationTokenSource.Token).GetAwaiter();
            bool awaiterIsCompleted = awaiter.IsCompleted;
            awaiter.OnCompleted(
                () => {
                    onCompletedCalled = true;
                    Assert.Throws<OperationCanceledException>(() => awaiter.GetResult());
                });

            cancellationTokenSource.Cancel();

            Assert.False(awaiterIsCompleted);
            Assert.True(onCompletedCalled);
        }

        [Fact]
        public void GetResultThrowsIfReadAsyncCanceledBeforeOnCompleted()
        {
            var onCompletedCalled = false;
            var cancellationTokenSource = new CancellationTokenSource();

            ValueTaskAwaiter<ReadResult> awaiter = Pipe.Reader.ReadAsync(cancellationTokenSource.Token).GetAwaiter();
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
        public async Task ReadAsyncCancellationE2E()
        {
            var cts = new CancellationTokenSource();
            var e = new AutoResetEvent(false);
            var cancelled = false;

            Func<Task> taskFunc = async () => {
                try
                {
                    ReadResult result = await Pipe.Reader.ReadAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    cancelled = true;
                    ReadResult result = await Pipe.Reader.ReadAsync();
                    Assert.Equal(new byte[] { 1, 2, 3 }, result.Buffer.ToArray());
                    Pipe.Reader.AdvanceTo(result.Buffer.End);
                }
            };

            Task task = taskFunc();

            cts.Cancel();

            await Pipe.Writer.WriteAsync(new byte[] { 1, 2, 3 });
            await task;
            Assert.True(cancelled);
        }

        [Fact]
        public void ReadAsyncCompletedAfterPreCancellation()
        {
            Pipe.Reader.CancelPendingRead();
            Pipe.Writer.WriteAsync(new byte[] { 1, 2, 3 }).GetAwaiter().GetResult();

            ValueTaskAwaiter<ReadResult> awaitable = Pipe.Reader.ReadAsync().GetAwaiter();

            Assert.True(awaitable.IsCompleted);

            ReadResult result = awaitable.GetResult();

            Assert.True(result.IsCanceled);

            awaitable = Pipe.Reader.ReadAsync().GetAwaiter();

            Assert.True(awaitable.IsCompleted);

            Pipe.Reader.AdvanceTo(awaitable.GetResult().Buffer.End);
        }

        [Fact]
        public void ReadAsyncNotCompletedAfterCancellation()
        {
            var onCompletedCalled = false;
            ValueTaskAwaiter<ReadResult> awaitable = Pipe.Reader.ReadAsync().GetAwaiter();

            Assert.False(awaitable.IsCompleted);
            awaitable.OnCompleted(
                () => {
                    onCompletedCalled = true;
                    Assert.True(awaitable.IsCompleted);

                    ReadResult readResult = awaitable.GetResult();
                    Assert.True(readResult.IsCanceled);

                    awaitable = Pipe.Reader.ReadAsync().GetAwaiter();
                    Assert.False(awaitable.IsCompleted);
                });

            Pipe.Reader.CancelPendingRead();
            Assert.True(onCompletedCalled);
        }

        [Fact]
        public void ReadAsyncNotCompletedAfterCancellationTokenCanceled()
        {
            var onCompletedCalled = false;
            var cts = new CancellationTokenSource();
            ValueTaskAwaiter<ReadResult> awaitable = Pipe.Reader.ReadAsync(cts.Token).GetAwaiter();

            Assert.False(awaitable.IsCompleted);
            awaitable.OnCompleted(
                () => {
                    onCompletedCalled = true;
                    Assert.True(awaitable.IsCompleted);

                    Assert.Throws<OperationCanceledException>(() => awaitable.GetResult());

                    awaitable = Pipe.Reader.ReadAsync().GetAwaiter();
                    Assert.False(awaitable.IsCompleted);
                });

            cts.Cancel();
            Assert.True(onCompletedCalled);
        }

        [Fact]
        public void ReadAsyncReturnsIsCancelOnCancelPendingReadAfterGetResult()
        {
            ValueTaskAwaiter<ReadResult> awaitable = Pipe.Reader.ReadAsync().GetAwaiter();

            Assert.False(awaitable.IsCompleted);
            awaitable.OnCompleted(() => { });

            Pipe.Writer.WriteAsync(new byte[] { });
            Pipe.Reader.CancelPendingRead();

            Assert.True(awaitable.IsCompleted);

            ReadResult result = awaitable.GetResult();
            Assert.True(result.IsCanceled);
        }

        [Fact]
        public void ReadAsyncReturnsIsCancelOnCancelPendingReadBeforeGetResult()
        {
            ValueTaskAwaiter<ReadResult> awaitable = Pipe.Reader.ReadAsync().GetAwaiter();

            Assert.False(awaitable.IsCompleted);
            awaitable.OnCompleted(() => { });

            Pipe.Writer.WriteAsync(new byte[] { });
            Pipe.Reader.CancelPendingRead();

            Assert.True(awaitable.IsCompleted);

            ReadResult result = awaitable.GetResult();
            Assert.True(result.IsCanceled);
        }

        [Fact]
        public void ReadAsyncThrowsIfPassedCanceledCancellationToken()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            Assert.Throws<OperationCanceledException>(() => Pipe.Reader.ReadAsync(cancellationTokenSource.Token));
        }

        [Fact]
        public async Task ReadAsyncWithNewCancellationTokenNotAffectedByPrevious()
        {
            await Pipe.Writer.WriteAsync(new byte[] { 0 });

            var cancellationTokenSource1 = new CancellationTokenSource();
            ReadResult result = await Pipe.Reader.ReadAsync(cancellationTokenSource1.Token);
            Pipe.Reader.AdvanceTo(result.Buffer.Start);

            cancellationTokenSource1.Cancel();
            var cancellationTokenSource2 = new CancellationTokenSource();

            // Verifying that ReadAsync does not throw
            result = await Pipe.Reader.ReadAsync(cancellationTokenSource2.Token);
            Pipe.Reader.AdvanceTo(result.Buffer.Start);
        }

        [Fact]
        public async Task ReadingCanBeCanceled()
        {
            var cts = new CancellationTokenSource();
            cts.Token.Register(() => { Pipe.Writer.Complete(new OperationCanceledException(cts.Token)); });

            Task ignore = Task.Run(
                async () => {
                    await Task.Delay(1000);
                    cts.Cancel();
                });

            await Assert.ThrowsAsync<OperationCanceledException>(
                async () => {
                    ReadResult result = await Pipe.Reader.ReadAsync();
                    ReadOnlySequence<byte> buffer = result.Buffer;
                });
        }

        [Fact]
        public async Task WriteAndCancellingPendingReadBeforeReadAsync()
        {
            byte[] bytes = Encoding.ASCII.GetBytes("Hello World");
            PipeWriter output = Pipe.Writer;
            output.Write(bytes);
            await output.FlushAsync();

            Pipe.Reader.CancelPendingRead();

            ReadResult result = await Pipe.Reader.ReadAsync();
            ReadOnlySequence<byte> buffer = result.Buffer;

            Assert.False(result.IsCompleted);
            Assert.True(result.IsCanceled);
            Assert.False(buffer.IsEmpty);
            Assert.Equal(11, buffer.Length);
            Assert.True(buffer.IsSingleSegment);
            var array = new byte[11];
            buffer.First.Span.CopyTo(array);
            Assert.Equal("Hello World", Encoding.ASCII.GetString(array));
            Pipe.Reader.AdvanceTo(buffer.End, buffer.End);
        }

        [Fact]
        public async Task ReadAsyncIsNotCancelledWhenCancellationTokenCancelledBetweenReads()
        {
            await Pipe.Writer.WriteAsync(new byte[1]);

            var cts = new CancellationTokenSource();

            ReadResult rr = await Pipe.Reader.ReadAsync(cts.Token);
            Assert.False(rr.IsCanceled);

            cts.Cancel();
            Pipe.Reader.AdvanceTo(rr.Buffer.End);

            cts = new CancellationTokenSource();
            ValueTask<ReadResult> awaiter = Pipe.Reader.ReadAsync(cts.Token);
            Assert.False(awaiter.IsCompleted);
        }

        [Fact]
        public async Task TryReadIsNotCancelledWhenCancellationTokenCancelledBetweenReads()
        {
            await Pipe.Writer.WriteAsync(new byte[1]);

            var cts = new CancellationTokenSource();
            ReadResult result = await Pipe.Reader.ReadAsync(cts.Token);
            Pipe.Reader.AdvanceTo(result.Buffer.End);

            cts.Cancel();

            Assert.False(Pipe.Reader.TryRead(out result));
            Assert.False(result.IsCanceled);
        }

        [Fact]
        public async Task TryReadWithDataIsNotCancelledWhenCancellationTokenCancelledBetweenReads()
        {
            await Pipe.Writer.WriteAsync(new byte[1]);

            var cts = new CancellationTokenSource();
            ReadResult result = await Pipe.Reader.ReadAsync(cts.Token);
            Pipe.Reader.AdvanceTo(result.Buffer.End);

            cts.Cancel();
            await Pipe.Writer.WriteAsync(new byte[1]);
            Assert.True(Pipe.Reader.TryRead(out result));
        }
    }
}
