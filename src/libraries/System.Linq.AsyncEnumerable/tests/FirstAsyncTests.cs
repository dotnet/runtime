// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class FirstAsyncTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.FirstAsync<int>(null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.FirstAsync<int>(null, i => i % 2 == 0));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.FirstAsync<int>(null, async (i, ct) => i % 2 == 0));

            AssertExtensions.Throws<ArgumentNullException>("predicate", () => AsyncEnumerable.FirstAsync(AsyncEnumerable.Empty<int>(), (Func<int, bool>)null));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => AsyncEnumerable.FirstAsync(AsyncEnumerable.Empty<int>(), (Func<int, CancellationToken, ValueTask<bool>>)null));
        }

        [Fact]
        public async Task EmptyInputs_Throws()
        {
            ValueTask<int> first;

            first = AsyncEnumerable.Empty<int>().FirstAsync();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await first);

            first = AsyncEnumerable.Empty<int>().FirstAsync(i => i % 2 == 0);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await first);

            first = AsyncEnumerable.Empty<int>().FirstAsync(async (i, ct) => i % 2 == 0);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await first);

            first = new int[] { 1, 3, 5 }.ToAsyncEnumerable().FirstAsync(i => i % 2 == 0);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await first);

            first = new int[] { 1, 3, 5 }.ToAsyncEnumerable().FirstAsync(async (i, ct) => i % 2 == 0);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await first);
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
                Assert.Equal(
                    values.First(),
                    await source.FirstAsync());

                Func<int, bool> predicate = i => i == values.Last();

                Assert.Equal(
                    values.First(predicate),
                    await source.FirstAsync(predicate));

                Assert.Equal(
                    values.First(predicate),
                    await source.FirstAsync(async (i, ct) => predicate(i)));
            }
        }

        [Fact]
        public async Task Cancellation_Cancels()
        {
            IAsyncEnumerable<int> source = CreateSource(2, 4, 8, 16);
            CancellationTokenSource cts;

            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.FirstAsync(new CancellationToken(true)));

            cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.FirstAsync(x =>
            {
                cts.Cancel();
                return x > 32;
            }, cts.Token));

            cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.FirstAsync(async (x, ct) =>
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

            source = CreateSource(2, 4, 8, 16).Track();
            await source.FirstAsync();
            Assert.Equal(1, source.MoveNextAsyncCount);
            Assert.Equal(1, source.CurrentCount);
            Assert.Equal(1, source.DisposeAsyncCount);

            source = CreateSource(2, 4, 8, 16).Track();
            await source.FirstAsync(i => i == 8);
            Assert.Equal(3, source.MoveNextAsyncCount);
            Assert.Equal(3, source.CurrentCount);
            Assert.Equal(1, source.DisposeAsyncCount);

            source = CreateSource(2, 4, 8, 16).Track();
            await source.FirstAsync(async (i, ct) => i == 16);
            Assert.Equal(4, source.MoveNextAsyncCount);
            Assert.Equal(4, source.CurrentCount);
            Assert.Equal(1, source.DisposeAsyncCount);
        }
    }
}
