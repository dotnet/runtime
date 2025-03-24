// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class ElementAtOrDefaultAsyncTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.ElementAtOrDefaultAsync((IAsyncEnumerable<int>)null, 0));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.ElementAtOrDefaultAsync((IAsyncEnumerable<int>)null, new Index(0)));
        }

        [Fact]
        public async Task OutOfRange_ReturnsDefault()
        {
            foreach (int length in new[] { 0, 1, 2, 10 })
            {
                int[] values = Enumerable.Range(42, length).ToArray();

                Assert.Equal(0, await values.ToAsyncEnumerable().ElementAtOrDefaultAsync(-1));
                Assert.Equal(0, await values.ToAsyncEnumerable().ElementAtOrDefaultAsync(length));
                Assert.Equal(0, await values.ToAsyncEnumerable().ElementAtOrDefaultAsync(new Index(length)));
                Assert.Equal(0, await values.ToAsyncEnumerable().ElementAtOrDefaultAsync(new Index(100, fromEnd: true)));

                Assert.Null(await values.Select(i => (int?)i).ToAsyncEnumerable().ElementAtOrDefaultAsync(-1));
                Assert.Null(await values.Select(i => (int?)i).ToAsyncEnumerable().ElementAtOrDefaultAsync(length));
                Assert.Null(await values.Select(i => (int?)i).ToAsyncEnumerable().ElementAtOrDefaultAsync(new Index(length)));
                Assert.Null(await values.Select(i => (int?)i).ToAsyncEnumerable().ElementAtOrDefaultAsync(new Index(100, fromEnd: true)));

                Assert.Null(await values.Select(i => i.ToString()).ToAsyncEnumerable().ElementAtOrDefaultAsync(-1));
                Assert.Null(await values.Select(i => i.ToString()).ToAsyncEnumerable().ElementAtOrDefaultAsync(length));
                Assert.Null(await values.Select(i => i.ToString()).ToAsyncEnumerable().ElementAtOrDefaultAsync(new Index(length)));
                Assert.Null(await values.Select(i => i.ToString()).ToAsyncEnumerable().ElementAtOrDefaultAsync(new Index(100, fromEnd: true)));
            }
        }

        [Theory]
        [InlineData(new int[] { 1 })]
        [InlineData(new int[] { 2, 4, 8 })]
        [InlineData(new int[] { -1, 2, 5, 6, 7, 8 })]
        [InlineData(new int[] { 1, 3, 5, 7 })]
        public async Task VariousValues_MatchesEnumerable(int[] values)
        {
            foreach (IAsyncEnumerable<int> source in CreateSources(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    Assert.Equal(
                        values.ElementAtOrDefault(i),
                        await source.ElementAtOrDefaultAsync(i));

#if NET
                    Assert.Equal(
                        values.ElementAtOrDefault(new Index(i)),
                        await source.ElementAtOrDefaultAsync(new Index(i)));

                    Assert.Equal(
                        values.ElementAtOrDefault(new Index(values.Length - i, fromEnd: true)),
                        await source.ElementAtOrDefaultAsync(new Index(values.Length - i, fromEnd: true)));
#endif
                }
            }
        }

        [Fact]
        public async Task Cancellation_Cancels()
        {
            IAsyncEnumerable<int> source = CreateSource(2, 4, 8, 16);

            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.ElementAtOrDefaultAsync(1, new CancellationToken(true)));
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.ElementAtOrDefaultAsync(new Index(1), new CancellationToken(true)));
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.ElementAtOrDefaultAsync(new Index(1, fromEnd: true), new CancellationToken(true)));
        }

        [Fact]
        public async Task InterfaceCalls_ExpectedCounts()
        {
            TrackingAsyncEnumerable<int> source;

            source = CreateSource(2, 4, 8, 16).Track();
            await source.ElementAtOrDefaultAsync(0);
            Assert.Equal(1, source.MoveNextAsyncCount);
            Assert.Equal(1, source.CurrentCount);
            Assert.Equal(1, source.DisposeAsyncCount);

            source = CreateSource(2, 4, 8, 16).Track();
            await source.ElementAtOrDefaultAsync(3);
            Assert.Equal(4, source.MoveNextAsyncCount);
            Assert.Equal(1, source.CurrentCount);
            Assert.Equal(1, source.DisposeAsyncCount);

            source = CreateSource(2, 4, 8, 16).Track();
            await source.ElementAtOrDefaultAsync(new Index(0));
            Assert.Equal(1, source.MoveNextAsyncCount);
            Assert.Equal(1, source.CurrentCount);
            Assert.Equal(1, source.DisposeAsyncCount);

            source = CreateSource(2, 4, 8, 16).Track();
            await source.ElementAtOrDefaultAsync(new Index(3));
            Assert.Equal(4, source.MoveNextAsyncCount);
            Assert.Equal(1, source.CurrentCount);
            Assert.Equal(1, source.DisposeAsyncCount);

            source = CreateSource(2, 4, 8, 16).Track();
            await source.ElementAtOrDefaultAsync(new Index(1, fromEnd: true));
            Assert.Equal(5, source.MoveNextAsyncCount);
            Assert.Equal(4, source.CurrentCount);
            Assert.Equal(1, source.DisposeAsyncCount);
        }
    }
}
