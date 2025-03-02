// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class AverageAsyncTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.AverageAsync((IAsyncEnumerable<int>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.AverageAsync((IAsyncEnumerable<long>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.AverageAsync((IAsyncEnumerable<float>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.AverageAsync((IAsyncEnumerable<double>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.AverageAsync((IAsyncEnumerable<decimal>)null));

            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.AverageAsync((IAsyncEnumerable<int?>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.AverageAsync((IAsyncEnumerable<long?>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.AverageAsync((IAsyncEnumerable<float?>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.AverageAsync((IAsyncEnumerable<double?>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.AverageAsync((IAsyncEnumerable<decimal?>)null));
        }
        [Fact]
        public async Task EmptyInputs_NonNullable_Throws()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await AsyncEnumerable.AverageAsync(AsyncEnumerable.Empty<int>()));
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await AsyncEnumerable.AverageAsync(AsyncEnumerable.Empty<long>()));
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await AsyncEnumerable.AverageAsync(AsyncEnumerable.Empty<float>()));
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await AsyncEnumerable.AverageAsync(AsyncEnumerable.Empty<double>()));
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await AsyncEnumerable.AverageAsync(AsyncEnumerable.Empty<decimal>()));

            Assert.Null(await AsyncEnumerable.AverageAsync(AsyncEnumerable.Empty<int?>()));
            Assert.Null(await AsyncEnumerable.AverageAsync(AsyncEnumerable.Empty<long?>()));
            Assert.Null(await AsyncEnumerable.AverageAsync(AsyncEnumerable.Empty<float?>()));
            Assert.Null(await AsyncEnumerable.AverageAsync(AsyncEnumerable.Empty<double?>()));
            Assert.Null(await AsyncEnumerable.AverageAsync(AsyncEnumerable.Empty<decimal?>()));
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
                Assert.Equal(values.Select(i => (int)i).Average(), await source.Select(i => (int)i).AverageAsync());
                Assert.Equal(values.Select(i => (long)i).Average(), await source.Select(i => (long)i).AverageAsync());
                Assert.Equal(values.Select(i => (float)i).Average(), await source.Select(i => (float)i).AverageAsync());
                Assert.Equal(values.Select(i => (double)i).Average(), await source.Select(i => (double)i).AverageAsync());
                Assert.Equal(values.Select(i => (decimal)i).Average(), await source.Select(i => (decimal)i).AverageAsync());

                Assert.Equal(values.Select(i => (int?)i).Average(), await source.Select(i => (int?)i).AverageAsync());
                Assert.Equal(values.Select(i => (long?)i).Average(), await source.Select(i => (long?)i).AverageAsync());
                Assert.Equal(values.Select(i => (float?)i).Average(), await source.Select(i => (float?)i).AverageAsync());
                Assert.Equal(values.Select(i => (double?)i).Average(), await source.Select(i => (double?)i).AverageAsync());
                Assert.Equal(values.Select(i => (decimal?)i).Average(), await source.Select(i => (decimal?)i).AverageAsync());

                Assert.Equal(values.Select(i => (int?)i).Average(), await source.SelectMany<int, int?>(i => [i, null]).AverageAsync());
                Assert.Equal(values.Select(i => (long?)i).Average(), await source.SelectMany<int, long?>(i => [i, null]).AverageAsync());
                Assert.Equal(values.Select(i => (float?)i).Average(), await source.SelectMany<int, float?>(i => [i, null]).AverageAsync());
                Assert.Equal(values.Select(i => (double?)i).Average(), await source.SelectMany<int, double?>(i => [i, null]).AverageAsync());
                Assert.Equal(values.Select(i => (decimal?)i).Average(), await source.SelectMany<int, decimal?>(i => [i, null]).AverageAsync());
            }
        }

        [Fact]
        public async Task Cancellation_Cancels()
        {
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await CreateSource(2, 4).Select(i => (int)i).AverageAsync(new CancellationToken(true)));
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await CreateSource(2, 4).Select(i => (long)i).AverageAsync(new CancellationToken(true)));
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await CreateSource(2, 4).Select(i => (float)i).AverageAsync(new CancellationToken(true)));
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await CreateSource(2, 4).Select(i => (double)i).AverageAsync(new CancellationToken(true)));
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await CreateSource(2, 4).Select(i => (decimal)i).AverageAsync(new CancellationToken(true)));

            await Assert.ThrowsAsync<OperationCanceledException>(async () => await CreateSource(2, 4).Select(i => (int?)i).AverageAsync(new CancellationToken(true)));
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await CreateSource(2, 4).Select(i => (long?)i).AverageAsync(new CancellationToken(true)));
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await CreateSource(2, 4).Select(i => (float?)i).AverageAsync(new CancellationToken(true)));
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await CreateSource(2, 4).Select(i => (double?)i).AverageAsync(new CancellationToken(true)));
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await CreateSource(2, 4).Select(i => (decimal?)i).AverageAsync(new CancellationToken(true)));
        }

        [Fact]
        public async Task InterfaceCalls_ExpectedCounts()
        {
            await Validate(source => source.Select(i => (int)i).AverageAsync());
            await Validate(source => source.Select(i => (long)i).AverageAsync());
            await Validate(source => source.Select(i => (float)i).AverageAsync());
            await Validate(source => source.Select(i => (double)i).AverageAsync());
            await Validate(source => source.Select(i => (decimal)i).AverageAsync());

            await Validate(source => source.Select(i => (int?)i).AverageAsync());
            await Validate(source => source.Select(i => (long?)i).AverageAsync());
            await Validate(source => source.Select(i => (float?)i).AverageAsync());
            await Validate(source => source.Select(i => (double?)i).AverageAsync());
            await Validate(source => source.Select(i => (decimal?)i).AverageAsync());

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
