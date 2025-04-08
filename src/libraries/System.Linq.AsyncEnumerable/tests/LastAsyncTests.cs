// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class LastAsyncTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.LastAsync<int>(null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.LastAsync<int>(null, i => i % 2 == 0));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.LastAsync<int>(null, async (i, ct) => i % 2 == 0));

            AssertExtensions.Throws<ArgumentNullException>("predicate", () => AsyncEnumerable.LastAsync(AsyncEnumerable.Empty<int>(), (Func<int, bool>)null));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => AsyncEnumerable.LastAsync(AsyncEnumerable.Empty<int>(), (Func<int, CancellationToken, ValueTask<bool>>)null));
        }

        [Fact]
        public async Task EmptyInputs_Throws()
        {
            ValueTask<int> first;

            first = AsyncEnumerable.Empty<int>().LastAsync();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await first);

            first = AsyncEnumerable.Empty<int>().LastAsync(i => i % 2 == 0);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await first);

            first = AsyncEnumerable.Empty<int>().LastAsync(async (i, ct) => i % 2 == 0);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await first);

            first = new int[] { 1, 3, 5 }.ToAsyncEnumerable().LastAsync(i => i % 2 == 0);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await first);

            first = new int[] { 1, 3, 5 }.ToAsyncEnumerable().LastAsync(async (i, ct) => i % 2 == 0);
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
                    values.Last(),
                    await source.LastAsync());

                Func<int, bool> predicate = i => i < 5;

                Assert.Equal(
                    values.Last(predicate),
                    await source.LastAsync(predicate));

                Assert.Equal(
                    values.Last(predicate),
                    await source.LastAsync(async (i, ct) => predicate(i)));
            }
        }

        [Fact]
        public async Task Cancellation_Cancels()
        {
            IAsyncEnumerable<int> source = CreateSource(2, 4, 8, 16);
            CancellationTokenSource cts;

            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.LastAsync(new CancellationToken(true)));

            cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.LastAsync(x =>
            {
                cts.Cancel();
                return x > 32;
            }, cts.Token));

            cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.LastAsync(async (x, ct) =>
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
            int predicateCount;

            source = CreateSource(2, 4, 8, 16).Track();
            await source.LastAsync();
            Assert.Equal(5, source.MoveNextAsyncCount);
            Assert.Equal(4, source.CurrentCount);
            Assert.Equal(1, source.DisposeAsyncCount);

            predicateCount = 0;
            source = CreateSource(2, 4, 8, 16).Track();
            await source.LastAsync(i =>
            {
                predicateCount++;
                return i == 8;
            });
            Assert.Equal(5, source.MoveNextAsyncCount);
            Assert.Equal(4, source.CurrentCount);
            Assert.Equal(1, source.DisposeAsyncCount);

            source = CreateSource(2, 4, 8, 16).Track();
            await source.LastAsync(async (i, ct) =>
            {
                predicateCount++;
                return i == 16;
            });
            Assert.Equal(5, source.MoveNextAsyncCount);
            Assert.Equal(4, source.CurrentCount);
            Assert.Equal(1, source.DisposeAsyncCount);
        }
    }
}
