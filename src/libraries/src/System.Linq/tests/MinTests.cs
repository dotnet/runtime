// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class MinTests : EnumerableTests
    {
        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                    where x > Int32.MinValue
                    select x;

            Assert.Equal(q.Min(), q.Min());
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", String.Empty }
                    where !String.IsNullOrEmpty(x)
                    select x;

            Assert.Equal(q.Min(), q.Min());
        }

        [Fact]
        public void MinInt32()
        {
            var one = Enumerable.Range(1, 10).ToArray();
            var minusTen = new[] { -1, -10, 10, 200, 1000 };
            var hundred = new[] { 3000, 100, 200, 1000 };
            Assert.Equal(1, one.Min());
            Assert.Equal(-10, minusTen.Min());
            Assert.Equal(100, hundred.Min());
            Assert.Equal(int.MinValue, one.Concat(Enumerable.Repeat(int.MinValue, 1)).Min());
        }

        [Fact]
        public void EmptyInt32Source()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<int>().Min());
        }

        [Fact]
        public void NullInt32Source()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).Min());
        }

        [Fact]
        public void SolitaryInt32()
        {
            Assert.Equal(20, Enumerable.Repeat(20, 1).Min());
        }

        [Fact]
        public void Int32AllSame()
        {
            Assert.Equal(-2, Enumerable.Repeat(-2, 5).Min());
        }

        [Fact]
        public void Int32MinimumFirst()
        {
            int[] source = { 6, 9, 10, 7, 8 };
            Assert.Equal(source.First(), source.Min());
        }

        [Fact]
        public void Int32MinimumLast()
        {
            int[] source = { 6, 9, 10, 0, -5 };
            Assert.Equal(source.Last(), source.Min());
        }

        [Fact]
        public void Int32MinimumRepeated()
        {
            int[] source = { 6, 0, 9, 0, 10, 0 };
            Assert.Equal(0, source.Min());
        }

        [Fact]
        public void MinInt64()
        {
            var one = Enumerable.Range(1, 10).Select(i => (long)i).ToArray();
            var minusTen = new[] { -1L, -10, 10, 200, 1000 };
            var hundred = new[] { 3000L, 100, 200, 1000 };
            Assert.Equal(1, one.Min());
            Assert.Equal(-10, minusTen.Min());
            Assert.Equal(100, hundred.Min());
            Assert.Equal(long.MinValue, one.Concat(Enumerable.Repeat(long.MinValue, 1)).Min());
        }

        [Fact]
        public void EmptyInt64Source()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<long>().Min());
        }

        [Fact]
        public void NullInt64Source()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<long>)null).Min());
        }

        [Fact]
        public void SolitaryInt64()
        {
            Assert.Equal(int.MaxValue + 10L, Enumerable.Repeat(int.MaxValue + 10L, 1).Min());
        }

        [Fact]
        public void Int64AllSame()
        {
            Assert.Equal(500L, Enumerable.Repeat(500L, 5).Min());
        }

        [Fact]
        public void Int64MinimumFirst()
        {
            long[] source = { -250, 49, 130, 47, 28 };
            Assert.Equal(source.First(), source.Min());
        }

        [Fact]
        public void Int64MinimumLast()
        {
            long[] source = { 6, 9, 10, 0, -int.MaxValue - 50L };
            Assert.Equal(source.Last(), source.Min());
        }

        [Fact]
        public void Int64MinimumRepeated()
        {
            long[] source = { 6, -5, 9, -5, 10, -5 };
            Assert.Equal(-5, source.Min());
        }

        [Fact]
        public void MinSingle()
        {
            var one = Enumerable.Range(1, 10).Select(i => (float)i).ToArray();
            var minusTen = new[] { -1F, -10, 10, 200, 1000 };
            var hundred = new[] { 3000F, 100, 200, 1000 };
            Assert.Equal(1L, one.Min());
            Assert.Equal(-10L, minusTen.Min());
            Assert.Equal(100L, hundred.Min());
            Assert.Equal(float.MinValue, one.Concat(Enumerable.Repeat(float.MinValue, 1)).Min());
        }

        [Fact]
        public void NullSingleSource()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<float>)null).Min());
        }

        [Fact]
        public void EmptySingle()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<float>().Min());
        }

        [Fact]
        public void SolitarySingle()
        {
            Assert.Equal(5.5f, Enumerable.Repeat(5.5f, 1).Min());
        }

        [Fact]
        public void SingleAllSame()
        {
            Assert.True(float.IsNaN(Enumerable.Repeat(float.NaN, 5).Min()));
        }

        [Fact]
        public void SingleMinimumFirst()
        {
            float[] source = { -2.5f, 4.9f, 130f, 4.7f, 28f };
            Assert.Equal(source.First(), source.Min());
        }

        [Fact]
        public void SingleMinimumLast()
        {
            float[] source = { 6.8f, 9.4f, 10f, 0, -5.6f };
            Assert.Equal(source.Last(), source.Min());
        }

        [Fact]
        public void SingleMinimumRepeated()
        {
            float[] source = { -5.5f, float.NegativeInfinity, 9.9f, float.NegativeInfinity };
            Assert.True(float.IsNegativeInfinity(source.Min()));
        }

        [Fact]
        public void SingleNaNFirst()
        {
            float[] source = { float.NaN, 6.8f, 9.4f, 10f, 0, -5.6f };
            Assert.True(float.IsNaN(source.Min()));
        }

        [Fact]
        public void SingleNaNLast()
        {
            float[] source = { 6.8f, 9.4f, 10f, 0, -5.6f, float.NaN };
            Assert.True(float.IsNaN(source.Min()));
        }

        [Fact]
        public void SingleNaNThenNegativeInfinity()
        {
            float[] source = { float.NaN, float.NegativeInfinity };
            Assert.True(float.IsNaN(source.Min()));
        }

        [Fact]
        public void SingleNegativeInfinityThenNaN()
        {
            float[] source = { float.NegativeInfinity, float.NaN };
            Assert.True(float.IsNaN(source.Min()));
        }

        [Fact]
        public void SingleAllNaN()
        {
            Assert.True(float.IsNaN(Enumerable.Repeat(float.NaN, int.MaxValue).Min()));
        }

        [Fact]
        public void MinDouble()
        {
            var one = Enumerable.Range(1, 10).Select(i => (double)i).ToArray();
            var minusTen = new[] { -1D, -10, 10, 200, 1000 };
            var hundred = new[] { 3000D, 100, 200, 1000 };
            Assert.Equal(1D, one.Min());
            Assert.Equal(-10D, minusTen.Min());
            Assert.Equal(100D, hundred.Min());
            Assert.Equal(double.MinValue, one.Concat(Enumerable.Repeat(double.MinValue, 1)).Min());
        }

        [Fact]
        public void EmptyDoubleSource()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<double>().Min());
        }

        [Fact]
        public void NullDoubleSource()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<double>)null).Min());
        }

        [Fact]
        public void SolitaryDouble()
        {
            Assert.Equal(5.5, Enumerable.Repeat(5.5, 1).Min());
        }

        [Fact]
        public void DoubleAllNaN()
        {
            Assert.True(double.IsNaN(Enumerable.Repeat(double.NaN, int.MaxValue).Min()));
        }

        [Fact]
        public void DoubleMinimumFirst()
        {
            double[] source = { -2.5, 4.9, 130, 4.7, 28 };
            Assert.Equal(source.First(), source.Min());
        }

        [Fact]
        public void DoubleMinimumLast()
        {
            double[] source = { 6.8, 9.4, 10, 0, -5.6 };
            Assert.Equal(source.Last(), source.Min());
        }

        [Fact]
        public void DoubleMinimumRepeated()
        {
            double[] source = { -5.5, Double.NegativeInfinity, 9.9, Double.NegativeInfinity };
            Assert.True(double.IsNegativeInfinity(source.Min()));
        }

        [Fact]
        public void DoubleNaNFirst()
        {
            double[] source = { double.NaN, 6.8, 9.4, 10, 0, -5.6 };
            Assert.True(double.IsNaN(source.Min()));
        }

        [Fact]
        public void DoubleNaNLast()
        {
            double[] source = { 6.8, 9.4, 10, 0, -5.6, double.NaN };
            Assert.True(double.IsNaN(source.Min()));
        }

        [Fact]
        public void DoubleNaNThenNegativeInfinity()
        {
            double[] source = { double.NaN, double.NegativeInfinity };
            Assert.True(double.IsNaN(source.Min()));
        }

        [Fact]
        public void DoubleNegativeInfinityThenNaN()
        {
            double[] source = { double.NegativeInfinity, double.NaN };
            Assert.True(double.IsNaN(source.Min()));
        }

        [Fact]
        public void MinDecimal()
        {
            var one = Enumerable.Range(1, 10).Select(i => (decimal)i).ToArray();
            var minusTen = new[] { -1M, -10, 10, 200, 1000 };
            var hundred = new[] { 3000M, 100, 200, 1000 };
            Assert.Equal(1M, one.Min());
            Assert.Equal(-10M, minusTen.Min());
            Assert.Equal(100M, hundred.Min());
            Assert.Equal(decimal.MinValue, one.Concat(Enumerable.Repeat(decimal.MinValue, 1)).Min());
        }

        [Fact]
        public void EmptyDecimalSource()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<decimal>().Min());
        }

        [Fact]
        public void NullDecimalSource()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<decimal>)null).Min());
        }

        [Fact]
        public void SolitaryDecimal()
        {
            Assert.Equal(5.5m, Enumerable.Repeat(5.5m, 1).Min());
        }

        [Fact]
        public void DecimalAllSame()
        {
            Assert.Equal(-3.4m, Enumerable.Repeat(-3.4m, 5).Min());
        }

        [Fact]
        public void DecimalMinimumFirst()
        {
            decimal[] source = { -2.5m, 4.9m, 130m, 4.7m, 28m };
            Assert.Equal(source.First(), source.Min());
        }

        [Fact]
        public void DecimalMinimumLast()
        {
            decimal[] source = { 6.8m, 9.4m, 10m, 0m, 0m, Decimal.MinValue };
            Assert.Equal(source.Last(), source.Min());
        }

        [Fact]
        public void DecimalMinimumRepeated()
        {
            decimal[] source = { -5.5m, 0m, 9.9m, -5.5m, 5m };
            Assert.Equal(-5.5m, source.Min());
        }

        [Fact]
        public void MinNullableInt32()
        {
            var one = Enumerable.Range(1, 10).Select(i => (int?)i).ToArray();
            var minusTen = new[] { default(int?), -1, -10, 10, 200, 1000 };
            var hundred = new[] { default(int?), 3000, 100, 200, 1000 };
            Assert.Equal(1, one.Min());
            Assert.Equal(-10, minusTen.Min());
            Assert.Equal(100, hundred.Min());
            Assert.Equal(int.MinValue, one.Concat(Enumerable.Repeat((int?)int.MinValue, 1)).Min());
            Assert.Null(Enumerable.Repeat(default(int?), 100).Min());
        }

        [Fact]
        public void EmptyNullableInt32Source()
        {
            Assert.Null(Enumerable.Empty<int?>().Min());
        }

        [Fact]
        public void NullNullableInt32Source()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<int?>)null).Min());
        }

        [Fact]
        public void SolitaryNullableInt32()
        {
            Assert.Equal(20, Enumerable.Repeat((int?)20, 1).Min());
        }

        [Fact]
        public void NullableInt32AllNull()
        {
            Assert.Null(Enumerable.Repeat(default(int?), 5).Min());
        }

        [Fact]
        public void NullableInt32MinimumFirst()
        {
            int?[] source = { 6, null, 9, 10, null, 7, 8 };
            Assert.Equal(source.First(), source.Min());
        }

        [Fact]
        public void NullableInt32MinimumLast()
        {
            int?[] source = { null, null, null, null, null, -5 };
            Assert.Equal(source.Last(), source.Min());
        }

        [Fact]
        public void NullableInt32MinimumRepeated()
        {
            int?[] source = { 6, null, null, 0, 9, 0, 10, 0 };
            Assert.Equal(0, source.Min());
        }

        [Fact]
        public void MinNullableInt64()
        {
            var one = Enumerable.Range(1, 10).Select(i => (long?)i).ToArray();
            var minusTen = new[] { default(long?), -1L, -10, 10, 200, 1000 };
            var hundred = new[] { default(long?), 3000L, 100, 200, 1000 };
            Assert.Equal(1, one.Min());
            Assert.Equal(-10, minusTen.Min());
            Assert.Equal(100, hundred.Min());
            Assert.Equal(long.MinValue, one.Concat(Enumerable.Repeat((long?)long.MinValue, 1)).Min());
        }

        [Fact]
        public void EmptyNullableInt64Source()
        {
            Assert.Null(Enumerable.Empty<long?>().Min());
        }

        [Fact]
        public void NullNullableInt64Source()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<long?>)null).Min());
        }

        [Fact]
        public void SolitaryNullableInt64()
        {
            Assert.Equal(long.MaxValue, Enumerable.Repeat((long?)long.MaxValue, 1).Min());
        }

        [Fact]
        public void NullableInt64AllNull()
        {
            Assert.Null(Enumerable.Repeat(default(long?), 5).Min());
        }

        [Fact]
        public void NullableInt64MinimumFirst()
        {
            long?[] source = { long.MinValue, null, 9, 10, null, 7, 8 };
            Assert.Equal(source.First(), source.Min());
        }

        [Fact]
        public void NullableInt64MinimumLast()
        {
            long?[] source = { null, null, null, null, null, -long.MaxValue };
            Assert.Equal(source.Last(), source.Min());
        }

        [Fact]
        public void NullableInt64MinimumRepeated()
        {
            long?[] source = { 6, null, null, 0, 9, 0, 10, 0 };
            Assert.Equal(0, source.Min());
        }

        [Fact]
        public void MinNullableSingle()
        {
            var one = Enumerable.Range(1, 10).Select(i => (float?)i).ToArray();
            var minusTen = new[] { default(float?), -1F, -10, 10, 200, 1000 };
            var hundred = new[] { default(float?), 3000F, 100, 200, 1000 };
            Assert.Equal(1F, one.Min());
            Assert.Equal(-10F, minusTen.Min());
            Assert.Equal(100F, hundred.Min());
            Assert.Equal(float.MinValue, one.Concat(Enumerable.Repeat((float?)float.MinValue, 1)).Min());
        }

        [Fact]
        public void EmptyNullableSingleSource()
        {
            Assert.Null(Enumerable.Empty<float?>().Min());
        }

        [Fact]
        public void SolitaryNullableSingle()
        {
            Assert.Equal(float.MinValue, Enumerable.Repeat((float?)float.MinValue, 1).Min());
        }

        [Fact]
        public void NullableSingleAllNull()
        {
            Assert.Null(Enumerable.Repeat(default(float?), 100).Min());
        }

        [Fact]
        public void NullableSingleMinimumFirst()
        {
            float?[] source = { -4.50f, null, 10.98f, null, 7.5f, 8.6f };
            Assert.Equal(source.First(), source.Min());
        }

        [Fact]
        public void NullableSingleMinimumLast()
        {
            float?[] source = { null, null, null, null, null, 0f };
            Assert.Equal(source.Last(), source.Min());
        }

        [Fact]
        public void NullableSingleMinimumRepated()
        {
            float?[] source = { 6.4f, null, null, -0.5f, 9.4f, -0.5f, 10.9f, -0.5f };
            Assert.Equal(-0.5f, source.Min());
        }

        [Fact]
        public void NullNullableSingleSource()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<float?>)null).Min());
        }

        [Fact]
        public void NullableSingleNaNFirstWithNulls()
        {
            float?[] source = { float.NaN, 6.8f, 9.4f, 10f, 0, null, -5.6f };
            Assert.True(float.IsNaN(source.Min().Value));
        }

        [Fact]
        public void NullableSingleNaNLastWithNulls()
        {
            float?[] source = { 6.8f, 9.4f, 10f, 0, null, -5.6f, float.NaN };
            Assert.True(float.IsNaN(source.Min().Value));
        }

        [Fact]
        public void NullableSingleNaNLastWithNullsWithSelector()
        {
            float?[] source = { 6.8f, 9.4f, 10f, 0, null, -5.6f, float.NaN };
            Assert.True(float.IsNaN(source.Min(i => i).Value));
        }

        [Fact]
        public void NullableSingleNaNThenNegativeInfinity()
        {
            float?[] source = { float.NaN, float.NegativeInfinity };
            Assert.True(float.IsNaN(source.Min().Value));
        }

        [Fact]
        public void NullableSingleNegativeInfinityThenNaN()
        {
            float?[] source = { float.NegativeInfinity, float.NaN };
            Assert.True(float.IsNaN(source.Min().Value));
        }

        [Fact]
        public void NullableSingleOnlyNaN()
        {
            Assert.True(float.IsNaN(Enumerable.Repeat((float?)float.NaN, int.MaxValue).Min().Value));
        }

        [Fact]
        public void NullableSingleNaNThenNulls()
        {
            float?[] source = { float.NaN, null, null, null };
            Assert.True(float.IsNaN(source.Min().Value));
        }

        [Fact]
        public void NullableSingleNullsThenNaN()
        {
            float?[] source = { null, null, null, float.NaN };
            Assert.True(float.IsNaN(source.Min().Value));
        }

        [Fact]
        public void MinNullableDouble()
        {
            var one = Enumerable.Range(1, 10).Select(i => (double?)i).ToArray();
            var minusTen = new[] { default(double?), -1D, -10, 10, 200, 1000 };
            var hundred = new[] { default(double?), 3000D, 100, 200, 1000 };
            Assert.Equal(1D, one.Min());
            Assert.Equal(-10D, minusTen.Min());
            Assert.Equal(100D, hundred.Min());
            Assert.Equal(double.MinValue, one.Concat(Enumerable.Repeat((double?)double.MinValue, 1)).Min());
        }

        [Fact]
        public void EmptyNullableDoubleSource()
        {
            Assert.Null(Enumerable.Empty<double?>().Min());
        }

        [Fact]
        public void NullNullableDoubleSource()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<double?>)null).Min());
        }

        [Fact]
        public void SolitaryNullableDouble()
        {
            Assert.Equal(double.MinValue, Enumerable.Repeat((double?)double.MinValue, 1).Min());
        }

        [Fact]
        public void NullableDoubleAllNull()
        {
            Assert.Null(Enumerable.Repeat(default(double?), 5).Min());
        }

        [Fact]
        public void NullableDoubleMinimumFirst()
        {
            double?[] source = { -4.50, null, 10.98, null, 7.5, 8.6 };
            Assert.Equal(source.First(), source.Min());
        }

        [Fact]
        public void NullableDoubleMinimumLast()
        {
            double?[] source = { null, null, null, null, null, 0 };
            Assert.Equal(source.Last(), source.Min());
        }

        [Fact]
        public void NullableDoubleMinimumRepeated()
        {
            double?[] source = { 6.4, null, null, -0.5, 9.4, -0.5, 10.9, -0.5 };
            Assert.Equal(-0.5, source.Min());
        }

        [Fact]
        public void NullableDoubleNaNFirstWithNulls()
        {
            double?[] source = { double.NaN, 6.8, 9.4, 10.0, 0.0, null, -5.6 };
            Assert.True(double.IsNaN(source.Min().Value));
        }

        [Fact]
        public void NullableDoubleNaNLastWIthNullsWithSelector()
        {
            double?[] source = { 6.8, 9.4, 10, 0.0, null, -5.6f, double.NaN };
            Assert.True(double.IsNaN(source.Min(i => i).Value));
        }

        [Fact]
        public void NullableDoubleNaNLastWIthNulls()
        {
            double?[] source = { 6.8, 9.4, 10, 0.0, null, -5.6f, double.NaN };
            Assert.True(double.IsNaN(source.Min().Value));
        }

        [Fact]
        public void NullableDoubleNaNThenNegativeInfinity()
        {
            double?[] source = { double.NaN, double.NegativeInfinity };
            Assert.True(double.IsNaN(source.Min().Value));
        }

        [Fact]
        public void NullableDoubleNegativeInfinityThenNaN()
        {
            double?[] source = { double.NegativeInfinity, double.NaN };
            Assert.True(double.IsNaN(source.Min().Value));
        }

        [Fact]
        public void NullableDoubleOnlyNaN()
        {
            Assert.True(double.IsNaN(Enumerable.Repeat((double?)double.NaN, int.MaxValue).Min().Value));
        }

        [Fact]
        public void NullableDoubleNaNThenNulls()
        {
            double?[] source = { double.NaN, null, null, null };
            Assert.True(double.IsNaN(source.Min().Value));
        }

        [Fact]
        public void NullableDoubleNullsThenNaN()
        {
            double?[] source = { null, null, null, double.NaN };
            Assert.True(double.IsNaN(source.Min().Value));
        }

        [Fact]
        public void MinNullableDecimal()
        {
            var one = Enumerable.Range(1, 10).Select(i => (decimal?)i).ToArray();
            var minusTen = new[] { default(decimal?), -1M, -10, 10, 200, 1000 };
            var hundred = new[] { default(decimal?), 3000M, 100, 200, 1000 };
            Assert.Equal(1M, one.Min());
            Assert.Equal(-10M, minusTen.Min());
            Assert.Equal(100M, hundred.Min());
            Assert.Equal(decimal.MinValue, one.Concat(Enumerable.Repeat((decimal?)decimal.MinValue, 1)).Min());
        }

        [Fact]
        public void EmptyNullableDecimalSource()
        {
            Assert.Null(Enumerable.Empty<decimal?>().Min());
        }

        [Fact]
        public void NullNullableDecimalSource()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<decimal?>)null).Min());
        }

        [Fact]
        public void SolitaryNullableDecimal()
        {
            Assert.Equal(decimal.MaxValue, Enumerable.Repeat((decimal?)decimal.MaxValue, 1).Min());
        }

        [Fact]
        public void NullableDecimalAllNull()
        {
            Assert.Null(Enumerable.Repeat(default(decimal?), 5).Min());
        }

        [Fact]
        public void NullableDecimalMinimumFirst()
        {
            decimal?[] source = { -4.50m, null, null, 10.98m, null, 7.5m, 8.6m };
            Assert.Equal(source.First(), source.Min());
        }

        [Fact]
        public void NullableDecimalMinimumLast()
        {
            decimal?[] source = { null, null, null, null, null, 0m };
            Assert.Equal(source.Last(), source.Min());
        }

        [Fact]
        public void NullableDecimalMinimumRepeated()
        {
            decimal?[] source = { 6.4m, null, null, decimal.MinValue, 9.4m, decimal.MinValue, 10.9m, decimal.MinValue };
            Assert.Equal(decimal.MinValue, source.Min());
        }

        // Normally NaN < anything is false, as is anything < NaN
        // However, this leads to some irksome outcomes in Min and Max.
        // If we use those semantics then Min(NaN, 5.0) is NaN, but
        // Min(5.0, NaN) is 5.0!  To fix this, we impose a total
        // ordering where NaN is smaller than every value, including
        // negative infinity.
        // This behaviour must be confirmed as happening.
        // Note that further to this, null is taken as not being in a sequence at all
        // (Null will be returned if the only element in a sequence, but this will also
        // happen with an empty sequence).

        [Fact]
        public void NaNFirstSingle()
        {
            var nanThenOne = Enumerable.Range(1, 10).Select(i => (float)i).Concat(Enumerable.Repeat(float.NaN, 1)).ToArray();
            var nanThenMinusTen = new[] { -1F, -10, float.NaN, 10, 200, 1000 };
            var nanThenMinValue = new[] { float.MinValue, 3000F, 100, 200, float.NaN, 1000 };
            Assert.True(float.IsNaN(nanThenOne.Min()));
            Assert.True(float.IsNaN(nanThenMinusTen.Min()));
            Assert.True(float.IsNaN(nanThenMinValue.Min()));
            var nanWithNull = new[] { default(float?), float.NaN, default(float?) };
            Assert.True(float.IsNaN(nanWithNull.Min().Value));
        }

        [Fact]
        public void NaNFirstDouble()
        {
            var nanThenOne = Enumerable.Range(1, 10).Select(i => (double)i).Concat(Enumerable.Repeat(double.NaN, 1)).ToArray();
            var nanThenMinusTen = new[] { -1F, -10, double.NaN, 10, 200, 1000 };
            var nanThenMinValue = new[] { double.MinValue, 3000F, 100, 200, double.NaN, 1000 };
            Assert.True(double.IsNaN(nanThenOne.Min()));
            Assert.True(double.IsNaN(nanThenMinusTen.Min()));
            Assert.True(double.IsNaN(nanThenMinValue.Min()));
            var nanWithNull = new[] { default(double?), double.NaN, default(double?) };
            Assert.True(double.IsNaN(nanWithNull.Min().Value));
        }

        [Fact]
        public void MinDateTime()
        {
            var one = Enumerable.Range(1, 10).Select(i => new DateTime(2000, 1, i)).ToArray();
            var newYears = new[]
            {
                new DateTime(2000, 12, 1),
                new DateTime(2000, 1, 1),
                new DateTime(2000, 1, 12)
            };
            var hundred = new[]
            {
                new DateTime(3000, 1, 1),
                new DateTime(100, 1, 1),
                new DateTime(200, 1, 1),
                new DateTime(1000, 1, 1)
            };
            Assert.Equal(DateTime.MinValue, one.Concat(Enumerable.Repeat(DateTime.MinValue, 1)).Min());
            Assert.Equal(new DateTime(2000, 1, 1), one.Min());
            Assert.Equal(new DateTime(2000, 1, 1), newYears.Min());
            Assert.Equal(new DateTime(100, 1, 1), hundred.Min());
        }

        [Fact]
        public void EmptyDateTimeSource()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<DateTime>().Min());
        }

        [Fact]
        public void NullNullableDateTimeSource()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<DateTime>)null).Min());
        }

        [Fact]
        public void MinString()
        {
            var one = Enumerable.Range(1, 10).Select(i => i.ToString()).ToArray();
            var agents = new[] { "Alice", "Bob", "Charlie", "Eve", "Mallory", "Trent", "Victor"};
            var confusedAgents = new[] { null, "Charlie", null, "Victor", "Trent", null, "Eve", "Alice", "Mallory", "Bob" };
            Assert.Equal("1", one.Min());
            Assert.Equal("Alice", agents.Min());
            Assert.Equal("Alice", confusedAgents.Min());
        }

        [Fact]
        public void EmptyStringSource()
        {
            Assert.Null(Enumerable.Empty<string>().Min());
        }

        [Fact]
        public void NullStringSource()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<string>)null).Min());
        }

        [Fact]
        public void SolitaryString()
        {
            Assert.Equal("Hello", Enumerable.Repeat("Hello", 1).Min());
        }

        [Fact]
        public void StringAllSame()
        {
            Assert.Equal("hi", Enumerable.Repeat("hi", 5).Min());
        }

        [Fact]
        public void StringMinimumFirst()
        {
            string[] source = { "aaa", "abcd", "bark", "temp", "cat" };
            Assert.Equal(source.First(), source.Min());
        }

        [Fact]
        public void StringMinimumLast()
        {
            string[] source = { null, null, null, null, "aAa" };
            Assert.Equal(source.Last(), source.Min());
        }

        [Fact]
        public void StringMinimumRepeated()
        {
            string[] source = { "ooo", "www", "www", "ooo", "ooo", "ppp" };
            Assert.Equal("ooo", source.Min());
        }

        [Fact]
        public void StringAllNull()
        {
            Assert.Null(Enumerable.Repeat(default(string), 5).Min());
        }

        [Fact]
        public void MinInt32WithSelector()
        {
            var one = Enumerable.Range(1, 10).ToArray();
            var minusTen = new[] { -1, -10, 10, 200, 1000 };
            var hundred = new[] { 3000, 100, 200, 1000 };
            Assert.Equal(42, Enumerable.Repeat(42, 1).Min(x => x));
            Assert.Equal(1, one.Min(x => x));
            Assert.Equal(-10, minusTen.Min(x => x));
            Assert.Equal(100, hundred.Min(x => x));
            Assert.Equal(int.MinValue, one.Concat(Enumerable.Repeat(int.MinValue, 1)).Min(x => x));
        }

        [Fact]
        public void MinInt32WithSelectorAccessingProperty()
        {
            var source = new[]{
                new { name="Tim", num=10 },
                new { name="John", num=-105 },
                new { name="Bob", num=-30 }
            };
            Assert.Equal(-105, source.Min(e => e.num));
        }

        [Fact]
        public void EmptyInt32WithSelector()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<int>().Min(x => x));
        }

        [Fact]
        public void NullInt32SourceWithSelector()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).Min(i => i));
        }

        [Fact]
        public void Int32SourceWithNullSelector()
        {
            Func<int, int> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<int>().Min(selector));
        }

        [Fact]
        public void MinInt64WithSelector()
        {
            var one = Enumerable.Range(1, 10).Select(i => (long)i).ToArray();
            var minusTen = new[] { -1L, -10, 10, 200, 1000 };
            var hundred = new[] { 3000L, 100, 200, 1000 };
            Assert.Equal(42, Enumerable.Repeat(42L, 1).Min(x => x));
            Assert.Equal(1, one.Min(x => x));
            Assert.Equal(-10, minusTen.Min(x => x));
            Assert.Equal(100, hundred.Min(x => x));
            Assert.Equal(long.MinValue, one.Concat(Enumerable.Repeat(long.MinValue, 1)).Min(x => x));
        }

        [Fact]
        public void MinInt64WithSelectorAccessingProperty()
        {
            var source = new[]{
                new { name="Tim", num=10L },
                new { name="John", num=long.MinValue },
                new { name="Bob", num=-10L }
            };

            Assert.Equal(long.MinValue, source.Min(e => e.num));
        }

        [Fact]
        public void EmptyInt64WithSelector()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<long>().Min(x => x));
        }

        [Fact]
        public void NullInt64SourceWithSelector()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<long>)null).Min(i => i));
        }

        [Fact]
        public void Int64SourceWithNullSelector()
        {
            Func<long, long> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<long>().Min(selector));
        }

        [Fact]
        public void MinSingleWithSelector()
        {
            var one = Enumerable.Range(1, 10).Select(i => (float)i).ToArray();
            var minusTen = new[] { -1F, -10, 10, 200, 1000 };
            var hundred = new[] { 3000F, 100, 200, 1000 };
            Assert.Equal(42F, Enumerable.Repeat(42F, 1).Min(x => x));
            Assert.Equal(1L, one.Min(x => x));
            Assert.Equal(-10L, minusTen.Min(x => x));
            Assert.Equal(100L, hundred.Min(x => x));
            Assert.Equal(float.MinValue, one.Concat(Enumerable.Repeat(float.MinValue, 1)).Min(x => x));
        }

        [Fact]
        public void EmptySingleWithSelector()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<float>().Min(x => x));
        }

        [Fact]
        public void NullSingleSourceWithSelector()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<float>)null).Min(i => i));
        }

        [Fact]
        public void SingleSourceWithNullSelector()
        {
            Func<float, float> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<float>().Min(selector));
        }

        [Fact]
        public void MinSingleWithSelectorAccessingProperty()
        {
            var source = new []{
                new { name="Tim", num=-45.5f },
                new { name="John", num=-132.5f },
                new { name="Bob", num=20.45f }
            };
            Assert.Equal(-132.5f, source.Min(e => e.num));
        }

        [Fact]
        public void MinDoubleWithSelector()
        {
            var one = Enumerable.Range(1, 10).Select(i => (double)i).ToArray();
            var minusTen = new[] { -1D, -10, 10, 200, 1000 };
            var hundred = new[] { 3000D, 100, 200, 1000 };
            Assert.Equal(42D, Enumerable.Repeat(42D, 1).Min(x => x));
            Assert.Equal(1D, one.Min(x => x));
            Assert.Equal(-10D, minusTen.Min(x => x));
            Assert.Equal(100D, hundred.Min(x => x));
            Assert.Equal(double.MinValue, one.Concat(Enumerable.Repeat(double.MinValue, 1)).Min(x => x));
        }

        [Fact]
        public void MinDoubleWithSelectorAccessingProperty()
        {
            var source = new[]{
                new { name="Tim", num=-45.5 },
                new { name="John", num=-132.5 },
                new { name="Bob", num=20.45 }
            };
            Assert.Equal(-132.5, source.Min(e => e.num));
        }

        [Fact]
        public void EmptyDoubleWithSelector()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<double>().Min(x => x));
        }

        [Fact]
        public void NullDoubleSourceWithSelector()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<double>)null).Min(i => i));
        }

        [Fact]
        public void DoubleSourceWithNullSelector()
        {
            Func<double, double> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<double>().Min(selector));
        }

        [Fact]
        public void MinDecimalWithSelector()
        {
            var one = Enumerable.Range(1, 10).Select(i => (decimal)i).ToArray();
            var minusTen = new[] { -1M, -10, 10, 200, 1000 };
            var hundred = new[] { 3000M, 100, 200, 1000 };
            Assert.Equal(42M, Enumerable.Repeat(42M, 1).Min(x => x));
            Assert.Equal(1M, one.Min(x => x));
            Assert.Equal(-10M, minusTen.Min(x => x));
            Assert.Equal(100M, hundred.Min(x => x));
            Assert.Equal(decimal.MinValue, one.Concat(Enumerable.Repeat(decimal.MinValue, 1)).Min(x => x));
        }

        [Fact]
        public void MinDecimalWithSelectorAccessingProperty()
        {
            var source = new[]{
                new {name="Tim", num=100.45m},
                new {name="John", num=10.5m},
                new {name="Bob", num=0.05m}
            };
            Assert.Equal(0.05m, source.Min(e => e.num));
        }

        [Fact]
        public void EmptyDecimalWithSelector()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<decimal>().Min(x => x));
        }

        [Fact]
        public void NullDecimalSourceWithSelector()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<decimal>)null).Min(i => i));
        }

        [Fact]
        public void DecimalSourceWithNullSelector()
        {
            Func<decimal, decimal> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<decimal>().Min(selector));
        }

        [Fact]
        public void MinNullableInt32WithSelector()
        {
            var one = Enumerable.Range(1, 10).Select(i => (int?)i).ToArray();
            var minusTen = new[] { default(int?), -1, -10, 10, 200, 1000 };
            var hundred = new[] { default(int?), 3000, 100, 200, 1000 };
            Assert.Equal(42, Enumerable.Repeat((int?)42, 1).Min(x => x));
            Assert.Equal(1, one.Min(x => x));
            Assert.Equal(-10, minusTen.Min(x => x));
            Assert.Equal(100, hundred.Min(x => x));
            Assert.Null(Enumerable.Empty<int?>().Min(x => x));
            Assert.Equal(int.MinValue, one.Concat(Enumerable.Repeat((int?)int.MinValue, 1)).Min(x => x));
            Assert.Null(Enumerable.Repeat(default(int?), 100).Min(x => x));
        }

        [Fact]
        public void MinNullableInt32WithSelectorAccessingProperty()
        {
            var source = new[]{
                new { name="Tim", num=(int?)10 },
                new { name="John", num=default(int?) },
                new { name="Bob", num=(int?)-30 }
            };
            Assert.Equal(-30, source.Min(e => e.num));
        }

        [Fact]
        public void NullNullableInt32SourceWithSelector()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<int?>)null).Min(i => i));
        }

        [Fact]
        public void NullableInt32SourceWithNullSelector()
        {
            Func<int?, int?> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<int?>().Min(selector));
        }

        [Fact]
        public void MinNullableInt64WithSelector()
        {
            var one = Enumerable.Range(1, 10).Select(i => (long?)i).ToArray();
            var minusTen = new[] { default(long?), -1L, -10, 10, 200, 1000 };
            var hundred = new[] { default(long?), 3000L, 100, 200, 1000 };
            Assert.Equal(42, Enumerable.Repeat((long?)42, 1).Min(x => x));
            Assert.Equal(1, one.Min(x => x));
            Assert.Equal(-10, minusTen.Min(x => x));
            Assert.Equal(100, hundred.Min(x => x));
            Assert.Null(Enumerable.Empty<long?>().Min(x => x));
            Assert.Equal(long.MinValue, one.Concat(Enumerable.Repeat((long?)long.MinValue, 1)).Min(x => x));
            Assert.Null(Enumerable.Repeat(default(long?), 100).Min(x => x));
        }

        [Fact]
        public void MinNullableInt64WithSelectorAccessingProperty()
        {
            var source = new[]{
                new { name="Tim", num=default(long?) },
                new { name="John", num=(long?)long.MinValue },
                new { name="Bob", num=(long?)-10L }
            };
            Assert.Equal(long.MinValue, source.Min(e => e.num));
        }

        [Fact]
        public void NullNullableInt64SourceWithSelector()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<long?>)null).Min(i => i));
        }

        [Fact]
        public void NullableInt64SourceWithNullSelector()
        {
            Func<long?, long?> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<long?>().Min(selector));
        }

        [Fact]
        public void MinNullableSingleWithSelector()
        {
            var one = Enumerable.Range(1, 10).Select(i => (float?)i).ToArray();
            var minusTen = new[] { default(float?), -1F, -10, 10, 200, 1000 };
            var hundred = new[] { default(float?), 3000F, 100, 200, 1000 };
            Assert.Equal(42F, Enumerable.Repeat((float?)42, 1).Min(x => x));
            Assert.Equal(1F, one.Min(x => x));
            Assert.Equal(-10F, minusTen.Min(x => x));
            Assert.Equal(100F, hundred.Min(x => x));
            Assert.Null(Enumerable.Empty<float?>().Min(x => x));
            Assert.Equal(float.MinValue, one.Concat(Enumerable.Repeat((float?)float.MinValue, 1)).Min(x => x));
            Assert.Null(Enumerable.Repeat(default(float?), 100).Min(x => x));
        }

        [Fact]
        public void MinNullableSingleWithSelectorAccessingProperty()
        {
            var source = new[]{
                new {name="Tim", num=(float?)-45.5f},
                new {name="John", num=(float?)-132.5f},
                new {name="Bob", num=default(float?)}
            };

            Assert.Equal(-132.5f, source.Min(e => e.num));
        }

        [Fact]
        public void NullNullableSingleSourceWithSelector()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<float?>)null).Min(i => i));
        }

        [Fact]
        public void NullableSingleSourceWithNullSelector()
        {
            Func<float?, float?> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<float?>().Min(selector));
        }

        [Fact]
        public void MinNullableDoubleWithSelector()
        {
            var one = Enumerable.Range(1, 10).Select(i => (double?)i).ToArray();
            var minusTen = new[] { default(double?), -1D, -10, 10, 200, 1000 };
            var hundred = new[] { default(double?), 3000D, 100, 200, 1000 };
            Assert.Equal(42D, Enumerable.Repeat((double?)42, 1).Min(x => x));
            Assert.Equal(1D, one.Min(x => x));
            Assert.Equal(-10D, minusTen.Min(x => x));
            Assert.Equal(100D, hundred.Min(x => x));
            Assert.Equal(double.MinValue, one.Concat(Enumerable.Repeat((double?)double.MinValue, 1)).Min(x => x));
            Assert.Null(Enumerable.Empty<double?>().Min(x => x));
            Assert.Null(Enumerable.Repeat(default(double?), 100).Min(x => x));
        }

        [Fact]
        public void MinNullableDoubleWithSelectorAccessingProperty()
        {
            var source = new[] {
                new { name="Tim", num=(double?)-45.5 },
                new { name="John", num=(double?)-132.5 },
                new { name="Bob", num=default(double?) }
            };
            Assert.Equal(-132.5, source.Min(e => e.num));
        }
        [Fact]
        public void NullNullableDoubleSourceWithSelector()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<double?>)null).Min(i => i));
        }

        [Fact]
        public void NullableDoubleSourceWithNullSelector()
        {
            Func<double?, double?> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<double?>().Min(selector));
        }

        [Fact]
        public void MinNullableDecimalWithSelector()
        {
            var one = Enumerable.Range(1, 10).Select(i => (decimal?)i).ToArray();
            var minusTen = new[] { default(decimal?), -1M, -10, 10, 200, 1000 };
            var hundred = new[] { default(decimal?), 3000M, 100, 200, 1000 };
            Assert.Equal(42M, Enumerable.Repeat((decimal?)42, 1).Min(x => x));
            Assert.Equal(1M, one.Min(x => x));
            Assert.Equal(-10M, minusTen.Min(x => x));
            Assert.Equal(100M, hundred.Min(x => x));
            Assert.Equal(decimal.MinValue, one.Concat(Enumerable.Repeat((decimal?)decimal.MinValue, 1)).Min(x => x));
            Assert.Null(Enumerable.Empty<decimal?>().Min(x => x));
            Assert.Null(Enumerable.Repeat(default(decimal?), 100).Min(x => x));
        }

        [Fact]
        public void MinNullableDecimalWithSelectorAccessingProperty()
        {
            var source = new[]{
                new { name="Tim", num=(decimal?)100.45m },
                new { name="John", num=(decimal?)10.5m },
                new { name="Bob", num=default(decimal?) }
            };
            Assert.Equal(10.5m, source.Min(e => e.num));
        }

        [Fact]
        public void NullNullableDecimalSourceWithSelector()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<decimal?>)null).Min(i => i));
        }

        [Fact]
        public void NullableDecimalSourceWithNullSelector()
        {
            Func<decimal?, decimal?> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<decimal?>().Min(selector));
        }

        // Normally NaN < anything is false, as is anything < NaN
        // However, this leads to some irksome outcomes in Min and Max.
        // If we use those semantics then Min(NaN, 5.0) is NaN, but
        // Min(5.0, NaN) is 5.0!  To fix this, we impose a total
        // ordering where NaN is smaller than every value, including
        // negative infinity.
        // This behaviour must be confirmed as happening.
        // Note that further to this, null is taken as not being in a sequence at all
        // (Null will be returned if the only element in a sequence, but this will also
        // happen with an empty sequence).

        [Fact]
        public void NaNFirstSingleWithSelector()
        {
            var nanThenOne = Enumerable.Range(1, 10).Select(i => (float)i).Concat(Enumerable.Repeat(float.NaN, 1)).ToArray();
            var nanThenMinusTen = new[] { -1F, -10, float.NaN, 10, 200, 1000 };
            var nanThenMinValue = new[] { float.MinValue, 3000F, 100, 200, float.NaN, 1000 };
            Assert.True(float.IsNaN(nanThenOne.Min(x => x)));
            Assert.True(float.IsNaN(nanThenMinusTen.Min(x => x)));
            Assert.True(float.IsNaN(nanThenMinValue.Min(x => x)));
            var nanWithNull = new[] { default(float?), float.NaN, default(float?) };
            Assert.True(float.IsNaN(nanWithNull.Min(x => x).Value));
        }

        [Fact]
        public void NaNFirstDoubleWithSelector()
        {
            var nanThenOne = Enumerable.Range(1, 10).Select(i => (double)i).Concat(Enumerable.Repeat(double.NaN, 1)).ToArray();
            var nanThenMinusTen = new[] { -1F, -10, double.NaN, 10, 200, 1000 };
            var nanThenMinValue = new[] { double.MinValue, 3000F, 100, 200, double.NaN, 1000 };
            Assert.True(double.IsNaN(nanThenOne.Min(x => x)));
            Assert.True(double.IsNaN(nanThenMinusTen.Min(x => x)));
            Assert.True(double.IsNaN(nanThenMinValue.Min(x => x)));
            var nanWithNull = new[] { default(double?), double.NaN, default(double?) };
            Assert.True(double.IsNaN(nanWithNull.Min(x => x).Value));
        }

        [Fact]
        public void MinDateTimeWithSelector()
        {
            var one = Enumerable.Range(1, 10).Select(i => new DateTime(2000, 1, i)).ToArray();
            var newYears = new[]
            {
                new DateTime(2000, 12, 1),
                new DateTime(2000, 1, 1),
                new DateTime(2000, 1, 12)
            };
            var hundred = new[]
            {
                new DateTime(3000, 1, 1),
                new DateTime(100, 1, 1),
                new DateTime(200, 1, 1),
                new DateTime(1000, 1, 1)
            };
            Assert.Equal(DateTime.MinValue, one.Concat(Enumerable.Repeat(DateTime.MinValue, 1)).Min(x => x));
            Assert.Equal(new DateTime(2000, 1, 1), one.Min(x => x));
            Assert.Equal(new DateTime(2000, 1, 1), newYears.Min(x => x));
            Assert.Equal(new DateTime(100, 1, 1), hundred.Min(x => x));
        }

        [Fact]
        public void EmptyDateTimeWithSelector()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<DateTime>().Min(x => x));
        }

        [Fact]
        public void NullDateTimeSourceWithSelector()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<DateTime>)null).Min(i => i));
        }

        [Fact]
        public void DateTimeSourceWithNullSelector()
        {
            Func<DateTime, DateTime> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<DateTime>().Min(selector));
        }

        [Fact]
        public void MinStringWithSelector()
        {
            var one = Enumerable.Range(1, 10).Select(i => i.ToString()).ToArray();
            var agents = new[] { "Alice", "Bob", "Charlie", "Eve", "Mallory", "Trent", "Victor" };
            var confusedAgents = new[] { null, "Charlie", null, "Victor", "Trent", null, "Eve", "Alice", "Mallory", "Bob" };
            Assert.Equal("1", one.Min(x => x));
            Assert.Equal("Alice", agents.Min(x => x));
            Assert.Equal("Alice", confusedAgents.Min(x => x));
            Assert.Null(Enumerable.Empty<string>().Min(x => x));
        }

        [Fact]
        public void MinStringWitSelectorAccessingProperty()
        {
            var source = new[]{
                new { name="Tim", num=100.45m },
                new { name="John", num=10.5m },
                new { name="Bob", num=0.05m }
            };
            Assert.Equal("Bob", source.Min(e => e.name));
        }

        [Fact]
        public void NullStringSourceWithSelector()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<string>)null).Min(i => i));
        }

        [Fact]
        public void StringSourceWithNullSelector()
        {
            Func<string, string> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<string>().Min(selector));
        }

        [Fact]
        public void EmptyBooleanSource()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<bool>().Min());
        }
    }
}
