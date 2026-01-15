// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class ConcatTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("first", () => AsyncEnumerable.Concat(null, AsyncEnumerable.Empty<int>()));
            AssertExtensions.Throws<ArgumentNullException>("second", () => AsyncEnumerable.Concat(AsyncEnumerable.Empty<int>(), null));
        }

        [Fact]
        public void Empty_ProducesEmpty() // validating an optimization / implementation detail
        {
            IAsyncEnumerable<int> empty = AsyncEnumerable.Empty<int>();
            IAsyncEnumerable<int> nonEmpty = CreateSource(1, 3, 5);

            Assert.Same(empty, empty.Concat(empty));
            Assert.Same(nonEmpty, nonEmpty.Concat(empty));
            Assert.Same(nonEmpty, empty.Concat(nonEmpty));
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
                        first.Concat(second),
                        firstSource.Concat(secondSource));

                    await AssertEqual(
                        second.Concat(first),
                        secondSource.Concat(firstSource));
                }
            }
        }

        [Fact]
        public async Task Cancellation_Cancels()
        {
            IAsyncEnumerable<int> first = CreateSource(2, 4, 8, 16);
            IAsyncEnumerable<int> second = CreateSource(1, 3, 5);
            CancellationTokenSource cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await foreach (int item in first.Concat(second).WithCancellation(cts.Token))
                {
                    cts.Cancel();
                }
            });
        }

        [Fact]
        public async Task InterfaceCalls_ExpectedCounts()
        {
            TrackingAsyncEnumerable<int> first, second;

            first = CreateSource(2, 4, 8, 16).Track();
            second = CreateSource(1, 3, 5).Track();
            await ConsumeAsync(first.Concat(second));

            Assert.Equal(5, first.MoveNextAsyncCount);
            Assert.Equal(4, first.CurrentCount);
            Assert.Equal(1, first.DisposeAsyncCount);

            Assert.Equal(4, second.MoveNextAsyncCount);
            Assert.Equal(3, second.CurrentCount);
            Assert.Equal(1, second.DisposeAsyncCount);
        }

        [Fact]
        public async Task MultipleConcat_Coalesced()
        {
            IAsyncEnumerable<int> first = CreateSource(1, 2);
            IAsyncEnumerable<int> second = CreateSource(3, 4);
            IAsyncEnumerable<int> third = CreateSource(5, 6);
            IAsyncEnumerable<int> result = first.Concat(second).Concat(third);

            await AssertEqual(
                new[] { 1, 2, 3, 4, 5, 6 },
                result);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(5)]
        [InlineData(10)]
        public async Task ChainedConcat_ProducesCorrectSequence(int concatCount)
        {
            IAsyncEnumerable<int> result = AsyncEnumerable.Empty<int>();
            List<int> expected = new();

            for (int i = 0; i < concatCount; i++)
            {
                int start = i * 2;
                var source = CreateSource(start, start + 1);
                result = result.Concat(source);
                expected.Add(start);
                expected.Add(start + 1);
            }

            await AssertEqual(expected, result);
        }

        [Fact]
        public async Task LongConcatChain_WorksCorrectly()
        {
            var enumerable = AsyncEnumerable.Empty<int>();
            for (int i = 0; i < 100; i++)
            {
                enumerable = enumerable.Concat(CreateSource(i));
            }

            int sum = 0;
            await foreach (int item in enumerable)
            {
                sum += item;
            }

            Assert.Equal(Enumerable.Range(0, 100).Sum(), sum);
        }

        [Fact]
        public async Task ConcatWithAppendPrepend_WorksCorrectly()
        {
            IAsyncEnumerable<int> first = CreateSource(2, 3);
            IAsyncEnumerable<int> second = CreateSource(5, 6);
            IAsyncEnumerable<int> result = first.Append(4).Concat(second).Prepend(1).Append(7);

            await AssertEqual(
                new[] { 1, 2, 3, 4, 5, 6, 7 },
                result);
        }

        [Fact]
        public async Task MultipleEnumerations_ProducesSameResults()
        {
            IAsyncEnumerable<int> first = CreateSource(1, 2);
            IAsyncEnumerable<int> second = CreateSource(3, 4);
            IAsyncEnumerable<int> source = first.Concat(second);

            List<int> firstEnumeration = await source.ToListAsync();
            List<int> secondEnumeration = await source.ToListAsync();

            Assert.Equal(new[] { 1, 2, 3, 4 }, firstEnumeration);
            Assert.Equal(new[] { 1, 2, 3, 4 }, secondEnumeration);
        }

        [Fact]
        public async Task DisposeBeforeComplete_DoesNotThrow()
        {
            IAsyncEnumerable<int> first = CreateSource(1, 2, 3);
            IAsyncEnumerable<int> second = CreateSource(4, 5, 6);
            IAsyncEnumerable<int> source = first.Concat(second);

            await using (var enumerator = source.GetAsyncEnumerator())
            {
                Assert.True(await enumerator.MoveNextAsync());
                Assert.Equal(1, enumerator.Current);
                Assert.True(await enumerator.MoveNextAsync());
                Assert.Equal(2, enumerator.Current);
                // Dispose before completing enumeration
            }
        }

        [Fact]
        public async Task ConcatEmptySequences_ReturnsEmpty()
        {
            IAsyncEnumerable<int> first = AsyncEnumerable.Empty<int>();
            IAsyncEnumerable<int> second = AsyncEnumerable.Empty<int>();
            IAsyncEnumerable<int> result = first.Concat(second);

            await AssertEqual(Array.Empty<int>(), result);
        }

        [Fact]
        public async Task Concat_TransitionsCorrectlyBetweenSources()
        {
            IAsyncEnumerable<int> first = CreateSource(1, 2);
            IAsyncEnumerable<int> second = CreateSource(3, 4);
            IAsyncEnumerable<int> third = CreateSource(5, 6);

            IAsyncEnumerable<int> result = first.Concat(second).Concat(third);

            List<int> items = new();
            await foreach (int item in result)
            {
                items.Add(item);
            }

            Assert.Equal(new[] { 1, 2, 3, 4, 5, 6 }, items);
        }

        [Fact]
        public async Task ParallelEnumeration_ConcatClones()
        {
            // Test that multiple enumerations work correctly (tests Clone() method)
            IAsyncEnumerable<int> first = CreateSource(1, 2);
            IAsyncEnumerable<int> second = CreateSource(3, 4);
            IAsyncEnumerable<int> source = first.Concat(second);

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

            Assert.Equal(new[] { 1, 2, 3, 4 }, list1);
            Assert.Equal(new[] { 1, 2, 3, 4 }, list2);
        }
    }
}
