// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace System.Threading.Channels.Tests
{
    public abstract class UnboundedChannelTests : ChannelTestBase
    {
        protected override Channel<T> CreateChannel<T>() => Channel.CreateUnbounded<T>(
            new UnboundedChannelOptions
            {
                SingleReader = RequiresSingleReader,
                AllowSynchronousContinuations = AllowSynchronousContinuations
            });
        protected override Channel<T> CreateFullChannel<T>() => null;

        [Fact]
        public void Count_IncrementsDecrementsAsExpected()
        {
            Channel<int> c = CreateChannel();

            if (RequiresSingleReader)
            {
                Assert.False(c.Reader.CanCount);
                return;
            }

            Assert.True(c.Reader.CanCount);

            const int ItemsToAdd = 5;
            for (int iter = 0; iter < 2; iter++)
            {
                for (int i = 0; i < ItemsToAdd; i++)
                {
                    Assert.Equal(i, c.Reader.Count);
                    Assert.True(c.Writer.TryWrite(i));
                    Assert.Equal(i + 1, c.Reader.Count);
                }

                if (iter != 0)
                {
                    c.Writer.Complete();
                }

                while (c.Reader.TryRead(out int item))
                {
                    Assert.Equal(ItemsToAdd - (item + 1), c.Reader.Count);
                }

                Assert.Equal(0, c.Reader.Count);
            }
        }

        [Fact]
        public async Task Complete_BeforeEmpty_NoWaiters_TriggersCompletion()
        {
            Channel<int> c = CreateChannel();
            Assert.True(c.Writer.TryWrite(42));
            c.Writer.Complete();
            Assert.False(c.Reader.Completion.IsCompleted);
            Assert.Equal(42, await c.Reader.ReadAsync());
            await c.Reader.Completion;
        }

        [Fact]
        public void TryWrite_TryRead_Many()
        {
            Channel<int> c = CreateChannel();

            const int NumItems = 100000;
            for (int i = 0; i < NumItems; i++)
            {
                Assert.True(c.Writer.TryWrite(i));
            }
            for (int i = 0; i < NumItems; i++)
            {
                Assert.True(c.Reader.TryRead(out int result));
                Assert.Equal(i, result);
            }
        }

        [Fact]
        public void TryWrite_TryRead_OneAtATime()
        {
            Channel<int> c = CreateChannel();

            for (int i = 0; i < 10; i++)
            {
                Assert.True(c.Writer.TryWrite(i));
                Assert.True(c.Reader.TryRead(out int result));
                Assert.Equal(i, result);
            }
        }

        [Fact]
        public void WaitForReadAsync_DataAvailable_CompletesSynchronously()
        {
            Channel<int> c = CreateChannel();
            Assert.True(c.Writer.TryWrite(42));
            AssertSynchronousTrue(c.Reader.WaitToReadAsync());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public async Task WriteMany_ThenComplete_SuccessfullyReadAll(int readMode)
        {
            Channel<int> c = CreateChannel();
            for (int i = 0; i < 10; i++)
            {
                Assert.True(c.Writer.TryWrite(i));
            }

            c.Writer.Complete();
            Assert.False(c.Reader.Completion.IsCompleted);

            for (int i = 0; i < 10; i++)
            {
                Assert.False(c.Reader.Completion.IsCompleted);
                switch (readMode)
                {
                    case 0:
                        int result;
                        Assert.True(c.Reader.TryRead(out result));
                        Assert.Equal(i, result);
                        break;
                    case 1:
                        Assert.Equal(i, await c.Reader.ReadAsync());
                        break;
                }
            }

            await c.Reader.Completion;
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void AllowSynchronousContinuations_WaitToReadAsync_ContinuationsInvokedAccordingToSetting()
        {
            Channel<int> c = CreateChannel();

            int expectedId = Environment.CurrentManagedThreadId;
            Task r = c.Reader.WaitToReadAsync().AsTask().ContinueWith(_ =>
            {
                Assert.Equal(AllowSynchronousContinuations, expectedId == Environment.CurrentManagedThreadId);
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

            Assert.True(c.Writer.WriteAsync(42).IsCompletedSuccessfully);
            ((IAsyncResult)r).AsyncWaitHandle.WaitOne(); // avoid inlining the continuation
            r.GetAwaiter().GetResult();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void AllowSynchronousContinuations_CompletionTask_ContinuationsInvokedAccordingToSetting()
        {
            Channel<int> c = CreateChannel();

            int expectedId = Environment.CurrentManagedThreadId;
            Task r = c.Reader.Completion.ContinueWith(_ =>
            {
                Assert.Equal(AllowSynchronousContinuations, expectedId == Environment.CurrentManagedThreadId);
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

            Assert.True(c.Writer.TryComplete());
            ((IAsyncResult)r).AsyncWaitHandle.WaitOne(); // avoid inlining the continuation
            r.GetAwaiter().GetResult();
        }
    }

    public abstract class SingleReaderUnboundedChannelTests : UnboundedChannelTests
    {
        protected override bool RequiresSingleReader => true;

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsDebuggerTypeProxyAttributeSupported))]
        public void ValidateInternalDebuggerAttributes()
        {
            Channel<int> c = CreateChannel();
            Assert.True(c.Writer.TryWrite(1));
            Assert.True(c.Writer.TryWrite(2));

            object queue = DebuggerAttributes.GetFieldValue(c, "_items");
            DebuggerAttributes.ValidateDebuggerDisplayReferences(queue);
            DebuggerAttributes.InvokeDebuggerTypeProxyProperties(queue);
        }

        [Fact]
        public async Task MultipleWaiters_CancelsPreviousWaiter()
        {
            Channel<int> c = CreateChannel();
            ValueTask<bool> t1 = c.Reader.WaitToReadAsync();
            ValueTask<bool> t2 = c.Reader.WaitToReadAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await t1);
            Assert.True(c.Writer.TryWrite(42));
            Assert.True(await t2);
        }

        [Fact]
        public async Task MultipleReaders_CancelsPreviousReader()
        {
            Channel<int> c = CreateChannel();
            ValueTask<int> t1 = c.Reader.ReadAsync();
            ValueTask<int> t2 = c.Reader.ReadAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await t1);
            Assert.True(c.Writer.TryWrite(42));
            Assert.Equal(42, await t2);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void Stress_TryWrite_TryRead()
        {
            const int NumItems = 3000000;
            Channel<int> c = CreateChannel();

            Task.WaitAll(
                Task.Run(async () =>
                {
                    int received = 0;
                    while (await c.Reader.WaitToReadAsync())
                    {
                        while (c.Reader.TryRead(out int i))
                        {
                            Assert.Equal(received, i);
                            received++;
                        }
                    }
                }),
                Task.Run(() =>
                {
                    for (int i = 0; i < NumItems; i++)
                    {
                        Assert.True(c.Writer.TryWrite(i));
                    }
                    c.Writer.Complete();
                }));
        }
    }

    public sealed class SyncMultiReaderUnboundedChannelTests : UnboundedChannelTests
    {
        protected override bool AllowSynchronousContinuations => true;
    }

    public sealed class AsyncMultiReaderUnboundedChannelTests : UnboundedChannelTests
    {
        protected override bool AllowSynchronousContinuations => false;
    }

    public sealed class SyncSingleReaderUnboundedChannelTests : SingleReaderUnboundedChannelTests
    {
        protected override bool AllowSynchronousContinuations => true;
    }

    public sealed class AsyncSingleReaderUnboundedChannelTests : SingleReaderUnboundedChannelTests
    {
        protected override bool AllowSynchronousContinuations => false;
    }
}
