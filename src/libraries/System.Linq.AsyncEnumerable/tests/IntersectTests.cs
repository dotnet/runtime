// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class IntersectTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("first", () => AsyncEnumerable.Intersect(null, AsyncEnumerable.Empty<int>()));
            AssertExtensions.Throws<ArgumentNullException>("second", () => AsyncEnumerable.Intersect(AsyncEnumerable.Empty<int>(), null));
        }

        [Fact]
        public void Empty_ProducesEmpty() // validating an optimization / implementation detail
        {
            IAsyncEnumerable<int> empty = AsyncEnumerable.Empty<int>();
            IAsyncEnumerable<int> nonEmpty = CreateSource(1, 2, 3);

            Assert.Same(empty, empty.Intersect(nonEmpty));
            Assert.Same(empty, nonEmpty.Intersect(empty));
        }

        [Theory]
        [InlineData(new int[0], new int[0])]
        [InlineData(new int[0], new int[] { 42 })]
        [InlineData(new int[] { 42, 43 }, new int[0])]
        [InlineData(new int[] { 1 }, new int[] { 2, 3 })]
        [InlineData(new int[] { 2, 4, 8 }, new int[] { 3, 5 })]
        [InlineData(new int[] { 2, 4, 8 }, new int[] { 2, 4, 8 })]
        [InlineData(new int[] { 2, 4, 8 }, new int[] { 2, 5, 8 })]
        [InlineData(new int[] { -1, 2, 5, 6, 7, 8 }, new int[] { int.MinValue, int.MaxValue })]
        public async Task VariousValues_MatchesEnumerable(int[] first, int[] second)
        {
            foreach (IAsyncEnumerable<int> firstSource in CreateSources(first))
            {
                foreach (IAsyncEnumerable<int> secondSource in CreateSources(second))
                {
                    await AssertEqual(
                        first.Intersect(second),
                        firstSource.Intersect(secondSource));

                    await AssertEqual(
                        second.Intersect(first),
                        secondSource.Intersect(firstSource));

                    await AssertEqual(
                        first.Intersect(second, OddEvenComparer),
                        firstSource.Intersect(secondSource, OddEvenComparer));
                }
            }
        }

        [Fact]
        public async Task Cancellation_Cancels()
        {
            IAsyncEnumerable<int> first = CreateSource(2, 4, 8, 16);
            IAsyncEnumerable<int> second = CreateSource(2, 5, 6, 7);
            CancellationTokenSource cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await foreach (int item in first.Intersect(second).WithCancellation(cts.Token))
                {
                    cts.Cancel();
                }
            });
        }

        [Fact]
        public async Task InterfaceCalls_ExpectedCounts()
        {
            TrackingAsyncEnumerable<int> first = CreateSource(2, 4, 8, 16).Track();
            TrackingAsyncEnumerable<int> second = CreateSource(1, 3, 5).Track();
            await ConsumeAsync(first.Intersect(second));

            Assert.Equal(5, first.MoveNextAsyncCount);
            Assert.Equal(4, first.CurrentCount);
            Assert.Equal(1, first.DisposeAsyncCount);

            Assert.Equal(4, second.MoveNextAsyncCount);
            Assert.Equal(3, second.CurrentCount);
            Assert.Equal(1, second.DisposeAsyncCount);
        }
    }
}
