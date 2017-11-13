// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace System.Threading.Channels.Tests
{
    public abstract class ChannelTestBase : TestBase
    {
        protected abstract Channel<int> CreateChannel();
        protected abstract Channel<int> CreateFullChannel();

        protected virtual bool RequiresSingleReader => false;
        protected virtual bool RequiresSingleWriter => false;
        protected virtual bool BuffersItems => true;

        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Requires internal reflection on framework types.")]
        [Fact]
        public void ValidateDebuggerAttributes()
        {
            Channel<int> c = CreateChannel();
            for (int i = 1; i <= 10; i++)
            {
                c.Writer.WriteAsync(i);
            }
            DebuggerAttributes.ValidateDebuggerDisplayReferences(c);
            DebuggerAttributes.InvokeDebuggerTypeProxyProperties(c);
        }

        [Fact]
        public void Cast_MatchesInOut()
        {
            Channel<int> c = CreateChannel();
            ChannelReader<int> rc = c;
            ChannelWriter<int> wc = c;
            Assert.Same(rc, c.Reader);
            Assert.Same(wc, c.Writer);
        }

        [Fact]
        public void Completion_Idempotent()
        {
            Channel<int> c = CreateChannel();

            Task completion = c.Reader.Completion;
            Assert.Equal(TaskStatus.WaitingForActivation, completion.Status);

            Assert.Same(completion, c.Reader.Completion);
            c.Writer.Complete();
            Assert.Same(completion, c.Reader.Completion);

            Assert.Equal(TaskStatus.RanToCompletion, completion.Status);
        }

        [Fact]
        public async Task Complete_AfterEmpty_NoWaiters_TriggersCompletion()
        {
            Channel<int> c = CreateChannel();
            c.Writer.Complete();
            await c.Reader.Completion;
        }

        [Fact]
        public async Task Complete_AfterEmpty_WaitingReader_TriggersCompletion()
        {
            Channel<int> c = CreateChannel();
            Task<int> r = c.Reader.ReadAsync().AsTask();
            c.Writer.Complete();
            await c.Reader.Completion;
            await Assert.ThrowsAnyAsync<InvalidOperationException>(() => r);
        }

        [Fact]
        public async Task Complete_BeforeEmpty_WaitingReaders_TriggersCompletion()
        {
            Channel<int> c = CreateChannel();
            Task<int> read = c.Reader.ReadAsync().AsTask();
            c.Writer.Complete();
            await c.Reader.Completion;
            await Assert.ThrowsAnyAsync<InvalidOperationException>(() => read);
        }

        [Fact]
        public void Complete_Twice_ThrowsInvalidOperationException()
        {
            Channel<int> c = CreateChannel();
            c.Writer.Complete();
            Assert.ThrowsAny<InvalidOperationException>(() => c.Writer.Complete());
        }

        [Fact]
        public void TryComplete_Twice_ReturnsTrueThenFalse()
        {
            Channel<int> c = CreateChannel();
            Assert.True(c.Writer.TryComplete());
            Assert.False(c.Writer.TryComplete());
            Assert.False(c.Writer.TryComplete());
        }

        [Fact]
        public async Task TryComplete_ErrorsPropage()
        {
            Channel<int> c;

            // Success
            c = CreateChannel();
            Assert.True(c.Writer.TryComplete());
            await c.Reader.Completion;

            // Error
            c = CreateChannel();
            Assert.True(c.Writer.TryComplete(new FormatException()));
            await Assert.ThrowsAsync<FormatException>(() => c.Reader.Completion);

            // Canceled
            c = CreateChannel();
            var cts = new CancellationTokenSource();
            cts.Cancel();
            Assert.True(c.Writer.TryComplete(new OperationCanceledException(cts.Token)));
            await AssertCanceled(c.Reader.Completion, cts.Token);
        }

        [Fact]
        public void SingleProducerConsumer_ConcurrentReadWrite_Success()
        {
            Channel<int> c = CreateChannel();

            const int NumItems = 100000;
            Task.WaitAll(
                Task.Run(async () =>
                {
                    for (int i = 0; i < NumItems; i++)
                    {
                        await c.Writer.WriteAsync(i);
                    }
                }),
                Task.Run(async () =>
                {
                    for (int i = 0; i < NumItems; i++)
                    {
                        Assert.Equal(i, await c.Reader.ReadAsync());
                    }
                }));
        }

        [Fact]
        public void SingleProducerConsumer_PingPong_Success()
        {
            Channel<int> c1 = CreateChannel();
            Channel<int> c2 = CreateChannel();

            const int NumItems = 100000;
            Task.WaitAll(
                Task.Run(async () =>
                {
                    for (int i = 0; i < NumItems; i++)
                    {
                        Assert.Equal(i, await c1.Reader.ReadAsync());
                        await c2.Writer.WriteAsync(i);
                    }
                }),
                Task.Run(async () =>
                {
                    for (int i = 0; i < NumItems; i++)
                    {
                        await c1.Writer.WriteAsync(i);
                        Assert.Equal(i, await c2.Reader.ReadAsync());
                    }
                }));
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(1, 10)]
        [InlineData(10, 1)]
        [InlineData(10, 10)]
        public void ManyProducerConsumer_ConcurrentReadWrite_Success(int numReaders, int numWriters)
        {
            if (RequiresSingleReader && numReaders > 1)
            {
                return;
            }

            if (RequiresSingleWriter && numWriters > 1)
            {
                return;
            }

            Channel<int> c = CreateChannel();

            const int NumItems = 10000;

            long readTotal = 0;
            int remainingWriters = numWriters;
            int remainingItems = NumItems;

            Task[] tasks = new Task[numWriters + numReaders];

            for (int i = 0; i < numReaders; i++)
            {
                tasks[i] = Task.Run(async () =>
                {
                    try
                    {
                        while (await c.Reader.WaitToReadAsync())
                        {
                            if (c.Reader.TryRead(out int value))
                            {
                                Interlocked.Add(ref readTotal, value);
                            }
                        }
                    }
                    catch (ChannelClosedException) { }
                });
            }

            for (int i = 0; i < numWriters; i++)
            {
                tasks[numReaders + i] = Task.Run(async () =>
                {
                    while (true)
                    {
                        int value = Interlocked.Decrement(ref remainingItems);
                        if (value < 0)
                        {
                            break;
                        }
                        await c.Writer.WriteAsync(value + 1);
                    }
                    if (Interlocked.Decrement(ref remainingWriters) == 0)
                    {
                        c.Writer.Complete();
                    }
                });
            }

            Task.WaitAll(tasks);
            Assert.Equal((NumItems * (NumItems + 1L)) / 2, readTotal);
        }

        [Fact]
        public void WaitToReadAsync_DataAvailableBefore_CompletesSynchronously()
        {
            Channel<int> c = CreateChannel();
            Task write = c.Writer.WriteAsync(42);
            Task<bool> read = c.Reader.WaitToReadAsync();
            Assert.Equal(TaskStatus.RanToCompletion, read.Status);
        }

        [Fact]
        public void WaitToReadAsync_DataAvailableAfter_CompletesAsynchronously()
        {
            Channel<int> c = CreateChannel();
            Task<bool> read = c.Reader.WaitToReadAsync();
            Assert.False(read.IsCompleted);
            Task write = c.Writer.WriteAsync(42);
            Assert.True(read.Result);
        }

        [Fact]
        public void WaitToReadAsync_AfterComplete_SynchronouslyCompletes()
        {
            Channel<int> c = CreateChannel();
            c.Writer.Complete();
            Task<bool> read = c.Reader.WaitToReadAsync();
            Assert.Equal(TaskStatus.RanToCompletion, read.Status);
            Assert.False(read.Result);
        }

        [Fact]
        public void WaitToReadAsync_BeforeComplete_AsynchronouslyCompletes()
        {
            Channel<int> c = CreateChannel();
            Task<bool> read = c.Reader.WaitToReadAsync();
            Assert.False(read.IsCompleted);
            c.Writer.Complete();
            Assert.False(read.Result);
        }

        [Fact]
        public void WaitToWriteAsync_AfterComplete_SynchronouslyCompletes()
        {
            Channel<int> c = CreateChannel();
            c.Writer.Complete();
            Task<bool> write = c.Writer.WaitToWriteAsync();
            Assert.Equal(TaskStatus.RanToCompletion, write.Status);
            Assert.False(write.Result);
        }

        [Fact]
        public void WaitToWriteAsync_EmptyChannel_SynchronouslyCompletes()
        {
            if (!BuffersItems)
            {
                return;
            }

            Channel<int> c = CreateChannel();
            Task<bool> write = c.Writer.WaitToWriteAsync();
            Assert.Equal(TaskStatus.RanToCompletion, write.Status);
            Assert.True(write.Result);
        }

        [Fact]
        public async Task WaitToWriteAsync_ManyConcurrent_SatisifedByReaders()
        {
            if (RequiresSingleReader || RequiresSingleWriter)
            {
                return;
            }

            Channel<int> c = CreateChannel();

            Task[] writers = Enumerable.Range(0, 100).Select(_ => c.Writer.WaitToWriteAsync()).ToArray();
            Task[] readers = Enumerable.Range(0, 100).Select(_ => c.Reader.ReadAsync().AsTask()).ToArray();

            await Task.WhenAll(writers);
        }

        [Fact]
        public void WaitToWriteAsync_BlockedReader_ReturnsTrue()
        {
            Channel<int> c = CreateChannel();
            ValueTask<int> reader = c.Reader.ReadAsync();
            AssertSynchronousSuccess(c.Writer.WaitToWriteAsync());
        }

        [Fact]
        public void TryRead_DataAvailable_Success()
        {
            Channel<int> c = CreateChannel();
            Task write = c.Writer.WriteAsync(42);
            Assert.True(c.Reader.TryRead(out int result));
            Assert.Equal(42, result);
        }

        [Fact]
        public void TryRead_AfterComplete_ReturnsFalse()
        {
            Channel<int> c = CreateChannel();
            c.Writer.Complete();
            Assert.False(c.Reader.TryRead(out int result));
        }

        [Fact]
        public void TryWrite_AfterComplete_ReturnsFalse()
        {
            Channel<int> c = CreateChannel();
            c.Writer.Complete();
            Assert.False(c.Writer.TryWrite(42));
        }

        [Fact]
        public async Task WriteAsync_AfterComplete_ThrowsException()
        {
            Channel<int> c = CreateChannel();
            c.Writer.Complete();
            await Assert.ThrowsAnyAsync<InvalidOperationException>(() => c.Writer.WriteAsync(42));
        }

        [Fact]
        public async Task Complete_WithException_PropagatesToCompletion()
        {
            Channel<int> c = CreateChannel();
            var exc = new FormatException();
            c.Writer.Complete(exc);
            Assert.Same(exc, await Assert.ThrowsAsync<FormatException>(() => c.Reader.Completion));
        }

        [Fact]
        public async Task Complete_WithCancellationException_PropagatesToCompletion()
        {
            Channel<int> c = CreateChannel();
            var cts = new CancellationTokenSource();
            cts.Cancel();

            Exception exc = null;
            try { cts.Token.ThrowIfCancellationRequested(); }
            catch (Exception e) { exc = e; }

            c.Writer.Complete(exc);
            await AssertCanceled(c.Reader.Completion, cts.Token);
        }

        [Fact]
        public async Task Complete_WithException_PropagatesToExistingWriter()
        {
            Channel<int> c = CreateFullChannel();
            if (c != null)
            {
                Task write = c.Writer.WriteAsync(42);
                var exc = new FormatException();
                c.Writer.Complete(exc);
                Assert.Same(exc, (await Assert.ThrowsAsync<ChannelClosedException>(() => write)).InnerException);
            }
        }

        [Fact]
        public async Task Complete_WithException_PropagatesToNewWriter()
        {
            Channel<int> c = CreateChannel();
            var exc = new FormatException();
            c.Writer.Complete(exc);
            Task write = c.Writer.WriteAsync(42);
            Assert.Same(exc, (await Assert.ThrowsAsync<ChannelClosedException>(() => write)).InnerException);
        }

        [Fact]
        public async Task Complete_WithException_PropagatesToExistingWaitingReader()
        {
            Channel<int> c = CreateChannel();
            Task<bool> read = c.Reader.WaitToReadAsync();
            var exc = new FormatException();
            c.Writer.Complete(exc);
            await Assert.ThrowsAsync<FormatException>(() => read);
        }

        [Fact]
        public async Task Complete_WithException_PropagatesToNewWaitingReader()
        {
            Channel<int> c = CreateChannel();
            var exc = new FormatException();
            c.Writer.Complete(exc);
            Task<bool> read = c.Reader.WaitToReadAsync();
            await Assert.ThrowsAsync<FormatException>(() => read);
        }

        [Fact]
        public async Task Complete_WithException_PropagatesToNewWaitingWriter()
        {
            Channel<int> c = CreateChannel();
            var exc = new FormatException();
            c.Writer.Complete(exc);
            Task<bool> write = c.Writer.WaitToWriteAsync();
            await Assert.ThrowsAsync<FormatException>(() => write);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void ManyWriteAsync_ThenManyTryRead_Success(int readMode)
        {
            if (RequiresSingleReader || RequiresSingleWriter)
            {
                return;
            }

            Channel<int> c = CreateChannel();

            const int NumItems = 2000;

            Task[] writers = new Task[NumItems];
            for (int i = 0; i < writers.Length; i++)
            {
                writers[i] = c.Writer.WriteAsync(i);
            }

            Task<int>[] readers = new Task<int>[NumItems];
            for (int i = 0; i < readers.Length; i++)
            {
                int result;
                Assert.True(c.Reader.TryRead(out result));
                Assert.Equal(i, result);
            }

            Assert.All(writers, w => Assert.True(w.IsCompleted));
        }

        [Fact]
        public void Precancellation_Writing_ReturnsImmediately()
        {
            Channel<int> c = CreateChannel();

            Task writeTask = c.Writer.WriteAsync(42, new CancellationToken(true));
            Assert.Equal(TaskStatus.Canceled, writeTask.Status);

            Task<bool> waitTask = c.Writer.WaitToWriteAsync(new CancellationToken(true));
            Assert.Equal(TaskStatus.Canceled, waitTask.Status);
        }

        [Fact]
        public void Write_WaitToReadAsync_CompletesSynchronously()
        {
            Channel<int> c = CreateChannel();
            c.Writer.WriteAsync(42);
            AssertSynchronousTrue(c.Reader.WaitToReadAsync());
        }

        [Fact]
        public void Precancellation_WaitToReadAsync_ReturnsImmediately()
        {
            Channel<int> c = CreateChannel();

            Task writeTask = c.Reader.WaitToReadAsync(new CancellationToken(true));
            Assert.Equal(TaskStatus.Canceled, writeTask.Status);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task WaitToReadAsync_DataWritten_CompletesSuccessfully(bool cancelable)
        {
            Channel<int> c = CreateChannel();
            CancellationToken token = cancelable ? new CancellationTokenSource().Token : default;

            Task<bool> read = c.Reader.WaitToReadAsync(token);
            Assert.False(read.IsCompleted);

            Task write = c.Writer.WriteAsync(42, token);

            Assert.True(await read);
        }

        [Fact]
        public async Task WaitToReadAsync_NoDataWritten_Canceled_CompletesAsCanceled()
        {
            Channel<int> c = CreateChannel();
            var cts = new CancellationTokenSource();

            Task<bool> read = c.Reader.WaitToReadAsync(cts.Token);
            Assert.False(read.IsCompleted);
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => read);
        }

        [Fact]
        public async Task ReadAsync_ThenWriteAsync_Succeeds()
        {
            Channel<int> c = CreateChannel();

            ValueTask<int> r = c.Reader.ReadAsync();
            Assert.False(r.IsCompleted);

            Task w = c.Writer.WriteAsync(42);
            AssertSynchronousSuccess(w);

            Assert.Equal(42, await r);
        }

        [Fact]
        public async Task WriteAsync_ReadAsync_Succeeds()
        {
            Channel<int> c = CreateChannel();

            Task w = c.Writer.WriteAsync(42);
            ValueTask<int> r = c.Reader.ReadAsync();

            await Task.WhenAll(w, r.AsTask());

            Assert.Equal(42, await r);
        }

        [Fact]
        public void ReadAsync_Precanceled_CanceledSynchronously()
        {
            Channel<int> c = CreateChannel();
            var cts = new CancellationTokenSource();
            cts.Cancel();
            AssertSynchronouslyCanceled(c.Reader.ReadAsync(cts.Token).AsTask(), cts.Token);
        }

        [Fact]
        public async Task ReadAsync_Canceled_CanceledAsynchronously()
        {
            Channel<int> c = CreateChannel();
            var cts = new CancellationTokenSource();

            ValueTask<int> r = c.Reader.ReadAsync(cts.Token);
            Assert.False(r.IsCompleted);

            cts.Cancel();

            await AssertCanceled(r.AsTask(), cts.Token);

            if (c.Writer.TryWrite(42))
            {
                Assert.Equal(42, await c.Reader.ReadAsync());
            }
        }

        [Fact]
        public async Task ReadAsync_WriteAsync_ManyConcurrentReaders_SerializedWriters_Success()
        {
            if (RequiresSingleReader)
            {
                return;
            }

            Channel<int> c = CreateChannel();
            const int Items = 100;

            ValueTask<int>[] readers = (from i in Enumerable.Range(0, Items) select c.Reader.ReadAsync()).ToArray();
            for (int i = 0; i < Items; i++)
            {
                await c.Writer.WriteAsync(i);
            }

            Assert.Equal((Items * (Items - 1)) / 2, Enumerable.Sum(await Task.WhenAll(readers.Select(r => r.AsTask()))));
        }

        [Fact]
        public async Task ReadAsync_TryWrite_ManyConcurrentReaders_SerializedWriters_Success()
        {
            if (RequiresSingleReader)
            {
                return;
            }

            Channel<int> c = CreateChannel();
            const int Items = 100;

            ValueTask<int>[] readers = (from i in Enumerable.Range(0, Items) select c.Reader.ReadAsync()).ToArray();
            var remainingReaders = new List<Task<int>>(readers.Select(r => r.AsTask()));

            for (int i = 0; i < Items; i++)
            {
                Assert.True(c.Writer.TryWrite(i), $"Failed to write at {i}");
                Task<int> r = await Task.WhenAny(remainingReaders);
                await r;
                remainingReaders.Remove(r);
            }

            Assert.Equal((Items * (Items - 1)) / 2, Enumerable.Sum(await Task.WhenAll(readers.Select(r => r.AsTask()))));
        }

        [Fact]
        public async Task ReadAsync_AlreadyCompleted_Throws()
        {
            Channel<int> c = CreateChannel();
            c.Writer.Complete();
            await Assert.ThrowsAsync<ChannelClosedException>(() => c.Reader.ReadAsync().AsTask());
        }

        [Fact]
        public async Task ReadAsync_SubsequentlyCompleted_Throws()
        {
            Channel<int> c = CreateChannel();
            Task<int> r = c.Reader.ReadAsync().AsTask();
            Assert.False(r.IsCompleted);
            c.Writer.Complete();
            await Assert.ThrowsAsync<ChannelClosedException>(() => r);
        }

        [Fact]
        public async Task ReadAsync_AfterFaultedChannel_Throws()
        {
            Channel<int> c = CreateChannel();

            var e = new FormatException();
            c.Writer.Complete(e);
            Assert.True(c.Reader.Completion.IsFaulted);

            ChannelClosedException cce = await Assert.ThrowsAsync<ChannelClosedException>(() => c.Reader.ReadAsync().AsTask());
            Assert.Same(e, cce.InnerException);
        }

        [Fact]
        public async Task ReadAsync_AfterCanceledChannel_Throws()
        {
            Channel<int> c = CreateChannel();

            var e = new OperationCanceledException();
            c.Writer.Complete(e);
            Assert.True(c.Reader.Completion.IsCanceled);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => c.Reader.ReadAsync().AsTask());
        }

        [Fact]
        public async Task ReadAsync_Canceled_WriteAsyncCompletesNextReader()
        {
            Channel<int> c = CreateChannel();

            for (int i = 0; i < 5; i++)
            {
                var cts = new CancellationTokenSource();
                ValueTask<int> r = c.Reader.ReadAsync(cts.Token);
                cts.Cancel();
                await AssertCanceled(r.AsTask(), cts.Token);
            }

            for (int i = 0; i < 7; i++)
            {
                ValueTask<int> r = c.Reader.ReadAsync();
                await c.Writer.WriteAsync(i);
                Assert.Equal(i, await r);
            }
        }
    }
}
