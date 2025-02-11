// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class AggregateByTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.AggregateBy((IAsyncEnumerable<int>)null, x => x, 42, (x, y) => x + y));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => AsyncEnumerable.AggregateBy(AsyncEnumerable.Empty<int>(), (Func<int, int>)null, 42, (x, y) => x + y));
            AssertExtensions.Throws<ArgumentNullException>("func", () => AsyncEnumerable.AggregateBy(AsyncEnumerable.Empty<int>(), x => x, 42, (Func<int, int, int>)null));

            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.AggregateBy((IAsyncEnumerable<int>)null, async (x, ct) => x, 42, async (x, y, ct) => x + y));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => AsyncEnumerable.AggregateBy(AsyncEnumerable.Empty<int>(), (Func<int, CancellationToken, ValueTask<int>>)null, 42, async (x, y, ct) => x + y));
            AssertExtensions.Throws<ArgumentNullException>("func", () => AsyncEnumerable.AggregateBy(AsyncEnumerable.Empty<int>(), async (x, ct) => x, 42, (Func<int, int, CancellationToken, ValueTask<int>>)null));

            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.AggregateBy((IAsyncEnumerable<int>)null, x => x, x => x, (x, y) => x + y));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => AsyncEnumerable.AggregateBy(AsyncEnumerable.Empty<int>(), (Func<int, int>)null, x => x, (x, y) => x + y));
            AssertExtensions.Throws<ArgumentNullException>("seedSelector", () => AsyncEnumerable.AggregateBy(AsyncEnumerable.Empty<int>(), x => x, (Func<int, int>)null, (x, y) => x + y));
            AssertExtensions.Throws<ArgumentNullException>("func", () => AsyncEnumerable.AggregateBy(AsyncEnumerable.Empty<int>(), x => x, x => x, (Func<int, int, int>)null));

            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.AggregateBy((IAsyncEnumerable<int>)null, async (x, ct) => x, async (x, ct) => x, async (x, y, ct) => x + y));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => AsyncEnumerable.AggregateBy(AsyncEnumerable.Empty<int>(), (Func<int, CancellationToken, ValueTask<int>>)null, async (x, ct) => x, async (x, y, ct) => x + y));
            AssertExtensions.Throws<ArgumentNullException>("seedSelector", () => AsyncEnumerable.AggregateBy(AsyncEnumerable.Empty<int>(), async (x, ct) => x, (Func<int, CancellationToken, ValueTask<int>>)null, async (x, y, ct) => x + y));
            AssertExtensions.Throws<ArgumentNullException>("func", () => AsyncEnumerable.AggregateBy(AsyncEnumerable.Empty<int>(), async (x, ct) => x, async (x, ct) => x, (Func<int, int, CancellationToken, ValueTask<int>>)null));
        }

        [Fact]
        public void Empty_ProducesEmpty() // validating an optimization / implementation detail
        {
            Assert.Same(AsyncEnumerable.Empty<KeyValuePair<int, int>>(), AsyncEnumerable.Empty<int>().AggregateBy(x => x, x => x, (x, y) => x + y));
            Assert.Same(AsyncEnumerable.Empty<KeyValuePair<int, int>>(), AsyncEnumerable.Empty<int>().AggregateBy(x => x, 42, (x, y) => x + y));
            Assert.Same(AsyncEnumerable.Empty<KeyValuePair<int, int>>(), AsyncEnumerable.Empty<int>().AggregateBy(async (x, ct) => x, async (x, ct) => x, async (x, y, ct) => x + y));
            Assert.Same(AsyncEnumerable.Empty<KeyValuePair<int, int>>(), AsyncEnumerable.Empty<int>().AggregateBy(async (x, ct) => x, 42, async (x, y, ct) => x + y));
        }

        public static IEnumerable<object[]> VariousValues_MatchesEnumerable_String_MemberData()
        {
            yield return new object[] { new string[0] };
            yield return new object[] { new string[] { "1" } };
            yield return new object[] { new string[] { "2", "4", "8" } };
            yield return new object[] { new string[] { "12", "4", "8" } };
            yield return new object[] { new string[] { "12", "13", "14", "15", "22", "23", "24" } };
            yield return new object[] { new string[] { "-1", "2", "5", "6", "7", "8" } };
        }

#if NET
        [Theory]
        [MemberData(nameof(VariousValues_MatchesEnumerable_String_MemberData))]
        public async Task VariousValues_MatchesEnumerable_String(string[] values)
        {
            foreach (IAsyncEnumerable<string> source in CreateSources(values))
            {
                Assert.Equal(
                    values.AggregateBy(x => x[0], "", (x, y) => x + y).ToArray(),
                    await source.AggregateBy(x => x[0], "", (x, y) => x + y).ToArrayAsync());

                Assert.Equal(
                    values.AggregateBy(x => x, "", (x, y) => x + y).ToArray(),
                    await source.AggregateBy(async (x, ct) => x, "", async (x, y, ct) => x + y).ToArrayAsync());

                Assert.Equal(
                    values.AggregateBy(x => x[0], x => x.ToString() + x, (x, y) => x + y).ToArray(),
                    await source.AggregateBy(x => x[0], x => x.ToString() + x, (x, y) => x + y).ToArrayAsync());

                Assert.Equal(
                    values.AggregateBy(x => x, x => x.ToString() + x, (x, y) => x + y).ToArray(),
                    await source.AggregateBy(async (x, ct) => x, async (x, ct) => x.ToString() + x, async (x, y, ct) => x + y).ToArrayAsync());

                Assert.Equal(
                    values.AggregateBy(x => x[0], "", (x, y) => x + y, OddEvenComparer).ToArray(),
                    await source.AggregateBy(x => x[0], "", (x, y) => x + y, OddEvenComparer).ToArrayAsync());

                Assert.Equal(
                    values.AggregateBy(x => x, "", (x, y) => x + y, LengthComparer).ToArray(),
                    await source.AggregateBy(async (x, ct) => x, "", async (x, y, ct) => x + y, LengthComparer).ToArrayAsync());

                Assert.Equal(
                    values.AggregateBy(x => x[0], x => x.ToString() + x, (x, y) => x + y, OddEvenComparer).ToArray(),
                    await source.AggregateBy(x => x[0], x => x.ToString() + x, (x, y) => x + y, OddEvenComparer).ToArrayAsync());

                Assert.Equal(
                    values.AggregateBy(x => x, x => x.ToString() + x, (x, y) => x + y, LengthComparer).ToArray(),
                    await source.AggregateBy(async (x, ct) => x, async (x, ct) => x.ToString() + x, async (x, y, ct) => x + y, LengthComparer).ToArrayAsync());
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
                await ConsumeAsync(source.AggregateBy(x =>
                {
                    cts.Cancel();
                    return x;
                }, 42, (x, y) => x + y).WithCancellation(cts.Token));
            });

            cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await ConsumeAsync(source.AggregateBy(async (x, ct) =>
                {
                    Assert.Equal(cts.Token, ct);
                    await Task.Yield();
                    cts.Cancel();
                    return x;
                }, 42, async (x, y, ct) => x + y).WithCancellation(cts.Token));
            });

            cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await ConsumeAsync(source.AggregateBy(x =>
                {
                    cts.Cancel();
                    return x;
                }, x => x, (x, y) => x + y).WithCancellation(cts.Token));
            });

            cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await ConsumeAsync(source.AggregateBy(async (x, ct) =>
                {
                    Assert.Equal(cts.Token, ct);
                    await Task.Yield();
                    cts.Cancel();
                    return x;
                }, async (x, ct) => x, async (x, y, ct) => x + y).WithCancellation(cts.Token));
            });
        }

        [Fact]
        public async Task InterfaceCalls_ExpectedCounts()
        {
            TrackingAsyncEnumerable<int> source;
            int keySelectorCount, funcCount, seedSelectorCount;

            foreach (bool useAsync in TrueFalseBools)
            {
                keySelectorCount = funcCount = 0;
                source = CreateSource(2, 4, 8, 16).Track();
                Assert.Equal([
                        new(2, 44),
                        new(4, 46),
                        new(8, 50),
                        new(16, 58),
                    ],
                    useAsync ?
                        await source.AggregateBy(x => { keySelectorCount++; return x; }, 42, (x, y) => { funcCount++; return x + y; }).ToArrayAsync() :
                        await source.AggregateBy(async (x, ct) => { keySelectorCount++; return x; }, 42, async (x, y, ct) => { funcCount++; return x + y; }).ToArrayAsync());
                Assert.Equal(5, source.MoveNextAsyncCount);
                Assert.Equal(4, source.CurrentCount);
                Assert.Equal(1, source.DisposeAsyncCount);
                Assert.Equal(4, keySelectorCount);
                Assert.Equal(4, funcCount);

                keySelectorCount = funcCount = seedSelectorCount = 0;
                source = CreateSource(2, 2, 2, 16).Track();
                Assert.Equal([
                        new(2, 48),
                        new(16, 58),
                    ],
                    useAsync ?
                        await source.AggregateBy(x => { keySelectorCount++; return x; }, x => { seedSelectorCount++; return 42; }, (x, y) => { funcCount++; return x + y; }).ToArrayAsync() :
                        await source.AggregateBy(async (x, ct) => { keySelectorCount++; return x; }, async (x, ct) => { seedSelectorCount++; return 42; }, async (x, y, ct) => { funcCount++; return x + y; }).ToArrayAsync());
                Assert.Equal(5, source.MoveNextAsyncCount);
                Assert.Equal(4, source.CurrentCount);
                Assert.Equal(1, source.DisposeAsyncCount);
                Assert.Equal(4, keySelectorCount);
                Assert.Equal(2, seedSelectorCount);
                Assert.Equal(4, funcCount);
            }
        }
    }
}
