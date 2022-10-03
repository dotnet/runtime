// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.XUnitExtensions;
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

        public static IEnumerable<object[]> ChannelDropModes()
        {
            foreach (BoundedChannelFullMode mode in Enum.GetValues(typeof(BoundedChannelFullMode)))
            {
                if (mode != BoundedChannelFullMode.Wait)
                {
                    yield return new object[] { mode };
                }
            }
        }

        [Fact]
        public void Count_IncrementsDecrementsAsExpected()
        {
            const int Bound = 3;

            Channel<int> c = Channel.CreateBounded<int>(Bound);
            Assert.True(c.Reader.CanCount);

            for (int iter = 0; iter < 2; iter++)
            {
                for (int i = 0; i < Bound; i++)
                {
                    Assert.Equal(i, c.Reader.Count);
                    Assert.True(c.Writer.TryWrite(i));
                    Assert.Equal(i + 1, c.Reader.Count);
                }

                Assert.False(c.Writer.TryWrite(42));
                Assert.Equal(Bound, c.Reader.Count);

                if (iter != 0)
                {
                    c.Writer.Complete();
                }

                for (int i = 0; i < Bound; i++)
                {
                    Assert.Equal(Bound - i, c.Reader.Count);
                    Assert.True(c.Reader.TryRead(out int item));
                    Assert.Equal(i, item);
                    Assert.Equal(Bound - (i + 1), c.Reader.Count);
                }
            }
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
        public void DroppedDelegateNotCalledOnWaitMode_SyncWrites()
        {
            bool dropDelegateCalled = false;

            Channel<int> c = Channel.CreateBounded<int>(new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.Wait },
                item =>
                {
                    dropDelegateCalled = true;
                });

            Assert.True(c.Writer.TryWrite(1));
            Assert.False(c.Writer.TryWrite(1));

            Assert.False(dropDelegateCalled);
        }

        [Fact]
        public async Task DroppedDelegateNotCalledOnWaitMode_AsyncWrites()
        {
            bool dropDelegateCalled = false;

            Channel<int> c = Channel.CreateBounded<int>(new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.Wait },
                item =>
                {
                    dropDelegateCalled = true;
                });

            // First async write should pass
            await c.Writer.WriteAsync(1);

            // Second write should wait
            var secondWriteTask = c.Writer.WriteAsync(2);
            Assert.False(secondWriteTask.IsCompleted);

            // Read from channel to free up space
            var readItem = await c.Reader.ReadAsync();
            // Second write should complete
            await secondWriteTask;

            // No dropped delegate should be called
            Assert.False(dropDelegateCalled);
        }

        [Theory]
        [MemberData(nameof(ChannelDropModes))]
        public void DroppedDelegateIsNull_SyncWrites(BoundedChannelFullMode boundedChannelFullMode)
        {
            Channel<int> c = Channel.CreateBounded<int>(new BoundedChannelOptions(1) { FullMode = boundedChannelFullMode }, itemDropped: null);

            Assert.True(c.Writer.TryWrite(5));
            Assert.True(c.Writer.TryWrite(5));
        }

        [Theory]
        [MemberData(nameof(ChannelDropModes))]
        public async Task DroppedDelegateIsNull_AsyncWrites(BoundedChannelFullMode boundedChannelFullMode)
        {
            Channel<int> c = Channel.CreateBounded<int>(new BoundedChannelOptions(1) { FullMode = boundedChannelFullMode }, itemDropped: null);

            await c.Writer.WriteAsync(5);
            await c.Writer.WriteAsync(5);
        }

        [Theory]
        [MemberData(nameof(ChannelDropModes))]
        public void DroppedDelegateCalledOnChannelFull_SyncWrites(BoundedChannelFullMode boundedChannelFullMode)
        {
            var droppedItems = new HashSet<int>();

            void AddDroppedItem(int itemDropped)
            {
                Assert.True(droppedItems.Add(itemDropped));
            }

            const int channelCapacity = 10;
            var c = Channel.CreateBounded<int>(new BoundedChannelOptions(channelCapacity)
            {
                FullMode = boundedChannelFullMode
            }, AddDroppedItem);

            for (int i = 0; i < channelCapacity; i++)
            {
                Assert.True(c.Writer.TryWrite(i));
            }

            // No dropped delegate should be called while channel is not full
            Assert.Empty(droppedItems);

            for (int i = channelCapacity; i < channelCapacity + 10; i++)
            {
                Assert.True(c.Writer.TryWrite(i));
            }

            // Assert expected number of dropped items delegate calls
            Assert.Equal(10, droppedItems.Count);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [MemberData(nameof(ChannelDropModes))]
        public void DroppedDelegateCalledAfterLockReleased_SyncWrites(BoundedChannelFullMode boundedChannelFullMode)
        {
            Channel<int> c = null;
            bool dropDelegateCalled = false;

            c = Channel.CreateBounded<int>(new BoundedChannelOptions(1)
            {
                FullMode = boundedChannelFullMode
            }, (droppedItem) =>
            {
                if (dropDelegateCalled)
                {
                    // Prevent infinite callbacks being called
                    return;
                }

                dropDelegateCalled = true;

                // Dropped delegate should not be called while holding the channel lock.
                // Verify this by trying to write into the channel from different thread.
                // If lock is held during callback, this should effectively cause deadlock.
                var mres = new ManualResetEventSlim();
                ThreadPool.QueueUserWorkItem(delegate
                {
                    c.Writer.TryWrite(3);
                    mres.Set();
                });

                mres.Wait();
            });

            Assert.True(c.Writer.TryWrite(1));
            Assert.True(c.Writer.TryWrite(2));

            Assert.True(dropDelegateCalled);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [MemberData(nameof(ChannelDropModes))]
        public async Task DroppedDelegateCalledAfterLockReleased_AsyncWrites(BoundedChannelFullMode boundedChannelFullMode)
        {
            Channel<int> c = null;
            bool dropDelegateCalled = false;

            c = Channel.CreateBounded<int>(new BoundedChannelOptions(1)
            {
                FullMode = boundedChannelFullMode
            }, (droppedItem) =>
            {
                if (dropDelegateCalled)
                {
                    // Prevent infinite callbacks being called
                    return;
                }

                dropDelegateCalled = true;

                // Dropped delegate should not be called while holding the channel synchronisation lock.
                // Verify this by trying to write into the channel from different thread.
                // If lock is held during callback, this should effectively cause deadlock.
                var mres = new ManualResetEventSlim();
                ThreadPool.QueueUserWorkItem(delegate
                {
                    c.Writer.TryWrite(11);
                    mres.Set();
                });

                mres.Wait();
            });

            await c.Writer.WriteAsync(1);
            await c.Writer.WriteAsync(2);

            Assert.True(dropDelegateCalled);
        }

        [Theory]
        [MemberData(nameof(ChannelDropModes))]
        public async Task DroppedDelegateCalledOnChannelFull_AsyncWrites(BoundedChannelFullMode boundedChannelFullMode)
        {
            var droppedItems = new HashSet<int>();

            void AddDroppedItem(int itemDropped)
            {
                Assert.True(droppedItems.Add(itemDropped));
            }

            const int channelCapacity = 10;
            var c = Channel.CreateBounded<int>(new BoundedChannelOptions(channelCapacity)
            {
                FullMode = boundedChannelFullMode
            }, AddDroppedItem);

            for (int i = 0; i < channelCapacity; i++)
            {
                await c.Writer.WriteAsync(i);
            }

            // No dropped delegate should be called while channel is not full
            Assert.Empty(droppedItems);

            for (int i = channelCapacity; i < channelCapacity + 10; i++)
            {
                await c.Writer.WriteAsync(i);
            }

            // Assert expected number of dropped items delegate calls
            Assert.Equal(10, droppedItems.Count);
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

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
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

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
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

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [MemberData(nameof(ThreeBools))]
        public void AllowSynchronousContinuations_Reading_ContinuationsInvokedAccordingToSetting(bool allowSynchronousContinuations, bool cancelable, bool waitToReadAsync)
        {
            var c = Channel.CreateBounded<int>(new BoundedChannelOptions(1) { AllowSynchronousContinuations = allowSynchronousContinuations });

            CancellationToken ct = cancelable ? new CancellationTokenSource().Token : CancellationToken.None;

            int expectedId = Environment.CurrentManagedThreadId;
            Task t = waitToReadAsync ? (Task)c.Reader.WaitToReadAsync(ct).AsTask() : c.Reader.ReadAsync(ct).AsTask();
            Task r = t.ContinueWith(_ =>
            {
                Assert.Equal(allowSynchronousContinuations && !cancelable, expectedId == Environment.CurrentManagedThreadId);
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

            Assert.True(c.Writer.WriteAsync(42).IsCompletedSuccessfully);
            ((IAsyncResult)r).AsyncWaitHandle.WaitOne(); // avoid inlining the continuation
            r.GetAwaiter().GetResult();
        }

        [ConditionalTheory]
        [InlineData(false)]
        [InlineData(true)]
        public void AllowSynchronousContinuations_CompletionTask_ContinuationsInvokedAccordingToSetting(bool allowSynchronousContinuations)
        {
            if (!allowSynchronousContinuations && !PlatformDetection.IsThreadingSupported)
            {
                throw new SkipTestException(nameof(PlatformDetection.IsThreadingSupported));
            }

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
