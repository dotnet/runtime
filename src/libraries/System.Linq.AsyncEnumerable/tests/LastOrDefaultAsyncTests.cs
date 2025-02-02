// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class LastOrDefaultAsyncTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.LastOrDefaultAsync<int>(null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.LastOrDefaultAsync<int>(null, i => i % 2 == 0));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.LastOrDefaultAsync<int>(null, async (i, ct) => i % 2 == 0));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.LastOrDefaultAsync<int>(null, 42));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.LastOrDefaultAsync<int>(null, i => i % 2 == 0, 42));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.LastOrDefaultAsync<int>(null, async (i, ct) => i % 2 == 0, 42));

            AssertExtensions.Throws<ArgumentNullException>("predicate", () => AsyncEnumerable.LastOrDefaultAsync(AsyncEnumerable.Empty<int>(), (Func<int, bool>)null));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => AsyncEnumerable.LastOrDefaultAsync(AsyncEnumerable.Empty<int>(), (Func<int, CancellationToken, ValueTask<bool>>)null));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => AsyncEnumerable.LastOrDefaultAsync(AsyncEnumerable.Empty<int>(), (Func<int, bool>)null, 42));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => AsyncEnumerable.LastOrDefaultAsync(AsyncEnumerable.Empty<int>(), (Func<int, CancellationToken, ValueTask<bool>>)null, 42));
        }

        [Fact]
        public async Task EmptyInputs_DefaultValueReturned()
        {
            Assert.Equal(0, await AsyncEnumerable.Empty<int>().LastOrDefaultAsync());
            Assert.Equal(42, await AsyncEnumerable.Empty<int>().LastOrDefaultAsync(42));
            Assert.Equal(0, await AsyncEnumerable.Empty<int>().LastOrDefaultAsync(i => i % 2 == 0));
            Assert.Equal(42, await AsyncEnumerable.Empty<int>().LastOrDefaultAsync(i => i % 2 == 0, 42));
            Assert.Equal(0, await AsyncEnumerable.Empty<int>().LastOrDefaultAsync(async (i, ct) => i % 2 == 0));
            Assert.Equal(42, await AsyncEnumerable.Empty<int>().LastOrDefaultAsync(async (i, ct) => i % 2 == 0, 42));

            IAsyncEnumerable<int> source = new int[] { 1, 3, 5 }.ToAsyncEnumerable();
            Assert.Equal(0, await source.LastOrDefaultAsync(i => i % 2 == 0));
            Assert.Equal(42, await source.LastOrDefaultAsync(i => i % 2 == 0, 42));
            Assert.Equal(0, await source.LastOrDefaultAsync(async (i, ct) => i % 2 == 0));
            Assert.Equal(42, await source.LastOrDefaultAsync(async (i, ct) => i % 2 == 0, 42));
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
                    values.LastOrDefault(),
                    await source.LastOrDefaultAsync());

                Func<int, bool> predicate = i => i < 5;

                Assert.Equal(
                    values.LastOrDefault(predicate),
                    await source.LastOrDefaultAsync(predicate));

                Assert.Equal(
                    values.LastOrDefault(predicate),
                    await source.LastOrDefaultAsync(async (i, ct) => predicate(i)));
            }
        }

        [Fact]
        public async Task Cancellation_Cancels()
        {
            IAsyncEnumerable<int> source = CreateSource(2, 4, 8, 16);
            CancellationTokenSource cts;

            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.LastOrDefaultAsync(new CancellationToken(true)));

            cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.LastOrDefaultAsync(x =>
            {
                cts.Cancel();
                return x > 32;
            }, cts.Token));

            cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.LastOrDefaultAsync(async (x, ct) =>
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
            await source.LastOrDefaultAsync();
            Assert.Equal(5, source.MoveNextAsyncCount);
            Assert.Equal(4, source.CurrentCount);
            Assert.Equal(1, source.DisposeAsyncCount);

            predicateCount = 0;
            source = CreateSource(2, 4, 8, 16).Track();
            await source.LastOrDefaultAsync(i =>
            {
                predicateCount++;
                return i == 8;
            });
            Assert.Equal(5, source.MoveNextAsyncCount);
            Assert.Equal(4, source.CurrentCount);
            Assert.Equal(1, source.DisposeAsyncCount);

            source = CreateSource(2, 4, 8, 16).Track();
            await source.LastOrDefaultAsync(async (i, ct) =>
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
