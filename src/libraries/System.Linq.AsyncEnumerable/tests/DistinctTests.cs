// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class DistinctTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.Distinct<int>(null));
        }

        [Fact]
        public void Empty_ProducesEmpty() // validating an optimization / implementation detail
        {
            Assert.Same(AsyncEnumerable.Empty<int>(), AsyncEnumerable.Empty<int>().Distinct());
            Assert.Same(AsyncEnumerable.Empty<int>(), AsyncEnumerable.Empty<int>().Distinct());
        }

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
                await AssertEqual(
                    values.Distinct(),
                    source.Distinct());

                await AssertEqual(
                    values.Distinct(OddEvenComparer),
                    source.Distinct(OddEvenComparer));
            }
        }

        [Fact]
        public async Task Cancellation_Cancels()
        {
            IAsyncEnumerable<int> source = CreateSource(2, 4, 8, 16);
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await ConsumeAsync(source.Distinct().WithCancellation(new CancellationToken(true)));
            });
        }

        [Fact]
        public async Task InterfaceCalls_ExpectedCounts()
        {
            TrackingAsyncEnumerable<int> source = CreateSource(2, 4, 8, 16).Track();
            await ConsumeAsync(source.Distinct());
            Assert.Equal(5, source.MoveNextAsyncCount);
            Assert.Equal(4, source.CurrentCount);
            Assert.Equal(1, source.DisposeAsyncCount);
        }
    }
}
