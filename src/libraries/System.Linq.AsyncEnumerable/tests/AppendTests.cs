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
    }
}
