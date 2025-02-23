// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class ZipTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("first", () => AsyncEnumerable.Zip((IAsyncEnumerable<string>)null, AsyncEnumerable.Empty<int>()));
            AssertExtensions.Throws<ArgumentNullException>("second", () => AsyncEnumerable.Zip(AsyncEnumerable.Empty<string>(), (IAsyncEnumerable<int>)null));

            AssertExtensions.Throws<ArgumentNullException>("first", () => AsyncEnumerable.Zip((IAsyncEnumerable<string>)null, AsyncEnumerable.Empty<int>(), (s, i) => (s, i)));
            AssertExtensions.Throws<ArgumentNullException>("second", () => AsyncEnumerable.Zip(AsyncEnumerable.Empty<string>(), (IAsyncEnumerable<int>)null, (s, i) => (s, i)));
            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => AsyncEnumerable.Zip(AsyncEnumerable.Empty<string>(), AsyncEnumerable.Empty<int>(), (Func<string, int, DateTime>)null));

            AssertExtensions.Throws<ArgumentNullException>("first", () => AsyncEnumerable.Zip((IAsyncEnumerable<string>)null, AsyncEnumerable.Empty<int>(), async (s, i, ct) => (s, i)));
            AssertExtensions.Throws<ArgumentNullException>("second", () => AsyncEnumerable.Zip(AsyncEnumerable.Empty<string>(), (IAsyncEnumerable<int>)null, async (s, i, ct) => (s, i)));
            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => AsyncEnumerable.Zip(AsyncEnumerable.Empty<string>(), AsyncEnumerable.Empty<int>(), (Func<string, int, CancellationToken, ValueTask<DateTime>>)null));

            AssertExtensions.Throws<ArgumentNullException>("first", () => AsyncEnumerable.Zip((IAsyncEnumerable<string>)null, AsyncEnumerable.Empty<int>(), AsyncEnumerable.Empty<DateTime>()));
            AssertExtensions.Throws<ArgumentNullException>("second", () => AsyncEnumerable.Zip(AsyncEnumerable.Empty<string>(), (IAsyncEnumerable<int>)null, AsyncEnumerable.Empty<DateTime>()));
            AssertExtensions.Throws<ArgumentNullException>("third", () => AsyncEnumerable.Zip(AsyncEnumerable.Empty<string>(), AsyncEnumerable.Empty<int>(), (IAsyncEnumerable<DateTime>)null));
        }

        [Fact]
        public void Empty_ProducesEmpty() // validating an optimization / implementation detail
        {
            IAsyncEnumerable<int> empty = AsyncEnumerable.Empty<int>();
            IAsyncEnumerable<int> nonEmpty = CreateSource(1, 2, 3);

            Assert.Same(AsyncEnumerable.Empty<(int, int)>(), empty.Zip(empty));
            Assert.Same(AsyncEnumerable.Empty<(int, int)>(), empty.Zip(nonEmpty));
            Assert.Same(AsyncEnumerable.Empty<(int, int)>(), nonEmpty.Zip(empty));
            Assert.NotSame(AsyncEnumerable.Empty<(int, int)>(), nonEmpty.Zip(nonEmpty));

            Assert.Same(AsyncEnumerable.Empty<int>(), empty.Zip(empty, (i1, i2) => i1 + i2));
            Assert.Same(AsyncEnumerable.Empty<int>(), nonEmpty.Zip(empty, (i1, i2) => i1 + i2));
            Assert.Same(AsyncEnumerable.Empty<int>(), empty.Zip(nonEmpty, (i1, i2) => i1 + i2));
            Assert.NotSame(AsyncEnumerable.Empty<int>(), nonEmpty.Zip(nonEmpty, (i1, i2) => i1 + i2));

            Assert.Same(AsyncEnumerable.Empty<int>(), empty.Zip(empty, async (i1, i2, ct) => i1 + i2));
            Assert.Same(AsyncEnumerable.Empty<int>(), nonEmpty.Zip(empty, async (i1, i2, ct) => i1 + i2));
            Assert.Same(AsyncEnumerable.Empty<int>(), empty.Zip(nonEmpty, async (i1, i2, ct) => i1 + i2));
            Assert.NotSame(AsyncEnumerable.Empty<int>(), nonEmpty.Zip(nonEmpty, async (i1, i2, ct) => i1 + i2));

            Assert.Same(AsyncEnumerable.Empty<(int, int, int)>(), empty.Zip(empty, empty));
            Assert.Same(AsyncEnumerable.Empty<(int, int, int)>(), nonEmpty.Zip(empty, empty));
            Assert.Same(AsyncEnumerable.Empty<(int, int, int)>(), empty.Zip(nonEmpty, empty));
            Assert.Same(AsyncEnumerable.Empty<(int, int, int)>(), empty.Zip(empty, nonEmpty));
            Assert.Same(AsyncEnumerable.Empty<(int, int, int)>(), nonEmpty.Zip(nonEmpty, empty));
            Assert.Same(AsyncEnumerable.Empty<(int, int, int)>(), nonEmpty.Zip(empty, nonEmpty));
            Assert.Same(AsyncEnumerable.Empty<(int, int, int)>(), empty.Zip(nonEmpty, nonEmpty));
            Assert.NotSame(AsyncEnumerable.Empty<(int, int, int)>(), nonEmpty.Zip(nonEmpty, nonEmpty));
        }

        [Theory]
        [InlineData(new int[0], new int[0])]
        [InlineData(new int[0], new int[] { 42 })]
        [InlineData(new int[] { 42, 43 }, new int[0])]
        [InlineData(new int[] { 1 }, new int[] { 2, 3 })]
        [InlineData(new int[] { 2, 4, 8 }, new int[] { 3, 5 })]
        [InlineData(new int[] { 2, 4, 8 }, new int[] { 2, 4, 8 })]
        [InlineData(new int[] { 2, 4, 8 }, new int[] { 2, 5, 8 })]
        [InlineData(new int[] { -1, 2, 5, 6, 7, 8 }, new int[] { int.MinValue, int.MaxValue })]
        public async Task VariousValues_MatchesEnumerable(int[] first, int[] second)
        {
            foreach (IAsyncEnumerable<int> firstSource in CreateSources(first))
            {
                foreach (IAsyncEnumerable<int> secondSource in CreateSources(second))
                {
                    await AssertEqual(
                        first.Zip(second, (f, s) => (f, s)),
                        firstSource.Zip(secondSource, (f, s) => (f, s)));

                    await AssertEqual(
                        first.Zip(second, (f, s) => (f, s)),
                        firstSource.Zip(secondSource, async (f, s, ct) => (f, s)));

#if NET
                    await AssertEqual(
                        first.Zip(second),
                        firstSource.Zip(secondSource));

                    await AssertEqual(
                        first.Zip(second, second),
                        firstSource.Zip(secondSource, secondSource));

                    await AssertEqual(
                        first.Zip(second, first),
                        firstSource.Zip(secondSource, firstSource));
#endif
                }
            }
        }

        [Fact]
        public async Task Cancellation_Cancels()
        {
            await Validate((first, second) => first.Zip(second, (f, s) => (f, s)));
            await Validate((first, second) => first.Zip(second, async (f, s, ct) => (f, s)));
#if NET
            await Validate((first, second) => first.Zip(second));
#endif

            static async Task Validate(Func<IAsyncEnumerable<int>, IAsyncEnumerable<int>, IAsyncEnumerable<(int, int)>> factory)
            {
                IAsyncEnumerable<int> first = CreateSource(2, 4, 8, 16);
                IAsyncEnumerable<int> second = CreateSource(1, 3, 5);
                CancellationTokenSource cts = new();
                await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                {
                    await foreach ((int, int) item in factory(first, second).WithCancellation(cts.Token))
                    {
                        cts.Cancel();
                    }
                });
            }
        }

        [Fact]
        public async Task InterfaceCalls_ExpectedCounts()
        {
            TrackingAsyncEnumerable<int> first, second, third;

            first = CreateSource(2, 4, 8, 16).Track();
            second = CreateSource(1, 3, 5).Track();
            await ConsumeAsync(first.Zip(second));
            Assert.Equal(4, first.MoveNextAsyncCount);
            Assert.Equal(3, first.CurrentCount);
            Assert.Equal(1, first.DisposeAsyncCount);
            Assert.Equal(4, second.MoveNextAsyncCount);
            Assert.Equal(3, second.CurrentCount);
            Assert.Equal(1, second.DisposeAsyncCount);

            first = CreateSource(2, 4, 8, 16).Track();
            second = CreateSource(1, 3, 5).Track();
            await ConsumeAsync(first.Zip(second, (f, s) => (f, s)));
            Assert.Equal(4, first.MoveNextAsyncCount);
            Assert.Equal(3, first.CurrentCount);
            Assert.Equal(1, first.DisposeAsyncCount);
            Assert.Equal(4, second.MoveNextAsyncCount);
            Assert.Equal(3, second.CurrentCount);
            Assert.Equal(1, second.DisposeAsyncCount);

            first = CreateSource(1, 3, 5).Track();
            second = CreateSource(2, 4, 8, 16).Track();
            await ConsumeAsync(first.Zip(second, async (f, s, ct) => (f, s)));
            Assert.Equal(4, first.MoveNextAsyncCount);
            Assert.Equal(3, first.CurrentCount);
            Assert.Equal(1, first.DisposeAsyncCount);
            Assert.Equal(3, second.MoveNextAsyncCount);
            Assert.Equal(3, second.CurrentCount);
            Assert.Equal(1, second.DisposeAsyncCount);

            first = CreateSource(1, 3, 5).Track();
            second = CreateSource(2, 4, 8, 16).Track();
            third = CreateSource(42, 84).Track();
            await ConsumeAsync(first.Zip(second, third));
            Assert.Equal(3, first.MoveNextAsyncCount);
            Assert.Equal(2, first.CurrentCount);
            Assert.Equal(1, first.DisposeAsyncCount);
            Assert.Equal(3, second.MoveNextAsyncCount);
            Assert.Equal(2, second.CurrentCount);
            Assert.Equal(1, second.DisposeAsyncCount);
            Assert.Equal(3, third.MoveNextAsyncCount);
            Assert.Equal(2, third.CurrentCount);
            Assert.Equal(1, third.DisposeAsyncCount);
        }
    }
}
