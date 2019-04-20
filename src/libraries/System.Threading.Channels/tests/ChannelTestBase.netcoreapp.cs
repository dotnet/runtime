// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

namespace System.Threading.Channels.Tests
{
    public abstract partial class ChannelTestBase : TestBase
    {
        [Fact]
        public void ReadAllAsync_NotIdempotent()
        {
            Channel<int> c = CreateChannel();
            IAsyncEnumerable<int> e = c.Reader.ReadAllAsync();
            Assert.NotNull(e);
            Assert.NotSame(e, c.Reader.ReadAllAsync());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ReadAllAsync_UseMoveNextAsyncAfterCompleted_ReturnsFalse(bool completeWhilePending)
        {
            Channel<int> c = CreateChannel();
            IAsyncEnumerator<int> e = c.Reader.ReadAllAsync().GetAsyncEnumerator();

            ValueTask<bool> vt;
            if (completeWhilePending)
            {
                c.Writer.Complete();
                vt = e.MoveNextAsync();
                Assert.True(vt.IsCompletedSuccessfully);
                Assert.False(vt.Result);
            }
            else
            {
                vt = e.MoveNextAsync();
                Assert.False(vt.IsCompleted);
                c.Writer.Complete();
                Assert.False(await vt);
            }

            vt = e.MoveNextAsync();
            Assert.True(vt.IsCompletedSuccessfully);
            Assert.False(vt.Result);
        }

        [Fact]
        public void ReadAllAsync_AvailableDataCompletesSynchronously()
        {
            Channel<int> c = CreateChannel();

            IAsyncEnumerator<int> e = c.Reader.ReadAllAsync().GetAsyncEnumerator();
            try
            {
                for (int i = 100; i < 110; i++)
                {
                    Assert.True(c.Writer.TryWrite(i));
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
        public async Task ReadAllAsync_UnavailableDataCompletesAsynchronously()
        {
            Channel<int> c = CreateChannel();

            IAsyncEnumerator<int> e = c.Reader.ReadAllAsync().GetAsyncEnumerator();
            try
            {
                for (int i = 100; i < 110; i++)
                {
                    ValueTask<bool> vt = e.MoveNextAsync();
                    Assert.False(vt.IsCompleted);
                    Task producer = Task.Run(() => c.Writer.TryWrite(i));
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
        public async Task ReadAllAsync_ProducerConsumer_ConsumesAllData(int items)
        {
            Channel<int> c = CreateChannel();

            int producedTotal = 0, consumedTotal = 0;
            await Task.WhenAll(
                Task.Run(async () =>
                {
                    for (int i = 0; i < items; i++)
                    {
                        await c.Writer.WriteAsync(i);
                        producedTotal += i;
                    }
                    c.Writer.Complete();
                }),
                Task.Run(async () =>
                {
                    IAsyncEnumerator<int> e = c.Reader.ReadAllAsync().GetAsyncEnumerator();
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
        public async Task ReadAllAsync_MultipleEnumerationsToEnd()
        {
            Channel<int> c = CreateChannel();
            
            Assert.True(c.Writer.TryWrite(42));
            c.Writer.Complete();

            IAsyncEnumerable<int> enumerable = c.Reader.ReadAllAsync();
            IAsyncEnumerator<int> e = enumerable.GetAsyncEnumerator();

            Assert.True(await e.MoveNextAsync());
            Assert.Equal(42, e.Current);

            Assert.False(await e.MoveNextAsync());
            Assert.False(await e.MoveNextAsync());

            await e.DisposeAsync();

            e = enumerable.GetAsyncEnumerator();
            Assert.Same(enumerable, e);

            Assert.False(await e.MoveNextAsync());
            Assert.False(await e.MoveNextAsync());
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void ReadAllAsync_MultipleSingleElementEnumerations_AllItemsEnumerated(bool sameEnumerable, bool dispose)
        {
            Channel<int> c = CreateChannel();
            IAsyncEnumerable<int> enumerable = c.Reader.ReadAllAsync();

            for (int i = 0; i < 10; i++)
            {
                Assert.True(c.Writer.TryWrite(i));
                IAsyncEnumerator<int> e = (sameEnumerable ? enumerable : c.Reader.ReadAllAsync()).GetAsyncEnumerator();
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
        public async Task ReadAllAsync_DualConcurrentEnumeration_AllItemsEnumerated(bool sameEnumerable)
        {
            if (RequiresSingleReader)
            {
                return;
            }

            Channel<int> c = CreateChannel();

            IAsyncEnumerable<int> enumerable = c.Reader.ReadAllAsync();

            IAsyncEnumerator<int> e1 = enumerable.GetAsyncEnumerator();
            IAsyncEnumerator<int> e2 = (sameEnumerable ? enumerable : c.Reader.ReadAllAsync()).GetAsyncEnumerator();
            Assert.NotSame(e1, e2);

            ValueTask<bool> vt1, vt2;
            int producerTotal = 0, consumerTotal = 0;
            for (int i = 0; i < 10; i++)
            {
                vt1 = e1.MoveNextAsync();
                vt2 = e2.MoveNextAsync();

                await c.Writer.WriteAsync(i);
                producerTotal += i;
                await c.Writer.WriteAsync(i * 2);
                producerTotal += i * 2;

                Assert.True(await vt1);
                Assert.True(await vt2);
                consumerTotal += e1.Current;
                consumerTotal += e2.Current;
            }

            vt1 = e1.MoveNextAsync();
            vt2 = e2.MoveNextAsync();
            c.Writer.Complete();
            Assert.False(await vt1);
            Assert.False(await vt2);

            Assert.Equal(producerTotal, consumerTotal);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ReadAllAsync_CanceledBeforeMoveNextAsync_Throws(bool dataAvailable)
        {
            Channel<int> c = CreateChannel();
            if (dataAvailable)
            {
                Assert.True(c.Writer.TryWrite(42));
            }

            var cts = new CancellationTokenSource();
            cts.Cancel();

            IAsyncEnumerator<int> e = c.Reader.ReadAllAsync(cts.Token).GetAsyncEnumerator();
            ValueTask<bool> vt = e.MoveNextAsync();
            Assert.True(vt.IsCompleted);
            Assert.False(vt.IsCompletedSuccessfully);
            OperationCanceledException oce = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await vt);
            Assert.Equal(cts.Token, oce.CancellationToken);
        }

        [Fact]
        public async Task ReadAllAsync_CanceledAfterMoveNextAsync_Throws()
        {
            Channel<int> c = CreateChannel();
            var cts = new CancellationTokenSource();

            IAsyncEnumerator<int> e = c.Reader.ReadAllAsync(cts.Token).GetAsyncEnumerator();
            ValueTask<bool> vt = e.MoveNextAsync();
            Assert.False(vt.IsCompleted);

            cts.Cancel();
            OperationCanceledException oce = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await vt);
            Assert.Equal(cts.Token, oce.CancellationToken);

            vt = e.MoveNextAsync();
            Assert.True(vt.IsCompletedSuccessfully);
            Assert.False(vt.Result);
        }
    }
}
