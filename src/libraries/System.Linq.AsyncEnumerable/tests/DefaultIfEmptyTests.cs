// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class DefaultIfEmptyTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.DefaultIfEmpty<int>(null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.DefaultIfEmpty<int>(null, 42));

            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.DefaultIfEmpty<string>(null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.DefaultIfEmpty<string>(null, ""));

            _ = AsyncEnumerable.DefaultIfEmpty(AsyncEnumerable.Empty<string>(), null);
        }

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
                    values.DefaultIfEmpty(),
                    source.DefaultIfEmpty());

                await AssertEqual(
                    values.DefaultIfEmpty(42),
                    source.DefaultIfEmpty(42));
            }
        }

        [Fact]
        public async Task Cancellation_Cancels()
        {
            IAsyncEnumerable<int> source = CreateSource(2, 4, 8, 16);
            CancellationTokenSource cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await foreach (int item in source.DefaultIfEmpty().WithCancellation(cts.Token))
                {
                    cts.Cancel();
                }
            });
        }

        [Fact]
        public async Task InterfaceCalls_ExpectedCounts()
        {
            TrackingAsyncEnumerable<int> source;

            source = CreateSource(2, 4, 8, 16).Track();
            await ConsumeAsync(source.DefaultIfEmpty());
            Assert.Equal(5, source.MoveNextAsyncCount);
            Assert.Equal(4, source.CurrentCount);
            Assert.Equal(1, source.DisposeAsyncCount);

            source = AsyncEnumerable.Empty<int>().Track();
            await ConsumeAsync(source.DefaultIfEmpty(42));
            Assert.Equal(1, source.MoveNextAsyncCount);
            Assert.Equal(0, source.CurrentCount);
            Assert.Equal(1, source.DisposeAsyncCount);
        }
    }
}
