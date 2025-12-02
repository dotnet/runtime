// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class WhereTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.Where<int>(null, i => i % 2 == 0));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.Where<int>(null, (i, index) => i % 2 == 0));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.Where<int>(null, async (i, ct) => i % 2 == 0));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.Where<int>(null, async (i, index, ct) => i % 2 == 0));

            AssertExtensions.Throws<ArgumentNullException>("predicate", () => AsyncEnumerable.Where(AsyncEnumerable.Empty<int>(), (Func<int, bool>)null));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => AsyncEnumerable.Where(AsyncEnumerable.Empty<int>(), (Func<int, int, bool>)null));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => AsyncEnumerable.Where(AsyncEnumerable.Empty<int>(), (Func<int, CancellationToken, ValueTask<bool>>)null));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => AsyncEnumerable.Where(AsyncEnumerable.Empty<int>(), (Func<int, int, CancellationToken, ValueTask<bool>>)null));
        }

        [Fact]
        public void Empty_ProducesEmpty() // validating an optimization / implementation detail
        {
            Assert.Same(AsyncEnumerable.Empty<int>(), AsyncEnumerable.Empty<int>().Where(i => true));
            Assert.Same(AsyncEnumerable.Empty<int>(), AsyncEnumerable.Empty<int>().Where((i, index) => true));
            Assert.Same(AsyncEnumerable.Empty<int>(), AsyncEnumerable.Empty<int>().Where(async (i, ct) => true));
            Assert.Same(AsyncEnumerable.Empty<int>(), AsyncEnumerable.Empty<int>().Where(async (i, index, ct) => true));
        }

        [Theory]
        [InlineData(new int[0])]
        [InlineData(new int[] { 42 })]
        [InlineData(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 })]
        [InlineData(new int[] { -1, 1, -2, 2, -10, 10 })]
        [InlineData(new int[] { int.MinValue, int.MaxValue })]
        public async Task VariousValues_MatchesEnumerable(int[] ints)
        {
            foreach (IAsyncEnumerable<int> source in CreateSources(ints))
            {
                await AssertEqual(
                    ints.Where(i => i % 2 == 0),
                    source.Where(i => i % 2 == 0));

                await AssertEqual(
                    ints.Where((i, index) => (i + index) % 2 == 0),
                    source.Where((i, index) => (i + index) % 2 == 0));

                await AssertEqual(
                    ints.Where(i => i % 2 == 0),
                    source.Where(async (int i, CancellationToken ct) => i % 2 == 0));

                await AssertEqual(
                    ints.Where((i, index) => (i + index) % 2 == 0),
                    source.Where(async (i, index, ct) => (i + index) % 2 == 0));
            }
        }

        [Fact]
        public async Task Cancellation_Cancels()
        {
            IAsyncEnumerable<int> source = CreateSource(1, 3, 5, 6, 7, 8);

            await Validate(source.Where(i => i % 2 == 0));
            await Validate(source.Where((i, index) => (i + index) % 2 == 0));
            await Validate(source.Where(async (int i, CancellationToken index) => i % 2 == 0));
            await Validate(source.Where(async (i, index, ct) => (i + index) % 2 == 0));

            static async Task Validate(IAsyncEnumerable<int> source)
            {
                CancellationTokenSource cts = new();
                await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                {
                    await foreach (int item in source.WithCancellation(cts.Token))
                    {
                        cts.Cancel();
                    }
                });
            }
        }

        [Fact]
        public async Task InterfaceCalls_ExpectedCounts()
        {
            await Validate(source => source.Where(i => i % 2 == 0));
            await Validate(source => source.Where((i, index) => (i + index) % 2 == 0));
            await Validate(source => source.Where(async (int i, CancellationToken cancellationToken) => i % 2 == 0));
            await Validate(source => source.Where(async (i, index, ct) => (i + index) % 2 == 0));

            async Task Validate(Func<IAsyncEnumerable<int>, IAsyncEnumerable<int>> factory)
            {
                TrackingAsyncEnumerable<int> source = CreateSource(1, 2, 3, 4).Track();
                await ConsumeAsync(factory(source));
                Assert.Equal(5, source.MoveNextAsyncCount);
                Assert.Equal(4, source.CurrentCount);
                Assert.Equal(1, source.DisposeAsyncCount);
            }
        }
    }
}
