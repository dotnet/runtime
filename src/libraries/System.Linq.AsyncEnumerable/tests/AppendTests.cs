// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class AppendTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.Append(null, 42));
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
                    values.Append(42),
                    source.Append(42));
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
                await foreach (int item in source.Append(42).WithCancellation(cts.Token))
                {
                    cts.Cancel();
                }
            });
        }

        [Fact]
        public async Task InterfaceCalls_ExpectedCounts()
        {
            TrackingAsyncEnumerable<int> source = CreateSource(2, 4, 8, 16).Track();
            await ConsumeAsync(source.Append(42));
            Assert.Equal(5, source.MoveNextAsyncCount);
            Assert.Equal(4, source.CurrentCount);
            Assert.Equal(1, source.DisposeAsyncCount);
        }

        [Fact]
        public async Task MultipleAppends_Coalesced()
        {
            IAsyncEnumerable<int> source = CreateSource(1, 2, 3);
            IAsyncEnumerable<int> result = source.Append(4).Append(5).Append(6);

            await AssertEqual(
                new[] { 1, 2, 3, 4, 5, 6 },
                result);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(5)]
        [InlineData(10)]
        public async Task ChainedAppends_ProducesCorrectSequence(int appendCount)
        {
            IAsyncEnumerable<int> source = AsyncEnumerable.Empty<int>();
            for (int i = 0; i < appendCount; i++)
            {
                source = source.Append(i);
            }

            int[] expected = Enumerable.Range(0, appendCount).ToArray();
            await AssertEqual(expected, source);
        }

        [Fact]
        public async Task AppendThenPrepend_Coalesced()
        {
            IAsyncEnumerable<int> source = CreateSource(2, 3);
            IAsyncEnumerable<int> result = source.Append(4).Prepend(1).Append(5);

            await AssertEqual(
                new[] { 1, 2, 3, 4, 5 },
                result);
        }

        [Fact]
        public async Task LongAppendChain_WorksCorrectly()
        {
            var enumerable = AsyncEnumerable.Empty<int>();
            for (int i = 0; i < 1000; i++)
            {
                enumerable = enumerable.Append(i);
            }

            int sum = 0;
            await foreach (int item in enumerable)
            {
                sum += item;
            }

            Assert.Equal(Enumerable.Range(0, 1000).Sum(), sum);
        }

        [Fact]
        public async Task VeryLongAppendChain_MatchesBenchmarkScenario()
        {
            // This test matches the exact scenario from the performance regression benchmark
            var enumerable = AsyncEnumerable.Empty<int>();
            for (int i = 0; i < 10000; i++)
            {
                enumerable = enumerable.Append(i);
            }

            int result = await enumerable.SumAsync();
            Assert.Equal(Enumerable.Range(0, 10000).Sum(), result);
        }

        [Fact]
        public async Task MultipleEnumerations_ProducesSameResults()
        {
            IAsyncEnumerable<int> source = CreateSource(1, 2, 3).Append(4).Append(5);

            List<int> firstEnumeration = await source.ToListAsync();
            List<int> secondEnumeration = await source.ToListAsync();

            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, firstEnumeration);
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, secondEnumeration);
        }

        [Fact]
        public async Task AppendOnEmptySource_WorksCorrectly()
        {
            IAsyncEnumerable<int> result = AsyncEnumerable.Empty<int>().Append(42);

            await AssertEqual(new[] { 42 }, result);
        }

        [Fact]
        public async Task DisposeBeforeComplete_DoesNotThrow()
        {
            IAsyncEnumerable<int> source = CreateSource(1, 2, 3).Append(4).Append(5);

            await using (var enumerator = source.GetAsyncEnumerator())
            {
                Assert.True(await enumerator.MoveNextAsync());
                Assert.Equal(1, enumerator.Current);
                // Dispose before completing enumeration
            }
        }

        [Fact]
        public async Task PrependAfterAppend_BothWork()
        {
            IAsyncEnumerable<int> source = CreateSource(2, 3);
            IAsyncEnumerable<int> result = source.Append(4).Prepend(1);

            await AssertEqual(new[] { 1, 2, 3, 4 }, result);
        }

        [Fact]
        public async Task ParallelEnumeration_AppendClones()
        {
            // Test that multiple enumerations work correctly (tests Clone() method)
            IAsyncEnumerable<int> source = CreateSource(1, 2, 3).Append(4).Append(5);

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
