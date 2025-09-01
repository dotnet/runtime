// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class ToAsyncEnumerableTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.ToAsyncEnumerable<int>(null));
        }

        [Fact]
        public void Empty_ProducesEmpty() // validating an optimization / implementation detail
        {
            Assert.Same(AsyncEnumerable.Empty<int>(), Enumerable.Empty<int>().ToAsyncEnumerable());
            Assert.Same(AsyncEnumerable.Empty<int>(), Array.Empty<int>().ToAsyncEnumerable());
            Assert.Same(AsyncEnumerable.Empty<int>(), new int[0].ToAsyncEnumerable());

            Assert.NotSame(AsyncEnumerable.Empty<int>(), new List<int>().ToAsyncEnumerable());
            Assert.NotSame(AsyncEnumerable.Empty<int>(), new HashSet<int>().ToAsyncEnumerable());
            Assert.NotSame(AsyncEnumerable.Empty<int>(), new ReadOnlyCollection<int>([]).ToAsyncEnumerable());
        }

        [Theory]
        [InlineData(new int[0])]
        [InlineData(new int[] { 1 })]
        [InlineData(new int[] { 1, 1, 1 })]
        [InlineData(new int[] { 2, 4, 8 })]
        [InlineData(new int[] { -1, 2, 5, 6, 7, 8 })]
        [InlineData(new int[] { -1, 2, 5, 6, 7, 8, 6, -1, 5, 14 })]
        public async Task VariousValues_MatchesEnumerable(int[] values)
        {
            foreach (IAsyncEnumerable<int> source in CreateSources(values))
            {
                await AssertEqual(values, (await source.ToArrayAsync()).ToAsyncEnumerable());
                await AssertEqual(values, (await source.ToListAsync()).ToAsyncEnumerable());
                await AssertEqual(values, new ReadOnlyCollection<int>(await source.ToListAsync()).ToAsyncEnumerable());
                await AssertEqual(values, new Queue<int>(await source.ToListAsync()).ToAsyncEnumerable());
            }
        }

        [Fact]
        public async Task InterfaceCalls_ExpectedCounts()
        {
            TrackingAsyncEnumerable<int> source = CreateSource(2, 4, 8, 16).Track();
            await source.ToArrayAsync();
            Assert.Equal(5, source.MoveNextAsyncCount);
            Assert.Equal(4, source.CurrentCount);
            Assert.Equal(1, source.DisposeAsyncCount);
        }
    }
}
