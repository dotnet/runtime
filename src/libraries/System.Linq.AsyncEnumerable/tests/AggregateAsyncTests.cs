// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class AggregateAsyncTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.AggregateAsync<int>(null, (x, y) => x + y));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.AggregateAsync<int>(null, async (x, y, ct) => x + y));
            AssertExtensions.Throws<ArgumentNullException>("func", () => AsyncEnumerable.AggregateAsync(AsyncEnumerable.Empty<int>(), (Func<int, int, int>)null));
            AssertExtensions.Throws<ArgumentNullException>("func", () => AsyncEnumerable.AggregateAsync(AsyncEnumerable.Empty<int>(), (Func<int, int, CancellationToken, ValueTask<int>>)null));

            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.AggregateAsync<int, int>(null, 42, (x, y) => x + y));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.AggregateAsync<int, int>(null, 42, async (x, y, ct) => x + y));
            AssertExtensions.Throws<ArgumentNullException>("func", () => AsyncEnumerable.AggregateAsync(AsyncEnumerable.Empty<int>(), 42, (Func<int, int, int>)null));
            AssertExtensions.Throws<ArgumentNullException>("func", () => AsyncEnumerable.AggregateAsync(AsyncEnumerable.Empty<int>(), 42, (Func<int, int, CancellationToken, ValueTask<int>>)null));

            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.AggregateAsync<int, int, int>(null, 42, (x, y) => x + y, x => x * 2));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.AggregateAsync<int, int, int>(null, 42, async (x, y, ct) => x + y, async (x, ct) => x * 2));
            AssertExtensions.Throws<ArgumentNullException>("func", () => AsyncEnumerable.AggregateAsync(AsyncEnumerable.Empty<int>(), 42, (Func<int, int, int>)null, x => x * 2));
            AssertExtensions.Throws<ArgumentNullException>("func", () => AsyncEnumerable.AggregateAsync(AsyncEnumerable.Empty<int>(), 42, (Func<int, int, CancellationToken, ValueTask<int>>)null, async (x, ct) => x * 2));
            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => AsyncEnumerable.AggregateAsync(AsyncEnumerable.Empty<int>(), 42, (x, y) => x + y, (Func<int, int>)null));
            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => AsyncEnumerable.AggregateAsync(AsyncEnumerable.Empty<int>(), 42, async (x, y, ct) => x + y, (Func<int, CancellationToken, ValueTask<int>>)null));
        }

        [Theory]
        [InlineData(new int[0])]
        [InlineData(new int[] { 1 })]
        [InlineData(new int[] { 2, 4, 8 })]
        [InlineData(new int[] { -1, 2, 5, 6, 7, 8 })]
        public async Task VariousValues_MatchesEnumerable_Int32(int[] values)
        {
            foreach (IAsyncEnumerable<int> source in CreateSources(values))
            {
                if (values.Length > 0)
                {
                    Assert.Equal(
                        values.Aggregate((x, y) => x + (2 * y)),
                        await source.AggregateAsync((x, y) => x + (2 * y)));

                    Assert.Equal(
                        values.Aggregate((x, y) => x + (2 * y)),
                        await source.AggregateAsync(async (x, y, ct) =>
                        {
                            await Task.Yield();
                            return x + (2 * y);
                        }));
                }
                else
                {
                    Assert.Throws<InvalidOperationException>(() => values.Aggregate((x, y) => x + (2 * y)));
                    await Assert.ThrowsAsync<InvalidOperationException>(async () => await source.AggregateAsync((x, y) => x + (2 * y)));
                    await Assert.ThrowsAsync<InvalidOperationException>(async () => await source.AggregateAsync(async (x, y, ct) => x + (2 * y)));
                }

                Assert.Equal(
                    values.Aggregate(42, (x, y) => x + (2 * y)),
                    await source.AggregateAsync(42, (x, y) => x + (2 * y)));

                Assert.Equal(
                    values.Aggregate(42, (x, y) => x + (2 * y)),
                    await source.AggregateAsync(42, async (x, y, ct) =>
                    {
                        await Task.Yield();
                        return x + (2 * y);
                    }));

                Assert.Equal(
                    values.Aggregate(42, (x, y) => x + (2 * y), x => x * 2),
                    await source.AggregateAsync(42, (x, y) => x + (2 * y), x => x * 2));

                Assert.Equal(
                    values.Aggregate(42, (x, y) => x + (2 * y), x => x * 2),
                    await source.AggregateAsync(42, async (x, y, ct) =>
                    {
                        await Task.Yield();
                        return x + (2 * y);
                    }, async (x, ct) =>
                    {
                        await Task.Yield();
                        return x * 2;
                    }));
            }
        }

        public static IEnumerable<object[]> VariousValues_MatchesEnumerable_String_MemberData()
        {
            yield return new object[] { new string[0] };
            yield return new object[] { new string[] { "1" } };
            yield return new object[] { new string[] { "2", "4", "8" } };
            yield return new object[] { new string[] { "-1", "2", "5", "6", "7", "8" } };
        }

        [Theory]
        [MemberData(nameof(VariousValues_MatchesEnumerable_String_MemberData))]
        public async Task VariousValues_MatchesEnumerable_String(string[] values)
        {
            foreach (IAsyncEnumerable<string> source in CreateSources(values))
            {
                if (values.Length > 0)
                {
                    Assert.Equal(
                        values.Aggregate((x, y) => x + y + y),
                        await source.AggregateAsync((x, y) => x + y + y));

                    Assert.Equal(
                        values.Aggregate((x, y) => x + y + y),
                        await source.AggregateAsync(async (x, y, ct) =>
                        {
                            await Task.Yield();
                            return x + y + y;
                        }));
                }
                else
                {
                    Assert.Throws<InvalidOperationException>(() => values.Aggregate((x, y) => x + y + y));
                    await Assert.ThrowsAsync<InvalidOperationException>(async () => await source.AggregateAsync((x, y) => x + y + y));
                }

                Assert.Equal(
                    values.Aggregate((string)null, (x, y) => x + y + y),
                    await source.AggregateAsync((string)null, (x, y) => x + y + y));

                Assert.Equal(
                    values.Aggregate((string)null, (x, y) => x + y + y),
                    await source.AggregateAsync((string)null, async (x, y, ct) =>
                    {
                        await Task.Yield();
                        return x + y + y;
                    }));

                Assert.Equal(
                    values.Aggregate((string)null, (x, y) => x + y + y, x => x + x),
                    await source.AggregateAsync((string)null, (x, y) => x + y + y, x => x + x));

                Assert.Equal(
                    values.Aggregate((string)null, (x, y) => x + y + y, x => x + x),
                    await source.AggregateAsync((string)null, async (x, y, ct) =>
                    {
                        await Task.Yield();
                        return x + y + y;
                    }, async (x, ct) =>
                    {
                        await Task.Yield();
                        return x + x;
                    }));
            }
        }

        [Fact]
        public async Task Cancellation_Cancels()
        {
            IAsyncEnumerable<int> source = CreateSource(2, 4, 8, 16);
            CancellationTokenSource cts;

            cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.AggregateAsync((x, y) =>
            {
                cts.Cancel();
                return x + y;
            }, cts.Token));

            cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.AggregateAsync(async (x, y, ct) =>
            {
                Assert.Equal(cts.Token, ct);
                await Task.Yield();
                cts.Cancel();
                return x + y;
            }, cts.Token));

            cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.AggregateAsync(42, (x, y) =>
            {
                cts.Cancel();
                return x + y;
            }, cts.Token));

            cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.AggregateAsync(42, async (x, y, ct) =>
            {
                Assert.Equal(cts.Token, ct);
                await Task.Yield();
                cts.Cancel();
                return x + y;
            }, cts.Token));

            cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.AggregateAsync(42, (x, y) =>
            {
                cts.Cancel();
                return x + y;
            }, x => x, cts.Token));

            cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.AggregateAsync(42, async (x, y, ct) =>
            {
                Assert.Equal(cts.Token, ct);
                await Task.Yield();
                cts.Cancel();
                return x + y;
            }, async (x, ct) => x, cts.Token));
        }

        [Fact]
        public async Task InterfaceCalls_ExpectedCounts()
        {
            TrackingAsyncEnumerable<int> source;
            int funcCount, resultCount;

            foreach (bool useAsync in TrueFalseBools)
            {
                funcCount = 0;
                source = CreateSource(2, 4, 8, 16).Track();
                Assert.Equal(30, useAsync ?
                    await source.AggregateAsync((x, y) => { funcCount++; return x + y; }) :
                    await source.AggregateAsync(async (x, y, ct) => { funcCount++; return x + y; }));
                Assert.Equal(5, source.MoveNextAsyncCount);
                Assert.Equal(4, source.CurrentCount);
                Assert.Equal(1, source.DisposeAsyncCount);
                Assert.Equal(3, funcCount);

                funcCount = 0;
                source = CreateSource(2, 4, 8, 16).Track();
                await Assert.ThrowsAsync<Exception>(async () =>
                {
                    await (useAsync ?
                        source.AggregateAsync((x, y) => { funcCount++; throw new Exception(); }) :
                        source.AggregateAsync(async (x, y, ct) => { funcCount++; throw new Exception(); }));
                });
                Assert.Equal(2, source.MoveNextAsyncCount);
                Assert.Equal(2, source.CurrentCount);
                Assert.Equal(1, source.DisposeAsyncCount);
                Assert.Equal(1, funcCount);

                funcCount = 0;
                source = CreateSource(2, 4, 8, 16).Track();
                Assert.Equal(72, useAsync ?
                    await source.AggregateAsync(42, (x, y) => { funcCount++; return x + y; }) :
                    await source.AggregateAsync(42, async (x, y, ct) => { funcCount++; return x + y; }));
                Assert.Equal(5, source.MoveNextAsyncCount);
                Assert.Equal(4, source.CurrentCount);
                Assert.Equal(1, source.DisposeAsyncCount);
                Assert.Equal(4, funcCount);

                funcCount = 0;
                source = CreateSource(2, 4, 8, 16).Track();
                await Assert.ThrowsAsync<Exception>(async () =>
                {
                    await (useAsync ?
                        source.AggregateAsync(42, (x, y) => { funcCount++; throw new Exception(); }) :
                        source.AggregateAsync(42, async (x, y, ct) => { funcCount++; throw new Exception(); }));
                });
                Assert.Equal(1, source.MoveNextAsyncCount);
                Assert.Equal(1, source.CurrentCount);
                Assert.Equal(1, source.DisposeAsyncCount);
                Assert.Equal(1, funcCount);

                funcCount = resultCount = 0;
                source = CreateSource(2, 4, 8, 16).Track();
                Assert.Equal(144, useAsync ?
                    await source.AggregateAsync(42, (x, y) => { funcCount++; return x + y; }, x => { resultCount++; return x * 2; }) :
                    await source.AggregateAsync(42, async (x, y, ct) => { funcCount++; return x + y; }, async (x, ct) => { resultCount++; return x * 2; }));
                Assert.Equal(5, source.MoveNextAsyncCount);
                Assert.Equal(4, source.CurrentCount);
                Assert.Equal(1, source.DisposeAsyncCount);
                Assert.Equal(4, funcCount);
                Assert.Equal(1, resultCount);

                funcCount = resultCount = 0;
                source = CreateSource(2, 4, 8, 16).Track();
                await Assert.ThrowsAsync<Exception>(async () =>
                {
                    await (useAsync ?
                        source.AggregateAsync(42, (x, y) => { funcCount++; throw new Exception(); }, x => { resultCount++; return x * 2; }) :
                        source.AggregateAsync(42, async (x, y, ct) => { funcCount++; throw new Exception(); }, async (x, ct) => { resultCount++; return x * 2; }));
                });
                Assert.Equal(1, source.MoveNextAsyncCount);
                Assert.Equal(1, source.CurrentCount);
                Assert.Equal(1, source.DisposeAsyncCount);
                Assert.Equal(1, funcCount);
                Assert.Equal(0, resultCount);

                funcCount = resultCount = 0;
                source = CreateSource(2, 4, 8, 16).Track();
                await Assert.ThrowsAsync<Exception>(async () =>
                {
                    await (useAsync ?
                        source.AggregateAsync<int, int, int>(42, (x, y) => { funcCount++; return x + y; }, x => { resultCount++; throw new Exception(); }) :
                        source.AggregateAsync<int, int, int>(42, async (x, y, ct) => { funcCount++; return x + y; }, async (x, ct) => { resultCount++; throw new Exception(); }));
                });
                Assert.Equal(5, source.MoveNextAsyncCount);
                Assert.Equal(4, source.CurrentCount);
                Assert.Equal(1, source.DisposeAsyncCount);
                Assert.Equal(4, funcCount);
                Assert.Equal(1, resultCount);
            }
        }
    }
}
