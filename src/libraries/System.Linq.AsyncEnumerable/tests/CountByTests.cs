// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class CountByTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.CountBy((IAsyncEnumerable<string>)null, x => x.Length));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => AsyncEnumerable.CountBy(AsyncEnumerable.Empty<string>(), (Func<string, int>)null));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => AsyncEnumerable.CountBy(AsyncEnumerable.Empty<string>(), (Func<string, CancellationToken, ValueTask<int>>)null));
        }

        [Fact]
        public void Empty_ProducesEmpty() // validating an optimization / implementation detail
        {
            Assert.Same(AsyncEnumerable.Empty<KeyValuePair<object, int>>(), AsyncEnumerable.Empty<object>().CountBy(i => i));
            Assert.Same(AsyncEnumerable.Empty<KeyValuePair<object, int>>(), AsyncEnumerable.Empty<object>().CountBy(async (i, ct) => i));
        }

#if NET
        [Fact]
        public async Task VariousValues_MatchesEnumerable_Strings()
        {
            Random rand = new(42);
            foreach (int length in new[] { 0, 1, 2, 1000 })
            {
                string[] values = new string[length];
                FillRandom(rand, values);

                foreach (IAsyncEnumerable<string> source in CreateSources(values))
                {
                    await AssertEqual(
                        values.CountBy(x => x.Length),
                        source.CountBy(x => x.Length));

                    await AssertEqual(
                        values.CountBy(x => x.Length),
                        source.CountBy(async (x, ct) => x.Length));

                    await AssertEqual(
                        values.CountBy(x => x.Length, OddEvenComparer),
                        source.CountBy(x => x.Length, OddEvenComparer));

                    await AssertEqual(
                        values.CountBy(x => x.Length, OddEvenComparer),
                        source.CountBy(async (x, ct) => x.Length, OddEvenComparer));
                }
            }
        }
#endif

        [Fact]
        public async Task Cancellation_Cancels()
        {
            IAsyncEnumerable<int> source = CreateSource(2, 4, 8, 16);
            CancellationTokenSource cts;

            cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await ConsumeAsync(source.CountBy(x =>
                {
                    cts.Cancel();
                    return x;
                }).WithCancellation(cts.Token));
            });

            cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await ConsumeAsync(source.CountBy(async (x, ct) =>
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
            TrackingAsyncEnumerable<int> source = CreateSource(2, 4, 8, 16, 2, 7, 8).Track();
            await ConsumeAsync(useAsync ? source.CountBy(x => x) : source.CountBy(async (x, ct) => x));
            Assert.Equal(8, source.MoveNextAsyncCount);
            Assert.Equal(7, source.CurrentCount);
            Assert.Equal(1, source.DisposeAsyncCount);
        }
    }
}
