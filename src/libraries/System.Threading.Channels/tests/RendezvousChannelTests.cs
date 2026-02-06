// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Threading.Channels.Tests
{
    public class RendezvousChannelTests : ChannelTestBase
    {
        protected override Channel<T> CreateChannel<T>() => Channel.CreateBounded<T>(0);

        protected override Channel<T> CreateFullChannel<T>() => CreateChannel<T>();

        protected override bool BuffersItems => false;

        protected override bool HasDebuggerTypeProxy => false;

        [Fact]
        public async Task Count_AlwaysZero()
        {
            Channel<int> c = CreateChannel<int>();

            Assert.True(c.Reader.CanCount);
            Assert.Equal(0, c.Reader.Count);

            var write1 = c.Writer.WriteAsync(1);
            var write2 = c.Writer.WriteAsync(2);

            Assert.Equal(0, c.Reader.Count);
            Assert.False(write1.IsCompleted);
            Assert.False(write2.IsCompleted);

            Assert.Equal(1, await c.Reader.ReadAsync());

            await write1;
            Assert.Equal(0, c.Reader.Count);
            Assert.False(write2.IsCompleted);

            Assert.Equal(2, await c.Reader.ReadAsync());

            await write2;
            Assert.Equal(0, c.Reader.Count);
        }

        [Fact]
        public void TryWrite_TryRead_NoPairing_ReturnsFalse()
        {
            Channel<int> c = CreateChannel();

            for (int i = 0; i < 3; i++)
            {
                Assert.False(c.Writer.TryWrite(42));
                Assert.False(c.Reader.TryRead(out int item));
                Assert.Equal(0, item);
            }
        }

        [Theory]
        [InlineData(BoundedChannelFullMode.DropWrite)]
        [InlineData(BoundedChannelFullMode.DropOldest)]
        [InlineData(BoundedChannelFullMode.DropNewest)]
        public async Task TryWrite_DropXx_DropsWrite(BoundedChannelFullMode mode)
        {
            int? dropped = null;
            Channel<int> c = Channel.CreateBounded<int>(new BoundedChannelOptions(0) { FullMode = mode }, item => dropped = item);

            for (int i = 42; i < 52; i++)
            {
                var waiter = c.Writer.WaitToWriteAsync();
                AssertSynchronousSuccess(waiter);
                Assert.True(await waiter);

                Assert.True(c.Writer.TryWrite(i));
                Assert.Equal(i, dropped);

                dropped = null;
                AssertSynchronousSuccess(c.Writer.WriteAsync(i));
                Assert.Equal(i, dropped);
            }
        }

        [Theory]
        [InlineData(BoundedChannelFullMode.DropWrite)]
        [InlineData(BoundedChannelFullMode.DropOldest)]
        [InlineData(BoundedChannelFullMode.DropNewest)]
        public async Task TryWrite_DropXx_ReaderTakesPriority(BoundedChannelFullMode mode)
        {
            int? dropped = null;
            Channel<int> c = Channel.CreateBounded<int>(new BoundedChannelOptions(0) { FullMode = mode }, item => dropped = item);

            for (int i = 42; i < 52; i++)
            {
                ValueTask<int> reader;

                reader = c.Reader.ReadAsync();
                Assert.True(c.Writer.TryWrite(i));
                Assert.Null(dropped);
                Assert.Equal(i, await reader);

                reader = c.Reader.ReadAsync();
                AssertSynchronousSuccess(c.Writer.WriteAsync(i));
                Assert.Null(dropped);
                Assert.Equal(i, await reader);
            }
        }

        [Fact]
        public async Task DroppedDelegateNotCalledOnWaitMode_SyncWrites()
        {
            bool dropDelegateCalled = false;

            Channel<int> c = Channel.CreateBounded<int>(new BoundedChannelOptions(0) { FullMode = BoundedChannelFullMode.Wait },
                item =>
                {
                    dropDelegateCalled = true;
                });

            ValueTask<int> reader;

            reader = c.Reader.ReadAsync();
            Assert.True(c.Writer.TryWrite(42));
            Assert.Equal(42, await reader);

            reader = c.Reader.ReadAsync();
            AssertSynchronousSuccess(c.Writer.WriteAsync(43));
            Assert.Equal(43, await reader);

            _ = c.Writer.WriteAsync(44);

            Assert.False(dropDelegateCalled);
        }

        [Theory]
        [InlineData(BoundedChannelFullMode.DropWrite)]
        [InlineData(BoundedChannelFullMode.DropOldest)]
        [InlineData(BoundedChannelFullMode.DropNewest)]
        public void DroppedDelegateIsNull_SyncAndAsyncWrites(BoundedChannelFullMode boundedChannelFullMode)
        {
            Channel<int> c = Channel.CreateBounded<int>(new BoundedChannelOptions(0) { FullMode = boundedChannelFullMode }, itemDropped: null);

            Assert.True(c.Writer.TryWrite(1));
            AssertSynchronousSuccess(c.Writer.WriteAsync(2));

            Assert.False(c.Reader.TryRead(out int item));
            Assert.Equal(0, item);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData(BoundedChannelFullMode.DropWrite)]
        [InlineData(BoundedChannelFullMode.DropOldest)]
        [InlineData(BoundedChannelFullMode.DropNewest)]
        public void DroppedDelegateCalledAfterLockReleased_SyncWrites(BoundedChannelFullMode boundedChannelFullMode)
        {
            Channel<int> c = null;
            bool dropDelegateCalled = false;

            c = Channel.CreateBounded<int>(new BoundedChannelOptions(0)
            {
                FullMode = boundedChannelFullMode
            }, (droppedItem) =>
            {
                if (dropDelegateCalled)
                {
                    return;
                }
                dropDelegateCalled = true;

                // Dropped delegate should not be called while holding the channel lock.
                // Verify this by trying to write into the channel from different thread.
                // If lock is held during callback, this should effectively cause deadlock.
                ManualResetEventSlim mres = new();
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
        [InlineData(BoundedChannelFullMode.DropWrite)]
        [InlineData(BoundedChannelFullMode.DropOldest)]
        [InlineData(BoundedChannelFullMode.DropNewest)]
        public async Task DroppedDelegateCalledAfterLockReleased_AsyncWrites(BoundedChannelFullMode boundedChannelFullMode)
        {
            Channel<int> c = null;
            bool dropDelegateCalled = false;

            c = Channel.CreateBounded<int>(new BoundedChannelOptions(0)
            {
                FullMode = boundedChannelFullMode
            }, (droppedItem) =>
            {
                if (dropDelegateCalled)
                {
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

        [Fact]
        public async Task CancelPendingWrite_Reading_DataTransferredFromCorrectWriter()
        {
            Channel<int> c = CreateChannel();

            CancellationTokenSource cts = new();

            ValueTask write1 = c.Writer.WriteAsync(42);
            ValueTask write2 = c.Writer.WriteAsync(43, cts.Token);
            ValueTask write3 = c.Writer.WriteAsync(44);

            cts.Cancel();

            Assert.Equal(42, await c.Reader.ReadAsync());
            Assert.Equal(44, await c.Reader.ReadAsync());

            await write1;
            await AssertExtensions.CanceledAsync(cts.Token, async () => await write2);
            await write3;
        }

        [Fact]
        public async Task WaitToWriteAsync_AfterRead_ReturnsTrue()
        {
            Channel<int> c = CreateChannel();

            ValueTask<bool> write1 = c.Writer.WaitToWriteAsync();
            ValueTask<bool> write2 = c.Writer.WaitToWriteAsync();
            Assert.False(write1.IsCompleted);
            Assert.False(write2.IsCompleted);

            _ = c.Reader.ReadAsync();

            Assert.True(await write1);
            Assert.True(await write2);

            ValueTask<bool> write3 = c.Writer.WaitToWriteAsync();
            AssertSynchronousSuccess(write3);
            Assert.True(await write3);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [MemberData(nameof(ThreeBools))]
        public void AllowSynchronousContinuations_Reading_ContinuationsInvokedAccordingToSetting(bool allowSynchronousContinuations, bool cancelable, bool waitToReadAsync)
        {
            var c = Channel.CreateBounded<int>(new BoundedChannelOptions(0) { AllowSynchronousContinuations = allowSynchronousContinuations });

            CancellationToken ct = cancelable ? new CancellationTokenSource().Token : CancellationToken.None;

            int expectedId = Environment.CurrentManagedThreadId;
            Task t = waitToReadAsync ? c.Reader.WaitToReadAsync(ct).AsTask() : c.Reader.ReadAsync(ct).AsTask();
            Task r = t.ContinueWith(_ =>
            {
                Assert.Equal(allowSynchronousContinuations, expectedId == Environment.CurrentManagedThreadId);
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

            ValueTask write = c.Writer.WriteAsync(42);
            if (!waitToReadAsync)
            {
                AssertSynchronousSuccess(write);
            }

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

            var c = Channel.CreateBounded<int>(new BoundedChannelOptions(0) { AllowSynchronousContinuations = allowSynchronousContinuations });

            int expectedId = Environment.CurrentManagedThreadId;
            Task r = c.Reader.Completion.ContinueWith(_ =>
            {
                Assert.Equal(allowSynchronousContinuations, expectedId == Environment.CurrentManagedThreadId);
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

            Assert.True(c.Writer.TryComplete());
            ((IAsyncResult)r).AsyncWaitHandle.WaitOne(); // avoid inlining the continuation
            r.GetAwaiter().GetResult();
        }
    }
}
