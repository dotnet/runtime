// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class MaxTests : EnumerableTests
    {
        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                    where x > Int32.MinValue
                    select x;

            Assert.Equal(q.Max(), q.Max());
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", String.Empty }
                    where !String.IsNullOrEmpty(x)
                    select x;

            Assert.Equal(q.Max(), q.Max());
        }

        [Fact]
        public void MaxInt32()
        {
            var ten = Enumerable.Range(1, 10).ToArray();
            var minusTen = new[] { -100, -15, -50, -10 };
            var thousand = new[] { -16, 0, 50, 100, 1000 };
            Assert.Equal(10, ten.Max());
            Assert.Equal(-10, minusTen.Max());
            Assert.Equal(1000, thousand.Max());
            Assert.Equal(int.MaxValue, thousand.Concat(Enumerable.Repeat(int.MaxValue, 1)).Max());
        }

        [Fact]
        public void NullInt32Source()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).Max());
        }

        [Fact]
        public void EmptyInt32()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<int>().Max());
        }

        [Fact]
        public void SolitaryInt32()
        {
            Assert.Equal(20, Enumerable.Repeat(20, 1).Max());
        }

        [Fact]
        public void RepeatedInt32()
        {
            Assert.Equal(-2, Enumerable.Repeat(-2, 5).Max());
        }

        [Fact]
        public void Int32FirstMax()
        {
            int[] source = { 16, 9, 10, 7, 8 };
            Assert.Equal(source.First(), source.Max());
        }

        [Fact]
        public void Int32LastMax()
        {
            int[] source = { 6, 9, 10, 0, 50 };
            Assert.Equal(source.Last(), source.Max());
        }

        [Fact]
        public void Int32MaxRepeated()
        {
            int[] source = { -6, 0, -9, 0, -10, 0 };
            Assert.Equal(0, source.Max());
        }

        [Fact]
        public void MaxInt64()
        {
            var ten = Enumerable.Range(1, 10).Select(i => (long)i).ToArray();
            var minusTen = new[] { -100L, -15, -50, -10 };
            var thousand = new[] { -16L, 0, 50, 100, 1000 };
            Assert.Equal(42, Enumerable.Repeat(42L, 1).Max());
            Assert.Equal(10, ten.Max());
            Assert.Equal(-10, minusTen.Max());
            Assert.Equal(1000, thousand.Max());
            Assert.Equal(long.MaxValue, thousand.Concat(Enumerable.Repeat(long.MaxValue, 1)).Max());
        }

        [Fact]
        public void NullInt64Source()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<long>)null).Max());
        }

        [Fact]
        public void EmptyInt64()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<long>().Max());
        }

        [Fact]
        public void SolitaryInt64()
        {
            Assert.Equal(Int32.MaxValue + 10L, Enumerable.Repeat(Int32.MaxValue + 10L, 1).Max());
        }

        [Fact]
        public void Int64AllEqual()
        {
            Assert.Equal(500L, Enumerable.Repeat(500L, 5).Max());
        }

        [Fact]
        public void Int64MaximumFirst()
        {
            long[] source = { 250, 49, 130, 47, 28 };
            Assert.Equal(source.First(), source.Max());
        }

        [Fact]
        public void Int64MaximumLast()
        {
            long[] source = { 6, 9, 10, 0, Int32.MaxValue + 50L };
            Assert.Equal(source.Last(), source.Max());
        }

        [Fact]
        public void Int64MaxRepeated()
        {
            long[] source = { 6, 50, 9, 50, 10, 50 };
            Assert.Equal(50, source.Max());
        }

        [Fact]
        public void MaxSingle()
        {
            var ten = Enumerable.Range(1, 10).Select(i => (float)i).ToArray();
            var minusTen = new[] { -100F, -15, -50, -10 };
            var thousand = new[] { -16F, 0, 50, 100, 1000 };
            Assert.Equal(10F, ten.Max());
            Assert.Equal(-10F, minusTen.Max());
            Assert.Equal(1000F, thousand.Max());
            Assert.Equal(float.MaxValue, thousand.Concat(Enumerable.Repeat(float.MaxValue, 1)).Max());
        }

        [Fact]
        public void EmptySingle()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<float>().Max());
        }

        [Fact]
        public void SolitarySingle()
        {
            Assert.Equal(5.5f, Enumerable.Repeat(5.5f, 1).Max());
        }

        [Fact]
        public void Single_MaximumFirst()
        {
            float[] source = { 112.5f, 4.9f, 30f, 4.7f, 28f };
            Assert.Equal(source.First(), source.Max());
        }

        [Fact]
        public void Single_MaximumLast()
        {
            float[] source = { 6.8f, 9.4f, -10f, 0f, float.NaN, 53.6f };
            Assert.Equal(source.Last(), source.Max());
        }

        [Fact]
        public void Single_MaxRepeated()
        {
            float[] source = { -5.5f, float.PositiveInfinity, 9.9f, float.PositiveInfinity };
            Assert.True(float.IsPositiveInfinity(source.Max()));
        }

        [Fact]
        public void SeveralNaNSingle()
        {
            Assert.True(float.IsNaN(Enumerable.Repeat(float.NaN, 5).Max()));
        }

        [Fact]
        public void SeveralNaNSingleWithSelector()
        {
            Assert.True(float.IsNaN(Enumerable.Repeat(float.NaN, 5).Max(i => i)));
        }

        [Fact]
        public void SeveralNaNOrNullSingleWithSelector()
        {
            float?[] source = new float?[] { float.NaN, null, float.NaN, null };
            Assert.True(float.IsNaN(source.Max(i => i).Value));
        }

        [Fact]
        public void NullSingleSource()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<float>)null).Max());
        }

        [Fact]
        public void SingleNaNFirst()
        {
            float[] source = { float.NaN, 6.8f, 9.4f, 10f, 0, -5.6f };
            Assert.Equal(10f, source.Max());
        }

        [Fact]
        public void SingleNaNFirstWithSelector()
        {
            float[] source = { float.NaN, 6.8f, 9.4f, 10f, 0, -5.6f };
            Assert.Equal(10f, source.Max(i => i));
        }

        [Fact]
        public void SingleNaNLast()
        {
            float[] source = { 6.8f, 9.4f, 10f, 0, -5.6f, float.NaN };
            Assert.Equal(10f, source.Max());
        }

        [Fact]
        public void SingleNaNThenNegativeInfinity()
        {
            float[] source = { float.NaN, float.NegativeInfinity };
            Assert.True(float.IsNegativeInfinity(source.Max()));
        }

        [Fact]
        public void SingleNegativeInfinityThenNaN()
        {
            float[] source = { float.NegativeInfinity, float.NaN };
            Assert.True(float.IsNegativeInfinity(source.Max()));
        }

        [Fact]
        public void MaxDouble()
        {
            var ten = Enumerable.Range(1, 10).Select(i => (double)i).ToArray();
            var minusTen = new[] { -100D, -15, -50, -10 };
            var thousand = new[] { -16D, 0, 50, 100, 1000 };
            Assert.Equal(10D, ten.Max());
            Assert.Equal(-10D, minusTen.Max());
            Assert.Equal(1000D, thousand.Max());
            Assert.Equal(double.MaxValue, thousand.Concat(Enumerable.Repeat(double.MaxValue, 1)).Max());
        }

        [Fact]
        public void NullDoubleSource()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<double>)null).Max());
        }

        [Fact]
        public void EmptyDouble()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<double>().Max());
        }

        [Fact]
        public void SolitaryDouble()
        {
            Assert.Equal(5.5, Enumerable.Repeat(5.5, 1).Max());
        }

        [Fact]
        public void DoubleAllEqual()
        {
            Assert.True(double.IsNaN(Enumerable.Repeat(double.NaN, 5).Max()));
        }

        [Fact]
        public void DoubleAllNaNWithSelector()
        {
            Assert.True(double.IsNaN(Enumerable.Repeat(double.NaN, 5).Max(i => i)));
        }

        [Fact]
        public void SeveralNaNOrNullDoubleWithSelector()
        {
            double?[] source = new double?[] { double.NaN, null, double.NaN, null };
            Assert.True(double.IsNaN(source.Max(i => i).Value));
        }

        [Fact]
        public void DoubleMaximumFirst()
        {
            double[] source = { 112.5, 4.9, 30, 4.7, 28 };
            Assert.Equal(source.First(), source.Max());
        }

        [Fact]
        public void DoubleMaximumLast()
        {
            double[] source = { 6.8, 9.4, -10, 0, double.NaN, 53.6 };
            Assert.Equal(source.Last(), source.Max());
        }

        [Fact]
        public void DoubleMaximumRepeated()
        {
            double[] source = { -5.5, double.PositiveInfinity, 9.9, double.PositiveInfinity };
            Assert.True(double.IsPositiveInfinity(source.Max()));
        }

        [Fact]
        public void DoubleFirstNaN()
        {
            double[] source = { double.NaN, 6.8, 9.4, 10.5, 0, -5.6 };
            Assert.Equal(10.5, source.Max());
        }

        [Fact]
        public void DoubleLastNaN()
        {
            double[] source = { 6.8, 9.4, 10.5, 0, -5.6, double.NaN };
            Assert.Equal(10.5, source.Max());
        }

        [Fact]
        public void DoubleNaNThenNegativeInfinity()
        {
            double[] source = { double.NaN, double.NegativeInfinity };
            Assert.True(double.IsNegativeInfinity(source.Max()));
        }

        [Fact]
        public void DoubleNaNThenNegativeInfinityWithSelector()
        {
            double[] source = { double.NaN, double.NegativeInfinity };
            Assert.True(double.IsNegativeInfinity(source.Max(i => i)));
        }

        [Fact]
        public void DoubleNegativeInfinityThenNaN()
        {
            double[] source = { double.NegativeInfinity, double.NaN, };
            Assert.True(double.IsNegativeInfinity(source.Max()));
        }

        [Fact]
        public void MaxDecimal()
        {
            var ten = Enumerable.Range(1, 10).Select(i => (decimal)i).ToArray();
            var minusTen = new[] { -100M, -15, -50, -10 };
            var thousand = new[] { -16M, 0, 50, 100, 1000 };
            Assert.Equal(10M, ten.Max());
            Assert.Equal(-10M, minusTen.Max());
            Assert.Equal(1000M, thousand.Max());
            Assert.Equal(decimal.MaxValue, thousand.Concat(Enumerable.Repeat(decimal.MaxValue, 1)).Max());
        }

        [Fact]
        public void NullDecimalSource()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<decimal>)null).Max());
        }

        [Fact]
        public void EmptyDecimal()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<decimal>().Max());
        }

        [Fact]
        public void SolitaryDecimal()
        {
            Assert.Equal(5.5m, Enumerable.Repeat(5.5m, 1).Max());
        }

        [Fact]
        public void DecimalAllEqual()
        {
            Assert.Equal(-3.4m, Enumerable.Repeat(-3.4m, 5).Max());
        }

        [Fact]
        public void DecimalMaximumFirst()
        {
            decimal[] source = { 122.5m, 4.9m, 10m, 4.7m, 28m };
            Assert.Equal(source.First(), source.Max());
        }

        [Fact]
        public void DecimalMaximumLast()
        {
            decimal[] source = { 6.8m, 9.4m, 10m, 0m, 0m, Decimal.MaxValue };
            Assert.Equal(source.Last(), source.Max());
        }

        [Fact]
        public void DecimalMaximumRepeated()
        {
            decimal[] source = { -5.5m, 0m, 9.9m, -5.5m, 9.9m };
            Assert.Equal(9.9m, source.Max());
        }
        [Fact]
        public void MaxNullableInt32()
        {
            var ten = Enumerable.Range(1, 10).Select(i => (int?)i).ToArray();
            var minusTen = new[] { default(int?), -100, -15, -50, -10 };
            var thousand = new[] { default(int?), -16, 0, 50, 100, 1000 };
            Assert.Equal(10, ten.Max());
            Assert.Equal(-10, minusTen.Max());
            Assert.Equal(1000, thousand.Max());
            Assert.Equal(int.MaxValue, thousand.Concat(Enumerable.Repeat((int?)int.MaxValue, 1)).Max());
            Assert.Null(Enumerable.Repeat(default(int?), 100).Max());
        }

        [Fact]
        public void NullNullableInt32Source()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<int?>)null).Max());
        }

        [Fact]
        public void EmptyNullableInt32()
        {
            Assert.Null(Enumerable.Empty<int?>().Max());
        }

        [Fact]
        public void SolitaryNullableInt32()
        {
            Assert.Equal(-20, Enumerable.Repeat((int?)-20, 1).Max());
        }

        [Fact]
        public void NullableInt32FirstMax()
        {
            int?[] source = { -6, null, -9, -10, null, -17, -18 };
            Assert.Equal(source.First(), source.Max());
        }

        [Fact]
        public void NullableInt32LastMax()
        {
            int?[] source = { null, null, null, null, null, -5 };
            Assert.Equal(source.Last(), source.Max());
        }

        [Fact]
        public void NullableInt32MaxRepeated()
        {
            int?[] source = { 6, null, null, 100, 9, 100, 10, 100 };
            Assert.Equal(100, source.Max());
        }

        [Fact]
        public void NullableInt32AllNull()
        {
            Assert.Null(Enumerable.Repeat(default(int?), 5).Max());
        }

        [Fact]
        public void MaxNullableInt64()
        {
            var ten = Enumerable.Range(1, 10).Select(i => (long?)i).ToArray();
            var minusTen = new[] { default(long?), -100L, -15, -50, -10 };
            var thousand = new[] { default(long?), -16L, 0, 50, 100, 1000 };
            Assert.Equal(10, ten.Max());
            Assert.Equal(-10, minusTen.Max());
            Assert.Equal(1000, thousand.Max());
            Assert.Equal(long.MaxValue, thousand.Concat(Enumerable.Repeat((long?)long.MaxValue, 1)).Max());
            Assert.Null(Enumerable.Repeat(default(long?), 100).Max());
        }

        [Fact]
        public void NullNullableInt64Source()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<long?>)null).Max());
        }

        [Fact]
        public void EmptyNullableInt64()
        {
            Assert.Null(Enumerable.Empty<long?>().Max(x => x));
        }

        [Fact]
        public void SolitaryNullableInt64()
        {
            Assert.Equal(long.MaxValue, Enumerable.Repeat(long.MaxValue, 1).Max());
        }

        [Fact]
        public void NullableInt64AllNull()
        {
            Assert.Null(Enumerable.Repeat(default(long?), 5).Max());
        }

        [Fact]
        public void NullableInt64MaximumFirst()
        {
            long?[] source = { Int64.MaxValue, null, 9, 10, null, 7, 8 };
            Assert.Equal(source.First(), source.Max());
        }

        [Fact]
        public void NullableInt64MaximumLast()
        {
            long?[] source = { null, null, null, null, null, -Int32.MaxValue };
            Assert.Equal(source.Last(), source.Max());
        }

        [Fact]
        public void NullableInt64MaximumRepeated()
        {
            long?[] source = { -6, null, null, 0, -9, 0, -10, -30 };
            Assert.Equal(0, source.Max());
        }

        [Fact]
        public void MaxNullableSingle()
        {
            var ten = Enumerable.Range(1, 10).Select(i => (float?)i).ToArray();
            var minusTen = new[] { default(float?), -100F, -15, -50, -10 };
            var thousand = new[] { default(float?), -16F, 0, 50, 100, 1000 };
            Assert.Equal(10F, ten.Max());
            Assert.Equal(-10F, minusTen.Max());
            Assert.Equal(1000F, thousand.Max());
            Assert.Equal(float.MaxValue, thousand.Concat(Enumerable.Repeat((float?)float.MaxValue, 1)).Max());
        }

        [Fact]
        public void EmptyNullableSingle()
        {
            Assert.Null(Enumerable.Empty<float?>().Max());
        }

        [Fact]
        public void SolitaryNullableSingle()
        {
            Assert.Equal(float.MinValue, Enumerable.Repeat((float?)float.MinValue, 1).Max());
        }

        [Fact]
        public void NullableSingleAllNull()
        {
            Assert.Null(Enumerable.Repeat(default(float?), 5).Max());
        }

        [Fact]
        public void NullableSingleMaxFirst()
        {
            float?[] source = { 14.50f, null, float.NaN, 10.98f, null, 7.5f, 8.6f };
            Assert.Equal(source.First(), source.Max());
        }

        [Fact]
        public void NullableSingleMaxLast()
        {
            float?[] source = { null, null, null, null, null, 0f };
            Assert.Equal(source.Last(), source.Max());
        }

        [Fact]
        public void NullableSingleMaxRepeated()
        {
            float?[] source = { -6.4f, null, null, -0.5f, -9.4f, -0.5f, -10.9f, -0.5f };
            Assert.Equal(-0.5f, source.Max());
        }

        [Fact]
        public void NullNullableSingleSource()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<float?>)null).Max());
        }

        [Fact]
        public void NullableSingleNaNFirstAndContainsNull()
        {
            float?[] source = { float.NaN, 6.8f, 9.4f, 10f, 0, null, -5.6f };
            Assert.Equal(10f, source.Max());
        }

        [Fact]
        public void NullableSingleNaNLastAndContainsNull()
        {
            float?[] source = { 6.8f, 9.4f, 10f, 0, null, -5.6f, float.NaN };
            Assert.Equal(10f, source.Max());
        }

        [Fact]
        public void NullableSingleNaNThenNegativeInfinity()
        {
            float?[] source = { float.NaN, float.NegativeInfinity };
            Assert.True(float.IsNegativeInfinity(source.Max().Value));
        }

        [Fact]
        public void NullableSingleNegativeInfinityThenNaN()
        {
            float?[] source = { float.NegativeInfinity, float.NaN };
            Assert.True(float.IsNegativeInfinity(source.Max().Value));
        }

        [Fact]
        public void NullableSingleAllNaN()
        {
            Assert.True(float.IsNaN(Enumerable.Repeat((float?)float.NaN, 3).Max().Value));
        }

        [Fact]
        public void NaNFirstRestJustNulls()
        {
            float?[] source = { float.NaN, null, null, null };
            Assert.True(float.IsNaN(source.Max().Value));
        }

        [Fact]
        public void NaNLastRestJustNulls()
        {
            float?[] source = { null, null, null, float.NaN };
            Assert.True(float.IsNaN(source.Max().Value));
        }

        [Fact]
        public void MaxNullableDouble()
        {
            var ten = Enumerable.Range(1, 10).Select(i => (double?)i).ToArray();
            var minusTen = new[] { default(double?), -100D, -15, -50, -10 };
            var thousand = new[] { default(double?), -16D, 0, 50, 100, 1000 };
            Assert.Equal(10D, ten.Max());
            Assert.Equal(-10D, minusTen.Max());
            Assert.Equal(1000D, thousand.Max());
            Assert.Equal(double.MaxValue, thousand.Concat(Enumerable.Repeat((double?)double.MaxValue, 1)).Max());
        }

        [Fact]
        public void NullNullableDoubleSource()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<double?>)null).Max());
        }

        [Fact]
        public void EmptyNullableDouble()
        {
            Assert.Null(Enumerable.Empty<double?>().Max());
        }

        [Fact]
        public void SolitaryNullableDouble()
        {
            Assert.Equal(double.MinValue, Enumerable.Repeat(double.MinValue, 1).Max());
        }

        [Fact]
        public void NullableDoubleAllNull()
        {
            Assert.Null(Enumerable.Repeat(default(double?), 5).Max());
        }

        [Fact]
        public void NullableDoubleMaximumFirst()
        {
            double?[] source = { 14.50, null, Double.NaN, 10.98, null, 7.5, 8.6 };
            Assert.Equal(source.First(), source.Max());
        }

        [Fact]
        public void NullableDoubleMaximumLast()
        {
            double?[] source = { null, null, null, null, null, 0 };
            Assert.Equal(source.Last(), source.Max());
        }

        [Fact]
        public void NullableDoubleMaximumRepeated()
        {
            double?[] source = { -6.4, null, null, -0.5, -9.4, -0.5, -10.9, -0.5 };
            Assert.Equal(-0.5, source.Max());
        }

        [Fact]
        public void NullableDoubleNaNFirstAndContainsNulls()
        {
            double?[] source = { double.NaN, 6.8, 9.4, 10.5, 0, null, -5.6 };
            Assert.Equal(10.5, source.Max());
        }

        [Fact]
        public void NullableDoubleNaNLastAndContainsNulls()
        {
            double?[] source = { 6.8, 9.4, 10.8, 0, null, -5.6, double.NaN };
            Assert.Equal(10.8, source.Max());
        }

        [Fact]
        public void NullableDoubleNaNThenNegativeInfinity()
        {
            double?[] source = { double.NaN, double.NegativeInfinity };
            Assert.True(double.IsNegativeInfinity(source.Max().Value));
        }

        [Fact]
        public void NullableDoubleNegativeInfinityThenNaN()
        {
            double?[] source = { double.NegativeInfinity, double.NaN };
            Assert.True(double.IsNegativeInfinity(source.Max().Value));
        }

        [Fact]
        public void NullableDoubleOnlyNaN()
        {
            Assert.True(double.IsNaN(Enumerable.Repeat((double?)double.NaN, 3).Max().Value));
        }

        [Fact]
        public void NullableDoubleNaNThenNulls()
        {
            double?[] source = { double.NaN, null, null, null };
            Assert.True(double.IsNaN(source.Max().Value));
        }

        [Fact]
        public void NullableDoubleNullsThenNaN()
        {
            double?[] source = { null, null, null, double.NaN };
            Assert.True(double.IsNaN(source.Max().Value));
        }

        [Fact]
        public void MaxNullableDecimal()
        {
            var ten = Enumerable.Range(1, 10).Select(i => (decimal?)i).ToArray();
            var minusTen = new[] { default(decimal?), -100M, -15, -50, -10 };
            var thousand = new[] { default(decimal?), -16M, 0, 50, 100, 1000 };
            Assert.Equal(42M, Enumerable.Repeat((decimal?)42, 1).Max());
            Assert.Equal(10M, ten.Max());
            Assert.Equal(-10M, minusTen.Max());
            Assert.Equal(1000M, thousand.Max());
            Assert.Equal(decimal.MaxValue, thousand.Concat(Enumerable.Repeat((decimal?)decimal.MaxValue, 1)).Max());
        }

        [Fact]
        public void NullNullableDecimalSource()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<decimal?>)null).Max());
        }

        [Fact]
        public void EmptyNullableDecimal()
        {
            Assert.Null(Enumerable.Empty<decimal?>().Max());
        }

        [Fact]
        public void SolitaryNullableDecimal()
        {
            Assert.Equal(decimal.MaxValue, Enumerable.Repeat((decimal?)decimal.MaxValue, 1).Max());
        }

        [Fact]
        public void NullableDecimalAllNulls()
        {
            Assert.Null(Enumerable.Repeat(default(decimal?), 5).Max());
        }

        [Fact]
        public void NullableDecimalMaximumFirst()
        {
            decimal?[] source = { 14.50m, null, null, 10.98m, null, 7.5m, 8.6m };
            Assert.Equal(source.First(), source.Max());
        }

        [Fact]
        public void NullableDecimalMaximumLast()
        {
            decimal?[] source = { null, null, null, null, null, 0m };
            Assert.Equal(source.Last(), source.Max());
        }

        [Fact]
        public void NullableDecimalMaximumRepeated()
        {
            decimal?[] source = { 6.4m, null, null, decimal.MaxValue, 9.4m, decimal.MaxValue, 10.9m, decimal.MaxValue };
            Assert.Equal(decimal.MaxValue, source.Max());
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
            Assert.False(float.IsNaN(nanThenOne.Max()));
            Assert.False(float.IsNaN(nanThenMinusTen.Max()));
            Assert.False(float.IsNaN(nanThenMinValue.Max()));
            var nanWithNull = new[] { default(float?), float.NaN, default(float?) };
            Assert.True(float.IsNaN(nanWithNull.Max().Value));
        }

        [Fact]
        public void NaNFirstDouble()
        {
            var nanThenOne = Enumerable.Range(1, 10).Select(i => (double)i).Concat(Enumerable.Repeat(double.NaN, 1)).ToArray();
            var nanThenMinusTen = new[] { -1F, -10, double.NaN, 10, 200, 1000 };
            var nanThenMinValue = new[] { double.MinValue, 3000F, 100, 200, double.NaN, 1000 };
            Assert.False(double.IsNaN(nanThenOne.Max()));
            Assert.False(double.IsNaN(nanThenMinusTen.Max()));
            Assert.False(double.IsNaN(nanThenMinValue.Max()));
            var nanWithNull = new[] { default(double?), double.NaN, default(double?) };
            Assert.True(double.IsNaN(nanWithNull.Max().Value));
        }

        [Fact]
        public void MaxDateTime()
        {
            var ten = Enumerable.Range(1, 10).Select(i => new DateTime(2000, 1, i)).ToArray();
            var newYearsEve = new[]
            {
                new DateTime(2000, 12, 1),
                new DateTime(2000, 12, 31),
                new DateTime(2000, 1, 12)
            };
            var threeThousand = new[]
            {
                new DateTime(3000, 1, 1),
                new DateTime(100, 1, 1),
                new DateTime(200, 1, 1),
                new DateTime(1000, 1, 1)
            };
            Assert.Equal(new DateTime(2000, 1, 10), ten.Max());
            Assert.Equal(new DateTime(2000, 12, 31), newYearsEve.Max());
            Assert.Equal(new DateTime(3000, 1, 1), threeThousand.Max());
            Assert.Equal(DateTime.MaxValue, threeThousand.Concat(Enumerable.Repeat(DateTime.MaxValue, 1)).Max());
        }

        [Fact]
        public void EmptyDateTime()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<DateTime>().Max());
        }

        [Fact]
        public void NullDateTimeSource()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<DateTime>)null).Max());
        }

        [Fact]
        public void MaxString()
        {
            var nine = Enumerable.Range(1, 10).Select(i => i.ToString()).ToArray();
            var agents = new[] { "Alice", "Bob", "Charlie", "Eve", "Mallory", "Victor", "Trent" };
            var confusedAgents = new[] { null, "Charlie", null, "Victor", "Trent", null, "Eve", "Alice", "Mallory", "Bob" };
            Assert.Equal("9", nine.Max());
            Assert.Equal("Victor", agents.Max());
            Assert.Equal("Victor", confusedAgents.Max());
        }

        [Fact]
        public void NullStringSource()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<string>)null).Max());
        }

        [Fact]
        public void EmptyString()
        {
            Assert.Null(Enumerable.Empty<string>().Max());
        }

        [Fact]
        public void SolitaryString()
        {
            Assert.Equal("Hello", Enumerable.Repeat("Hello", 1).Max());
        }

        [Fact]
        public void StringAllSame()
        {
            Assert.Equal("hi", Enumerable.Repeat("hi", 5).Max());
        }

        [Fact]
        public void StringMaximumFirst()
        {
            string[] source = { "zzz", "aaa", "abcd", "bark", "temp", "cat" };
            Assert.Equal(source.First(), source.Max());
        }

        [Fact]
        public void StringMaximumLast()
        {
            string[] source = { null, null, null, null, "aAa" };
            Assert.Equal(source.Last(), source.Max());
        }

        [Fact]
        public void StringMaximumRepeated()
        {
            string[] source = { "ooo", "ccc", "ccc", "ooo", "ooo", "nnn" };
            Assert.Equal("ooo", source.Max());
        }

        [Fact]
        public void StringAllNull()
        {
            Assert.Null(Enumerable.Repeat(default(string), 5).Max());
        }

        [Fact]
        public void MaxInt32WithSelector()
        {
            var ten = Enumerable.Range(1, 10).ToArray();
            var minusTen = new[] { -100, -15, -50, -10 };
            var thousand = new[] { -16, 0, 50, 100, 1000 };
            Assert.Equal(42, Enumerable.Repeat(42, 1).Max(x => x));
            Assert.Equal(10, ten.Max(x => x));
            Assert.Equal(-10, minusTen.Max(x => x));
            Assert.Equal(1000, thousand.Max(x => x));
            Assert.Equal(int.MaxValue, thousand.Concat(Enumerable.Repeat(int.MaxValue, 1)).Max(x => x));
        }

        [Fact]
        public void EmptyInt32WithSelector()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<int>().Max(x => x));
        }

        [Fact]
        public void NullInt32SourceWithSelector()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).Max(i => i));
        }

        [Fact]
        public void Int32SourceWithNullSelector()
        {
            Func<int, int> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<int>().Max(selector));
        }

        [Fact]
        public void MaxInt32WithSelectorAccessingProperty()
        {
            var source = new[]{
                new { name="Tim", num=10 },
                new { name="John", num=-105 },
                new { name="Bob", num=30 }
            };

            Assert.Equal(30, source.Max(e => e.num));
        }

        [Fact]
        public void MaxInt64WithSelector()
        {
            var ten = Enumerable.Range(1, 10).Select(i => (long)i).ToArray();
            var minusTen = new[] { -100L, -15, -50, -10 };
            var thousand = new[] { -16L, 0, 50, 100, 1000 };
            Assert.Equal(42, Enumerable.Repeat(42L, 1).Max(x => x));
            Assert.Equal(10, ten.Max(x => x));
            Assert.Equal(-10, minusTen.Max(x => x));
            Assert.Equal(1000, thousand.Max(x => x));
            Assert.Equal(long.MaxValue, thousand.Concat(Enumerable.Repeat(long.MaxValue, 1)).Max(x => x));
        }

        [Fact]
        public void EmptyInt64WithSelector()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<long>().Max(x => x));
        }

        [Fact]
        public void NullInt64SourceWithSelector()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<long>)null).Max(i => i));
        }

        [Fact]
        public void Int64SourceWithNullSelector()
        {
            Func<long, long> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<long>().Max(selector));
        }

        [Fact]
        public void MaxInt64WithSelectorAccessingProperty()
        {
            var source = new[]{
                new { name="Tim", num=10L },
                new { name="John", num=-105L },
                new { name="Bob", num=long.MaxValue }
            };
            Assert.Equal(long.MaxValue, source.Max(e => e.num));
        }

        [Fact]
        public void MaxSingleWithSelector()
        {
            var ten = Enumerable.Range(1, 10).Select(i => (float)i).ToArray();
            var minusTen = new[] { -100F, -15, -50, -10 };
            var thousand = new[] { -16F, 0, 50, 100, 1000 };
            Assert.Equal(42F, Enumerable.Repeat(42F, 1).Max(x => x));
            Assert.Equal(10F, ten.Max(x => x));
            Assert.Equal(-10F, minusTen.Max(x => x));
            Assert.Equal(1000F, thousand.Max(x => x));
            Assert.Equal(float.MaxValue, thousand.Concat(Enumerable.Repeat(float.MaxValue, 1)).Max(x => x));
        }

        [Fact]
        public void MaxSingleWithSelectorAccessingProperty()
        {
            var source = new []
            {
                new { name = "Tim", num = 40.5f },
                new { name = "John", num = -10.25f },
                new { name = "Bob", num = 100.45f }
            };

            Assert.Equal(100.45f, source.Select(e => e.num).Max());
        }

        [Fact]
        public void NullSingleSourceWithSelector()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<float>)null).Max(i => i));
        }

        [Fact]
        public void SingleSourceWithNullSelector()
        {
            Func<float, float> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<float>().Max(selector));
        }

        [Fact]
        public void EmptySingleWithSelector()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<float>().Max(x => x));
        }

        [Fact]
        public void MaxDoubleWithSelector()
        {
            var ten = Enumerable.Range(1, 10).Select(i => (double)i).ToArray();
            var minusTen = new[] { -100D, -15, -50, -10 };
            var thousand = new[] { -16D, 0, 50, 100, 1000 };
            Assert.Equal(42D, Enumerable.Repeat(42D, 1).Max(x => x));
            Assert.Equal(10D, ten.Max(x => x));
            Assert.Equal(-10D, minusTen.Max(x => x));
            Assert.Equal(1000D, thousand.Max(x => x));
            Assert.Equal(double.MaxValue, thousand.Concat(Enumerable.Repeat(double.MaxValue, 1)).Max(x => x));
        }

        [Fact]
        public void EmptyDoubleWithSelector()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<double>().Max(x => x));
        }

        [Fact]
        public void NullDoubleSourceWithSelector()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<double>)null).Max(i => i));
        }

        [Fact]
        public void DoubleSourceWithNullSelector()
        {
            Func<double, double> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<double>().Max(selector));
        }

        [Fact]
        public void MaxDoubleWithSelectorAccessingField()
        {
            var source = new[]{
                new { name="Tim", num=40.5 },
                new { name="John", num=-10.25 },
                new { name="Bob", num=100.45 }
            };
            Assert.Equal(100.45, source.Max(e => e.num));
        }

        [Fact]
        public void MaxDecimalWithSelector()
        {
            var ten = Enumerable.Range(1, 10).Select(i => (decimal)i).ToArray();
            var minusTen = new[] { -100M, -15, -50, -10 };
            var thousand = new[] { -16M, 0, 50, 100, 1000 };
            Assert.Equal(42M, Enumerable.Repeat(42M, 1).Max(x => x));
            Assert.Equal(10M, ten.Max(x => x));
            Assert.Equal(-10M, minusTen.Max(x => x));
            Assert.Equal(1000M, thousand.Max(x => x));
            Assert.Equal(decimal.MaxValue, thousand.Concat(Enumerable.Repeat(decimal.MaxValue, 1)).Max(x => x));
        }

        [Fact]
        public void EmptyDecimalWithSelector()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<decimal>().Max(x => x));
        }

        [Fact]
        public void NullDecimalSourceWithSelector()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<decimal>)null).Max(i => i));
        }

        [Fact]
        public void DecimalSourceWithNullSelector()
        {
            Func<decimal, decimal> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<decimal>().Max(selector));
        }

        [Fact]
        public void MaxDecimalWithSelectorAccessingProperty()
        {
            var source = new[]{
                new { name="Tim", num=420.5m },
                new { name="John", num=900.25m },
                new { name="Bob", num=10.45m }
            };
            Assert.Equal(900.25m, source.Max(e => e.num));
        }

        [Fact]
        public void MaxNullableInt32WithSelector()
        {
            var ten = Enumerable.Range(1, 10).Select(i => (int?)i).ToArray();
            var minusTen = new[] { default(int?), -100, -15, -50, -10 };
            var thousand = new[] { default(int?), -16, 0, 50, 100, 1000 };
            Assert.Equal(42, Enumerable.Repeat((int?)42, 1).Max(x => x));
            Assert.Equal(10, ten.Max(x => x));
            Assert.Equal(-10, minusTen.Max(x => x));
            Assert.Equal(1000, thousand.Max(x => x));
            Assert.Equal(int.MaxValue, thousand.Concat(Enumerable.Repeat((int?)int.MaxValue, 1)).Max(x => x));
            Assert.Null(Enumerable.Repeat(default(int?), 100).Max(x => x));
        }

        [Fact]
        public void EmptyNullableInt32WithSelector()
        {
            Assert.Null(Enumerable.Empty<int?>().Max(x => x));
        }

        [Fact]
        public void NullNullableInt32SourceWithSelector()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<int?>)null).Max(i => i));
        }

        [Fact]
        public void NullableInt32SourceWithNullSelector()
        {
            Func<int?, int?> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<int?>().Max(selector));
        }

        [Fact]
        public void MaxNullableInt32WithSelectorAccessingField()
        {
            var source = new[]{
                new { name="Tim", num=(int?)10 },
                new { name="John", num=(int?)-105 },
                new { name="Bob", num=(int?)null }
            };

            Assert.Equal(10, source.Max(e => e.num));
        }

        [Fact]
        public void MaxNullableInt64WithSelector()
        {
            var ten = Enumerable.Range(1, 10).Select(i => (long?)i).ToArray();
            var minusTen = new[] { default(long?), -100L, -15, -50, -10 };
            var thousand = new[] { default(long?), -16L, 0, 50, 100, 1000 };
            Assert.Equal(42, Enumerable.Repeat((long?)42, 1).Max(x => x));
            Assert.Equal(10, ten.Max(x => x));
            Assert.Equal(10, ten.Concat(new[] { default(long?) }).Max(x => x));
            Assert.Equal(-10, minusTen.Max(x => x));
            Assert.Equal(1000, thousand.Max(x => x));
            Assert.Equal(long.MaxValue, thousand.Concat(Enumerable.Repeat((long?)long.MaxValue, 1)).Max(x => x));
            Assert.Null(Enumerable.Repeat(default(long?), 100).Max(x => x));
        }

        [Fact]
        public void EmptyNullableInt64WithSelector()
        {
            Assert.Null(Enumerable.Empty<long?>().Max(x => x));
        }

        [Fact]
        public void NullNullableInt64SourceWithSelector()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<long?>)null).Max(i => i));
        }

        [Fact]
        public void NullableInt64SourceWithNullSelector()
        {
            Func<long?, long?> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<long?>().Max(selector));
        }

        [Fact]
        public void MaxNullableInt64WithSelectorAccessingField()
        {
            var source = new[]{
                new {name="Tim", num=default(long?) },
                new {name="John", num=(long?)-105L },
                new {name="Bob", num=(long?)long.MaxValue }
            };
            Assert.Equal(long.MaxValue, source.Max(e => e.num));
        }

        [Fact]
        public void MaxNullableSingleWithSelector()
        {
            var ten = Enumerable.Range(1, 10).Select(i => (float?)i).ToArray();
            var minusTen = new[] { default(float?), -100F, -15, -50, -10 };
            var thousand = new[] { default(float?), -16F, 0, 50, 100, 1000 };
            Assert.Equal(42F, Enumerable.Repeat((float?)42, 1).Max(x => x));
            Assert.Equal(10F, ten.Max(x => x));
            Assert.Equal(-10F, minusTen.Max(x => x));
            Assert.Equal(1000F, thousand.Max(x => x));
            Assert.Equal(float.MaxValue, thousand.Concat(Enumerable.Repeat((float?)float.MaxValue, 1)).Max(x => x));
            Assert.Null(Enumerable.Repeat(default(float?), 100).Max(x => x));
        }

        [Fact]
        public void EmptyNullableSingleWithSelector()
        {
            Assert.Null(Enumerable.Empty<float?>().Max(x => x));
        }

        [Fact]
        public void NullNullableSingleSourceWithSelector()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<float?>)null).Max(i => i));
        }

        [Fact]
        public void NullableSingleSourceWithNullSelector()
        {
            Func<float?, float?> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<float?>().Max(selector));
        }

        [Fact]
        public void MaxNullableSingleWithSelectorAccessingProperty()
        {
            var source = new[]
            {
                new { name="Tim", num=(float?)40.5f },
                new { name="John", num=(float?)null },
                new { name="Bob", num=(float?)100.45f }
            };
            Assert.Equal(100.45f, source.Max(e => e.num));
        }

        [Fact]
        public void MaxNullableDoubleWithSelector()
        {
            var ten = Enumerable.Range(1, 10).Select(i => (double?)i).ToArray();
            var minusTen = new[] { default(double?), -100D, -15, -50, -10 };
            var thousand = new[] { default(double?), -16D, 0, 50, 100, 1000 };
            Assert.Equal(42D, Enumerable.Repeat((double?)42, 1).Max(x => x));
            Assert.Equal(10D, ten.Max(x => x));
            Assert.Equal(-10D, minusTen.Max(x => x));
            Assert.Equal(1000D, thousand.Max(x => x));
            Assert.Equal(double.MaxValue, thousand.Concat(Enumerable.Repeat((double?)double.MaxValue, 1)).Max(x => x));
            Assert.Null(Enumerable.Repeat(default(double?), 100).Max(x => x));
        }

        [Fact]
        public void EmptyNullableDoubleWithSelector()
        {
            Assert.Null(Enumerable.Empty<double?>().Max(x => x));
        }

        [Fact]
        public void NullNullableDoubleSourceWithSelector()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<double?>)null).Max(i => i));
        }

        [Fact]
        public void NullableDoubleSourceWithNullSelector()
        {
            Func<double?, double?> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<double?>().Max(selector));
        }

        [Fact]
        public void MaxNullableDoubleWithSelectorAccessingProperty()
        {
            var source = new []{
                new { name = "Tim", num = (double?)40.5},
                new { name = "John", num = default(double?)},
                new { name = "Bob", num = (double?)100.45}
            };
            Assert.Equal(100.45, source.Max(e => e.num));
        }

        [Fact]
        public void MaxNullableDecimalWithSelector()
        {
            var ten = Enumerable.Range(1, 10).Select(i => (decimal?)i).ToArray();
            var minusTen = new[] { default(decimal?), -100M, -15, -50, -10 };
            var thousand = new[] { default(decimal?), -16M, 0, 50, 100, 1000 };
            Assert.Equal(42M, Enumerable.Repeat((decimal?)42, 1).Max(x => x));
            Assert.Equal(10M, ten.Max(x => x));
            Assert.Equal(-10M, minusTen.Max(x => x));
            Assert.Equal(1000M, thousand.Max(x => x));
            Assert.Equal(decimal.MaxValue, thousand.Concat(Enumerable.Repeat((decimal?)decimal.MaxValue, 1)).Max(x => x));
            Assert.Null(Enumerable.Repeat(default(decimal?), 100).Max(x => x));
        }

        [Fact]
        public void EmptyNullableDecimalWithSelector()
        {
            Assert.Null(Enumerable.Empty<decimal?>().Max(x => x));
        }

        [Fact]
        public void NullNullableDecimalSourceWithSelector()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<decimal?>)null).Max(i => i));
        }

        [Fact]
        public void NullableDecimalSourceWithNullSelector()
        {
            Func<decimal?, decimal?> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<decimal?>().Max(selector));
        }

        [Fact]
        public void MaxNullableDecimalWithSelectorAccessingProperty()
        {
            var source = new[] {
                new { name="Tim", num=(decimal?)420.5m },
                new { name="John", num=default(decimal?) },
                new { name="Bob", num=(decimal?)10.45m }
            };
            Assert.Equal(420.5m, source.Max(e => e.num));
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
            Assert.False(float.IsNaN(nanThenOne.Max(x => x)));
            Assert.False(float.IsNaN(nanThenMinusTen.Max(x => x)));
            Assert.False(float.IsNaN(nanThenMinValue.Max(x => x)));
            var nanWithNull = new[] { default(float?), float.NaN, default(float?) };
            Assert.True(float.IsNaN(nanWithNull.Max(x => x).Value));
        }

        [Fact]
        public void NaNFirstDoubleWithSelector()
        {
            var nanThenOne = Enumerable.Range(1, 10).Select(i => (double)i).Concat(Enumerable.Repeat(double.NaN, 1)).ToArray();
            var nanThenMinusTen = new[] { -1F, -10, double.NaN, 10, 200, 1000 };
            var nanThenMinValue = new[] { double.MinValue, 3000F, 100, 200, double.NaN, 1000 };
            Assert.False(double.IsNaN(nanThenOne.Max(x => x)));
            Assert.False(double.IsNaN(nanThenMinusTen.Max(x => x)));
            Assert.False(double.IsNaN(nanThenMinValue.Max(x => x)));
            var nanWithNull = new[] { default(double?), double.NaN, default(double?) };
            Assert.True(double.IsNaN(nanWithNull.Max(x => x).Value));
        }

        [Fact]
        public void MaxDateTimeWithSelector()
        {
            var ten = Enumerable.Range(1, 10).Select(i => new DateTime(2000, 1, i)).ToArray();
            var newYearsEve = new[]
            {
                new DateTime(2000, 12, 1),
                new DateTime(2000, 12, 31),
                new DateTime(2000, 1, 12)
            };
            var threeThousand = new[]
            {
                new DateTime(3000, 1, 1),
                new DateTime(100, 1, 1),
                new DateTime(200, 1, 1),
                new DateTime(1000, 1, 1)
            };
            Assert.Equal(new DateTime(2000, 1, 10), ten.Max(x => x));
            Assert.Equal(new DateTime(2000, 12, 31), newYearsEve.Max(x => x));
            Assert.Equal(new DateTime(3000, 1, 1), threeThousand.Max(x => x));
            Assert.Equal(DateTime.MaxValue, threeThousand.Concat(Enumerable.Repeat(DateTime.MaxValue, 1)).Max(x => x));
        }

        [Fact]
        public void EmptyMaxDateTimeWithSelector()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<DateTime>().Max(i => i));
        }

        [Fact]
        public void EmptyNullableDateTimeWithSelector()
        {
            Assert.Null(Enumerable.Empty<DateTime?>().Max(x => x));
        }

        [Fact]
        public void NullNullableDateTimeSourceWithSelector()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<DateTime?>)null).Max(i => i));
        }

        [Fact]
        public void NullableDateTimeSourceWithNullSelector()
        {
            Func<DateTime?, DateTime?> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<DateTime?>().Max(selector));
        }

        [Fact]
        public void MaxStringWithSelector()
        {
            var nine = Enumerable.Range(1, 10).Select(i => i.ToString()).ToArray();
            var agents = new[] { "Alice", "Bob", "Charlie", "Eve", "Mallory", "Victor", "Trent" };
            var confusedAgents = new[] { null, "Charlie", null, "Victor", "Trent", null, "Eve", "Alice", "Mallory", "Bob" };
            Assert.Equal("9", nine.Max(x => x));
            Assert.Equal("Victor", agents.Max(x => x));
            Assert.Equal("Victor", confusedAgents.Max(x => x));
        }

        public void EmptyStringSourceWithSelector()
        {
            Assert.Null(Enumerable.Empty<string>().Max(x => x));
        }

        [Fact]
        public void NullStringSourceWithSelector()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<string>)null).Max(i => i));
        }

        [Fact]
        public void StringSourceWithNullSelector()
        {
            Func<string, string> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<string>().Max(selector));
        }

        [Fact]
        public void MaxStringWithSelectorAccessingProperty()
        {
            var source = new[]{
                new { name="Tim", num=420.5m },
                new { name="John", num=900.25m },
                new { name="Bob", num=10.45m }
            };
            Assert.Equal("Tim", source.Max(e => e.name));
        }

        [Fact]
        public void EmptyBoolean()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<bool>().Max());
        }
    }
}
