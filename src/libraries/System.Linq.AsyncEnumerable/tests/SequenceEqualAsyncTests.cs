// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class SequenceEqualAsyncTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("first", () => AsyncEnumerable.SequenceEqualAsync(null, AsyncEnumerable.Empty<int>()));
            AssertExtensions.Throws<ArgumentNullException>("second", () => AsyncEnumerable.SequenceEqualAsync(AsyncEnumerable.Empty<int>(), null));
        }

        [Fact]
        public async Task VariousValues_MatchesEnumerable()
        {
            Random rand = new(42);
            foreach (int length in new[] { 0, 1, 10 })
            {
                int[] values = new int[length];
                FillRandom(rand, values);

                foreach (IAsyncEnumerable<int> source in CreateSources(values))
                {
                    foreach (IEqualityComparer<int> comparer in new[] { EqualityComparer<int>.Default, null, OddEvenComparer })
                    {
                        Assert.Equal(
                            values.SequenceEqual(values, comparer),
                            await source.SequenceEqualAsync(source, comparer));

                        Assert.Equal(
                            values.SequenceEqual(values.Concat([1]), comparer),
                            await source.SequenceEqualAsync(source.Concat(new[] { 1 }.ToAsyncEnumerable()), comparer));

                        Assert.Equal(
                            values.SequenceEqual(new[] { 42 }.Concat(values), comparer),
                            await source.SequenceEqualAsync(new[] { 1 }.ToAsyncEnumerable().Concat(source), comparer));
                    }
                }
            }
        }

        [Fact]
        public async Task Cancellation_Cancels()
        {
            IAsyncEnumerable<int> source = CreateSource(1, 3, 5);
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.SequenceEqualAsync(source, null, new CancellationToken(true)));
        }

        [Fact]
        public async Task InterfaceCalls_ExpectedCounts()
        {
            TrackingAsyncEnumerable<int> first, second;

            first = CreateSource(1, 3, 5).Track();
            second = CreateSource(1, 3, 5).Track();
            Assert.True(await first.SequenceEqualAsync(second));
            Assert.Equal(4, first.MoveNextAsyncCount);
            Assert.Equal(3, first.CurrentCount);
            Assert.Equal(1, first.DisposeAsyncCount);
            Assert.Equal(4, second.MoveNextAsyncCount);
            Assert.Equal(3, second.CurrentCount);
            Assert.Equal(1, second.DisposeAsyncCount);

            first = CreateSource(1).Track();
            second = CreateSource(1, 3, 5).Track();
            Assert.False(await first.SequenceEqualAsync(second));
            Assert.Equal(2, first.MoveNextAsyncCount);
            Assert.Equal(1, first.CurrentCount);
            Assert.Equal(1, first.DisposeAsyncCount);
            Assert.Equal(2, second.MoveNextAsyncCount);
            Assert.Equal(1, second.CurrentCount);
            Assert.Equal(1, second.DisposeAsyncCount);
        }
    }
}
