// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class SelectTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.Select<int, string>(null, i => i.ToString()));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.Select<int, string>(null, (i, index) => i.ToString()));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.Select<int, string>(null, async (i, ct) => i.ToString()));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.Select<int, string>(null, async (i, index, ct) => i.ToString()));

            AssertExtensions.Throws<ArgumentNullException>("selector", () => AsyncEnumerable.Select(AsyncEnumerable.Empty<int>(), (Func<int, string>)null));
            AssertExtensions.Throws<ArgumentNullException>("selector", () => AsyncEnumerable.Select(AsyncEnumerable.Empty<int>(), (Func<int, int, string>)null));
            AssertExtensions.Throws<ArgumentNullException>("selector", () => AsyncEnumerable.Select(AsyncEnumerable.Empty<int>(), (Func<int, CancellationToken, ValueTask<string>>)null));
            AssertExtensions.Throws<ArgumentNullException>("selector", () => AsyncEnumerable.Select(AsyncEnumerable.Empty<int>(), (Func<int, int, CancellationToken, ValueTask<string>>)null));
        }

        [Fact]
        public void Empty_ProducesEmpty() // validating an optimization / implementation detail
        {
            Assert.Same(AsyncEnumerable.Empty<string>(), AsyncEnumerable.Empty<string>().Select(s => s));
            Assert.Same(AsyncEnumerable.Empty<string>(), AsyncEnumerable.Empty<string>().Select((s, index) => s));
            Assert.Same(AsyncEnumerable.Empty<string>(), AsyncEnumerable.Empty<string>().Select(async (string s, CancellationToken ct) => s));
            Assert.Same(AsyncEnumerable.Empty<string>(), AsyncEnumerable.Empty<string>().Select(async (string s, int index, CancellationToken ct) => s));
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
                    ints.Select(i => i.ToString()),
                    source.Select(i => i.ToString()));

                await AssertEqual(
                    ints.Select((i, index) => (i + index).ToString()),
                    source.Select((i, index) => (i + index).ToString()));

                await AssertEqual(
                    ints.Select(i => i.ToString()),
                    source.Select(async (int i, CancellationToken ct) => i.ToString()));

                await AssertEqual(
                    ints.Select((i, index) => (i + index).ToString()),
                    source.Select(async (i, index, ct) => (i + index).ToString()));
            }
        }

        [Fact]
        public async Task Cancellation_Cancels()
        {
            IAsyncEnumerable<int> source = CreateSource(2, 4, 8, 16);

            await Validate(source.Select(i => i));
            await Validate(source.Select((i, index) => i));
            await Validate(source.Select(async (int i, CancellationToken index) => i));
            await Validate(source.Select(async (i, index, ct) => i));

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
            await Validate(source => source.Select(i => i));
            await Validate(source => source.Select((i, index) => i));
            await Validate(source => source.Select(async (int i, CancellationToken cancellationToken) => i));
            await Validate(source => source.Select(async (i, index, ct) => i));

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
