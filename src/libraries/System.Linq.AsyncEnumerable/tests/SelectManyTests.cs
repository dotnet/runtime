// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class SelectManyTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.SelectMany<int, string>(null, i => Enumerable.Empty<string>()));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.SelectMany<int, string>(null, async (i, ct) => Enumerable.Empty<string>()));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.SelectMany<int, string>(null, i => AsyncEnumerable.Empty<string>()));

            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.SelectMany<int, string>(null, (i, index) => Enumerable.Empty<string>()));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.SelectMany<int, string>(null, async (i, index, ct) => Enumerable.Empty<string>()));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.SelectMany<int, string>(null, (i, index) => AsyncEnumerable.Empty<string>()));

            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.SelectMany<int, string, string>(null, i => Enumerable.Empty<string>(), (i, s) => s));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.SelectMany<int, string, string>(null, i => AsyncEnumerable.Empty<string>(), (i, s) => s));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.SelectMany<int, string, string>(null, async (i, ct) => Enumerable.Empty<string>(), async (i, s, ct) => s));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.SelectMany<int, string, string>(null, i => AsyncEnumerable.Empty<string>(), async (i, s, ct) => s));

            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.SelectMany<int, string, string>(null, (i, index) => Enumerable.Empty<string>(), (i, s) => s));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.SelectMany<int, string, string>(null, async (i, index, ct) => Enumerable.Empty<string>(), async (i, s, ct) => s));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.SelectMany<int, string, string>(null, (i, index) => AsyncEnumerable.Empty<string>(), async (i, s, ct) => s));

            AssertExtensions.Throws<ArgumentNullException>("selector", () => AsyncEnumerable.SelectMany(AsyncEnumerable.Empty<int>(), (Func<int, IEnumerable<string>>)null));
            AssertExtensions.Throws<ArgumentNullException>("selector", () => AsyncEnumerable.SelectMany(AsyncEnumerable.Empty<int>(), (Func<int, CancellationToken, ValueTask<IEnumerable<string>>>)null));
            AssertExtensions.Throws<ArgumentNullException>("selector", () => AsyncEnumerable.SelectMany(AsyncEnumerable.Empty<int>(), (Func<int, IAsyncEnumerable<string>>)null));

            AssertExtensions.Throws<ArgumentNullException>("selector", () => AsyncEnumerable.SelectMany(AsyncEnumerable.Empty<int>(), (Func<int, int, IEnumerable<string>>)null));
            AssertExtensions.Throws<ArgumentNullException>("selector", () => AsyncEnumerable.SelectMany(AsyncEnumerable.Empty<int>(), (Func<int, int, CancellationToken, ValueTask<IEnumerable<string>>>)null));
            AssertExtensions.Throws<ArgumentNullException>("selector", () => AsyncEnumerable.SelectMany(AsyncEnumerable.Empty<int>(), (Func<int, int, IAsyncEnumerable<string>>)null));

            AssertExtensions.Throws<ArgumentNullException>("collectionSelector", () => AsyncEnumerable.SelectMany(AsyncEnumerable.Empty<int>(), (Func<int, IEnumerable<string>>)null, (i, s) => s));
            AssertExtensions.Throws<ArgumentNullException>("collectionSelector", () => AsyncEnumerable.SelectMany(AsyncEnumerable.Empty<int>(), (Func<int, IAsyncEnumerable<string>>)null, (i, s) => s));
            AssertExtensions.Throws<ArgumentNullException>("collectionSelector", () => AsyncEnumerable.SelectMany(AsyncEnumerable.Empty<int>(), (Func<int, CancellationToken, ValueTask<IEnumerable<string>>>)null, async (i, s, ct) => s));
            AssertExtensions.Throws<ArgumentNullException>("collectionSelector", () => AsyncEnumerable.SelectMany(AsyncEnumerable.Empty<int>(), (Func<int, IAsyncEnumerable<string>>)null, async (i, s, ct) => s));

            AssertExtensions.Throws<ArgumentNullException>("collectionSelector", () => AsyncEnumerable.SelectMany(AsyncEnumerable.Empty<int>(), (Func<int, int, IEnumerable<string>>)null, (i, s) => s));
            AssertExtensions.Throws<ArgumentNullException>("collectionSelector", () => AsyncEnumerable.SelectMany(AsyncEnumerable.Empty<int>(), (Func<int, int, CancellationToken, ValueTask<IEnumerable<string>>>)null, async (i, s, ct) => s));
            AssertExtensions.Throws<ArgumentNullException>("collectionSelector", () => AsyncEnumerable.SelectMany(AsyncEnumerable.Empty<int>(), (Func<int, int, IAsyncEnumerable<string>>)null, async (i, s, ct) => s));

            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => AsyncEnumerable.SelectMany(AsyncEnumerable.Empty<int>(), i => Enumerable.Empty<string>(), (Func<int, string, string>)null));
            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => AsyncEnumerable.SelectMany(AsyncEnumerable.Empty<int>(), i => AsyncEnumerable.Empty<string>(), (Func<int, string, string>)null));
            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => AsyncEnumerable.SelectMany(AsyncEnumerable.Empty<int>(), async (i, ct) => Enumerable.Empty<string>(), (Func<int, string, CancellationToken, ValueTask<string>>)null));
            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => AsyncEnumerable.SelectMany(AsyncEnumerable.Empty<int>(), i => AsyncEnumerable.Empty<string>(), (Func<int, string, CancellationToken, ValueTask<string>>)null));

            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => AsyncEnumerable.SelectMany(AsyncEnumerable.Empty<int>(), (i, index) => Enumerable.Empty<string>(), (Func<int, string, string>)null));
            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => AsyncEnumerable.SelectMany(AsyncEnumerable.Empty<int>(), async (i, index, ct) => Enumerable.Empty<string>(), (Func<int, string, CancellationToken, ValueTask<string>>)null));
            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => AsyncEnumerable.SelectMany(AsyncEnumerable.Empty<int>(), (i, index) => AsyncEnumerable.Empty<string>(), (Func<int, string, CancellationToken, ValueTask<string>>)null));
        }

        [Fact]
        public void Empty_ProducesEmpty() // validating an optimization / implementation detail
        {
            Assert.Same(AsyncEnumerable.Empty<char>(), AsyncEnumerable.Empty<string>().SelectMany(s => s.ToCharArray()));
            Assert.Same(AsyncEnumerable.Empty<char>(), AsyncEnumerable.Empty<string>().SelectMany(async (s, ct) => (IEnumerable<char>)s.ToCharArray()));
            Assert.Same(AsyncEnumerable.Empty<char>(), AsyncEnumerable.Empty<string>().SelectMany(s => s.ToAsyncEnumerable()));

            Assert.Same(AsyncEnumerable.Empty<char>(), AsyncEnumerable.Empty<string>().SelectMany((s, i) => s.ToCharArray()));
            Assert.Same(AsyncEnumerable.Empty<char>(), AsyncEnumerable.Empty<string>().SelectMany(async (s, i, ct) => (IEnumerable<char>)s.ToCharArray()));
            Assert.Same(AsyncEnumerable.Empty<char>(), AsyncEnumerable.Empty<string>().SelectMany((s, i) => s.ToAsyncEnumerable()));

            Assert.Same(AsyncEnumerable.Empty<string>(), AsyncEnumerable.Empty<string>().SelectMany(s => s.ToCharArray(), (s, c) => s));
            Assert.Same(AsyncEnumerable.Empty<string>(), AsyncEnumerable.Empty<string>().SelectMany(async (s, ct) => (IEnumerable<char>)s.ToCharArray(), async (s, c, ct) => s));
            Assert.Same(AsyncEnumerable.Empty<string>(), AsyncEnumerable.Empty<string>().SelectMany(s => s.ToAsyncEnumerable(), (s, c) => s));
            Assert.Same(AsyncEnumerable.Empty<string>(), AsyncEnumerable.Empty<string>().SelectMany(s => s.ToAsyncEnumerable(), async (s, c, ct) => s));

            Assert.Same(AsyncEnumerable.Empty<string>(), AsyncEnumerable.Empty<string>().SelectMany((s, i) => s.ToCharArray(), (s, c) => s));
            Assert.Same(AsyncEnumerable.Empty<string>(), AsyncEnumerable.Empty<string>().SelectMany(async (s, i, ct) => (IEnumerable<char>)s.ToCharArray(), async (s, c, ct) => s));
            Assert.Same(AsyncEnumerable.Empty<string>(), AsyncEnumerable.Empty<string>().SelectMany((s, i) => s.ToAsyncEnumerable(), async (s, c, ct) => s));
        }

        [Fact]
        public async Task VariousValues_MatchesEnumerable()
        {
            Random rand = new(42);
            foreach (int collectionSize in new[] { 0, 1, 10, 50 })
            {
                foreach (int chunkSize in new[] { 1, 2, 3, 5, 60 })
                {
                    int[] ints = new int[collectionSize];
                    FillRandom(rand, ints);

                    foreach (IAsyncEnumerable<int> source in CreateSources(ints))
                    {
                        Func<int, IEnumerable<int>>[] selectors =
                        [
                            i => Array.Empty<int>(),
                            i => [i],
                            i => [i, i * 2],
                        ];

                        foreach (Func<int, IEnumerable<int>> selector in selectors)
                        {
                            await AssertEqual(
                                ints.SelectMany(i => selector(i)),
                                source.SelectMany(i => selector(i)));

                            await AssertEqual(
                                ints.SelectMany(i => selector(i)),
                                source.SelectMany(async (i, ct) => selector(i)));

                            await AssertEqual(
                                ints.SelectMany(i => selector(i)),
                                source.SelectMany(i => selector(i).ToAsyncEnumerable()));

                            await AssertEqual(
                                ints.SelectMany((i, index) => selector(i * index)),
                                source.SelectMany((i, index) => selector(i * index)));

                            await AssertEqual(
                                ints.SelectMany((i, index) => selector(i * index)),
                                source.SelectMany(async (i, index, ct) => selector(i * index)));

                            await AssertEqual(
                                ints.SelectMany((i, index) => selector(i * index)),
                                source.SelectMany((i, index) => selector(i * index).ToAsyncEnumerable()));

                            await AssertEqual(
                                ints.SelectMany(i => selector(i), (i, result) => result),
                                source.SelectMany(i => selector(i), (i, result) => result));

                            await AssertEqual(
                                ints.SelectMany(i => selector(i), (i, result) => result),
                                source.SelectMany(async (i, ct) => selector(i), async (i, result, ct) => result));

                            await AssertEqual(
                                ints.SelectMany(i => selector(i), (i, result) => result),
                                source.SelectMany(i => selector(i).ToAsyncEnumerable(), (i, result) => result));

                            await AssertEqual(
                                ints.SelectMany(i => selector(i), (i, result) => result),
                                source.SelectMany(i => selector(i).ToAsyncEnumerable(), async (i, result, ct) => result));

                            await AssertEqual(
                                ints.SelectMany((i, index) => selector(i * index), (i, result) => result),
                                source.SelectMany((i, index) => selector(i * index), (i, result) => result));

                            await AssertEqual(
                                ints.SelectMany((i, index) => selector(i * index), (i, result) => result),
                                source.SelectMany(async (i, index, ct) => selector(i * index), async (i, result, ct) => result));

                            await AssertEqual(
                                ints.SelectMany((i, index) => selector(i * index), (i, result) => result),
                                source.SelectMany((i, index) => selector(i * index).ToAsyncEnumerable(), async (i, result, ct) => result));
                        }
                    }
                }
            }
        }

        [Fact]
        public async Task Cancellation_Cancels()
        {
            IAsyncEnumerable<int> source = CreateSource(2, 4, 8, 16);

            await Validate(source.SelectMany(i => new[] { i }));
            await Validate(source.SelectMany<int, int>(async (i, ct) => new[] { i }));
            await Validate(source.SelectMany(i => new[] { i }.ToAsyncEnumerable()));

            await Validate(source.SelectMany((i, index) => new[] { i }));
            await Validate(source.SelectMany<int, int>(async (i, index, ct) => new[] { i }));
            await Validate(source.SelectMany((i, index) => new[] { i }.ToAsyncEnumerable()));

            await Validate(source.SelectMany(i => new[] { i }, (i, result) => result));
            await Validate(source.SelectMany<int, int, int>(async (i, ct) => new[] { i }, async (i, result, ct) => result));
            await Validate(source.SelectMany(i => new[] { i }.ToAsyncEnumerable(), (i, result) => result));
            await Validate(source.SelectMany(i => new[] { i }.ToAsyncEnumerable(), async (i, result, ct) => result));

            await Validate(source.SelectMany((i, index) => new[] { i }));
            await Validate(source.SelectMany<int, int, int>(async (i, index, ct) => new[] { i }, async (i, result, ct) => result));
            await Validate(source.SelectMany((i, index) => new[] { i }.ToAsyncEnumerable(), async (i, result, ct) => result));

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
            await Validate(source => source.SelectMany(i => new[] { i, i + 1, i * 2 }));
            await Validate(source => source.SelectMany<int, int>(async (i, ct) => new[] { i, i + 1, i * 2 }));
            await Validate(source => source.SelectMany(i => new[] { i, i + 1, i * 2 }.ToAsyncEnumerable()));
            await Validate(source => source.SelectMany((i, index) => new[] { i, i + 1, i * 2 }));
            await Validate(source => source.SelectMany<int, int>(async (i, index, ct) => new[] { i, i + 1, i * 2 }));
            await Validate(source => source.SelectMany((i, index) => new[] { i, i + 1, i * 2 }.ToAsyncEnumerable()));
            await Validate(source => source.SelectMany(i => new[] { i, i + 1, i * 2 }, (i, result) => result));
            await Validate(source => source.SelectMany<int, int, int>(async (i, ct) => new[] { i, i + 1, i * 2 }, async (i, result, ct) => result));
            await Validate(source => source.SelectMany(i => new[] { i, i + 1, i * 2 }.ToAsyncEnumerable(), (i, result) => result));
            await Validate(source => source.SelectMany(i => new[] { i, i + 1, i * 2 }.ToAsyncEnumerable(), async (i, result, ct) => result));
            await Validate(source => source.SelectMany((i, index) => new[] { i, i + 1, i * 2 }, (i, result) => result));
            await Validate(source => source.SelectMany<int, int, int>(async (i, index, ct) => new[] { i, i + 1, i * 2 }, async (i, result, ct) => result));
            await Validate(source => source.SelectMany((i, index) => new[] { i, i + 1, i * 2 }.ToAsyncEnumerable(), async (i, result, ct) => result));

            async static Task Validate(Func<IAsyncEnumerable<int>, IAsyncEnumerable<int>> factory)
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
