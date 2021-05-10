// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Threading.Tasks.Dataflow.Tests
{
    public partial class DataflowBlockExtensionTests
    {
        [Fact]
        public void ReceiveAllAsync_ArgumentValidation()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IReceivableSourceBlock<int>)null).ReceiveAllAsync());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IReceivableSourceBlock<int>)null).ReceiveAllAsync(new CancellationToken(true)));
        }

        [Fact]
        public void ReceiveAllAsync_NotIdempotent()
        {
            var source = new BufferBlock<int>();
            IAsyncEnumerable<int> e = source.ReceiveAllAsync();
            Assert.NotNull(e);
            Assert.NotSame(e, source.ReceiveAllAsync());
        }

        [Fact]
        public async Task ReceiveAllAsync_UseMoveNextAsyncAfterCompleted_ReturnsFalse()
        {
            var source = new BufferBlock<int>();
            IAsyncEnumerator<int> e = source.ReceiveAllAsync().GetAsyncEnumerator();

            ValueTask<bool> vt = e.MoveNextAsync();
            Assert.False(vt.IsCompleted);
            source.Complete();
            Assert.False(await vt);

            vt = e.MoveNextAsync();
            Assert.True(vt.IsCompletedSuccessfully);
            Assert.False(vt.Result);
        }

        [Fact]
        public void ReceiveAllAsync_AvailableDataCompletesSynchronously()
        {
            var source = new BufferBlock<int>();

            IAsyncEnumerator<int> e = source.ReceiveAllAsync().GetAsyncEnumerator();
            try
            {
                for (int i = 100; i < 110; i++)
                {
                    Assert.True(source.Post(i));
                    ValueTask<bool> vt = e.MoveNextAsync();
                    Assert.True(vt.IsCompletedSuccessfully);
                    Assert.True(vt.Result);
                    Assert.Equal(i, e.Current);
                }
            }
            finally
            {
                ValueTask vt = e.DisposeAsync();
                Assert.True(vt.IsCompletedSuccessfully);
                vt.GetAwaiter().GetResult();
            }
        }

        [Fact]
        public async Task ReceiveAllAsync_UnavailableDataCompletesAsynchronously()
        {
            var source = new BufferBlock<int>();

            IAsyncEnumerator<int> e = source.ReceiveAllAsync().GetAsyncEnumerator();
            try
            {
                for (int i = 100; i < 110; i++)
                {
                    ValueTask<bool> vt = e.MoveNextAsync();
                    Assert.False(vt.IsCompleted);
                    Task producer = Task.Run(() => source.Post(i));
                    Assert.True(await vt);
                    await producer;
                    Assert.Equal(i, e.Current);
                }
            }
            finally
            {
                ValueTask vt = e.DisposeAsync();
                Assert.True(vt.IsCompletedSuccessfully);
                vt.GetAwaiter().GetResult();
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(128)]
        public async Task ReceiveAllAsync_ProducerConsumer_ConsumesAllData(int items)
        {
            var source = new BufferBlock<int>();

            int producedTotal = 0, consumedTotal = 0;
            await Task.WhenAll(
                Task.Run(async () =>
                {
                    for (int i = 0; i < items; i++)
                    {
                        await source.SendAsync(i);
                        producedTotal += i;
                    }
                    source.Complete();
                }),
                Task.Run(async () =>
                {
                    IAsyncEnumerator<int> e = source.ReceiveAllAsync().GetAsyncEnumerator();
                    try
                    {
                        while (await e.MoveNextAsync())
                        {
                            consumedTotal += e.Current;
                        }
                    }
                    finally
                    {
                        await e.DisposeAsync();
                    }
                }));

            Assert.Equal(producedTotal, consumedTotal);
        }

        [Fact]
        public async Task ReceiveAllAsync_MultipleEnumerationsToEnd()
        {
            var source = new BufferBlock<int>();

            Assert.True(source.Post(42));
            source.Complete();

            IAsyncEnumerable<int> enumerable = source.ReceiveAllAsync();
            IAsyncEnumerator<int> e = enumerable.GetAsyncEnumerator();

            Assert.True(await e.MoveNextAsync());
            Assert.Equal(42, e.Current);

            Assert.False(await e.MoveNextAsync());
            Assert.False(await e.MoveNextAsync());

            await e.DisposeAsync();

            e = enumerable.GetAsyncEnumerator();

            Assert.False(await e.MoveNextAsync());
            Assert.False(await e.MoveNextAsync());
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void ReceiveAllAsync_MultipleSingleElementEnumerations_AllItemsEnumerated(bool sameEnumerable, bool dispose)
        {
            var source = new BufferBlock<int>();
            IAsyncEnumerable<int> enumerable = source.ReceiveAllAsync();

            for (int i = 0; i < 10; i++)
            {
                Assert.True(source.Post(i));
                IAsyncEnumerator<int> e = (sameEnumerable ? enumerable : source.ReceiveAllAsync()).GetAsyncEnumerator();
                ValueTask<bool> vt = e.MoveNextAsync();
                Assert.True(vt.IsCompletedSuccessfully);
                Assert.True(vt.Result);
                Assert.Equal(i, e.Current);
                if (dispose)
                {
                    ValueTask dvt = e.DisposeAsync();
                    Assert.True(dvt.IsCompletedSuccessfully);
                    dvt.GetAwaiter().GetResult();
                }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ReceiveAllAsync_DualConcurrentEnumeration_AllItemsEnumerated(bool sameEnumerable)
        {
            var source = new BufferBlock<int>();

            IAsyncEnumerable<int> enumerable = source.ReceiveAllAsync();

            IAsyncEnumerator<int> e1 = enumerable.GetAsyncEnumerator();
            IAsyncEnumerator<int> e2 = (sameEnumerable ? enumerable : source.ReceiveAllAsync()).GetAsyncEnumerator();
            Assert.NotSame(e1, e2);

            ValueTask<bool> vt1, vt2;
            int producerTotal = 0, consumerTotal = 0;
            for (int i = 0; i < 10; i++)
            {
                vt1 = e1.MoveNextAsync();
                vt2 = e2.MoveNextAsync();

                await source.SendAsync(i);
                producerTotal += i;
                await source.SendAsync(i * 2);
                producerTotal += i * 2;

                Assert.True(await vt1);
                Assert.True(await vt2);
                consumerTotal += e1.Current;
                consumerTotal += e2.Current;
            }

            vt1 = e1.MoveNextAsync();
            vt2 = e2.MoveNextAsync();
            source.Complete();
            Assert.False(await vt1);
            Assert.False(await vt2);

            Assert.Equal(producerTotal, consumerTotal);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ReceiveAllAsync_CanceledBeforeMoveNextAsync_Throws(bool dataAvailable)
        {
            var source = new BufferBlock<int>();
            if (dataAvailable)
            {
                Assert.True(source.Post(42));
            }

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            IAsyncEnumerator<int> e = source.ReceiveAllAsync(cts.Token).GetAsyncEnumerator();
            ValueTask<bool> vt = e.MoveNextAsync();
            Assert.True(vt.IsCompleted);
            Assert.False(vt.IsCompletedSuccessfully);
            OperationCanceledException oce = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await vt);
            Assert.Equal(cts.Token, oce.CancellationToken);
        }

        [Fact]
        public async Task ReceiveAllAsync_CanceledAfterMoveNextAsync_Throws()
        {
            var source = new BufferBlock<int>();
            using var cts = new CancellationTokenSource();

            IAsyncEnumerator<int> e = source.ReceiveAllAsync(cts.Token).GetAsyncEnumerator();
            ValueTask<bool> vt = e.MoveNextAsync();
            Assert.False(vt.IsCompleted);

            cts.Cancel();
            OperationCanceledException oce = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await vt);

            vt = e.MoveNextAsync();
            Assert.True(vt.IsCompletedSuccessfully);
            Assert.False(vt.Result);
        }
    }
}
