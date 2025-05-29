// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class MinAsyncTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.MinAsync((IAsyncEnumerable<int>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.MinAsync((IAsyncEnumerable<long>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.MinAsync((IAsyncEnumerable<float>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.MinAsync((IAsyncEnumerable<double>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.MinAsync((IAsyncEnumerable<decimal>)null));

            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.MinAsync((IAsyncEnumerable<int?>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.MinAsync((IAsyncEnumerable<long?>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.MinAsync((IAsyncEnumerable<float?>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.MinAsync((IAsyncEnumerable<double?>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.MinAsync((IAsyncEnumerable<decimal?>)null));

            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.MinAsync<DateTime>(null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.MinAsync<DateTime>(null, Comparer<DateTime>.Default, default));
        }

        [Fact]
        public async Task EmptyInputs_NonNullable_Throws()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await AsyncEnumerable.MinAsync(AsyncEnumerable.Empty<int>()));
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await AsyncEnumerable.MinAsync(AsyncEnumerable.Empty<long>()));
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await AsyncEnumerable.MinAsync(AsyncEnumerable.Empty<float>()));
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await AsyncEnumerable.MinAsync(AsyncEnumerable.Empty<double>()));
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await AsyncEnumerable.MinAsync(AsyncEnumerable.Empty<decimal>()));

            Assert.Null(await AsyncEnumerable.MinAsync(AsyncEnumerable.Empty<int?>()));
            Assert.Null(await AsyncEnumerable.MinAsync(AsyncEnumerable.Empty<long?>()));
            Assert.Null(await AsyncEnumerable.MinAsync(AsyncEnumerable.Empty<float?>()));
            Assert.Null(await AsyncEnumerable.MinAsync(AsyncEnumerable.Empty<double?>()));
            Assert.Null(await AsyncEnumerable.MinAsync(AsyncEnumerable.Empty<decimal?>()));

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await AsyncEnumerable.MinAsync(AsyncEnumerable.Empty<DateTime>()));
        }

        [Theory]
        [InlineData(new int[0])]
        [InlineData(new int[] { 0 })]
        [InlineData(new int[] { 1 })]
        [InlineData(new int[] { 2, 4, 8 })]
        [InlineData(new int[] { -1, 2, 5, 6, 7, 8 })]
        [InlineData(new int[] { -1000, 1000 })]
        [InlineData(new int[] { -1, -2, -3 })]
        public async Task VariousValues_MatchesEnumerable(int[] values)
        {
            foreach (IAsyncEnumerable<int> source in CreateSources(values))
            {
                if (values.Length > 0)
                {
                    Assert.Equal(values.Select(i => (int)i).Min(), await source.Select(i => (int)i).MinAsync());
                    Assert.Equal(values.Select(i => (long)i).Min(), await source.Select(i => (long)i).MinAsync());
                    Assert.Equal(values.Select(i => (float)i).Min(), await source.Select(i => (float)i).MinAsync());
                    Assert.Equal(values.Select(i => (double)i).Min(), await source.Select(i => (double)i).MinAsync());
                    Assert.Equal(values.Select(i => (decimal)i).Min(), await source.Select(i => (decimal)i).MinAsync());

#if NET
                    Assert.Equal(values.Select(i => (int)i).Min(Comparer<int>.Create((x, y) => y.CompareTo(x))), await source.Select(i => (int)i).MinAsync(Comparer<int>.Create((x, y) => y.CompareTo(x))));
                    Assert.Equal(values.Select(i => (long)i).Min(Comparer<long>.Create((x, y) => y.CompareTo(x))), await source.Select(i => (long)i).MinAsync(Comparer<long>.Create((x, y) => y.CompareTo(x))));
                    Assert.Equal(values.Select(i => (float)i).Min(Comparer<float>.Create((x, y) => y.CompareTo(x))), await source.Select(i => (float)i).MinAsync(Comparer<float>.Create((x, y) => y.CompareTo(x))));
                    Assert.Equal(values.Select(i => (double)i).Min(Comparer<double>.Create((x, y) => y.CompareTo(x))), await source.Select(i => (double)i).MinAsync(Comparer<double>.Create((x, y) => y.CompareTo(x))));
                    Assert.Equal(values.Select(i => (decimal)i).Min(Comparer<decimal>.Create((x, y) => y.CompareTo(x))), await source.Select(i => (decimal)i).MinAsync(Comparer<decimal>.Create((x, y) => y.CompareTo(x))));
#endif

                    Assert.Equal(values.Select(i => TimeSpan.FromSeconds(i)).Min(), await source.Select(i => TimeSpan.FromSeconds(i)).MinAsync());
                }

                Assert.Equal(values.Select(i => (int?)i).Min(), await source.Select(i => (int?)i).MinAsync());
                Assert.Equal(values.Select(i => (long?)i).Min(), await source.Select(i => (long?)i).MinAsync());
                Assert.Equal(values.Select(i => (float?)i).Min(), await source.Select(i => (float?)i).MinAsync());
                Assert.Equal(values.Select(i => (double?)i).Min(), await source.Select(i => (double?)i).MinAsync());
                Assert.Equal(values.Select(i => (decimal?)i).Min(), await source.Select(i => (decimal?)i).MinAsync());

#if NET
                Assert.Equal(values.Select(i => (int?)i).Min(Comparer<int?>.Create((x, y) => Nullable.Compare(y, x))), await source.Select(i => (int?)i).MinAsync(Comparer<int?>.Create((x, y) => Nullable.Compare(y, x))));
                Assert.Equal(values.Select(i => (long?)i).Min(Comparer<long?>.Create((x, y) => Nullable.Compare(y, x))), await source.Select(i => (long?)i).MinAsync(Comparer<long?>.Create((x, y) => Nullable.Compare(y, x))));
                Assert.Equal(values.Select(i => (float?)i).Min(Comparer<float?>.Create((x, y) => Nullable.Compare(y, x))), await source.Select(i => (float?)i).MinAsync(Comparer<float?>.Create((x, y) => Nullable.Compare(y, x))));
                Assert.Equal(values.Select(i => (double?)i).Min(Comparer<double?>.Create((x, y) => Nullable.Compare(y, x))), await source.Select(i => (double?)i).MinAsync(Comparer<double?>.Create((x, y) => Nullable.Compare(y, x))));
                Assert.Equal(values.Select(i => (decimal?)i).Min(Comparer<decimal?>.Create((x, y) => Nullable.Compare(y, x))), await source.Select(i => (decimal?)i).MinAsync(Comparer<decimal?>.Create((x, y) => Nullable.Compare(y, x))));
#endif

                // With NaNs
                foreach (double[] special in new double[][] { [double.NaN, double.NaN], [1.0, double.NaN], [double.NaN, 1.0] })
                {
                    Assert.Equal(
                        special.Select(d => (float)d).Concat(values.Select(i => (float)i)).Concat(special.Select(d => (float)d)).Min(),
                        await special.Select(d => (float)d).ToAsyncEnumerable().Concat(source.Select(i => (float)i)).Concat(special.Select(d => (float)d).ToAsyncEnumerable()).MinAsync());
                    Assert.Equal(
                        special.Concat(values.Select(i => (double)i)).Concat(special).Min(),
                        await special.ToAsyncEnumerable().Concat(source.Select(i => (double)i)).Concat(special.ToAsyncEnumerable()).MinAsync());
                    Assert.Equal(
                        special.Select(d => (float?)d).Concat(values.Select(i => (float?)i)).Concat(special.Select(d => (float?)d)).Min(),
                        await special.Select(d => (float?)d).ToAsyncEnumerable().Concat(source.Select(i => (float?)i)).Concat(special.Select(d => (float?)d).ToAsyncEnumerable()).MinAsync());
                    Assert.Equal(
                        special.Select(d => (double?)d).Concat(values.Select(i => (double?)i)).Concat(special.Select(d => (double?)d)).Min(),
                        await special.Select(d => (double?)d).ToAsyncEnumerable().Concat(source.Select(i => (double?)i)).Concat(special.Select(d => (double?)d).ToAsyncEnumerable()).MinAsync());
                }

                // With nulls
                Assert.Equal(
                    new float?[] { null, null }.Concat(values.Select(i => (float?)i)).Concat(new float?[] { null, null }).Min(),
                    await new float?[] { null, null }.ToAsyncEnumerable().Concat(source.Select(i => (float?)i)).Concat(new float?[] { null, null }.ToAsyncEnumerable()).MinAsync());
                Assert.Equal(
                    new double?[] { null, null }.Concat(values.Select(i => (double?)i)).Concat(new double?[] { null, null }).Min(),
                    await new double?[] { null, null }.ToAsyncEnumerable().Concat(source.Select(i => (double?)i)).Concat(new double?[] { null, null }.ToAsyncEnumerable()).MinAsync());
            }
        }

        [Fact]
        public async Task Cancellation_Cancels()
        {
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await CreateSource(2, 4).Select(i => (int)i).MinAsync(null, new CancellationToken(true)));
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await CreateSource(2, 4).Select(i => (long)i).MinAsync(null, new CancellationToken(true)));
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await CreateSource(2, 4).Select(i => (float)i).MinAsync(null, new CancellationToken(true)));
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await CreateSource(2, 4).Select(i => (double)i).MinAsync(null, new CancellationToken(true)));
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await CreateSource(2, 4).Select(i => (decimal)i).MinAsync(null, new CancellationToken(true)));

            await Assert.ThrowsAsync<OperationCanceledException>(async () => await CreateSource(2, 4).Select(i => (int?)i).MinAsync(null, new CancellationToken(true)));
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await CreateSource(2, 4).Select(i => (long?)i).MinAsync(null, new CancellationToken(true)));
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await CreateSource(2, 4).Select(i => (float?)i).MinAsync(null, new CancellationToken(true)));
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await CreateSource(2, 4).Select(i => (double?)i).MinAsync(null, new CancellationToken(true)));
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await CreateSource(2, 4).Select(i => (decimal?)i).MinAsync(null, new CancellationToken(true)));

            await Assert.ThrowsAsync<OperationCanceledException>(async () => await CreateSource(2, 4).Select(i => TimeSpan.FromSeconds(i)).MinAsync(null, new CancellationToken(true)));
        }

        [Fact]
        public async Task InterfaceCalls_ExpectedCounts()
        {
            await Validate(source => source.Select(i => (int)i).MinAsync());
            await Validate(source => source.Select(i => (long)i).MinAsync());
            await Validate(source => source.Select(i => (float)i).MinAsync());
            await Validate(source => source.Select(i => (double)i).MinAsync());
            await Validate(source => source.Select(i => (decimal)i).MinAsync());

            await Validate(source => source.Select(i => (int?)i).MinAsync());
            await Validate(source => source.Select(i => (long?)i).MinAsync());
            await Validate(source => source.Select(i => (float?)i).MinAsync());
            await Validate(source => source.Select(i => (double?)i).MinAsync());
            await Validate(source => source.Select(i => (decimal?)i).MinAsync());

            await Validate(source => source.Select(i => TimeSpan.FromSeconds(i)).MinAsync());

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
