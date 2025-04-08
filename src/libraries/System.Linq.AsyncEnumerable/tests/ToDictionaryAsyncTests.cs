// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class ToDictionaryAsyncTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.ToDictionaryAsync((IAsyncEnumerable<KeyValuePair<string, int>>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.ToDictionaryAsync((IAsyncEnumerable<string>)null, s => s.Length));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.ToDictionaryAsync((IAsyncEnumerable<string>)null, async (s, ct) => s.Length));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.ToDictionaryAsync((IAsyncEnumerable<string>)null, s => s.Length, s => s));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.ToDictionaryAsync((IAsyncEnumerable<string>)null, async (s, ct) => s.Length, async (s, ct) => s));

            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => AsyncEnumerable.ToDictionaryAsync(AsyncEnumerable.Empty<string>(), (Func<string, int>)null));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => AsyncEnumerable.ToDictionaryAsync(AsyncEnumerable.Empty<string>(), (Func<string, CancellationToken, ValueTask<int>>)null));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => AsyncEnumerable.ToDictionaryAsync(AsyncEnumerable.Empty<string>(), (Func<string, int>)null, s => s));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => AsyncEnumerable.ToDictionaryAsync(AsyncEnumerable.Empty<string>(), (Func<string, CancellationToken, ValueTask<int>>)null, async (s, ct) => s));

            AssertExtensions.Throws<ArgumentNullException>("elementSelector", () => AsyncEnumerable.ToDictionaryAsync(AsyncEnumerable.Empty<string>(), s => s.Length, (Func<string, DateTime>)null));
            AssertExtensions.Throws<ArgumentNullException>("elementSelector", () => AsyncEnumerable.ToDictionaryAsync(AsyncEnumerable.Empty<string>(), async (s, ct) => s.Length, (Func<string, CancellationToken, ValueTask<DateTime>>)null));
        }

        [Fact]
        public async Task Duplicates_Throws()
        {
            ValueTask<Dictionary<string, string>> result;

            result = CreateSource("a", "b", "a").ToDictionaryAsync(s => s);
            await Assert.ThrowsAsync<ArgumentException>(async () => await result);

            result = CreateSource("a", "b", "c").ToDictionaryAsync(s => "a");
            await Assert.ThrowsAsync<ArgumentException>(async () => await result);

            result = CreateSource("a", "b", "c").ToDictionaryAsync(async (s, ct) => "a");
            await Assert.ThrowsAsync<ArgumentException>(async () => await result);

            result = CreateSource("a", "b", "c").ToDictionaryAsync(s => "a", s => s);
            await Assert.ThrowsAsync<ArgumentException>(async () => await result);

            result = CreateSource("a", "b", "c").ToDictionaryAsync(async (s, ct) => "a", async (s, ct) => s);
            await Assert.ThrowsAsync<ArgumentException>(async () => await result);
        }

        [Fact]
        public async Task VariousValues_MatchesEnumerable()
        {
            Random rand = new(42);
            foreach (int length in new[] { 0, 1, 2, 100 })
            {
                string[] values = new string[length];
                FillRandom(rand, values);
                for (int i = 0; i < length; i++)
                {
                    values[i] = values[i] + (char)('A' + i);
                }

                foreach (IAsyncEnumerable<string> source in CreateSources(values))
                {
                    foreach (IEqualityComparer<string> comparer in new IEqualityComparer<string>[] { null, EqualityComparer<string>.Default, StringComparer.OrdinalIgnoreCase })
                    {
#if NET
                        Assert.Equal(
                            values.Select(s => KeyValuePair.Create(s, s)).ToDictionary(comparer),
                            await source.Select(s => KeyValuePair.Create(s, s)).ToDictionaryAsync(comparer));

                        Assert.Equal(
                            values.Select(s => (s, s)).ToDictionary(comparer),
                            await source.Select(s => (s, s)).ToDictionaryAsync(comparer));
#endif

                        Assert.Equal(
                            values.ToDictionary(s => s + s, comparer),
                            await source.ToDictionaryAsync(s => s + s, comparer));

                        Assert.Equal(
                            values.ToDictionary(s => s + s, comparer),
                            await source.ToDictionaryAsync(async (s, ct) =>
                            {
                                await Task.Yield();
                                return s + s;
                            }, comparer));

                        Assert.Equal(
                            values.ToDictionary(s => s + s, s => s.Length > 0 ? s.Substring(1) : "", comparer),
                            await source.ToDictionaryAsync(s => s + s, s => s.Length > 0 ? s.Substring(1) : "", comparer));

                        Assert.Equal(
                            values.ToDictionary(s => s + s, s => s.Length > 0 ? s.Substring(1) : "", comparer),
                            await source.ToDictionaryAsync(async (s, ct) =>
                            {
                                await Task.Yield();
                                return s + s;
                            }, async (s, ct) =>
                            {
                                await Task.Yield();
                                return s.Length > 0 ? s.Substring(1) : "";
                            }, comparer));
                    }
                }
            }
        }

        [Fact]
        public async Task Cancellation_Cancels()
        {
            IAsyncEnumerable<int> source = CreateSource(2, 4, 8, 16);
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.ToDictionaryAsync(i => i, null, new CancellationToken(true)));

            CancellationTokenSource cts;

            cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.ToDictionaryAsync(i =>
            {
                cts.Cancel();
                return i;
            }, null, cts.Token));

            cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.ToDictionaryAsync(i =>
            {
                cts.Cancel();
                return i;
            }, i => i, null, cts.Token));

            cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.ToDictionaryAsync(i => i, i =>
            {
                cts.Cancel();
                return i;
            }, null, cts.Token));

            cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.ToDictionaryAsync(async (i, ct) =>
            {
                Assert.Equal(cts.Token, ct);
                await Task.Yield();
                cts.Cancel();
                return i;
            }, null, cts.Token));

            cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.ToDictionaryAsync(async (i, ct) =>
            {
                Assert.Equal(cts.Token, ct);
                await Task.Yield();
                cts.Cancel();
                return i;
            }, async (i, ct) => i, null, cts.Token));

            cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.ToDictionaryAsync(async (i, ct) =>
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
            await source.ToDictionaryAsync(i =>
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
            await source.ToDictionaryAsync(async (i, ct) =>
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
            await source.ToDictionaryAsync(i =>
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
            await source.ToDictionaryAsync(async (i, ct) =>
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
