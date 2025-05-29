// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class GroupByTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.GroupBy((IAsyncEnumerable<string>)null, s => s));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.GroupBy((IAsyncEnumerable<string>)null, async (s, ct) => s));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.GroupBy((IAsyncEnumerable<string>)null, s => s, s => s));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.GroupBy((IAsyncEnumerable<string>)null, async (s, ct) => s, async (s, ct) => s));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.GroupBy((IAsyncEnumerable<string>)null, s => s, (s, group) => s));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.GroupBy((IAsyncEnumerable<string>)null, async (s, ct) => s, async (s, group, ct) => s));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.GroupBy((IAsyncEnumerable<string>)null, s => s, s => s, (s, group) => s));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.GroupBy((IAsyncEnumerable<string>)null, async (s, ct) => s, async (s, ct) => s, async (s, group, ct) => s));

            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => AsyncEnumerable.GroupBy(AsyncEnumerable.Empty<string>(), (Func<string, string>)null));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => AsyncEnumerable.GroupBy(AsyncEnumerable.Empty<string>(), (Func<string, CancellationToken, ValueTask<string>>)null));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => AsyncEnumerable.GroupBy(AsyncEnumerable.Empty<string>(), (Func<string, string>)null, s => s));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => AsyncEnumerable.GroupBy(AsyncEnumerable.Empty<string>(), (Func<string, CancellationToken, ValueTask<string>>)null, async (s, ct) => s));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => AsyncEnumerable.GroupBy(AsyncEnumerable.Empty<string>(), (Func<string, string>)null, (s, group) => s));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => AsyncEnumerable.GroupBy(AsyncEnumerable.Empty<string>(), (Func<string, CancellationToken, ValueTask<string>>)null, async (s, group, ct) => s));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => AsyncEnumerable.GroupBy(AsyncEnumerable.Empty<string>(), (Func<string, string>)null, s => s, (s, group) => s));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => AsyncEnumerable.GroupBy(AsyncEnumerable.Empty<string>(), (Func<string, CancellationToken, ValueTask<string>>)null, async (s, ct) => s, async (s, group, ct) => s));

            AssertExtensions.Throws<ArgumentNullException>("elementSelector", () => AsyncEnumerable.GroupBy(AsyncEnumerable.Empty<string>(), s => s, (Func<string, string>)null));
            AssertExtensions.Throws<ArgumentNullException>("elementSelector", () => AsyncEnumerable.GroupBy(AsyncEnumerable.Empty<string>(), async (s, ct) => s, (Func<string, CancellationToken, ValueTask<string>>)null));
            AssertExtensions.Throws<ArgumentNullException>("elementSelector", () => AsyncEnumerable.GroupBy(AsyncEnumerable.Empty<string>(), s => s, (Func<string, string>)null, (s, group) => s));
            AssertExtensions.Throws<ArgumentNullException>("elementSelector", () => AsyncEnumerable.GroupBy(AsyncEnumerable.Empty<string>(), async (s, ct) => s, (Func<string, CancellationToken, ValueTask<string>>)null, async (s, group, ct) => s));

            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => AsyncEnumerable.GroupBy(AsyncEnumerable.Empty<string>(), s => s, (Func<string, IEnumerable<string>, string>)null));
            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => AsyncEnumerable.GroupBy(AsyncEnumerable.Empty<string>(), async (s, ct) => s, (Func<string, IEnumerable<string>, CancellationToken, ValueTask<string>>)null));
            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => AsyncEnumerable.GroupBy(AsyncEnumerable.Empty<string>(), s => s, s => s, (Func<string, IEnumerable<string>, string>)null));
            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => AsyncEnumerable.GroupBy(AsyncEnumerable.Empty<string>(), async (s, ct) => s, async (s, ct) => s, (Func<string, IEnumerable<string>, CancellationToken, ValueTask<string>>)null));
        }

        [Fact]
        public void Empty_ProducesEmpty() // validating an optimization / implementation detail
        {
            Assert.Same(AsyncEnumerable.Empty<IGrouping<string, string>>(), AsyncEnumerable.Empty<string>().GroupBy(i => i));
            Assert.Same(AsyncEnumerable.Empty<IGrouping<string, string>>(), AsyncEnumerable.Empty<string>().GroupBy(async (i, ct) => i));

            Assert.Same(AsyncEnumerable.Empty<IGrouping<string, int>>(), AsyncEnumerable.Empty<string>().GroupBy(i => i, i => i.Length));
            Assert.Same(AsyncEnumerable.Empty<IGrouping<string, int>>(), AsyncEnumerable.Empty<string>().GroupBy(async (i, ct) => i, async (i, ct) => i.Length));

            Assert.Same(AsyncEnumerable.Empty<int>(), AsyncEnumerable.Empty<string>().GroupBy(i => i, (i, elements) => i.Length));
            Assert.Same(AsyncEnumerable.Empty<int>(), AsyncEnumerable.Empty<string>().GroupBy(async (i, ct) => i, async (i, elements, ct) => i.Length));

            Assert.Same(AsyncEnumerable.Empty<int>(), AsyncEnumerable.Empty<string>().GroupBy(i => i, i => i.Length, (i, elements) => i.Length));
            Assert.Same(AsyncEnumerable.Empty<int>(), AsyncEnumerable.Empty<string>().GroupBy(async (i, ct) => i, async (i, ct) => i.Length, async (i, elements, ct) => i.Length));
        }

        [Fact]
        public async Task VariousValues_MatchesEnumerable_String()
        {
            Random rand = new(42);
            foreach (int length in new[] { 0, 1, 2, 1000 })
            {
                string[] values = new string[length];
                FillRandom(rand, values);

                foreach (IEqualityComparer<int> comparer in new[] { null, EqualityComparer<int>.Default, OddEvenComparer })
                {
                    foreach (IAsyncEnumerable<string> source in CreateSources(values))
                    {
                        await AssertEqual(
                            values.GroupBy(s => s.Length, comparer),
                            source.GroupBy(s => s.Length, comparer));

                        await AssertEqual(
                            values.GroupBy(s => s.Length, comparer),
                            source.GroupBy(async (s, ct) => s.Length, comparer));

                        await AssertEqual(
                            values.GroupBy(s => s.Length, s => s.Length > 0 ? s[0] : ' ', comparer),
                            source.GroupBy(s => s.Length, s => s.Length > 0 ? s[0] : ' ', comparer));

                        await AssertEqual(
                            values.GroupBy(s => s.Length, s => s.Length > 0 ? s[0] : ' ', comparer),
                            source.GroupBy(async (s, ct) => s.Length, async (s, ct) => s.Length > 0 ? s[0] : ' ', comparer));

                        await AssertEqual(
                            values.GroupBy(s => s.Length, (key, group) => key.ToString() + string.Concat(group), comparer),
                            source.GroupBy(s => s.Length, (key, group) => key.ToString() + string.Concat(group), comparer));

                        await AssertEqual(
                            values.GroupBy(s => s.Length, (key, group) => key.ToString() + string.Concat(group), comparer),
                            source.GroupBy(async (s, ct) => s.Length, async (key, group, ct) => key.ToString() + string.Concat(group), comparer));

                        await AssertEqual(
                            values.GroupBy(s => s.Length, s => s.Length > 0 ? s.Substring(1) : "", (key, group) => key.ToString() + string.Concat(group), comparer),
                            source.GroupBy(s => s.Length, s => s.Length > 0 ? s.Substring(1) : "", (key, group) => key.ToString() + string.Concat(group), comparer));

                        await AssertEqual(
                            values.GroupBy(s => s.Length, s => s.Length > 0 ? s.Substring(1) : "", (key, group) => key.ToString() + string.Concat(group), comparer),
                            source.GroupBy(async (s, ct) => s.Length, async (s, ct) => s.Length > 0 ? s.Substring(1) : "", async (key, group, ct) => key.ToString() + string.Concat(group), comparer));
                    }
                }
            }
        }

        [Fact]
        public async Task Cancellation_Cancels()
        {
            IAsyncEnumerable<int> source = CreateSource(2, 4, 8, 16);

            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                CancellationTokenSource cts = new();
                await ConsumeAsync(source.GroupBy(s =>
                {
                    cts.Cancel();
                    return s;
                }).WithCancellation(cts.Token));
            });

            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                CancellationTokenSource cts = new();
                await ConsumeAsync(source.GroupBy(async (s, ct) =>
                {
                    Assert.Equal(cts.Token, ct);
                    await Task.Yield();
                    cts.Cancel();
                    return s;
                }).WithCancellation(cts.Token));
            });

            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                CancellationTokenSource cts = new();
                await ConsumeAsync(source.GroupBy(s => s, s =>
                {
                    cts.Cancel();
                    return s;
                }).WithCancellation(cts.Token));
            });

            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                CancellationTokenSource cts = new();
                await ConsumeAsync(source.GroupBy(async (s, ct) => s, async (s, ct) =>
                {
                    Assert.Equal(cts.Token, ct);
                    await Task.Yield();
                    cts.Cancel();
                    return s;
                }).WithCancellation(cts.Token));
            });
        }

        [Fact]
        public async Task InterfaceCalls_ExpectedCounts()
        {
            TrackingAsyncEnumerable<int> source;
            int keySelectorCount, elementSelectorCount, resultSelectorCount;

            foreach (bool useAsync in TrueFalseBools)
            {
                keySelectorCount = 0;
                source = CreateSource(1, 2, 3, 4).Track();
                await ConsumeAsync(useAsync ?
                    source.GroupBy(async (i, ct) =>
                    {
                        keySelectorCount++;
                        return i % 2;
                    }) :
                    source.GroupBy(i =>
                    {
                        keySelectorCount++;
                        return i;
                    }));
                Assert.Equal(5, source.MoveNextAsyncCount);
                Assert.Equal(4, source.CurrentCount);
                Assert.Equal(1, source.DisposeAsyncCount);
                Assert.Equal(4, keySelectorCount);

                keySelectorCount = elementSelectorCount = 0;
                source = CreateSource(1, 2, 3, 4).Track();
                await ConsumeAsync(useAsync ?
                    source.GroupBy(async (i, ct) =>
                    {
                        keySelectorCount++;
                        return i % 2;
                    }, async (i, ct) =>
                    {
                        elementSelectorCount++;
                        return i;
                    }) :
                    source.GroupBy(i =>
                    {
                        keySelectorCount++;
                        return i;
                    }, i =>
                    {
                        elementSelectorCount++;
                        return i;
                    }));
                Assert.Equal(5, source.MoveNextAsyncCount);
                Assert.Equal(4, source.CurrentCount);
                Assert.Equal(1, source.DisposeAsyncCount);
                Assert.Equal(4, keySelectorCount);
                Assert.Equal(4, elementSelectorCount);

                keySelectorCount = resultSelectorCount = 0;
                source = CreateSource(1, 2, 3, 4).Track();
                await ConsumeAsync(useAsync ?
                    source.GroupBy(async (i, ct) =>
                    {
                        keySelectorCount++;
                        return i % 2;
                    }, async (key, group, ct) =>
                    {
                        resultSelectorCount++;
                        return key;
                    }) :
                    source.GroupBy(i =>
                    {
                        keySelectorCount++;
                        return i % 2;
                    }, (key, group) =>
                    {
                        resultSelectorCount++;
                        return key;
                    }));
                Assert.Equal(5, source.MoveNextAsyncCount);
                Assert.Equal(4, source.CurrentCount);
                Assert.Equal(1, source.DisposeAsyncCount);
                Assert.Equal(4, keySelectorCount);
                Assert.Equal(2, resultSelectorCount);

                keySelectorCount = elementSelectorCount = resultSelectorCount = 0;
                source = CreateSource(1, 2, 3, 4).Track();
                await ConsumeAsync(useAsync ?
                    source.GroupBy(async (i, ct) =>
                    {
                        keySelectorCount++;
                        return i % 2;
                    }, async (i, ct) =>
                    {
                        elementSelectorCount++;
                        return i;
                    }, async (key, group, ct) =>
                    {
                        resultSelectorCount++;
                        return key;
                    }) :
                    source.GroupBy(i =>
                    {
                        keySelectorCount++;
                        return i % 2;
                    }, i =>
                    {
                        elementSelectorCount++;
                        return i;
                    }, (key, group) =>
                    {
                        resultSelectorCount++;
                        return key;
                    }));
                Assert.Equal(5, source.MoveNextAsyncCount);
                Assert.Equal(4, source.CurrentCount);
                Assert.Equal(1, source.DisposeAsyncCount);
                Assert.Equal(4, keySelectorCount);
                Assert.Equal(4, elementSelectorCount);
                Assert.Equal(2, resultSelectorCount);
            }
        }

        [Fact]
        public async Task IGrouping_ImplementsIList()
        {
            List<IGrouping<int, int>> result = await AsyncEnumerable.Range(0, 100).GroupBy(i => i % 2).ToListAsync();
            foreach (IGrouping<int, int> group in result)
            {
                IList<int> list = Assert.IsAssignableFrom<IList<int>>(group);

                Assert.Equal(50, list.Count);
                Assert.True(list.IsReadOnly);

                if (group.Key == 0)
                {
                    Assert.Equal(0, list[0]);

                    Assert.True(list.Contains(0));
                    Assert.True(list.Contains(98));
                    Assert.False(list.Contains(1));
                    Assert.False(list.Contains(99));

                    Assert.Equal(0, list.IndexOf(0));
                    Assert.Equal(49, list.IndexOf(98));
                    Assert.Equal(-1, list.IndexOf(99));
                }
                else
                {
                    Assert.Equal(1, list[0]);
                    Assert.True(list.Contains(1));
                    Assert.True(list.Contains(99));
                    Assert.False(list.Contains(2));
                    Assert.False(list.Contains(98));

                    Assert.Equal(0, list.IndexOf(1));
                    Assert.Equal(49, list.IndexOf(99));
                    Assert.Equal(-1, list.IndexOf(98));
                }
                for (int i = 0; i < list.Count - 1; i++)
                {
                    Assert.Equal(list[i], list[i + 1] - 2);
                }
                AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => list[50]);

                int[] ints = new int[52];
                list.CopyTo(ints, 1);
                Assert.Equal(0, ints[0]);
                Assert.Equal(list[0], ints[1]);
                Assert.Equal(list[49], ints[50]);
                Assert.Equal(0, ints[51]);

                Assert.Throws<NotSupportedException>(() => list.Add(0));
                Assert.Throws<NotSupportedException>(() => list.Clear());
                Assert.Throws<NotSupportedException>(() => list.Insert(0, 0));
                Assert.Throws<NotSupportedException>(() => list.Remove(0));
                Assert.Throws<NotSupportedException>(() => list.RemoveAt(0));
                Assert.Throws<NotSupportedException>(() => list[0] = 0);
            }
        }
    }
}
