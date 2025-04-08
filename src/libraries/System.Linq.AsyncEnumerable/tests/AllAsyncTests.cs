// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class AllAsyncTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.AllAsync<int>(null, x => x % 2 == 0));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => AsyncEnumerable.AllAsync(AsyncEnumerable.Empty<int>(), (Func<int, bool>)null));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => AsyncEnumerable.AllAsync(AsyncEnumerable.Empty<int>(), (Func<int, CancellationToken, ValueTask<bool>>)null));
        }

        [Theory]
        [InlineData(new int[0])]
        [InlineData(new int[] { 1 })]
        [InlineData(new int[] { 2, 4, 8 })]
        [InlineData(new int[] { -1, 2, 5, 6, 7, 8 })]
        public async Task VariousValues_MatchesEnumerable(int[] values)
        {
            foreach (IAsyncEnumerable<int> source in CreateSources(values))
            {
                Func<int, bool> predicate = x => x % 2 == 0;

                Assert.Equal(
                    values.All(predicate),
                    await source.AllAsync(predicate));

                Assert.Equal(
                    values.All(predicate),
                    await source.AllAsync(async (x, ct) =>
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

            CancellationTokenSource cts = new();
            cts.Cancel();
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.AllAsync(x => x % 2 == 0, cts.Token));

            cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.AllAsync(async (x, ct) =>
            {
                Assert.Equal(cts.Token, ct);
                await Task.Yield();
                cts.Cancel();
                return x % 2 == 0;
            }, cts.Token));
        }

        [Fact]
        public async Task InterfaceCalls_ExpectedCounts()
        {
            TrackingAsyncEnumerable<int> source;

            foreach (bool useAsync in TrueFalseBools)
            {
                source = CreateSource(2, 4, 8, 16).Track();
                Assert.True(useAsync ?
                    await source.AllAsync(x => x % 2 == 0) :
                    await source.AllAsync(async (x, ct) => x % 2 == 0));
                Assert.Equal(5, source.MoveNextAsyncCount);
                Assert.Equal(4, source.CurrentCount);
                Assert.Equal(1, source.DisposeAsyncCount);

                source = CreateSource(2, 4, 8, 16).Track();
                Assert.False(useAsync ?
                    await source.AllAsync(x => x < 4) :
                    await source.AllAsync(async (x, ct) => x < 4));
                Assert.Equal(2, source.MoveNextAsyncCount);
                Assert.Equal(2, source.CurrentCount);
                Assert.Equal(1, source.DisposeAsyncCount);

                source = CreateSource(2, 4, 8, 16).Track();
                await Assert.ThrowsAsync<Exception>(async () =>
                {
                    await (useAsync ?
                        source.AllAsync(x => throw new Exception()) :
                        source.AllAsync(async (x, ct) => throw new Exception()));
                });
                Assert.Equal(1, source.MoveNextAsyncCount);
                Assert.Equal(1, source.CurrentCount);
                Assert.Equal(1, source.DisposeAsyncCount);
            }
        }
    }
}
