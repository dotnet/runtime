// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class ToLookupAsyncTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.ToLookupAsync((IAsyncEnumerable<string>)null, s => s.Length));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.ToLookupAsync((IAsyncEnumerable<string>)null, async (s, ct) => s.Length));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.ToLookupAsync((IAsyncEnumerable<string>)null, s => s.Length, s => s));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.ToLookupAsync((IAsyncEnumerable<string>)null, async (s, ct) => s.Length, async (s, ct) => s));

            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => AsyncEnumerable.ToLookupAsync(AsyncEnumerable.Empty<string>(), (Func<string, int>)null));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => AsyncEnumerable.ToLookupAsync(AsyncEnumerable.Empty<string>(), (Func<string, CancellationToken, ValueTask<int>>)null));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => AsyncEnumerable.ToLookupAsync(AsyncEnumerable.Empty<string>(), (Func<string, int>)null, s => s));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => AsyncEnumerable.ToLookupAsync(AsyncEnumerable.Empty<string>(), (Func<string, CancellationToken, ValueTask<int>>)null, async (s, ct) => s));

            AssertExtensions.Throws<ArgumentNullException>("elementSelector", () => AsyncEnumerable.ToLookupAsync(AsyncEnumerable.Empty<string>(), s => s.Length, (Func<string, DateTime>)null));
            AssertExtensions.Throws<ArgumentNullException>("elementSelector", () => AsyncEnumerable.ToLookupAsync(AsyncEnumerable.Empty<string>(), async (s, ct) => s.Length, (Func<string, CancellationToken, ValueTask<DateTime>>)null));
        }

        [Fact]
        public async Task VariousValues_MatchesEnumerable()
        {
            Random rand = new(42);
            foreach (int length in new[] { 0, 1, 2, 100 })
            {
                string[] values = new string[length];
                FillRandom(rand, values);

                foreach (IAsyncEnumerable<string> source in CreateSources(values))
                {
                    foreach (IEqualityComparer<int> comparer in new[] { null, EqualityComparer<int>.Default, OddEvenComparer })
                    {
                        AssertEqual(
                            values.ToLookup(s => s.Length, comparer),
                            await source.ToLookupAsync(s => s.Length, comparer));

                        AssertEqual(
                            values.ToLookup(s => s.Length, comparer),
                            await source.ToLookupAsync(async (s, ct) =>
                            {
                                await Task.Yield();
                                return s.Length;
                            }, comparer));

                        AssertEqual(
                            values.ToLookup(s => s.Length, s => s.Length > 0 ? s.Substring(1) : "", comparer),
                            await source.ToLookupAsync(s => s.Length, s => s.Length > 0 ? s.Substring(1) : "", comparer));

                        AssertEqual(
                            values.ToLookup(s => s.Length, s => s.Length > 0 ? s.Substring(1) : "", comparer),
                            await source.ToLookupAsync(async (s, ct) =>
                            {
                                await Task.Yield();
                                return s.Length;
                            }, async (s, ct) =>
                            {
                                await Task.Yield();
                                return s.Length > 0 ? s.Substring(1) : "";
                            }, comparer));

                        static void AssertEqual(
                            ILookup<int, string> expected,
                            ILookup<int, string> actual)
                        {
                            Assert.Equal(expected.Count, actual.Count);
                            Assert.Equal(expected.SelectMany(kvp => kvp), actual.SelectMany(kvp => kvp));

                            foreach (IGrouping<int, string> g in expected)
                            {
                                Assert.True(actual.Contains(g.Key));
                                Assert.Equal(g, actual[g.Key]);
                            }

                            foreach (IGrouping<int, string> g in actual)
                            {
                                Assert.True(expected.Contains(g.Key));
                                Assert.Equal(g, expected[g.Key]);
                            }

                            foreach (IGrouping<int, string> g in (IEnumerable)actual)
                            {
                                Assert.True(expected.Contains(g.Key));
                                Assert.Equal(g, expected[g.Key]);
                            }
                        }
                    }
                }
            }
        }

        [Fact]
        public async Task Cancellation_Cancels()
        {
            IAsyncEnumerable<int> source = CreateSource(2, 4, 8, 16);
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.ToLookupAsync(i => i, null, new CancellationToken(true)));

            CancellationTokenSource cts;

            cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.ToLookupAsync(i =>
            {
                cts.Cancel();
                return i;
            }, null, cts.Token));

            cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.ToLookupAsync(i =>
            {
                cts.Cancel();
                return i;
            }, i => i, null, cts.Token));

            cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.ToLookupAsync(i => i, i =>
            {
                cts.Cancel();
                return i;
            }, null, cts.Token));

            cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.ToLookupAsync(async (i, ct) =>
            {
                Assert.Equal(cts.Token, ct);
                await Task.Yield();
                cts.Cancel();
                return i;
            }, null, cts.Token));

            cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.ToLookupAsync(async (i, ct) =>
            {
                Assert.Equal(cts.Token, ct);
                await Task.Yield();
                cts.Cancel();
                return i;
            }, async (i, ct) => i, null, cts.Token));

            cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.ToLookupAsync(async (i, ct) =>
            {
                Assert.Equal(cts.Token, ct);
                return i;
            }, async (i, ct) =>
            {
                Assert.Equal(cts.Token, ct);
                await Task.Yield();
                cts.Cancel();
                return i;
            }, null, cts.Token));
        }

        [Fact]
        public async Task InterfaceCalls_ExpectedCounts()
        {
            TrackingAsyncEnumerable<int> source;
            int keySelectorCount, elementSelectorCount;

            keySelectorCount = 0;
            source = CreateSource(2, 4, 8, 16).Track();
            await source.ToLookupAsync(i =>
            {
                keySelectorCount++;
                return i;
            });
            Assert.Equal(5, source.MoveNextAsyncCount);
            Assert.Equal(4, source.CurrentCount);
            Assert.Equal(1, source.DisposeAsyncCount);
            Assert.Equal(4, keySelectorCount);

            keySelectorCount = 0;
            source = CreateSource(2, 4, 8, 16).Track();
            await source.ToLookupAsync(async (i, ct) =>
            {
                keySelectorCount++;
                return i;
            });
            Assert.Equal(5, source.MoveNextAsyncCount);
            Assert.Equal(4, source.CurrentCount);
            Assert.Equal(1, source.DisposeAsyncCount);
            Assert.Equal(4, keySelectorCount);

            keySelectorCount = elementSelectorCount = 0;
            source = CreateSource(2, 4, 8, 16).Track();
            await source.ToLookupAsync(i =>
            {
                keySelectorCount++;
                return i;
            }, i =>
            {
                elementSelectorCount++;
                return i;
            });
            Assert.Equal(5, source.MoveNextAsyncCount);
            Assert.Equal(4, source.CurrentCount);
            Assert.Equal(1, source.DisposeAsyncCount);
            Assert.Equal(4, keySelectorCount);

            keySelectorCount = elementSelectorCount = 0;
            source = CreateSource(2, 4, 8, 16).Track();
            await source.ToLookupAsync(async (i, ct) =>
            {
                keySelectorCount++;
                return i;
            }, async (i, ct) =>
            {
                elementSelectorCount++;
                return i;
            });
            Assert.Equal(5, source.MoveNextAsyncCount);
            Assert.Equal(4, source.CurrentCount);
            Assert.Equal(1, source.DisposeAsyncCount);
            Assert.Equal(4, keySelectorCount);
            Assert.Equal(4, elementSelectorCount);
        }
    }
}
