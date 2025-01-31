// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class ToHashSetAsyncTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.ToHashSetAsync<int>(null));
        }

        [Theory]
        [InlineData(new int[0])]
        [InlineData(new int[] { 1 })]
        [InlineData(new int[] { 1, 1, 1 })]
        [InlineData(new int[] { 2, 4, 8 })]
        [InlineData(new int[] { -1, 2, 5, 6, 7, 8 })]
        [InlineData(new int[] { -1, 2, 5, 6, 7, 8, 6, -1, 5, 14 })]
        public async Task VariousValues_MatchesEnumerable(int[] values)
        {
            foreach (IAsyncEnumerable<int> source in CreateSources(values))
            {
                Assert.Equal(
                    new HashSet<int>(values),
                    await source.ToHashSetAsync());

                Assert.Equal(
                    new HashSet<int>(values, OddEvenComparer).OrderBy(s => s),
                    (await source.ToHashSetAsync(OddEvenComparer)).OrderBy(s => s));

                Assert.Equal(
                    new HashSet<string>(values.Select(i => i.ToString())),
                    await source.Select(i => i.ToString()).ToHashSetAsync());

                Assert.Equal(
                    new HashSet<string>(values.Select(i => i.ToString()), LengthComparer).OrderBy(s => s),
                    (await source.Select(i => i.ToString()).ToHashSetAsync(LengthComparer)).OrderBy(s => s));
            }
        }

        [Fact]
        public async Task Cancellation_Cancels()
        {
            IAsyncEnumerable<int> source = CreateSource(2, 4, 8, 16);
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.ToHashSetAsync(null, new CancellationToken(true)));
        }

        [Fact]
        public async Task InterfaceCalls_ExpectedCounts()
        {
            TrackingAsyncEnumerable<int> source = CreateSource(2, 4, 8, 16).Track();
            await source.ToHashSetAsync();
            Assert.Equal(5, source.MoveNextAsyncCount);
            Assert.Equal(4, source.CurrentCount);
            Assert.Equal(1, source.DisposeAsyncCount);
        }
    }
}
