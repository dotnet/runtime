// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class OrderByTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.Order((IAsyncEnumerable<int>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.OrderDescending((IAsyncEnumerable<int>)null));

            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.OrderBy((IAsyncEnumerable<int>)null, i => i));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.OrderBy((IAsyncEnumerable<int>)null, async (i, ct) => i));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.OrderByDescending((IAsyncEnumerable<int>)null, i => i));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.OrderByDescending((IAsyncEnumerable<int>)null, async (i, ct) => i));

            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => AsyncEnumerable.OrderBy(AsyncEnumerable.Empty<int>(), (Func<int, int>)null));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => AsyncEnumerable.OrderBy(AsyncEnumerable.Empty<int>(), (Func<int, CancellationToken, ValueTask<int>>)null));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => AsyncEnumerable.OrderByDescending(AsyncEnumerable.Empty<int>(), (Func<int, int>)null));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => AsyncEnumerable.OrderByDescending(AsyncEnumerable.Empty<int>(), (Func<int, CancellationToken, ValueTask<int>>)null));

            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.ThenBy((IOrderedAsyncEnumerable<int>)null, i => i));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.ThenBy((IOrderedAsyncEnumerable<int>)null, async (i, ct) => i));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.ThenByDescending((IOrderedAsyncEnumerable<int>)null, i => i));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.ThenByDescending((IOrderedAsyncEnumerable<int>)null, async (i, ct) => i));

            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => AsyncEnumerable.ThenBy(AsyncEnumerable.Empty<int>().Order(), (Func<int, int>)null));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => AsyncEnumerable.ThenBy(AsyncEnumerable.Empty<int>().Order(), (Func<int, CancellationToken, ValueTask<int>>)null));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => AsyncEnumerable.ThenByDescending(AsyncEnumerable.Empty<int>().Order(), (Func<int, int>)null));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => AsyncEnumerable.ThenByDescending(AsyncEnumerable.Empty<int>().Order(), (Func<int, CancellationToken, ValueTask<int>>)null));
        }

        [Fact]
        public void Empty_ProducesEmpty() // validating an optimization / implementation detail
        {
            Assert.Same(AsyncEnumerable.Empty<string>(), AsyncEnumerable.Empty<string>().OrderBy(i => i));
            Assert.Same(AsyncEnumerable.Empty<string>(), AsyncEnumerable.Empty<string>().OrderBy(async (i, ct) => i));

            Assert.Same(AsyncEnumerable.Empty<string>(), AsyncEnumerable.Empty<string>().OrderByDescending(i => i));
            Assert.Same(AsyncEnumerable.Empty<string>(), AsyncEnumerable.Empty<string>().OrderByDescending(async (i, ct) => i));

            Assert.Same(AsyncEnumerable.Empty<string>(), AsyncEnumerable.Empty<string>().OrderBy(i => i).ThenBy(i => i));
            Assert.Same(AsyncEnumerable.Empty<string>(), AsyncEnumerable.Empty<string>().OrderBy(async (i, ct) => i).ThenBy(async (i, ct) => i));

            Assert.Same(AsyncEnumerable.Empty<string>(), AsyncEnumerable.Empty<string>().OrderByDescending(i => i).ThenByDescending(i => i));
            Assert.Same(AsyncEnumerable.Empty<string>(), AsyncEnumerable.Empty<string>().OrderByDescending(async (i, ct) => i).ThenByDescending(async (i, ct) => i));
        }

        [Fact]
        public async Task VariousValues_MatchesEnumerable_Int32()
        {
            Random rand = new(42);
            foreach (int length in new[] { 0, 1, 2, 3, 4, 100, 1024 })
            {
                foreach (IComparer<int> comparer in new[] { null, Comparer<int>.Default, Comparer<int>.Create((x, y) => y.CompareTo(x)) })
                {
                    int[] ints = new int[length];
                    FillRandom(rand, ints);
                    int[] copy = ints.ToArray();

                    foreach (IAsyncEnumerable<int> source in CreateSources(ints))
                    {
#if NET
                        await AssertEqual(
                            ints.Order(comparer),
                            source.Order(comparer));

                        await AssertEqual(
                            ints.OrderDescending(comparer),
                            source.OrderDescending(comparer));
#endif

                        await AssertEqual(
                            ints.OrderBy(i => i % 2 == 0 ? i : -1, comparer),
                            source.OrderBy(i => i % 2 == 0 ? i : -1, comparer));

                        await AssertEqual(
                            ints.OrderBy(i => i % 2 == 0 ? i : -1, comparer),
                            source.OrderBy(async (i, ct) => i % 2 == 0 ? i : -1, comparer));

                        await AssertEqual(
                            ints.OrderByDescending(i => i % 2 == 0 ? i : -1, comparer),
                            source.OrderByDescending(i => i % 2 == 0 ? i : -1, comparer));

                        await AssertEqual(
                            ints.OrderByDescending(i => i % 2 == 0 ? i : -1, comparer),
                            source.OrderByDescending(async (i, ct) => i % 2 == 0 ? i : -1, comparer));

                        Assert.Equal(copy, ints);
                    }
                }
            }
        }

        [Fact]
        public async Task VariousValues_MatchesEnumerable_String()
        {
            Random rand = new(42);
            foreach (int length in new[] { 0, 1, 2, 3, 4, 100, 1024 })
            {
                foreach (IComparer<string> comparer in new IComparer<string>[] { null, Comparer<string>.Default, StringComparer.Ordinal, StringComparer.OrdinalIgnoreCase })
                {
                    string[] strings = new string[length];
                    FillRandom(rand, strings);

                    string[] copy = strings.ToArray();

                    foreach (IAsyncEnumerable<string> source in CreateSources(strings))
                    {
#if NET
                        await AssertEqual(
                            strings.Order(comparer),
                            source.Order(comparer));

                        await AssertEqual(
                            strings.OrderDescending(comparer),
                            source.OrderDescending(comparer));
#endif

                        await AssertEqual(
                            strings.OrderBy(s => s.Length),
                            source.OrderBy(s => s.Length));

                        await AssertEqual(
                            strings.OrderBy(s => s.Length),
                            source.OrderBy(async (s, ct) => s.Length));

                        await AssertEqual(
                            strings.OrderByDescending(s => s.Length),
                            source.OrderByDescending(s => s.Length));

                        await AssertEqual(
                            strings.OrderByDescending(s => s.Length),
                            source.OrderByDescending(async (s, ct) => s.Length));

                        await AssertEqual(
                            strings.OrderBy(s => s.Length).ThenBy(s => s.Length > 0 ? s[0] : ' '),
                            source.OrderBy(s => s.Length).ThenBy(s => s.Length > 0 ? s[0] : ' '));

                        await AssertEqual(
                            strings.OrderBy(s => s.Length).ThenBy(s => s.Length > 0 ? s[0] : ' '),
                            source.OrderBy(async (s, ct) => s.Length).ThenBy(async (s, ct) => s.Length > 0 ? s[0] : ' '));

                        await AssertEqual(
                            strings.OrderByDescending(s => s.Length).ThenByDescending(s => s.Length > 0 ? s[0] : ' '),
                            source.OrderByDescending(s => s.Length).ThenByDescending(s => s.Length > 0 ? s[0] : ' '));

                        await AssertEqual(
                            strings.OrderByDescending(s => s.Length).ThenByDescending(s => s.Length > 0 ? s[0] : ' '),
                            source.OrderByDescending(async (s, ct) => s.Length).ThenByDescending(async (s, ct) => s.Length > 0 ? s[0] : ' '));

                        Assert.Equal(copy, strings);
                    }
                }
            }
        }

        [Fact]
        public async Task Cancellation_Cancels()
        {
            IAsyncEnumerable<int> source = CreateSource(2, 4, 8, 16);

            await Validate(source.OrderBy(i => i));
            await Validate(source.OrderBy(async (i, ct) => i));
            await Validate(source.OrderByDescending(i => i));
            await Validate(source.OrderByDescending(async (i, ct) => i));

            static async Task Validate(IAsyncEnumerable<int> source)
            {
                await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                {
                    await ConsumeAsync(source.WithCancellation(new CancellationToken(true)));
                });
            }
        }

        [Fact]
        public async Task InterfaceCalls_ExpectedCounts()
        {
            await Validate(source => source.OrderBy(i => i));
            await Validate(source => source.OrderBy(async (i, ct) => i));
            await Validate(source => source.OrderByDescending(i => i));
            await Validate(source => source.OrderByDescending(async (i, ct) => i));

            async Task Validate(Func<IAsyncEnumerable<int>, IAsyncEnumerable<int>> factory)
            {
                TrackingAsyncEnumerable<int> source = CreateSource(Enumerable.Range(0, 100).ToArray()).Track();
                await ConsumeAsync(factory(source));
                Assert.Equal(101, source.MoveNextAsyncCount);
                Assert.Equal(100, source.CurrentCount);
                Assert.Equal(1, source.DisposeAsyncCount);
            }
        }
    }
}
