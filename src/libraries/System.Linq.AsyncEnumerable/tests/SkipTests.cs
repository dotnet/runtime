// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class SkipTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.Skip((IAsyncEnumerable<int>)null, 42));
        }

        [Fact]
        public void Empty_ProducesEmpty() // validating an optimization / implementation detail
        {
            Assert.Same(AsyncEnumerable.Empty<string>(), AsyncEnumerable.Empty<string>().Skip(42));

            IAsyncEnumerable<int> source = CreateSource(2, 4, 8, 16);
            Assert.Same(source, source.Skip(0));
            Assert.Same(source, source.Skip(-1));
        }

        [Theory]
        [InlineData(new int[0])]
        [InlineData(new int[] { 42 })]
        [InlineData(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 })]
        [InlineData(new int[] { -1, 1, -2, 2, -10, 10 })]
        [InlineData(new int[] { int.MinValue, int.MaxValue })]
        public async Task VariousValues_MatchesEnumerable(int[] ints)
        {
            foreach (IAsyncEnumerable<int> source in CreateSources(ints))
            {
                foreach (int count in new[] { -1, 0, 1, 2, 10 })
                {
                    await AssertEqual(
                        ints.Skip(count),
                        source.Skip(count));
                }
            }
        }

        [Fact]
        public async Task Cancellation_Cancels()
        {
            IAsyncEnumerable<int> source = CreateSource(2, 4, 8, 16);
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await ConsumeAsync(source.Skip(1).WithCancellation(new CancellationToken(true))));
        }

        [Fact]
        public async Task InterfaceCalls_ExpectedCounts()
        {
            TrackingAsyncEnumerable<int> source;

            source = CreateSource(1, 2, 3, 4).Track();
            await ConsumeAsync(source.Skip(0));
            Assert.Equal(5, source.MoveNextAsyncCount);
            Assert.Equal(4, source.CurrentCount);
            Assert.Equal(1, source.DisposeAsyncCount);

            source = CreateSource(1, 2, 3, 4).Track();
            await ConsumeAsync(source.Skip(3));
            Assert.Equal(5, source.MoveNextAsyncCount);
            Assert.Equal(1, source.CurrentCount);
            Assert.Equal(1, source.DisposeAsyncCount);
        }
    }
}
