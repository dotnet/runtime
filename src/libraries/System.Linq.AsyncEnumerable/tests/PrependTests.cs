// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class PrependTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.Prepend(null, 42));
        }

#if NET
        [Theory]
        [InlineData(new int[0])]
        [InlineData(new int[] { 1 })]
        [InlineData(new int[] { 2, 4, 8 })]
        [InlineData(new int[] { -1, 2, 5, 6, 7, 8 })]
        public async Task VariousValues_MatchesEnumerable(int[] values)
        {
            foreach (IAsyncEnumerable<int> source in CreateSources(values))
            {
                await AssertEqual(
                    values.Prepend(42),
                    source.Prepend(42));
            }
        }
#endif

        [Fact]
        public async Task Cancellation_Cancels()
        {
            IAsyncEnumerable<int> source = CreateSource(2, 4, 8, 16);
            CancellationTokenSource cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await foreach (int item in source.Prepend(42).WithCancellation(cts.Token))
                {
                    cts.Cancel();
                }
            });
        }

        [Fact]
        public async Task InterfaceCalls_ExpectedCounts()
        {
            TrackingAsyncEnumerable<int> source = CreateSource(2, 4, 8, 16).Track();
            await ConsumeAsync(source.Prepend(42));
            Assert.Equal(5, source.MoveNextAsyncCount);
            Assert.Equal(4, source.CurrentCount);
            Assert.Equal(1, source.DisposeAsyncCount);
        }

        [Fact]
        public async Task MultiplePrepends_Coalesced()
        {
            IAsyncEnumerable<int> source = CreateSource(4, 5, 6);
            IAsyncEnumerable<int> result = source.Prepend(3).Prepend(2).Prepend(1);

            await AssertEqual(
                new[] { 1, 2, 3, 4, 5, 6 },
                result);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(5)]
        [InlineData(10)]
        public async Task ChainedPrepends_ProducesCorrectSequence(int prependCount)
        {
            IAsyncEnumerable<int> source = AsyncEnumerable.Empty<int>();
            for (int i = prependCount - 1; i >= 0; i--)
            {
                source = source.Prepend(i);
            }

            int[] expected = Enumerable.Range(0, prependCount).ToArray();
            await AssertEqual(expected, source);
        }

        [Fact]
        public async Task PrependThenAppend_Coalesced()
        {
            IAsyncEnumerable<int> source = CreateSource(2, 3);
            IAsyncEnumerable<int> result = source.Prepend(1).Append(4).Prepend(0);

            await AssertEqual(
                new[] { 0, 1, 2, 3, 4 },
                result);
        }

        [Fact]
        public async Task LongPrependChain_WorksCorrectly()
        {
            var enumerable = AsyncEnumerable.Empty<int>();
            for (int i = 999; i >= 0; i--)
            {
                enumerable = enumerable.Prepend(i);
            }

            int sum = 0;
            await foreach (int item in enumerable)
            {
                sum += item;
            }

            Assert.Equal(Enumerable.Range(0, 1000).Sum(), sum);
        }

        [Fact]
        public async Task MultipleEnumerations_ProducesSameResults()
        {
            IAsyncEnumerable<int> source = CreateSource(2, 3, 4).Prepend(1).Prepend(0);

            List<int> firstEnumeration = await source.ToListAsync();
            List<int> secondEnumeration = await source.ToListAsync();

            Assert.Equal(new[] { 0, 1, 2, 3, 4 }, firstEnumeration);
            Assert.Equal(new[] { 0, 1, 2, 3, 4 }, secondEnumeration);
        }

        [Fact]
        public async Task PrependOnEmptySource_WorksCorrectly()
        {
            IAsyncEnumerable<int> result = AsyncEnumerable.Empty<int>().Prepend(42);

            await AssertEqual(new[] { 42 }, result);
        }

        [Fact]
        public async Task DisposeBeforeComplete_DoesNotThrow()
        {
            IAsyncEnumerable<int> source = CreateSource(2, 3, 4).Prepend(1).Prepend(0);

            await using (var enumerator = source.GetAsyncEnumerator())
            {
                Assert.True(await enumerator.MoveNextAsync());
                Assert.Equal(0, enumerator.Current);
                // Dispose before completing enumeration
            }
        }

        [Fact]
        public async Task AppendAfterPrepend_BothWork()
        {
            IAsyncEnumerable<int> source = CreateSource(2, 3);
            IAsyncEnumerable<int> result = source.Prepend(1).Append(4);

            await AssertEqual(new[] { 1, 2, 3, 4 }, result);
        }

        [Fact]
        public async Task ParallelEnumeration_PrependClones()
        {
            // Test that multiple enumerations work correctly (tests Clone() method)
            IAsyncEnumerable<int> source = CreateSource(3, 4, 5).Prepend(2).Prepend(1);

            // Get two enumerators explicitly to trigger cloning
            await using var enum1 = source.GetAsyncEnumerator();
            await using var enum2 = source.GetAsyncEnumerator();

            List<int> list1 = new();
            while (await enum1.MoveNextAsync())
            {
                list1.Add(enum1.Current);
            }

            List<int> list2 = new();
            while (await enum2.MoveNextAsync())
            {
                list2.Add(enum2.Current);
            }

            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, list1);
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, list2);
        }
    }
}
