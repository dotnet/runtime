// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class SkipWhileTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.SkipWhile((IAsyncEnumerable<int>)null, i => i % 2 == 0));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.SkipWhile((IAsyncEnumerable<int>)null, (i, index) => i % 2 == 0));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.SkipWhile((IAsyncEnumerable<int>)null, async (i, ct) => i % 2 == 0));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.SkipWhile((IAsyncEnumerable<int>)null, async (i, index, ct) => i % 2 == 0));

            AssertExtensions.Throws<ArgumentNullException>("predicate", () => AsyncEnumerable.SkipWhile(AsyncEnumerable.Empty<int>(), (Func<int, bool>)null));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => AsyncEnumerable.SkipWhile(AsyncEnumerable.Empty<int>(), (Func<int, int, bool>)null));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => AsyncEnumerable.SkipWhile(AsyncEnumerable.Empty<int>(), (Func<int, CancellationToken, ValueTask<bool>>)null));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => AsyncEnumerable.SkipWhile(AsyncEnumerable.Empty<int>(), (Func<int, int, CancellationToken, ValueTask<bool>>)null));
        }

        [Fact]
        public void Empty_ProducesEmpty() // validating an optimization / implementation detail
        {
            Assert.Same(AsyncEnumerable.Empty<string>(), AsyncEnumerable.Empty<string>().SkipWhile(i => true));
            Assert.Same(AsyncEnumerable.Empty<string>(), AsyncEnumerable.Empty<string>().SkipWhile((i, index) => true));
            Assert.Same(AsyncEnumerable.Empty<string>(), AsyncEnumerable.Empty<string>().SkipWhile(async (i, ct) => true));
            Assert.Same(AsyncEnumerable.Empty<string>(), AsyncEnumerable.Empty<string>().SkipWhile(async (i, index, ct) => true));
        }

        [Theory]
        [InlineData(new int[0])]
        [InlineData(new int[] { 42 })]
        [InlineData(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 })]
        [InlineData(new int[] { -1, 1, -2, 2, -10, 10 })]
        [InlineData(new int[] { int.MinValue, int.MaxValue })]
        public async Task VariousValues_MatchesEnumerable(int[] ints)
        {
            foreach (IAsyncEnumerable<int> source in CreateSources(ints))
            {
                foreach (bool b in TrueFalseBools)
                {
                    await AssertEqual(
                        ints.SkipWhile(i => b),
                        source.SkipWhile(i => b));

                    await AssertEqual(
                        ints.SkipWhile(i => b),
                        source.SkipWhile(async (i, ct) => b));

                    await AssertEqual(
                        ints.SkipWhile((i, index) => b),
                        source.SkipWhile((i, index) => b));

                    await AssertEqual(
                        ints.SkipWhile((i, index) => b),
                        source.SkipWhile(async (i, index, ct) => b));
                }

                await AssertEqual(
                    ints.SkipWhile((i, index) => index < 2),
                    source.SkipWhile((i, index) => index < 2));

                await AssertEqual(
                    ints.SkipWhile((i, index) => index < 2),
                    source.SkipWhile(async (i, index, ct) => index < 2));
            }
        }

        [Fact]
        public async Task Cancellation_Cancels()
        {
            IAsyncEnumerable<int> source = CreateSource(2, 4, 8, 16);
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await ConsumeAsync(source.SkipWhile(i => true).WithCancellation(new CancellationToken(true))));

            CancellationTokenSource cts;

            cts = new CancellationTokenSource();
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await ConsumeAsync(source.SkipWhile(i =>
            {
                cts.Cancel();
                return true;
            }).WithCancellation(cts.Token)));

            cts = new CancellationTokenSource();
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await ConsumeAsync(source.SkipWhile(async (i, ct) =>
            {
                Assert.Equal(cts.Token, ct);
                await Task.Yield();
                cts.Cancel();
                return true;
            }).WithCancellation(cts.Token)));

            cts = new CancellationTokenSource();
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await ConsumeAsync(source.SkipWhile((i, index) =>
            {
                cts.Cancel();
                return true;
            }).WithCancellation(cts.Token)));

            cts = new CancellationTokenSource();
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await ConsumeAsync(source.SkipWhile(async (i, index, ct) =>
            {
                Assert.Equal(cts.Token, ct);
                await Task.Yield();
                cts.Cancel();
                return true;
            }).WithCancellation(cts.Token)));
        }

        [Fact]
        public async Task InterfaceCalls_ExpectedCounts()
        {
            TrackingAsyncEnumerable<int> source;

            foreach (bool useAsync in TrueFalseBools)
            {
                foreach (bool useIndex in TrueFalseBools)
                {
                    foreach (bool trueFalse in TrueFalseBools)
                    {
                        source = CreateSource(1, 2, 3, 4).Track();
                        await ConsumeAsync((useAsync, useIndex) switch
                        {
                            (false, false) => source.SkipWhile(i => trueFalse),
                            (false, true) => source.SkipWhile((i, index) => trueFalse),
                            (true, false) => source.SkipWhile(async (i, ct) => trueFalse),
                            (true, true) => source.SkipWhile(async (i, index, ct) => trueFalse),
                        });
                        Assert.Equal(5, source.MoveNextAsyncCount);
                        Assert.Equal(4, source.CurrentCount);
                        Assert.Equal(1, source.DisposeAsyncCount);
                    }
                }
            }
        }
    }
}
