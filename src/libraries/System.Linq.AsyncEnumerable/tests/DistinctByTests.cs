// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class DistinctByTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.DistinctBy((IAsyncEnumerable<int>)null, x => x));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.DistinctBy((IAsyncEnumerable<int>)null, async (x, ct) => x));

            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => AsyncEnumerable.DistinctBy(AsyncEnumerable.Empty<int>(), (Func<int, int>)null));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => AsyncEnumerable.DistinctBy(AsyncEnumerable.Empty<int>(), (Func<int, CancellationToken, ValueTask<int>>)null));
        }

        [Fact]
        public void Empty_ProducesEmpty() // validating an optimization / implementation detail
        {
            Assert.Same(AsyncEnumerable.Empty<int>(), AsyncEnumerable.Empty<int>().DistinctBy(i => i));
            Assert.Same(AsyncEnumerable.Empty<int>(), AsyncEnumerable.Empty<int>().DistinctBy(async (i, ct) => i));
        }

#if NET
        [Theory]
        [InlineData(new int[0])]
        [InlineData(new int[] { 1 })]
        [InlineData(new int[] { 1, 1, 1, 2, 2, 2, 2, 2 })]
        [InlineData(new int[] { 2, 4, 8 })]
        [InlineData(new int[] { 2, 4, 8, 2, 4, 8, 2 })]
        [InlineData(new int[] { -1, 2, 5, 6, 7, 8 })]
        public async Task VariousValues_MatchesEnumerable(int[] values)
        {
            foreach (IAsyncEnumerable<int> source in CreateSources(values))
            {
                foreach (IEqualityComparer<int> comparer in new[] { null, EqualityComparer<int>.Default, OddEvenComparer })
                {
                    await AssertEqual(
                        values.DistinctBy(x => x, comparer),
                        source.DistinctBy(x => x, comparer));

                    await AssertEqual(
                        values.DistinctBy(x => x, comparer),
                        source.DistinctBy(async (x, ct) => x, comparer));

                    await AssertEqual(
                        values.DistinctBy(x => x / 3, comparer),
                        source.DistinctBy(x => x / 3, comparer));

                    await AssertEqual(
                        values.DistinctBy(x => x / 3, comparer),
                        source.DistinctBy(async (x, ct) => x / 3, comparer));
                }
            }
        }
#endif

        [Fact]
        public async Task Cancellation_Cancels()
        {
            IAsyncEnumerable<int> source = CreateSource(2, 4, 8, 16);

            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await ConsumeAsync(source.DistinctBy(x => x).WithCancellation(new CancellationToken(true)));
            });

            CancellationTokenSource cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await ConsumeAsync(source.DistinctBy(x =>
                {
                    cts.Cancel();
                    return x;
                }).WithCancellation(cts.Token));
            });

            cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await ConsumeAsync(source.DistinctBy(async (x, ct) =>
                {
                    Assert.Equal(cts.Token, ct);
                    await Task.Yield();
                    cts.Cancel();
                    return x;
                }).WithCancellation(cts.Token));
            });
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task InterfaceCalls_ExpectedCounts(bool useAsync)
        {
            TrackingAsyncEnumerable<int> source = CreateSource(2, 4, 8, 16).Track();
            int funcCount;

            funcCount = 0;
            await ConsumeAsync(useAsync ?
                source.DistinctBy(x =>
                {
                    funcCount++;
                    return x;
                }) :
                source.DistinctBy(async (x, ct) =>
                {
                    funcCount++;
                    return x;
                }));
            Assert.Equal(5, source.MoveNextAsyncCount);
            Assert.Equal(4, source.CurrentCount);
            Assert.Equal(1, source.DisposeAsyncCount);
            Assert.Equal(4, funcCount);
        }
    }
}
