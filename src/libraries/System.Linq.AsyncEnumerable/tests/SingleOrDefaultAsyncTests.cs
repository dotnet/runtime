// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class SingleOrDefaultAsyncTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.SingleOrDefaultAsync<int>(null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.SingleOrDefaultAsync<int>(null, i => i % 2 == 0));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.SingleOrDefaultAsync<int>(null, async (i, ct) => i % 2 == 0));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.SingleOrDefaultAsync<int>(null, 42));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.SingleOrDefaultAsync<int>(null, i => i % 2 == 0, 42));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.SingleOrDefaultAsync<int>(null, async (i, ct) => i % 2 == 0, 42));

            AssertExtensions.Throws<ArgumentNullException>("predicate", () => AsyncEnumerable.SingleOrDefaultAsync(AsyncEnumerable.Empty<int>(), (Func<int, bool>)null));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => AsyncEnumerable.SingleOrDefaultAsync(AsyncEnumerable.Empty<int>(), (Func<int, CancellationToken, ValueTask<bool>>)null));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => AsyncEnumerable.SingleOrDefaultAsync(AsyncEnumerable.Empty<int>(), (Func<int, bool>)null, 42));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => AsyncEnumerable.SingleOrDefaultAsync(AsyncEnumerable.Empty<int>(), (Func<int, CancellationToken, ValueTask<bool>>)null, 42));
        }

        [Fact]
        public async Task EmptyInputs_DefaultValueReturned()
        {
            Assert.Equal(0, await AsyncEnumerable.Empty<int>().SingleOrDefaultAsync());
            Assert.Equal(42, await AsyncEnumerable.Empty<int>().SingleOrDefaultAsync(42));
            Assert.Equal(0, await AsyncEnumerable.Empty<int>().SingleOrDefaultAsync(i => i % 2 == 0));
            Assert.Equal(42, await AsyncEnumerable.Empty<int>().SingleOrDefaultAsync(i => i % 2 == 0, 42));
            Assert.Equal(0, await AsyncEnumerable.Empty<int>().SingleOrDefaultAsync(async (i, ct) => i % 2 == 0));
            Assert.Equal(42, await AsyncEnumerable.Empty<int>().SingleOrDefaultAsync(async (i, ct) => i % 2 == 0, 42));

            IAsyncEnumerable<int> source = new int[] { 1, 3, 5 }.ToAsyncEnumerable();
            Assert.Equal(0, await source.SingleOrDefaultAsync(i => i % 2 == 0));
            Assert.Equal(42, await source.SingleOrDefaultAsync(i => i % 2 == 0, 42));
            Assert.Equal(0, await source.SingleOrDefaultAsync(async (i, ct) => i % 2 == 0));
            Assert.Equal(42, await source.SingleOrDefaultAsync(async (i, ct) => i % 2 == 0, 42));
        }

        [Fact]
        public async Task DoubleInputs_Throws()
        {
            await Validate(new int[] { 1, 2 }.ToAsyncEnumerable().SingleOrDefaultAsync());
            await Validate(new int[] { 1, 2, 1, 2 }.ToAsyncEnumerable().SingleOrDefaultAsync(i => i % 2 == 0));
            await Validate(new int[] { 1, 2, 1, 2 }.ToAsyncEnumerable().SingleOrDefaultAsync(async (i, ct) => i % 2 == 0));

            await Validate(new int[] { 1, 2 }.ToAsyncEnumerable().SingleOrDefaultAsync(42));
            await Validate(new int[] { 1, 2, 1, 2 }.ToAsyncEnumerable().SingleOrDefaultAsync(i => i % 2 == 0, 42));
            await Validate(new int[] { 1, 2, 1, 2 }.ToAsyncEnumerable().SingleOrDefaultAsync(async (i, ct) => i % 2 == 0, 42));

            static async Task Validate(ValueTask<int> task)
            {
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await task);
            }
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
                        values.SingleOrDefault(),
                        await source.SingleOrDefaultAsync());
                }

                Func<int, bool> predicate = i => i == values.Last();

                Assert.Equal(
                    values.SingleOrDefault(predicate),
                    await source.SingleOrDefaultAsync(predicate));

                Assert.Equal(
                    values.SingleOrDefault(predicate),
                    await source.SingleOrDefaultAsync(async (i, ct) => predicate(i)));
            }
        }

        [Fact]
        public async Task Cancellation_Cancels()
        {
            IAsyncEnumerable<int> source = CreateSource(2, 4, 8, 16);
            CancellationTokenSource cts;

            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.SingleOrDefaultAsync(new CancellationToken(true)));

            cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.SingleOrDefaultAsync(x =>
            {
                cts.Cancel();
                return x > 32;
            }, cts.Token));

            cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.SingleOrDefaultAsync(async (x, ct) =>
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
            await Validate(s => s.SingleOrDefaultAsync(), count: 1);
            await Validate(s => s.SingleOrDefaultAsync(42), count: 1);
            await Validate(s => s.SingleOrDefaultAsync(i => i == 1));
            await Validate(s => s.SingleOrDefaultAsync(i => i == 1, 42));
            await Validate(s => s.SingleOrDefaultAsync(async (i, ct) => i == 1));
            await Validate(s => s.SingleOrDefaultAsync(async (i, ct) => i == 1, 42));

            static async Task Validate(Func<IAsyncEnumerable<int>, ValueTask<int>> func, int count = 4)
            {
                TrackingAsyncEnumerable<int> source = CreateSource(Enumerable.Range(1, count).ToArray()).Track();
                await func(source);
                Assert.Equal(count + 1, source.MoveNextAsyncCount);
                Assert.Equal(count, source.CurrentCount);
                Assert.Equal(1, source.DisposeAsyncCount);
            }
        }
    }
}
