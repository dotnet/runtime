// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class ShuffleTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.Shuffle<int>(null));
        }

        [Fact]
        public void Empty_ProducesEmpty() // validating an optimization / implementation detail
        {
            Assert.Same(AsyncEnumerable.Empty<string>(), AsyncEnumerable.Empty<string>().Shuffle());
        }

        [Theory]
        [InlineData(new int[0])]
        [InlineData(new int[] { 1 })]
        [InlineData(new int[] { 2, 4, 8 })]
        [InlineData(new int[] { -1, 2, 5, 6, 7, 8 })]
        public async Task VariousValues_ContainsAllInputValues(int[] values)
        {
            foreach (IAsyncEnumerable<int> source in CreateSources(values))
            {
                int[] shuffled = await source.Shuffle().ToArrayAsync();
                Array.Sort(shuffled);
                Assert.Equal(values, shuffled);
            }
        }

        [Fact]
        public async Task ToArrayAsync_ElementsAreRandomized()
        {
            // The chance that shuffling a thousand elements produces the same order twice is infinitesimal.
            int length = 1000;
            foreach (IAsyncEnumerable<int> source in CreateSources(Enumerable.Range(0, length).ToArray()))
            {
                int[] first = await source.Shuffle().ToArrayAsync();
                int[] second = await source.Shuffle().ToArrayAsync();
                Assert.Equal(length, first.Length);
                Assert.Equal(length, second.Length);
                Assert.NotEqual(first, second);
            }
        }

        [Fact]
        public async Task Cancellation_Cancels()
        {
            IAsyncEnumerable<int> source = CreateSource(2, 4, 8, 16);
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await ConsumeAsync(source.Shuffle().WithCancellation(new CancellationToken(true)));
            });
        }

        [Fact]
        public async Task InterfaceCalls_ExpectedCounts()
        {
            TrackingAsyncEnumerable<int> source = CreateSource(2, 4, 8, 16).Track();
            await ConsumeAsync(source.Shuffle());
            Assert.Equal(5, source.MoveNextAsyncCount);
            Assert.Equal(4, source.CurrentCount);
            Assert.Equal(1, source.DisposeAsyncCount);
        }
    }
}
