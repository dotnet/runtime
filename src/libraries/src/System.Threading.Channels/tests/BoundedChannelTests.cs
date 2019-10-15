// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Xunit;

namespace System.Threading.Channels.Tests
{
    public class BoundedChannelTests : ChannelTestBase
    {
        protected override Channel<T> CreateChannel<T>() => Channel.CreateBounded<T>(new BoundedChannelOptions(1) { AllowSynchronousContinuations = AllowSynchronousContinuations });
        protected override Channel<T> CreateFullChannel<T>()
        {
            var c = Channel.CreateBounded<T>(new BoundedChannelOptions(1) { AllowSynchronousContinuations = AllowSynchronousContinuations });
            c.Writer.WriteAsync(default).AsTask().Wait();
            return c;
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(10000)]
        public void TryWrite_TryRead_Many_Wait(int bufferedCapacity)
        {
            var c = Channel.CreateBounded<int>(bufferedCapacity);

            for (int i = 0; i < bufferedCapacity; i++)
            {
                Assert.True(c.Writer.TryWrite(i));
            }
            Assert.False(c.Writer.TryWrite(bufferedCapacity));

            int result;
            for (int i = 0; i < bufferedCapacity; i++)
            {
                Assert.True(c.Reader.TryRead(out result));
                Assert.Equal(i, result);
            }

            Assert.False(c.Reader.TryRead(out result));
            Assert.Equal(0, result);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(10000)]
        public void TryWrite_TryRead_Many_DropOldest(int bufferedCapacity)
        {
            var c = Channel.CreateBounded<int>(new BoundedChannelOptions(bufferedCapacity) { FullMode = BoundedChannelFullMode.DropOldest });

            for (int i = 0; i < bufferedCapacity * 2; i++)
            {
                Assert.True(c.Writer.TryWrite(i));
            }

            int result;
            for (int i = bufferedCapacity; i < bufferedCapacity * 2; i++)
            {
                Assert.True(c.Reader.TryRead(out result));
                Assert.Equal(i, result);
            }

            Assert.False(c.Reader.TryRead(out result));
            Assert.Equal(0, result);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(10000)]
        public void WriteAsync_TryRead_Many_DropOldest(int bufferedCapacity)
        {
            var c = Channel.CreateBounded<int>(new BoundedChannelOptions(bufferedCapacity) { FullMode = BoundedChannelFullMode.DropOldest });

            for (int i = 0; i < bufferedCapacity * 2; i++)
            {
                AssertSynchronousSuccess(c.Writer.WriteAsync(i));
            }

            int result;
            for (int i = bufferedCapacity; i < bufferedCapacity * 2; i++)
            {
                Assert.True(c.Reader.TryRead(out result));
                Assert.Equal(i, result);
            }

            Assert.False(c.Reader.TryRead(out result));
            Assert.Equal(0, result);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(10000)]
        public void TryWrite_TryRead_Many_DropNewest(int bufferedCapacity)
        {
            var c = Channel.CreateBounded<int>(new BoundedChannelOptions(bufferedCapacity) { FullMode = BoundedChannelFullMode.DropNewest });

            for (int i = 0; i < bufferedCapacity * 2; i++)
            {
                Assert.True(c.Writer.TryWrite(i));
            }

            int result;
            for (int i = 0; i < bufferedCapacity - 1; i++)
            {
                Assert.True(c.Reader.TryRead(out result));
                Assert.Equal(i, result);
            }
            Assert.True(c.Reader.TryRead(out result));
            Assert.Equal(bufferedCapacity * 2 - 1, result);

            Assert.False(c.Reader.TryRead(out result));
            Assert.Equal(0, result);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(10000)]
        public void WriteAsync_TryRead_Many_DropNewest(int bufferedCapacity)
        {
            var c = Channel.CreateBounded<int>(new BoundedChannelOptions(bufferedCapacity) { FullMode = BoundedChannelFullMode.DropNewest });

            for (int i = 0; i < bufferedCapacity * 2; i++)
            {
                AssertSynchronousSuccess(c.Writer.WriteAsync(i));
            }

            int result;
            for (int i = 0; i < bufferedCapacity - 1; i++)
            {
                Assert.True(c.Reader.TryRead(out result));
                Assert.Equal(i, result);
            }
            Assert.True(c.Reader.TryRead(out result));
            Assert.Equal(bufferedCapacity * 2 - 1, result);

            Assert.False(c.Reader.TryRead(out result));
            Assert.Equal(0, result);
        }

        [Fact]
        public async Task TryWrite_DropNewest_WrappedAroundInternalQueue()
        {
            var c = Channel.CreateBounded<int>(new BoundedChannelOptions(3) { FullMode = BoundedChannelFullMode.DropNewest });

            // Move head of dequeue beyond the beginning
            Assert.True(c.Writer.TryWrite(1));
            Assert.True(c.Reader.TryRead(out int item));
            Assert.Equal(1, item);

            // Add items to fill the capacity and put the tail at 0
            Assert.True(c.Writer.TryWrite(2));
            Assert.True(c.Writer.TryWrite(3));
            Assert.True(c.Writer.TryWrite(4));

            // Add an item to overwrite the newest
            Assert.True(c.Writer.TryWrite(5));

            // Verify current contents
            Assert.Equal(2, await c.Reader.ReadAsync());
            Assert.Equal(3, await c.Reader.ReadAsync());
            Assert.Equal(5, await c.Reader.ReadAsync());
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(10000)]
        public void TryWrite_TryRead_Many_Ignore(int bufferedCapacity)
        {
            var c = Channel.CreateBounded<int>(new BoundedChannelOptions(bufferedCapacity) { FullMode = BoundedChannelFullMode.DropWrite });

            for (int i = 0; i < bufferedCapacity * 2; i++)
            {
                Assert.True(c.Writer.TryWrite(i));
            }

            int result;
            for (int i = 0; i < bufferedCapacity; i++)
            {
                Assert.True(c.Reader.TryRead(out result));
                Assert.Equal(i, result);
            }

            Assert.False(c.Reader.TryRead(out result));
            Assert.Equal(0, result);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(10000)]
        public void WriteAsync_TryRead_Many_Ignore(int bufferedCapacity)
        {
            var c = Channel.CreateBounded<int>(new BoundedChannelOptions(bufferedCapacity) { FullMode = BoundedChannelFullMode.DropWrite });

            for (int i = 0; i < bufferedCapacity * 2; i++)
            {
                AssertSynchronousSuccess(c.Writer.WriteAsync(i));
            }

            int result;
            for (int i = 0; i < bufferedCapacity; i++)
            {
                Assert.True(c.Reader.TryRead(out result));
                Assert.Equal(i, result);
            }

            Assert.False(c.Reader.TryRead(out result));
            Assert.Equal(0, result);
        }

        [Fact]
        public async Task CancelPendingWrite_Reading_DataTransferredFromCorrectWriter()
        {
            var c = Channel.CreateBounded<int>(1);
            Assert.True(c.Writer.WriteAsync(42).IsCompletedSuccessfully);

            var cts = new CancellationTokenSource();

            Task write1 = c.Writer.WriteAsync(43, cts.Token).AsTask();
            Assert.Equal(TaskStatus.WaitingForActivation, write1.Status);

            cts.Cancel();

            Task write2 = c.Writer.WriteAsync(44).AsTask();

            Assert.Equal(42, await c.Reader.ReadAsync());
            Assert.Equal(44, await c.Reader.ReadAsync());

            await AssertCanceled(write1, cts.Token);
            await write2;
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(10000)]
        public void TryWrite_TryRead_OneAtATime(int bufferedCapacity)
        {
            var c = Channel.CreateBounded<int>(bufferedCapacity);

            const int NumItems = 100000;
            for (int i = 0; i < NumItems; i++)
            {
                Assert.True(c.Writer.TryWrite(i));
                Assert.True(c.Reader.TryRead(out int result));
                Assert.Equal(i, result);
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(10000)]
        public void SingleProducerConsumer_ConcurrentReadWrite_WithBufferedCapacity_Success(int bufferedCapacity)
        {
            var c = Channel.CreateBounded<int>(bufferedCapacity);

            const int NumItems = 10000;
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

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(10000)]
        public void ManyProducerConsumer_ConcurrentReadWrite_WithBufferedCapacity_Success(int bufferedCapacity)
        {
            var c = Channel.CreateBounded<int>(bufferedCapacity);

            const int NumWriters = 10;
            const int NumReaders = 10;
            const int NumItems = 10000;

            long readTotal = 0;
            int remainingWriters = NumWriters;
            int remainingItems = NumItems;

            Task[] tasks = new Task[NumWriters + NumReaders];

            for (int i = 0; i < NumReaders; i++)
            {
                tasks[i] = Task.Run(async () =>
                {
                    try
                    {
                        while (true)
                        {
                            Interlocked.Add(ref readTotal, await c.Reader.ReadAsync());
                        }
                    }
                    catch (ChannelClosedException) { }
                });
            }

            for (int i = 0; i < NumWriters; i++)
            {
                tasks[NumReaders + i] = Task.Run(async () =>
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
        public async Task WaitToWriteAsync_AfterFullThenRead_ReturnsTrue()
        {
            var c = Channel.CreateBounded<int>(1);
            Assert.True(c.Writer.TryWrite(1));

            Task<bool> write1 = c.Writer.WaitToWriteAsync().AsTask();
            Assert.False(write1.IsCompleted);

            Task<bool> write2 = c.Writer.WaitToWriteAsync().AsTask();
            Assert.False(write2.IsCompleted);

            Assert.Equal(1, await c.Reader.ReadAsync());

            Assert.True(await write1);
            Assert.True(await write2);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void AllowSynchronousContinuations_WaitToReadAsync_ContinuationsInvokedAccordingToSetting(bool allowSynchronousContinuations)
        {
            var c = Channel.CreateBounded<int>(new BoundedChannelOptions(1) { AllowSynchronousContinuations = allowSynchronousContinuations });

            int expectedId = Environment.CurrentManagedThreadId;
            Task r = c.Reader.WaitToReadAsync().AsTask().ContinueWith(_ =>
            {
                Assert.Equal(allowSynchronousContinuations, expectedId == Environment.CurrentManagedThreadId);
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

            Assert.True(c.Writer.WriteAsync(42).IsCompletedSuccessfully);
            ((IAsyncResult)r).AsyncWaitHandle.WaitOne(); // avoid inlining the continuation
            r.GetAwaiter().GetResult();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void AllowSynchronousContinuations_CompletionTask_ContinuationsInvokedAccordingToSetting(bool allowSynchronousContinuations)
        {
            var c = Channel.CreateBounded<int>(new BoundedChannelOptions(1) { AllowSynchronousContinuations = allowSynchronousContinuations });

            int expectedId = Environment.CurrentManagedThreadId;
            Task r = c.Reader.Completion.ContinueWith(_ =>
            {
                Assert.Equal(allowSynchronousContinuations, expectedId == Environment.CurrentManagedThreadId);
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

            Assert.True(c.Writer.TryComplete());
            ((IAsyncResult)r).AsyncWaitHandle.WaitOne(); // avoid inlining the continuation
            r.GetAwaiter().GetResult();
        }

        [Fact]
        public async Task TryWrite_NoBlockedReaders_WaitingReader_WaiterNotified()
        {
            Channel<int> c = CreateChannel();

            Task<bool> r = c.Reader.WaitToReadAsync().AsTask();
            Assert.True(c.Writer.TryWrite(42));
            Assert.True(await r);
        }
    }
}
