// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class ExceptByTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("first", () => AsyncEnumerable.ExceptBy((IAsyncEnumerable<int>)null, AsyncEnumerable.Empty<string>(), x => x.ToString()));
            AssertExtensions.Throws<ArgumentNullException>("second", () => AsyncEnumerable.ExceptBy(AsyncEnumerable.Empty<string>(), null, x => x.Length));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => AsyncEnumerable.ExceptBy(AsyncEnumerable.Empty<string>(), AsyncEnumerable.Empty<int>(), (Func<string, int>)null));

            AssertExtensions.Throws<ArgumentNullException>("first", () => AsyncEnumerable.ExceptBy((IAsyncEnumerable<int>)null, AsyncEnumerable.Empty<string>(), async (x, ct) => x.ToString()));
            AssertExtensions.Throws<ArgumentNullException>("second", () => AsyncEnumerable.ExceptBy(AsyncEnumerable.Empty<string>(), null, async (x, ct) => x.Length));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => AsyncEnumerable.ExceptBy(AsyncEnumerable.Empty<string>(), AsyncEnumerable.Empty<int>(), (Func<string, CancellationToken, ValueTask<int>>)null));
        }

        [Fact]
        public void Empty_ProducesEmpty() // validating an optimization / implementation detail
        {
            Assert.Same(AsyncEnumerable.Empty<int>(), AsyncEnumerable.Empty<int>().ExceptBy(CreateSource(1, 2, 3), i => i));
            Assert.Same(AsyncEnumerable.Empty<int>(), AsyncEnumerable.Empty<int>().ExceptBy(CreateSource(1, 2, 3), async (i, ct) => i));
        }

#if NET
        [Theory]
        [InlineData(new int[0], new int[0])]
        [InlineData(new int[0], new int[] { 42 })]
        [InlineData(new int[] { 42, 43 }, new int[0])]
        [InlineData(new int[] { 1 }, new int[] { 2, 3 })]
        [InlineData(new int[] { 2, 4, 8 }, new int[] { 3, 5 })]
        [InlineData(new int[] { 2, 4, 8 }, new int[] { 2, 4, 8 })]
        [InlineData(new int[] { 2, 4, 8 }, new int[] { 2, 5, 8 })]
        [InlineData(new int[] { -1, 2, 5, 6, 7, 8 }, new int[] { int.MinValue, int.MaxValue })]
        public async Task VariousValues_MatchesEnumerable(int[] firstInts, int[] second)
        {
            string[] first = firstInts.Select(x => x.ToString()).ToArray();

            foreach (IAsyncEnumerable<string> firstSource in CreateSources(first))
            {
                foreach (IAsyncEnumerable<int> secondSource in CreateSources(second))
                {
                    await AssertEqual(
                        first.ExceptBy(second, int.Parse),
                        firstSource.ExceptBy(secondSource, int.Parse));
                    await AssertEqual(
                        first.ExceptBy(second, int.Parse, OddEvenComparer),
                        firstSource.ExceptBy(secondSource, int.Parse, OddEvenComparer));

                    await AssertEqual(
                        first.ExceptBy(second, int.Parse),
                        firstSource.ExceptBy(secondSource, async (x, ct) => int.Parse(x)));

                    await AssertEqual(
                        first.ExceptBy(second, int.Parse, OddEvenComparer),
                        firstSource.ExceptBy(secondSource, async (x, ct) => int.Parse(x), OddEvenComparer));
                }
            }
        }
#endif

        [Fact]
        public async Task Cancellation_Cancels()
        {
            IAsyncEnumerable<int> first = CreateSource(2, 4, 8, 16);
            IAsyncEnumerable<int> second = CreateSource(1, 3, 5);
            CancellationTokenSource cts;

            cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await foreach (int item in first.ExceptBy(second, x => x).WithCancellation(cts.Token))
                {
                    cts.Cancel();
                }
            });

            cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await ConsumeAsync(first.ExceptBy(second, x =>
                {
                    cts.Cancel();
                    return x;
                }).WithCancellation(cts.Token));
            });

            cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await ConsumeAsync(first.ExceptBy(second, async (x, ct) =>
                {
                    Assert.Equal(cts.Token, ct);
                    await Task.Yield();
                    cts.Cancel();
                    return x;
                }).WithCancellation(cts.Token));
            });
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task InterfaceCalls_ExpectedCounts(bool useAsync)
        {
            TrackingAsyncEnumerable<int> first = CreateSource(2, 4, 8, 16, 32, 64).Track();
            TrackingAsyncEnumerable<int> second = CreateSource(1, 3, 5).Track();
            int funcCount = 0;
            await ConsumeAsync(useAsync ?
                first.ExceptBy(second, async (x, ct) =>
                {
                    funcCount++;
                    return x;
                }) :
                first.ExceptBy(second, x =>
                {
                    funcCount++;
                    return x;
                }));
            Assert.Equal(7, first.MoveNextAsyncCount);
            Assert.Equal(6, first.CurrentCount);
            Assert.Equal(1, first.DisposeAsyncCount);
            Assert.Equal(4, second.MoveNextAsyncCount);
            Assert.Equal(3, second.CurrentCount);
            Assert.Equal(1, second.DisposeAsyncCount);
            Assert.Equal(6, funcCount);
        }
    }
}
