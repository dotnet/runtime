﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class LeftJoinTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("outer", () => AsyncEnumerable.LeftJoin((IAsyncEnumerable<string>)null, AsyncEnumerable.Empty<string>(), outer => outer, inner => inner, (outer, inner) => outer + inner));
            AssertExtensions.Throws<ArgumentNullException>("inner", () => AsyncEnumerable.LeftJoin(AsyncEnumerable.Empty<string>(), (IAsyncEnumerable<string>)null, outer => outer, inner => inner, (outer, inner) => outer + inner));
            AssertExtensions.Throws<ArgumentNullException>("outerKeySelector", () => AsyncEnumerable.LeftJoin(AsyncEnumerable.Empty<string>(), AsyncEnumerable.Empty<string>(), (Func<string, string>)null, inner => inner, (outer, inner) => outer + inner));
            AssertExtensions.Throws<ArgumentNullException>("innerKeySelector", () => AsyncEnumerable.LeftJoin(AsyncEnumerable.Empty<string>(), AsyncEnumerable.Empty<string>(), outer => outer, (Func<string, string>)null, (outer, inner) => outer + inner));
            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => AsyncEnumerable.LeftJoin(AsyncEnumerable.Empty<string>(), AsyncEnumerable.Empty<string>(), outer => outer, inner => inner, (Func<string, string, string>)null));

            AssertExtensions.Throws<ArgumentNullException>("outer", () => AsyncEnumerable.LeftJoin((IAsyncEnumerable<string>)null, AsyncEnumerable.Empty<string>(), async (outer, ct) => outer, async (inner, ct) => inner, async (outer, inner, ct) => outer + inner));
            AssertExtensions.Throws<ArgumentNullException>("inner", () => AsyncEnumerable.LeftJoin(AsyncEnumerable.Empty<string>(), (IAsyncEnumerable<string>)null, async (outer, ct) => outer, async (inner, ct) => inner, async (outer, inner, ct) => outer + inner));
            AssertExtensions.Throws<ArgumentNullException>("outerKeySelector", () => AsyncEnumerable.LeftJoin(AsyncEnumerable.Empty<string>(), AsyncEnumerable.Empty<string>(), (Func<string, CancellationToken, ValueTask<string>>)null, async (inner, ct) => inner, async (outer, inner, ct) => outer + inner));
            AssertExtensions.Throws<ArgumentNullException>("innerKeySelector", () => AsyncEnumerable.LeftJoin(AsyncEnumerable.Empty<string>(), AsyncEnumerable.Empty<string>(), async (outer, ct) => outer, (Func<string, CancellationToken, ValueTask<string>>)null, async (outer, inner, ct) => outer + inner));
            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => AsyncEnumerable.LeftJoin(AsyncEnumerable.Empty<string>(), AsyncEnumerable.Empty<string>(), async (outer, ct) => outer, async (inner, ct) => inner, (Func<string, string, CancellationToken, ValueTask<string>>)null));
        }

#if NET
        [Fact]
        public async Task VariousValues_MatchesEnumerable_String()
        {
            Random rand = new(42);
            foreach (int length in new[] { 0, 1, 2, 1000 })
            {
                string[] values = new string[length];
                FillRandom(rand, values);

                foreach (IAsyncEnumerable<string> source in CreateSources(values))
                {
                    await AssertEqual(
                        values.LeftJoin(values, s => s.Length > 0 ? s[0] : ' ', s => s.Length > 1 ? s[1] : ' ', (outer, inner) => outer + inner),
                        source.LeftJoin(source, s => s.Length > 0 ? s[0] : ' ', s => s.Length > 1 ? s[1] : ' ', (outer, inner) => outer + inner));

                    await AssertEqual(
                        values.LeftJoin(values, s => s.Length > 0 ? s[0] : ' ', s => s.Length > 1 ? s[1] : ' ', (outer, inner) => outer + inner),
                        source.LeftJoin(source, async (s, ct) => s.Length > 0 ? s[0] : ' ', async (s, ct) => s.Length > 1 ? s[1] : ' ', async (outer, inner, ct) => outer + inner));
                }
            }
        }
#endif

        [Fact]
        public async Task Cancellation_Cancels()
        {
            IAsyncEnumerable<int> source = CreateSource(2, 4, 8, 16);

            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                CancellationTokenSource cts = new();
                await ConsumeAsync(source.LeftJoin(source, outer =>
                {
                    cts.Cancel();
                    return outer;
                },
                inner =>
                {
                    return inner;
                },
                (outer, inner) =>
                {
                    return outer + inner;
                }).WithCancellation(cts.Token));
            });

            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                CancellationTokenSource cts = new();
                await ConsumeAsync(source.LeftJoin(source,
                async (outer, ct) =>
                {
                    Assert.Equal(cts.Token, ct);
                    await Task.Yield();
                    cts.Cancel();
                    return outer;
                },
                async (inner, ct) =>
                {
                    return inner;
                },
                async (outer, inner, ct) =>
                {
                    return outer + inner;
                }).WithCancellation(cts.Token));
            });

            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                CancellationTokenSource cts = new();
                await ConsumeAsync(source.LeftJoin(source,
                async (outer, ct) =>
                {
                    return outer;
                },
                async (inner, ct) =>
                {
                    Assert.Equal(cts.Token, ct);
                    await Task.Yield();
                    cts.Cancel();
                    return inner;
                },
                async (outer, inner, ct) =>
                {
                    return outer + inner;
                }).WithCancellation(cts.Token));
            });

            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                CancellationTokenSource cts = new();
                await ConsumeAsync(source.LeftJoin(source,
                async (outer, ct) =>
                {
                    return outer;
                },
                async (inner, ct) =>
                {
                    return inner;
                },
                async (outer, inner, ct) =>
                {
                    Assert.Equal(cts.Token, ct);
                    await Task.Yield();
                    cts.Cancel();
                    return outer + inner;
                }).WithCancellation(cts.Token));
            });
        }

        [Fact]
        public async Task InterfaceCalls_ExpectedCounts()
        {
            TrackingAsyncEnumerable<int> outer, inner;

            outer = CreateSource(2, 4, 8, 16).Track();
            inner = CreateSource(1, 2, 3, 4).Track();
            await ConsumeAsync(outer.LeftJoin(inner, outer => outer, inner => inner, (outer, inner) => outer + inner));
            Assert.Equal(5, outer.MoveNextAsyncCount);
            Assert.Equal(4, outer.CurrentCount);
            Assert.Equal(1, outer.DisposeAsyncCount);
            Assert.Equal(5, inner.MoveNextAsyncCount);
            Assert.Equal(4, inner.CurrentCount);
            Assert.Equal(1, inner.DisposeAsyncCount);

            outer = CreateSource(2, 4, 8, 16).Track();
            inner = CreateSource(1, 2, 3, 4).Track();
            await ConsumeAsync(outer.LeftJoin(inner, async (outer, ct) => outer, async (inner, ct) => inner, async (outer, inner, ct) => outer + inner));
            Assert.Equal(5, outer.MoveNextAsyncCount);
            Assert.Equal(4, outer.CurrentCount);
            Assert.Equal(1, outer.DisposeAsyncCount);
            Assert.Equal(5, inner.MoveNextAsyncCount);
            Assert.Equal(4, inner.CurrentCount);
            Assert.Equal(1, inner.DisposeAsyncCount);
        }
    }
}
