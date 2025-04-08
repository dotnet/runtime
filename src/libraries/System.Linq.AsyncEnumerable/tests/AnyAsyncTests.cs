// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class AnyAsyncTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.AnyAsync<int>(null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.AnyAsync<int>(null, x => x % 2 == 0));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => AsyncEnumerable.AnyAsync(AsyncEnumerable.Empty<int>(), (Func<int, bool>)null));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => AsyncEnumerable.AnyAsync(AsyncEnumerable.Empty<int>(), (Func<int, CancellationToken, ValueTask<bool>>)null));
        }

        [Theory]
        [InlineData(new int[0])]
        [InlineData(new int[] { 1 })]
        [InlineData(new int[] { 2, 4, 8 })]
        [InlineData(new int[] { -1, 2, 5, 6, 7, 8 })]
        [InlineData(new int[] { 1, 3, 5, 7 })]
        public async Task VariousValues_MatchesEnumerable(int[] values)
        {
            foreach (IAsyncEnumerable<int> source in CreateSources(values))
            {
                Func<int, bool> predicate = x => x % 2 == 0;

                Assert.Equal(
                    values.Any(),
                    await source.AnyAsync());

                Assert.Equal(
                    values.Any(predicate),
                    await source.AnyAsync(predicate));

                Assert.Equal(
                    values.Any(predicate),
                    await source.AnyAsync(async (x, ct) =>
                    {
                        await Task.Yield();
                        return predicate(x);
                    }));
            }
        }

        [Fact]
        public async Task Cancellation_Cancels()
        {
            IAsyncEnumerable<int> source = CreateSource(2, 4, 8, 16);

            CancellationTokenSource cts = new();
            cts.Cancel();
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.AnyAsync(x => x < 0, cts.Token));

            cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.AnyAsync(async (x, ct) =>
            {
                Assert.Equal(cts.Token, ct);
                await Task.Yield();
                cts.Cancel();
                return x < 0;
            }, cts.Token));
        }

        [Fact]
        public async Task InterfaceCalls_ExpectedCounts()
        {
            TrackingAsyncEnumerable<int> source;

            source = CreateSource(2, 4, 8, 16).Track();
            Assert.True(await source.AnyAsync());
            Assert.Equal(1, source.MoveNextAsyncCount);
            Assert.Equal(0, source.CurrentCount);
            Assert.Equal(1, source.DisposeAsyncCount);

            foreach (bool useAsync in TrueFalseBools)
            {
                source = CreateSource(2, 4, 8, 16).Track();
                Assert.True(useAsync ?
                    await source.AnyAsync(x => x > 7) :
                    await source.AnyAsync(async (x, ct) => x > 7));
                Assert.Equal(3, source.MoveNextAsyncCount);
                Assert.Equal(3, source.CurrentCount);
                Assert.Equal(1, source.DisposeAsyncCount);

                source = CreateSource(2, 4, 8, 16).Track();
                Assert.False(useAsync ?
                    await source.AnyAsync(x => x > 20) :
                    await source.AnyAsync(async (x, ct) => x > 20));
                Assert.Equal(5, source.MoveNextAsyncCount);
                Assert.Equal(4, source.CurrentCount);
                Assert.Equal(1, source.DisposeAsyncCount);

                source = CreateSource(2, 4, 8, 16).Track();
                await Assert.ThrowsAsync<Exception>(async () =>
                {
                    await (useAsync ?
                        source.AnyAsync(x => throw new Exception()) :
                        source.AnyAsync(async(x, ct) => throw new Exception()));
                });
                Assert.Equal(1, source.MoveNextAsyncCount);
                Assert.Equal(1, source.CurrentCount);
                Assert.Equal(1, source.DisposeAsyncCount);
            }
        }
    }
}
