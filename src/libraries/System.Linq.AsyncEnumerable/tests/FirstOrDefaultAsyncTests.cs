// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class FirstOrDefaultAsyncTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.FirstOrDefaultAsync<int>(null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.FirstOrDefaultAsync<int>(null, i => i % 2 == 0));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.FirstOrDefaultAsync<int>(null, async (i, ct) => i % 2 == 0));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.FirstOrDefaultAsync<int>(null, 42));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.FirstOrDefaultAsync<int>(null, i => i % 2 == 0, 42));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.FirstOrDefaultAsync<int>(null, async (i, ct) => i % 2 == 0, 42));

            AssertExtensions.Throws<ArgumentNullException>("predicate", () => AsyncEnumerable.FirstOrDefaultAsync(AsyncEnumerable.Empty<int>(), (Func<int, bool>)null));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => AsyncEnumerable.FirstOrDefaultAsync(AsyncEnumerable.Empty<int>(), (Func<int, CancellationToken, ValueTask<bool>>)null));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => AsyncEnumerable.FirstOrDefaultAsync(AsyncEnumerable.Empty<int>(), (Func<int, bool>)null, 42));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => AsyncEnumerable.FirstOrDefaultAsync(AsyncEnumerable.Empty<int>(), (Func<int, CancellationToken, ValueTask<bool>>)null, 42));
        }

        [Fact]
        public async Task EmptyInputs_DefaultValueReturned()
        {
            Assert.Equal(0, await AsyncEnumerable.Empty<int>().FirstOrDefaultAsync());
            Assert.Equal(42, await AsyncEnumerable.Empty<int>().FirstOrDefaultAsync(42));
            Assert.Equal(0, await AsyncEnumerable.Empty<int>().FirstOrDefaultAsync(i => i % 2 == 0));
            Assert.Equal(42, await AsyncEnumerable.Empty<int>().FirstOrDefaultAsync(i => i % 2 == 0, 42));
            Assert.Equal(0, await AsyncEnumerable.Empty<int>().FirstOrDefaultAsync(async (i, ct) => i % 2 == 0));
            Assert.Equal(42, await AsyncEnumerable.Empty<int>().FirstOrDefaultAsync(async (i, ct) => i % 2 == 0, 42));

            IAsyncEnumerable<int> source = new int[] { 1, 3, 5 }.ToAsyncEnumerable();
            Assert.Equal(0, await source.FirstOrDefaultAsync(i => i % 2 == 0));
            Assert.Equal(42, await source.FirstOrDefaultAsync(i => i % 2 == 0, 42));
            Assert.Equal(0, await source.FirstOrDefaultAsync(async (i, ct) => i % 2 == 0));
            Assert.Equal(42, await source.FirstOrDefaultAsync(async (i, ct) => i % 2 == 0, 42));
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
                    values.FirstOrDefault(),
                    await source.FirstOrDefaultAsync());

                Func<int, bool> predicate = i => i == values.Last();

                Assert.Equal(
                    values.FirstOrDefault(predicate),
                    await source.FirstOrDefaultAsync(predicate));

                Assert.Equal(
                    values.FirstOrDefault(predicate),
                    await source.FirstOrDefaultAsync(async (i, ct) => predicate(i)));
            }
        }

        [Fact]
        public async Task Cancellation_Cancels()
        {
            IAsyncEnumerable<int> source = CreateSource(2, 4, 8, 16);
            CancellationTokenSource cts;

            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.FirstOrDefaultAsync(new CancellationToken(true)));

            cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.FirstOrDefaultAsync(x =>
            {
                cts.Cancel();
                return x > 32;
            }, cts.Token));

            cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.FirstOrDefaultAsync(async (x, ct) =>
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
            await Validate(s => s.FirstOrDefaultAsync());
            await Validate(s => s.FirstOrDefaultAsync(42));
            await Validate(s => s.FirstOrDefaultAsync(i => i % 2 == 0));
            await Validate(s => s.FirstOrDefaultAsync(i => i % 2 == 0, 42));
            await Validate(s => s.FirstOrDefaultAsync(async (i, ct) => i % 2 == 0));
            await Validate(s => s.FirstOrDefaultAsync(async (i, ct) => i % 2 == 0, 42));

            static async Task Validate(Func<IAsyncEnumerable<int>, ValueTask<int>> func)
            {
                TrackingAsyncEnumerable<int> source = CreateSource(2, 4, 8, 16).Track();
                await func(source);
                Assert.Equal(1, source.MoveNextAsyncCount);
                Assert.Equal(1, source.CurrentCount);
                Assert.Equal(1, source.DisposeAsyncCount);
            }
        }
    }
}
