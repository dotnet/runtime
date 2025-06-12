// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class SingleAsyncTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.SingleAsync<int>(null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.SingleAsync<int>(null, i => i % 2 == 0));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.SingleAsync<int>(null, async (i, ct) => i % 2 == 0));

            AssertExtensions.Throws<ArgumentNullException>("predicate", () => AsyncEnumerable.SingleAsync(AsyncEnumerable.Empty<int>(), (Func<int, bool>)null));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => AsyncEnumerable.SingleAsync(AsyncEnumerable.Empty<int>(), (Func<int, CancellationToken, ValueTask<bool>>)null));
        }

        [Fact]
        public async Task EmptyInputs_Throws()
        {
            ValueTask<int> first;

            first = AsyncEnumerable.Empty<int>().SingleAsync();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await first);

            first = AsyncEnumerable.Empty<int>().SingleAsync(i => i % 2 == 0);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await first);

            first = AsyncEnumerable.Empty<int>().SingleAsync(async (i, ct) => i % 2 == 0);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await first);

            first = new int[] { 1, 3, 5 }.ToAsyncEnumerable().SingleAsync(i => i % 2 == 0);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await first);

            first = new int[] { 1, 3, 5 }.ToAsyncEnumerable().SingleAsync(async (i, ct) => i % 2 == 0);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await first);
        }

        [Fact]
        public async Task DoubleInputs_Throws()
        {
            ValueTask<int> single;

            single = new int[] { 1, 2 }.ToAsyncEnumerable().SingleAsync();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await single);

            single = new int[] { 1, 2, 1, 2 }.ToAsyncEnumerable().SingleAsync(i => i % 2 == 0);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await single);

            single = new int[] { 1, 2, 1, 2 }.ToAsyncEnumerable().SingleAsync(async (i, ct) => i % 2 == 0);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await single);
        }

        [Theory]
        [InlineData(new int[] { 1 })]
        [InlineData(new int[] { 2, 4, 8 })]
        [InlineData(new int[] { -1, 2, 5, 6, 7, 8 })]
        [InlineData(new int[] { 1, 3, 5, 7 })]
        public async Task VariousValues_MatchesEnumerable(int[] values)
        {
            foreach (IAsyncEnumerable<int> source in CreateSources(values))
            {
                if (values.Length == 1)
                {
                    Assert.Equal(
                        values.Single(),
                        await source.SingleAsync());
                }

                Func<int, bool> predicate = i => i == values.Last();

                Assert.Equal(
                    values.Single(predicate),
                    await source.SingleAsync(predicate));

                Assert.Equal(
                    values.Single(predicate),
                    await source.SingleAsync(async (i, ct) => predicate(i)));
            }
        }

        [Fact]
        public async Task Cancellation_Cancels()
        {
            IAsyncEnumerable<int> source = CreateSource(2, 4, 8, 16);
            CancellationTokenSource cts;

            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.SingleAsync(new CancellationToken(true)));

            cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.SingleAsync(x =>
            {
                cts.Cancel();
                return x > 32;
            }, cts.Token));

            cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.SingleAsync(async (x, ct) =>
            {
                Assert.Equal(cts.Token, ct);
                await Task.Yield();
                cts.Cancel();
                return x > 32;
            }, cts.Token));
        }

        [Fact]
        public async Task InterfaceCalls_ExpectedCounts()
        {
            TrackingAsyncEnumerable<int> source;

            source = CreateSource(2).Track();
            await source.SingleAsync();
            Assert.Equal(2, source.MoveNextAsyncCount);
            Assert.Equal(1, source.CurrentCount);
            Assert.Equal(1, source.DisposeAsyncCount);

            source = CreateSource(2, 4, 8, 16).Track();
            await source.SingleAsync(i => i == 8);
            Assert.Equal(5, source.MoveNextAsyncCount);
            Assert.Equal(4, source.CurrentCount);
            Assert.Equal(1, source.DisposeAsyncCount);

            source = CreateSource(2, 4, 8, 16).Track();
            await source.SingleAsync(async (i, ct) => i == 2);
            Assert.Equal(5, source.MoveNextAsyncCount);
            Assert.Equal(4, source.CurrentCount);
            Assert.Equal(1, source.DisposeAsyncCount);
        }
    }
}
