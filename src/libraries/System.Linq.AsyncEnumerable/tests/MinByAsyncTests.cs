// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class MinByAsyncTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.MinByAsync((IAsyncEnumerable<int>)null, i => i * 2));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.MinByAsync((IAsyncEnumerable<int>)null, async (i, ct) => i * 2));

            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => AsyncEnumerable.MinByAsync(AsyncEnumerable.Empty<int>(), (Func<int, int>)null));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => AsyncEnumerable.MinByAsync(AsyncEnumerable.Empty<int>(), (Func<int, CancellationToken, ValueTask<int>>)null));
        }

        [Fact]
        public async Task EmptyInputs_ThrowsForNonNullableTypes()
        {
            await AsyncEnumerable.Empty<string>().MinByAsync(i => i.Length);
            await AsyncEnumerable.Empty<string>().MinByAsync(async (i, ct) => i.Length);

            await AsyncEnumerable.Empty<int?>().MinByAsync(i => i);
            await AsyncEnumerable.Empty<int?>().MinByAsync(async (i, ct) => i);

            ValueTask<int> result;

            result = AsyncEnumerable.Empty<int>().MinByAsync(i => i);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await result);

            result = AsyncEnumerable.Empty<int>().MinByAsync(async (i, ct) => i);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await result);
        }

#if NET
        [Theory]
        [InlineData(new int[] { 0 })]
        [InlineData(new int[] { 1 })]
        [InlineData(new int[] { 2, 4, 8 })]
        [InlineData(new int[] { -1, 2, 5, 6, 7, 8 })]
        [InlineData(new int[] { 1, 8, 2, 7, 3, 6, 4, 5 })]
        [InlineData(new int[] { -1000, 1000 })]
        [InlineData(new int[] { -1, -2, -3 })]
        public async Task VariousValues_MatchesEnumerable(int[] values)
        {
            foreach (IAsyncEnumerable<int> source in CreateSources(values))
            {
                foreach (IComparer<int> comparer in new[] { null, Comparer<int>.Default, Comparer<int>.Create((x, y) => y.CompareTo(x)) })
                {
                    Assert.Equal(
                        values.MinBy(i => -i, comparer),
                        await source.MinByAsync(i => -i, comparer));

                    Assert.Equal(
                        values.MinBy(i => -i, comparer),
                        await source.MinByAsync(async (i, ct) => -i, comparer));
                }

                foreach (IComparer<string> comparer in new IComparer<string>[] { null, Comparer<string>.Default, StringComparer.OrdinalIgnoreCase })
                {
                    Assert.Equal(
                        values.Select(i => i.ToString()).MinBy(s => s.ToLower(), comparer),
                        await source.Select(i => i.ToString()).MinByAsync(s => s.ToLower(), comparer));

                    Assert.Equal(
                        values.Select(i => i.ToString()).MinBy(s => s.ToLower(), comparer),
                        await source.Select(i => i.ToString()).MinByAsync(async (s, ct) => s.ToLower(), comparer));
                }

                foreach (IComparer<string> comparer in new IComparer<string>[] { null, Comparer<string>.Default, StringComparer.OrdinalIgnoreCase })
                {
                    Assert.Equal(
                        values.Select(i => i.ToString()).MinBy(s => null, comparer),
                        await source.Select(i => i.ToString()).MinByAsync(s => null, comparer));

                    Assert.Equal(
                        values.Select(i => i.ToString()).MinBy(s => s.CompareTo("3") < 0 ? null : s, comparer),
                        await source.Select(i => i.ToString()).MinByAsync(s => s.CompareTo("3") < 0 ? null : s, comparer));

                    Assert.Equal(
                        values.Select(i => i.ToString()).MinBy(s => null, comparer),
                        await source.Select(i => i.ToString()).MinByAsync(async (s, ct) => null, comparer));

                    Assert.Equal(
                        values.Select(i => i.ToString()).MinBy(s => s.CompareTo("3") < 0 ? null : s, comparer),
                        await source.Select(i => i.ToString()).MinByAsync(async (s, ct) => s.CompareTo("3") < 0 ? null : s, comparer));
                }
            }
        }
#endif

        [Fact]
        public async Task Cancellation_Cancels()
        {
            TrackingAsyncEnumerable<int> source = CreateSource(2, 4).Track();

            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                CancellationTokenSource cts = new();
                await source.MinByAsync(i =>
                {
                    cts.Cancel();
                    return i;
                }, null, cts.Token);
            });

            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                CancellationTokenSource cts = new();
                await source.MinByAsync(async (i, ct) =>
                {
                    Assert.Equal(cts.Token, ct);
                    await Task.Yield();
                    cts.Cancel();
                    return i;
                }, null, cts.Token);
            });
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task InterfaceCalls_ExpectedCounts(bool useAsync)
        {
            TrackingAsyncEnumerable<int> source;
            int keySelectorCount;

            keySelectorCount = 0;
            source = CreateSource(2, 4, 8, 16).Track();
            await (useAsync ?
                source.MinByAsync(async (i, ct) =>
                {
                    keySelectorCount++;
                    return i;
                }) :
                source.MinByAsync(i =>
                {
                    keySelectorCount++;
                    return i;
                }));
            Assert.Equal(5, source.MoveNextAsyncCount);
            Assert.Equal(4, source.CurrentCount);
            Assert.Equal(1, source.DisposeAsyncCount);
            Assert.Equal(4, keySelectorCount);
        }
    }
}
