// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class TakeTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.Take((IAsyncEnumerable<int>)null, 42));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.Take((IAsyncEnumerable<int>)null, new Range(new(0), new(42))));
        }

        [Fact]
        public void Empty_ProducesEmpty() // validating an optimization / implementation detail
        {
            Assert.Same(AsyncEnumerable.Empty<string>(), AsyncEnumerable.Empty<string>().TakeLast(42));

            Assert.Same(AsyncEnumerable.Empty<int>(), AsyncEnumerable.Take(new int[] { 1, 2, 3 }.ToAsyncEnumerable(), 0));
            Assert.Same(AsyncEnumerable.Empty<int>(), AsyncEnumerable.Take(new int[] { 1, 2, 3 }.ToAsyncEnumerable(), -1));
            Assert.Same(AsyncEnumerable.Empty<int>(), AsyncEnumerable.Take(new int[] { 1, 2, 3 }.ToAsyncEnumerable(), new Range(new(0), new(0))));
            Assert.Same(AsyncEnumerable.Empty<int>(), AsyncEnumerable.Take(new int[] { 1, 2, 3 }.ToAsyncEnumerable(), new Range(new(0, fromEnd: true), new(0, fromEnd: true))));
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
                foreach (int count in new[] { 0, 1, 2, 10 })
                {
                    await AssertEqual(
                        ints.Take(count),
                        source.Take(count));

#if NET
                    await AssertEqual(
                        ints.Take(..count),
                        source.Take(..count));

                    await AssertEqual(
                        ints.Take(count..),
                        source.Take(count..));

                    await AssertEqual(
                        ints.Take(..^count),
                        source.Take(..^count));

                    await AssertEqual(
                        ints.Take(^count..),
                        source.Take(^count..));

                    await AssertEqual(
                        ints.Take(3..(3+count)),
                        source.Take(3..(3+count)));
#endif
                }
            }
        }

        [Fact]
        public async Task Cancellation_Cancels()
        {
            IAsyncEnumerable<int> source = CreateSource(2, 4, 8, 16);
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await ConsumeAsync(source.Take(1).WithCancellation(new CancellationToken(true))));
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await ConsumeAsync(source.Take(new Range(new(0), new(3))).WithCancellation(new CancellationToken(true))));
        }

        [Fact]
        public async Task InterfaceCalls_ExpectedCounts()
        {
            TrackingAsyncEnumerable<int> source;

            source = CreateSource(1, 2, 3, 4).Track();
            await ConsumeAsync(source.Take(1));
            Assert.Equal(1, source.MoveNextAsyncCount);
            Assert.Equal(1, source.CurrentCount);
            Assert.Equal(1, source.DisposeAsyncCount);

            source = CreateSource(1, 2, 3, 4).Track();
            await ConsumeAsync(source.Take(3));
            Assert.Equal(3, source.MoveNextAsyncCount);
            Assert.Equal(3, source.CurrentCount);
            Assert.Equal(1, source.DisposeAsyncCount);

            source = CreateSource(1, 2, 3, 4).Track();
            await ConsumeAsync(source.Take(new Range(new(0), new(1))));
            Assert.Equal(1, source.MoveNextAsyncCount);
            Assert.Equal(1, source.CurrentCount);
            Assert.Equal(1, source.DisposeAsyncCount);
        }
    }
}
