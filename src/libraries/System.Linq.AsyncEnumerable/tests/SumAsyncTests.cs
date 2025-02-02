// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class SumAsyncTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.SumAsync((IAsyncEnumerable<int>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.SumAsync((IAsyncEnumerable<long>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.SumAsync((IAsyncEnumerable<float>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.SumAsync((IAsyncEnumerable<double>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.SumAsync((IAsyncEnumerable<decimal>)null));

            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.SumAsync((IAsyncEnumerable<int?>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.SumAsync((IAsyncEnumerable<long?>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.SumAsync((IAsyncEnumerable<float?>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.SumAsync((IAsyncEnumerable<double?>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.SumAsync((IAsyncEnumerable<decimal?>)null));
        }
        [Fact]
        public async Task EmptyInputs_NonNullable_Throws()
        {
            Assert.Equal(0, await AsyncEnumerable.SumAsync(AsyncEnumerable.Empty<int>()));
            Assert.Equal(0, await AsyncEnumerable.SumAsync(AsyncEnumerable.Empty<long>()));
            Assert.Equal(0, await AsyncEnumerable.SumAsync(AsyncEnumerable.Empty<float>()));
            Assert.Equal(0, await AsyncEnumerable.SumAsync(AsyncEnumerable.Empty<double>()));
            Assert.Equal(0, await AsyncEnumerable.SumAsync(AsyncEnumerable.Empty<decimal>()));

            Assert.Equal(0, await AsyncEnumerable.SumAsync(AsyncEnumerable.Empty<int?>()));
            Assert.Equal(0, await AsyncEnumerable.SumAsync(AsyncEnumerable.Empty<long?>()));
            Assert.Equal(0, await AsyncEnumerable.SumAsync(AsyncEnumerable.Empty<float?>()));
            Assert.Equal(0, await AsyncEnumerable.SumAsync(AsyncEnumerable.Empty<double?>()));
            Assert.Equal(0, await AsyncEnumerable.SumAsync(AsyncEnumerable.Empty<decimal?>()));
        }

        [Theory]
        [InlineData(new int[] { 0 })]
        [InlineData(new int[] { 1 })]
        [InlineData(new int[] { 2, 4, 8 })]
        [InlineData(new int[] { -1, 2, 5, 6, 7, 8 })]
        [InlineData(new int[] { -int.MaxValue, int.MaxValue })]
        [InlineData(new int[] { -1, -2, -3 })]
        public async Task VariousValues_MatchesEnumerable(int[] values)
        {
            foreach (IAsyncEnumerable<int> source in CreateSources(values))
            {
                Assert.Equal(values.Select(i => (int)i).Sum(), await source.Select(i => (int)i).SumAsync());
                Assert.Equal(values.Select(i => (long)i).Sum(), await source.Select(i => (long)i).SumAsync());
                Assert.Equal(values.Select(i => (float)i).Sum(), await source.Select(i => (float)i).SumAsync());
                Assert.Equal(values.Select(i => (double)i).Sum(), await source.Select(i => (double)i).SumAsync());
                Assert.Equal(values.Select(i => (decimal)i).Sum(), await source.Select(i => (decimal)i).SumAsync());

                Assert.Equal(values.Select(i => (int?)i).Sum(), await source.Select(i => (int?)i).SumAsync());
                Assert.Equal(values.Select(i => (long?)i).Sum(), await source.Select(i => (long?)i).SumAsync());
                Assert.Equal(values.Select(i => (float?)i).Sum(), await source.Select(i => (float?)i).SumAsync());
                Assert.Equal(values.Select(i => (double?)i).Sum(), await source.Select(i => (double?)i).SumAsync());
                Assert.Equal(values.Select(i => (decimal?)i).Sum(), await source.Select(i => (decimal?)i).SumAsync());

                Assert.Equal(values.Select(i => (int?)i).Sum(), await source.SelectMany<int, int?>(i => [i, null]).SumAsync());
                Assert.Equal(values.Select(i => (long?)i).Sum(), await source.SelectMany<int, long?>(i => [i, null]).SumAsync());
                Assert.Equal(values.Select(i => (float?)i).Sum(), await source.SelectMany<int, float?>(i => [i, null]).SumAsync());
                Assert.Equal(values.Select(i => (double?)i).Sum(), await source.SelectMany<int, double?>(i => [i, null]).SumAsync());
                Assert.Equal(values.Select(i => (decimal?)i).Sum(), await source.SelectMany<int, decimal?>(i => [i, null]).SumAsync());
            }
        }

        [Fact]
        public async Task Cancellation_Cancels()
        {
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await CreateSource(2, 4).Select(i => (int)i).SumAsync(new CancellationToken(true)));
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await CreateSource(2, 4).Select(i => (long)i).SumAsync(new CancellationToken(true)));
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await CreateSource(2, 4).Select(i => (float)i).SumAsync(new CancellationToken(true)));
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await CreateSource(2, 4).Select(i => (double)i).SumAsync(new CancellationToken(true)));
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await CreateSource(2, 4).Select(i => (decimal)i).SumAsync(new CancellationToken(true)));

            await Assert.ThrowsAsync<OperationCanceledException>(async () => await CreateSource(2, 4).Select(i => (int?)i).SumAsync(new CancellationToken(true)));
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await CreateSource(2, 4).Select(i => (long?)i).SumAsync(new CancellationToken(true)));
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await CreateSource(2, 4).Select(i => (float?)i).SumAsync(new CancellationToken(true)));
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await CreateSource(2, 4).Select(i => (double?)i).SumAsync(new CancellationToken(true)));
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await CreateSource(2, 4).Select(i => (decimal?)i).SumAsync(new CancellationToken(true)));
        }

        [Fact]
        public async Task InterfaceCalls_ExpectedCounts()
        {
            await Validate(source => source.Select(i => (int)i).SumAsync());
            await Validate(source => source.Select(i => (long)i).SumAsync());
            await Validate(source => source.Select(i => (float)i).SumAsync());
            await Validate(source => source.Select(i => (double)i).SumAsync());
            await Validate(source => source.Select(i => (decimal)i).SumAsync());

            await Validate(source => source.Select(i => (int?)i).SumAsync());
            await Validate(source => source.Select(i => (long?)i).SumAsync());
            await Validate(source => source.Select(i => (float?)i).SumAsync());
            await Validate(source => source.Select(i => (double?)i).SumAsync());
            await Validate(source => source.Select(i => (decimal?)i).SumAsync());

            static async Task Validate<TResult>(Func<IAsyncEnumerable<int>, ValueTask<TResult>> factory)
            {
                TrackingAsyncEnumerable<int> source;

                source = CreateSource(2, 4, 8, 16).Track();
                await factory(source);
                Assert.Equal(5, source.MoveNextAsyncCount);
                Assert.Equal(4, source.CurrentCount);
                Assert.Equal(1, source.DisposeAsyncCount);

                source = CreateSource(2, 4, 8, 16).AppendException(new FormatException()).Track();
                await Assert.ThrowsAsync<FormatException>(async () => await factory(source));
                Assert.Equal(5, source.MoveNextAsyncCount);
                Assert.Equal(4, source.CurrentCount);
                Assert.Equal(1, source.DisposeAsyncCount);
            }
        }
    }
}
