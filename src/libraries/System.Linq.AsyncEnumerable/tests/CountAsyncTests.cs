// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class CountAsyncTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.CountAsync<int>(null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.CountAsync<int>(null, x => x % 2 == 0));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.CountAsync<int>(null, async (x, ct) => x % 2 == 0));

            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.LongCountAsync<int>(null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.LongCountAsync<int>(null, x => x % 2 == 0));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.LongCountAsync<int>(null, async (x, ct) => x % 2 == 0));

            AssertExtensions.Throws<ArgumentNullException>("predicate", () => AsyncEnumerable.CountAsync(AsyncEnumerable.Empty<int>(), (Func<int, bool>)null));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => AsyncEnumerable.CountAsync(AsyncEnumerable.Empty<int>(), (Func<int, CancellationToken, ValueTask<bool>>)null));

            AssertExtensions.Throws<ArgumentNullException>("predicate", () => AsyncEnumerable.LongCountAsync(AsyncEnumerable.Empty<int>(), (Func<int, bool>)null));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => AsyncEnumerable.LongCountAsync(AsyncEnumerable.Empty<int>(), (Func<int, CancellationToken, ValueTask<bool>>)null));
        }

        [Theory]
        [InlineData(new int[0])]
        [InlineData(new int[] { 1 })]
        [InlineData(new int[] { 2, 4, 8 })]
        [InlineData(new int[] { -1, 2, 5, 6, 7, 8 })]
        [InlineData(new int[] { 1, 3, 5, 7 })]
        public async Task VariousValues_MatchesEnumerable(int[] values)
        {
            foreach (IAsyncEnumerable<int> source in CreateSources(values))
            {
                Func<int, bool> predicate = x => x % 2 == 0;

                Assert.Equal(
                    values.Count(),
                    await source.CountAsync());

                Assert.Equal(
                    values.Count(predicate),
                    await source.CountAsync(predicate));

                Assert.Equal(
                    values.All(predicate),
                    await source.AllAsync(async (x, ct) =>
                    {
                        await Task.Yield();
                        return predicate(x);
                    }));

                Assert.Equal(
                    values.LongCount(),
                    await source.LongCountAsync());

                Assert.Equal(
                    values.LongCount(predicate),
                    await source.LongCountAsync(predicate));

                Assert.Equal(
                    values.LongCount(predicate),
                    await source.LongCountAsync(async (x, ct) =>
                    {
                        await Task.Yield();
                        return predicate(x);
                    }));
            }
        }

        [Fact]
        public async Task Cancellation_Cancels()
        {
            IAsyncEnumerable<int> source = CreateSource(2, 4, 8, 16);
            CancellationTokenSource cts;

            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.CountAsync(x => x < 0, new CancellationToken(true)));
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.LongCountAsync(x => x < 0, new CancellationToken(true)));

            cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.CountAsync(async (x, ct) =>
            {
                Assert.Equal(cts.Token, ct);
                await Task.Yield();
                cts.Cancel();
                return x < 0;
            }, cts.Token));

            cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.LongCountAsync(async (x, ct) =>
            {
                Assert.Equal(cts.Token, ct);
                await Task.Yield();
                cts.Cancel();
                return x < 0;
            }, cts.Token));
        }

        [Fact]
        public async Task InterfaceCalls_ExpectedCounts()
        {
            TrackingAsyncEnumerable<int> source;

            foreach (bool useLong in TrueFalseBools)
            {
                source = CreateSource(2, 4, 8, 16).Track();
                Assert.Equal(4, useLong ? await source.LongCountAsync() : await source.CountAsync());
                Assert.Equal(5, source.MoveNextAsyncCount);
                Assert.Equal(0, source.CurrentCount);
                Assert.Equal(1, source.DisposeAsyncCount);
            }

            foreach (bool useAsync in TrueFalseBools)
            {
                foreach (bool useLong in TrueFalseBools)
                {
                    source = CreateSource(2, 4, 8, 16).Track();
                    Assert.Equal(2, (useAsync, useLong) switch
                    {
                        (true, true) => await source.LongCountAsync(async (x, ct) => x > 7),
                        (true, false) => await source.CountAsync(async (x, ct) => x > 7),
                        (false, true) => await source.LongCountAsync(x => x > 7),
                        (false, false) => await source.CountAsync(x => x > 7)
                    });
                    Assert.Equal(5, source.MoveNextAsyncCount);
                    Assert.Equal(4, source.CurrentCount);
                    Assert.Equal(1, source.DisposeAsyncCount);

                    source = CreateSource(2, 4, 8, 16).Track();
                    Assert.Equal(0, (useAsync, useLong) switch
                    {
                        (true, true) => await source.LongCountAsync(async (x, ct) => x > 20),
                        (true, false) => await source.CountAsync(async (x, ct) => x > 20),
                        (false, true) => await source.LongCountAsync(x => x > 20),
                        (false, false) => await source.CountAsync(x => x > 20)
                    });
                    Assert.Equal(5, source.MoveNextAsyncCount);
                    Assert.Equal(4, source.CurrentCount);
                    Assert.Equal(1, source.DisposeAsyncCount);

                    source = CreateSource(2, 4, 8, 16).Track();
                    await Assert.ThrowsAsync<Exception>(async () =>
                    {
                        switch ((useAsync, useLong))
                        {
                            case (true, true): await source.LongCountAsync((x, ct) => throw new Exception()); break;
                            case (true, false): await source.CountAsync((x, ct) => throw new Exception()); break;
                            case (false, true): await source.LongCountAsync(x => throw new Exception()); break;
                            case (false, false): await source.CountAsync(x => throw new Exception()); break;
                        }
                    });
                    Assert.Equal(1, source.MoveNextAsyncCount);
                    Assert.Equal(1, source.CurrentCount);
                    Assert.Equal(1, source.DisposeAsyncCount);
                }
            }
        }
    }
}
