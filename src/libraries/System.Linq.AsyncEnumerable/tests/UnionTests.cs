﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class UnionTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("first", () => AsyncEnumerable.Union(null, AsyncEnumerable.Empty<int>()));
            AssertExtensions.Throws<ArgumentNullException>("second", () => AsyncEnumerable.Union(AsyncEnumerable.Empty<int>(), null));
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
                        first.Union(second),
                        firstSource.Union(secondSource));

                    await AssertEqual(
                        second.Union(first),
                        secondSource.Union(firstSource));

                    await AssertEqual(
                        first.Union(second, OddEvenComparer),
                        firstSource.Union(secondSource, OddEvenComparer));
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
                await foreach (int item in first.Union(second).WithCancellation(cts.Token))
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
            await ConsumeAsync(first.Union(second));

            Assert.Equal(5, first.MoveNextAsyncCount);
            Assert.Equal(4, first.CurrentCount);
            Assert.Equal(1, first.DisposeAsyncCount);

            Assert.Equal(4, second.MoveNextAsyncCount);
            Assert.Equal(3, second.CurrentCount);
            Assert.Equal(1, second.DisposeAsyncCount);
        }
    }
}
