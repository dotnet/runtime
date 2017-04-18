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
        public void Max_Int_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).Max());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).Max(i => i));
        }

        [Fact]
        public void Max_Int_EmptySource_ThrowsInvalidOpertionException()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<int>().Max());
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<int>().Max(x => x));
        }

        public static IEnumerable<object[]> Max_Int_TestData()
        {
            yield return new object[] { Enumerable.Repeat(42, 1), 42 };
            yield return new object[] { Enumerable.Range(1, 10).ToArray(), 10 };
            yield return new object[] { new int[] { -100, -15, -50, -10 }, -10 };
            yield return new object[] { new int[] { -16, 0, 50, 100, 1000 }, 1000 };
            yield return new object[] { new int[] { -16, 0, 50, 100, 1000 }.Concat(Enumerable.Repeat(int.MaxValue, 1)), int.MaxValue };

            yield return new object[] { Enumerable.Repeat(20, 1), 20 };
            yield return new object[] { Enumerable.Repeat(-2, 5), -2 };
            yield return new object[] { new int[] { 16, 9, 10, 7, 8 }, 16 };
            yield return new object[] { new int[] { 6, 9, 10, 0, 50 }, 50 };
            yield return new object[] { new int[] { -6, 0, -9, 0, -10, 0 }, 0 };
        }

        [Theory]
        [MemberData(nameof(Max_Int_TestData))]
        public void Max_Int(IEnumerable<int> source, int expected)
        {
            Assert.Equal(expected, source.Max());
            Assert.Equal(expected, source.Max(x => x));
        }

        public static IEnumerable<object[]> Max_Long_TestData()
        {
            yield return new object[] { Enumerable.Repeat(42L, 1), 42L };
            yield return new object[] { Enumerable.Range(1, 10).Select(i => (long)i).ToArray(), 10L };
            yield return new object[] { new long[] { -100, -15, -50, -10 }, -10L };
            yield return new object[] { new long[] { -16, 0, 50, 100, 1000 }, 1000L };
            yield return new object[] { new long[] { -16, 0, 50, 100, 1000 }.Concat(Enumerable.Repeat(long.MaxValue, 1)), long.MaxValue };

            yield return new object[] { Enumerable.Repeat(int.MaxValue + 10L, 1), int.MaxValue + 10L };
            yield return new object[] { Enumerable.Repeat(500L, 5), 500L };
            yield return new object[] { new long[] { 250, 49, 130, 47, 28 }, 250L };
            yield return new object[] { new long[] { 6, 9, 10, 0, int.MaxValue + 50L }, int.MaxValue + 50L };
            yield return new object[] { new long[] { 6, 50, 9, 50, 10, 50 }, 50L };
        }

        [Theory]
        [MemberData(nameof(Max_Long_TestData))]
        public void Max_Long(IEnumerable<long> source, long expected)
        {
            Assert.Equal(expected, source.Max());
            Assert.Equal(expected, source.Max(x => x));
        }

        [Fact]
        public void Max_Long_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<long>)null).Max());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<long>)null).Max(i => i));
        }

        [Fact]
        public void Max_Long_EmptySource_ThrowsInvalidOpertionException()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<long>().Max());
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<long>().Max(x => x));
        }

        public static IEnumerable<object[]> Max_Float_TestData()
        {
            yield return new object[] { Enumerable.Repeat(42f, 1), 42f };
            yield return new object[] { Enumerable.Range(1, 10).Select(i => (float)i).ToArray(), 10f };
            yield return new object[] { new float[] { -100, -15, -50, -10 }, -10f };
            yield return new object[] { new float[] { -16, 0, 50, 100, 1000 }, 1000f };
            yield return new object[] { new float[] { -16, 0, 50, 100, 1000 }.Concat(Enumerable.Repeat(float.MaxValue, 1)), float.MaxValue };

            yield return new object[] { Enumerable.Repeat(5.5f, 1), 5.5f };
            yield return new object[] { new float[] { 112.5f, 4.9f, 30f, 4.7f, 28f }, 112.5f };
            yield return new object[] { new float[] { 6.8f, 9.4f, -10f, 0f, float.NaN, 53.6f }, 53.6f };
            yield return new object[] { new float[] { -5.5f, float.PositiveInfinity, 9.9f, float.PositiveInfinity }, float.PositiveInfinity };

            yield return new object[] { Enumerable.Repeat(float.NaN, 5), float.NaN };
            yield return new object[] { new float[] { float.NaN, 6.8f, 9.4f, 10f, 0, -5.6f }, 10f };
            yield return new object[] { new float[] { 6.8f, 9.4f, 10f, 0, -5.6f, float.NaN }, 10f };
            yield return new object[] { new float[] { float.NaN, float.NegativeInfinity }, float.NegativeInfinity };
            yield return new object[] { new float[] { float.NegativeInfinity, float.NaN }, float.NegativeInfinity };

            // Normally NaN < anything and anything < NaN returns false
            // However, this leads to some irksome outcomes in Min and Max.
            // If we use those semantics then Min(NaN, 5.0) is NaN, but
            // Min(5.0, NaN) is 5.0!  To fix this, we impose a total
            // ordering where NaN is smaller than every value, including
            // negative infinity.
            yield return new object[] { Enumerable.Range(1, 10).Select(i => (float)i).Concat(Enumerable.Repeat(float.NaN, 1)).ToArray(), 10f };
            yield return new object[] { new float[] { -1f, -10, float.NaN, 10, 200, 1000 }, 1000f };
            yield return new object[] { new float[] { float.MinValue, 3000f, 100, 200, float.NaN, 1000 }, 3000f };
        }

        [Theory]
        [MemberData(nameof(Max_Float_TestData))]
        public void Max_Float(IEnumerable<float> source, float expected)
        {
            Assert.Equal(expected, source.Max());
            Assert.Equal(expected, source.Max(x => x));
        }

        [Fact]
        public void Max_Float_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<float>)null).Max());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<float>)null).Max(i => i));
        }

        [Fact]
        public void Max_Float_EmptySource_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<float>().Max());
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<float>().Max(x => x));
        }

        [Fact]
        public void Max_Float_SeveralNaNWithSelector()
        {
            Assert.True(float.IsNaN(Enumerable.Repeat(float.NaN, 5).Max(i => i)));
        }

        [Fact]
        public void Max_NullableFloat_SeveralNaNOrNullWithSelector()
        {
            float?[] source = new float?[] { float.NaN, null, float.NaN, null };
            Assert.True(float.IsNaN(source.Max(i => i).Value));
        }

        [Fact]
        public void Max_Float_NaNAtStartWithSelector()
        {
            float[] source = { float.NaN, 6.8f, 9.4f, 10f, 0, -5.6f };
            Assert.Equal(10f, source.Max(i => i));
        }

        public static IEnumerable<object[]> Max_Double_TestData()
        {
            yield return new object[] { Enumerable.Repeat(42.0, 1), 42.0 };
            yield return new object[] { Enumerable.Range(1, 10).Select(i => (double)i).ToArray(), 10.0 };
            yield return new object[] { new double[] { -100, -15, -50, -10 }, -10.0 };
            yield return new object[] { new double[] { -16, 0, 50, 100, 1000 }, 1000.0 };
            yield return new object[] { new double[] { -16, 0, 50, 100, 1000 }.Concat(Enumerable.Repeat(double.MaxValue, 1)), double.MaxValue };

            yield return new object[] { Enumerable.Repeat(5.5, 1), 5.5 };
            yield return new object[] { Enumerable.Repeat(double.NaN, 5), double.NaN };
            yield return new object[] { new double[] { 112.5, 4.9, 30, 4.7, 28 }, 112.5 };
            yield return new object[] { new double[] { 6.8, 9.4, -10, 0, double.NaN, 53.6 }, 53.6 };
            yield return new object[] { new double[] { -5.5, double.PositiveInfinity, 9.9, double.PositiveInfinity }, double.PositiveInfinity };
            yield return new object[] { new double[] { double.NaN, 6.8, 9.4, 10.5, 0, -5.6 }, 10.5 };
            yield return new object[] { new double[] { 6.8, 9.4, 10.5, 0, -5.6, double.NaN }, 10.5 };
            yield return new object[] { new double[] { double.NaN, double.NegativeInfinity }, double.NegativeInfinity };
            yield return new object[] { new double[] { double.NegativeInfinity, double.NaN }, double.NegativeInfinity };

            // Normally NaN < anything and anything < NaN returns false
            // However, this leads to some irksome outcomes in Min and Max.
            // If we use those semantics then Min(NaN, 5.0) is NaN, but
            // Min(5.0, NaN) is 5.0!  To fix this, we impose a total
            // ordering where NaN is smaller than every value, including
            // negative infinity.
            yield return new object[] { Enumerable.Range(1, 10).Select(i => (double)i).Concat(Enumerable.Repeat(double.NaN, 1)).ToArray(), 10.0 };
            yield return new object[] { new double[] { -1F, -10, double.NaN, 10, 200, 1000 }, 1000.0 };
            yield return new object[] { new double[] { double.MinValue, 3000F, 100, 200, double.NaN, 1000 }, 3000.0 };
        }

        [Theory]
        [MemberData(nameof(Max_Double_TestData))]
        public void Max_Double(IEnumerable<double> source, double expected)
        {
            Assert.Equal(expected, source.Max());
            Assert.Equal(expected, source.Max(x => x));
        }

        [Fact]
        public void Max_Double_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<double>)null).Max());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<double>)null).Max(i => i));
        }

        [Fact]
        public void Max_Double_EmptySource_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<double>().Max());
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<double>().Max(x => x));
        }

        [Fact]
        public void Max_Double_AllNaNWithSelector()
        {
            Assert.True(double.IsNaN(Enumerable.Repeat(double.NaN, 5).Max(i => i)));
        }

        [Fact]
        public void Max_Double_SeveralNaNOrNullWithSelector()
        {
            double?[] source = new double?[] { double.NaN, null, double.NaN, null };
            Assert.True(double.IsNaN(source.Max(i => i).Value));
        }

        [Fact]
        public void Max_Double_NaNThenNegativeInfinityWithSelector()
        {
            double[] source = { double.NaN, double.NegativeInfinity };
            Assert.True(double.IsNegativeInfinity(source.Max(i => i)));
        }

        public static IEnumerable<object[]> Max_Decimal_TestData()
        {
            yield return new object[] { Enumerable.Repeat(42m, 1), 42m };
            yield return new object[] { Enumerable.Range(1, 10).Select(i => (decimal)i).ToArray(), 10m };
            yield return new object[] { new decimal[] { -100, -15, -50, -10 }, -10m };
            yield return new object[] { new decimal[] { -16, 0, 50, 100, 1000 }, 1000m };
            yield return new object[] { new decimal[] { -16, 0, 50, 100, 1000 }.Concat(Enumerable.Repeat(decimal.MaxValue, 1)), decimal.MaxValue };

            yield return new object[] { new decimal[] { 5.5m }, 5.5m };
            yield return new object[] { Enumerable.Repeat(-3.4m, 5), -3.4m };
            yield return new object[] { new decimal[] { 122.5m, 4.9m, 10m, 4.7m, 28m }, 122.5m };
            yield return new object[] { new decimal[] { 6.8m, 9.4m, 10m, 0m, 0m, decimal.MaxValue }, decimal.MaxValue };
            yield return new object[] { new decimal[] { -5.5m, 0m, 9.9m, -5.5m, 9.9m }, 9.9m };
        }

        [Theory]
        [MemberData(nameof(Max_Decimal_TestData))]
        public void Max_Decimal(IEnumerable<decimal> source, decimal expected)
        {
            Assert.Equal(expected, source.Max());
            Assert.Equal(expected, source.Max(x => x));
        }

        [Fact]
        public void Max_Decimal_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<decimal>)null).Max());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<decimal>)null).Max(i => i));
        }

        [Fact]
        public void Max_Decimal_EmptySource_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<decimal>().Max());
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<decimal>().Max(x => x));
        }

        public static IEnumerable<object[]> Max_NullableInt_TestData()
        {
            yield return new object[] { Enumerable.Repeat((int?)42, 1), 42 };
            yield return new object[] { Enumerable.Range(1, 10).Select(i => (int?)i).ToArray(), 10 };
            yield return new object[] { new int?[] { null, -100, -15, -50, -10 }, -10 };
            yield return new object[] { new int?[] { null, -16, 0, 50, 100, 1000 }, 1000 };
            yield return new object[] { new int?[] { null, -16, 0, 50, 100, 1000 }.Concat(Enumerable.Repeat((int?)int.MaxValue, 1)), int.MaxValue };
            yield return new object[] { Enumerable.Repeat(default(int?), 100), null };

            yield return new object[] { Enumerable.Empty<int?>(), null };
            yield return new object[] { Enumerable.Repeat((int?)-20, 1), -20 };
            yield return new object[] { new int?[] { -6, null, -9, -10, null, -17, -18 }, -6 };
            yield return new object[] { new int?[] { null, null, null, null, null, -5 }, -5 };
            yield return new object[] { new int?[] { 6, null, null, 100, 9, 100, 10, 100 }, 100 };
            yield return new object[] { Enumerable.Repeat(default(int?), 5), null };
        }

        [Theory]
        [MemberData(nameof(Max_NullableInt_TestData))]
        public void Max_NullableInt(IEnumerable<int?> source, int? expected)
        {
            Assert.Equal(expected, source.Max());
            Assert.Equal(expected, source.Max(x => x));
        }

        [Theory, MemberData(nameof(Max_NullableInt_TestData))]
        public void Max_NullableIntRunOnce(IEnumerable<int?> source, int? expected)
        {
            Assert.Equal(expected, source.RunOnce().Max());
            Assert.Equal(expected, source.RunOnce().Max(x => x));
        }

        [Fact]
        public void Max_NullableInt_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int?>)null).Max());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int?>)null).Max(i => i));
        }

        public static IEnumerable<object[]> Max_NullableLong_TestData()
        {
            yield return new object[] { Enumerable.Repeat((long?)42, 1), 42L };
            yield return new object[] { Enumerable.Range(1, 10).Select(i => (long?)i).ToArray(), 10L };
            yield return new object[] { new long?[] { null, -100, -15, -50, -10 }, -10L };
            yield return new object[] { new long?[] { null, -16, 0, 50, 100, 1000 }, 1000L };
            yield return new object[] { new long?[] { null, -16, 0, 50, 100, 1000 }.Concat(Enumerable.Repeat((long?)long.MaxValue, 1)), long.MaxValue };
            yield return new object[] { Enumerable.Repeat(default(long?), 100), null };

            yield return new object[] { Enumerable.Empty<long?>(), null };
            yield return new object[] { Enumerable.Repeat((long?)long.MaxValue, 1), long.MaxValue };
            yield return new object[] { Enumerable.Repeat(default(long?), 5), null };
            yield return new object[] { new long?[] { long.MaxValue, null, 9, 10, null, 7, 8 }, long.MaxValue };
            yield return new object[] { new long?[] { null, null, null, null, null, -long.MaxValue }, -long.MaxValue };
            yield return new object[] { new long?[] { -6, null, null, 0, -9, 0, -10, -30 }, 0L };
        }

        [Theory]
        [MemberData(nameof(Max_NullableLong_TestData))]
        public void Max_NullableLong(IEnumerable<long?> source, long? expected)
        {
            Assert.Equal(expected, source.Max());
            Assert.Equal(expected, source.Max(x => x));
        }

        [Fact]
        public void Max_NullableLong_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<long?>)null).Max());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<long?>)null).Max(i => i));
        }

        public static IEnumerable<object[]> Max_NullableFloat_TestData()
        {
            yield return new object[] { Enumerable.Repeat((float?)42, 1), 42f };
            yield return new object[] { Enumerable.Range(1, 10).Select(i => (float?)i).ToArray(), 10f };
            yield return new object[] { new float?[] { null, -100, -15, -50, -10 }, -10f };
            yield return new object[] { new float?[] { null, -16, 0, 50, 100, 1000 }, 1000f };
            yield return new object[] { new float?[] { null, -16, 0, 50, 100, 1000 }.Concat(Enumerable.Repeat((float?)float.MaxValue, 1)), float.MaxValue };
            yield return new object[] { Enumerable.Repeat(default(float?), 100), null };

            yield return new object[] { Enumerable.Empty<float?>(), null };
            yield return new object[] { Enumerable.Repeat((float?)float.MinValue, 1), float.MinValue };
            yield return new object[] { Enumerable.Repeat(default(float?), 5), null };
            yield return new object[] { new float?[] { 14.50f, null, float.NaN, 10.98f, null, 7.5f, 8.6f }, 14.50f };
            yield return new object[] { new float?[] { null, null, null, null, null, 0f }, 0f };
            yield return new object[] { new float?[] { -6.4f, null, null, -0.5f, -9.4f, -0.5f, -10.9f, -0.5f }, -0.5f };

            yield return new object[] { new float?[] { float.NaN, 6.8f, 9.4f, 10f, 0, null, -5.6f }, 10f };
            yield return new object[] { new float?[] { 6.8f, 9.4f, 10f, 0, null, -5.6f, float.NaN }, 10f };
            yield return new object[] { new float?[] { float.NaN, float.NegativeInfinity }, float.NegativeInfinity };
            yield return new object[] { new float?[] { float.NegativeInfinity, float.NaN }, float.NegativeInfinity };
            yield return new object[] { Enumerable.Repeat((float?)float.NaN, 3), float.NaN };
            yield return new object[] { new float?[] { float.NaN, null, null, null }, float.NaN };
            yield return new object[] { new float?[] { null, null, null, float.NaN }, float.NaN };
            yield return new object[] { new float?[] { null, float.NaN, null }, float.NaN };
        }
        
        [Theory]
        [MemberData(nameof(Max_NullableFloat_TestData))]
        public void Max_NullableFloat(IEnumerable<float?> source, float? expected)
        {
            Assert.Equal(expected, source.Max());
            Assert.Equal(expected, source.Max(x => x));
        }

        [Fact]
        public void Max_NullableFloat_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<float?>)null).Max());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<float?>)null).Max(i => i));
        }

        public static IEnumerable<object[]> Max_NullableDouble_TestData()
        {
            yield return new object[] { Enumerable.Repeat((double?)42, 1), 42.0 };
            yield return new object[] { Enumerable.Range(1, 10).Select(i => (double?)i).ToArray(), 10.0 };
            yield return new object[] { new double?[] { null, -100, -15, -50, -10 }, -10.0 };
            yield return new object[] { new double?[] { null, -16, 0, 50, 100, 1000 }, 1000.0 };
            yield return new object[] { new double?[] { null, -16, 0, 50, 100, 1000 }.Concat(Enumerable.Repeat((double?)double.MaxValue, 1)), double.MaxValue };
            yield return new object[] { Enumerable.Repeat(default(double?), 100), null };

            yield return new object[] { Enumerable.Empty<double?>(), null };
            yield return new object[] { Enumerable.Repeat((double?)double.MinValue, 1), double.MinValue };
            yield return new object[] { Enumerable.Repeat(default(double?), 5), null };
            yield return new object[] { new double?[] { 14.50, null, double.NaN, 10.98, null, 7.5, 8.6 }, 14.50 };
            yield return new object[] { new double?[] { null, null, null, null, null, 0 }, 0.0 };
            yield return new object[] { new double?[] { -6.4, null, null, -0.5, -9.4, -0.5, -10.9, -0.5 }, -0.5 };

            yield return new object[] { new double?[] { double.NaN, 6.8, 9.4, 10.5, 0, null, -5.6 }, 10.5 };
            yield return new object[] { new double?[] { 6.8, 9.4, 10.8, 0, null, -5.6, double.NaN }, 10.8 };
            yield return new object[] { new double?[] { double.NaN, double.NegativeInfinity }, double.NegativeInfinity };
            yield return new object[] { new double?[] { double.NegativeInfinity, double.NaN }, double.NegativeInfinity };
            yield return new object[] { Enumerable.Repeat((double?)double.NaN, 3), double.NaN };
            yield return new object[] { new double?[] { double.NaN, null, null, null }, double.NaN };
            yield return new object[] { new double?[] { null, null, null, double.NaN }, double.NaN };
            yield return new object[] { new double?[] { null, double.NaN, null }, double.NaN };
        }

        [Theory]
        [MemberData(nameof(Max_NullableDouble_TestData))]
        public void Max_NullableDouble(IEnumerable<double?> source, double? expected)
        {
            Assert.Equal(expected, source.Max());
            Assert.Equal(expected, source.Max(x => x));
        }

        [Fact]
        public void Max_NullableDouble_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<double?>)null).Max());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<double?>)null).Max(i => i));
        }

        public static IEnumerable<object[]> Max_NullableDecimal_TestData()
        {
            yield return new object[] { Enumerable.Repeat((decimal?)42, 1), 42m };
            yield return new object[] { Enumerable.Range(1, 10).Select(i => (decimal?)i).ToArray(), 10m };
            yield return new object[] { new decimal?[] { null, -100M, -15, -50, -10 }, -10m };
            yield return new object[] { new decimal?[] { null, -16M, 0, 50, 100, 1000 }, 1000m };
            yield return new object[] { new decimal?[] { null, -16M, 0, 50, 100, 1000 }.Concat(Enumerable.Repeat((decimal?)decimal.MaxValue, 1)), decimal.MaxValue };
            yield return new object[] { Enumerable.Repeat(default(decimal?), 100), null };

            yield return new object[] { Enumerable.Empty<decimal?>(), null };
            yield return new object[] { Enumerable.Repeat((decimal?)decimal.MaxValue, 1), decimal.MaxValue };
            yield return new object[] { Enumerable.Repeat(default(decimal?), 5), null };
            yield return new object[] { new decimal?[] { 14.50m, null, null, 10.98m, null, 7.5m, 8.6m }, 14.50m };
            yield return new object[] { new decimal?[] { null, null, null, null, null, 0m }, 0m };
            yield return new object[] { new decimal?[] { 6.4m, null, null, decimal.MaxValue, 9.4m, decimal.MaxValue, 10.9m, decimal.MaxValue }, decimal.MaxValue };
        }

        [Theory]
        [MemberData(nameof(Max_NullableDecimal_TestData))]
        public void Max_NullableDecimal(IEnumerable<decimal?> source, decimal? expected)
        {
            Assert.Equal(expected, source.Max());
            Assert.Equal(expected, source.Max(x => x));
        }

        [Fact]
        public void Max_NullableDecimal_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<decimal?>)null).Max());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<decimal?>)null).Max(i => i));
        }

        public static IEnumerable<object[]> Max_DateTime_TestData()
        {
            yield return new object[] { Enumerable.Range(1, 10).Select(i => new DateTime(2000, 1, i)).ToArray(), new DateTime(2000, 1, 10) };
            yield return new object[] { new DateTime[] { new DateTime(2000, 12, 1), new DateTime(2000, 12, 31), new DateTime(2000, 1, 12) }, new DateTime(2000, 12, 31) };

            DateTime[] threeThousand = new DateTime[]
            {
                new DateTime(3000, 1, 1),
                new DateTime(100, 1, 1),
                new DateTime(200, 1, 1),
                new DateTime(1000, 1, 1)
            };
            yield return new object[] { threeThousand, new DateTime(3000, 1, 1) };
            yield return new object[] { threeThousand.Concat(Enumerable.Repeat(DateTime.MaxValue, 1)), DateTime.MaxValue };
        }

        [Theory]
        [MemberData(nameof(Max_DateTime_TestData))]
        public void Max_DateTime(IEnumerable<DateTime> source, DateTime expected)
        {
            Assert.Equal(expected, source.Max());
            Assert.Equal(expected, source.Max(x => x));
        }

        [Fact]
        public void Max_DateTime_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<DateTime>)null).Max());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<DateTime>)null).Max(i => i));
        }

        [Fact]
        public void Max_DateTime_EmptySource_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<DateTime>().Max());
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<DateTime>().Max(i => i));
        }
        
        public static IEnumerable<object[]> Max_String_TestData()
        {
            yield return new object[] { Enumerable.Range(1, 10).Select(i => i.ToString()).ToArray(), "9" };
            yield return new object[] { new string[] { "Alice", "Bob", "Charlie", "Eve", "Mallory", "Victor", "Trent" }, "Victor" };
            yield return new object[] { new string[] { null, "Charlie", null, "Victor", "Trent", null, "Eve", "Alice", "Mallory", "Bob" }, "Victor" };

            yield return new object[] { Enumerable.Empty<string>(), null };
            yield return new object[] { Enumerable.Repeat("Hello", 1), "Hello" };
            yield return new object[] { Enumerable.Repeat("hi", 5), "hi" };
            yield return new object[] { new string[] { "zzz", "aaa", "abcd", "bark", "temp", "cat" }, "zzz" };
            yield return new object[] { new string[] { null, null, null, null, "aAa" }, "aAa" };
            yield return new object[] { new string[] { "ooo", "ccc", "ccc", "ooo", "ooo", "nnn" }, "ooo" };
            yield return new object[] { Enumerable.Repeat(default(string), 5), null };
        }

        [Theory]
        [MemberData(nameof(Max_String_TestData))]
        public void Max_String(IEnumerable<string> source, string expected)
        {
            Assert.Equal(expected, source.Max());
            Assert.Equal(expected, source.Max(x => x));
        }

        [Theory, MemberData(nameof(Max_String_TestData))]
        public void Max_StringRunOnce(IEnumerable<string> source, string expected)
        {
            Assert.Equal(expected, source.RunOnce().Max());
            Assert.Equal(expected, source.RunOnce().Max(x => x));
        }

        [Fact]
        public void Max_String_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<string>)null).Max());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<string>)null).Max(i => i));
        }

        [Fact]
        public void Max_Int_NullSelector_ThrowsArgumentNullException()
        {
            Func<int, int> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<int>().Max(selector));
        }

        [Fact]
        public void Max_Int_WithSelectorAccessingProperty()
        {
            var source = new[]
            {
                new { name="Tim", num=10 },
                new { name="John", num=-105 },
                new { name="Bob", num=30 }
            };

            Assert.Equal(30, source.Max(e => e.num));
        }

        [Fact]
        public void Max_Long_NullSelector_ThrowsArgumentNullException()
        {
            Func<long, long> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<long>().Max(selector));
        }

        [Fact]
        public void Max_Long_WithSelectorAccessingProperty()
        {
            var source = new[]
            {
                new { name="Tim", num=10L },
                new { name="John", num=-105L },
                new { name="Bob", num=long.MaxValue }
            };
            Assert.Equal(long.MaxValue, source.Max(e => e.num));
        }

        [Fact]
        public void Max_Float_WithSelectorAccessingProperty()
        {
            var source = new[]
            {
                new { name = "Tim", num = 40.5f },
                new { name = "John", num = -10.25f },
                new { name = "Bob", num = 100.45f }
            };

            Assert.Equal(100.45f, source.Select(e => e.num).Max());
        }

        [Fact]
        public void Max_Float_NullSelector_ThrowsArgumentNullException()
        {
            Func<float, float> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<float>().Max(selector));
        }

        [Fact]
        public void Max_Double_NullSelector_ThrowsArgumentNullException()
        {
            Func<double, double> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<double>().Max(selector));
        }

        [Fact]
        public void Max_Double_WithSelectorAccessingField()
        {
            var source = new[]
            {
                new { name="Tim", num=40.5 },
                new { name="John", num=-10.25 },
                new { name="Bob", num=100.45 }
            };
            Assert.Equal(100.45, source.Max(e => e.num));
        }

        [Fact]
        public void Max_Decimal_NullSelector_ThrowsArgumentNullException()
        {
            Func<decimal, decimal> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<decimal>().Max(selector));
        }

        [Fact]
        public void Max_Decimal_WithSelectorAccessingProperty()
        {
            var source = new[]{
                new { name="Tim", num=420.5m },
                new { name="John", num=900.25m },
                new { name="Bob", num=10.45m }
            };
            Assert.Equal(900.25m, source.Max(e => e.num));
        }

        [Fact]
        public void Max_NullableInt_NullSelector_ThrowsArgumentNullException()
        {
            Func<int?, int?> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<int?>().Max(selector));
        }

        [Fact]
        public void Max_NullableInt_WithSelectorAccessingField()
        {
            var source = new[]{
                new { name="Tim", num=(int?)10 },
                new { name="John", num=(int?)-105 },
                new { name="Bob", num=(int?)null }
            };

            Assert.Equal(10, source.Max(e => e.num));
        }

        [Fact]
        public void Max_NullableLong_NullSelector_ThrowsArgumentNullException()
        {
            Func<long?, long?> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<long?>().Max(selector));
        }

        [Fact]
        public void Max_NullableLong_WithSelectorAccessingField()
        {
            var source = new[]
            {
                new {name="Tim", num=default(long?) },
                new {name="John", num=(long?)-105L },
                new {name="Bob", num=(long?)long.MaxValue }
            };
            Assert.Equal(long.MaxValue, source.Max(e => e.num));
        }
        
        [Fact]
        public void Max_NullableFloat_NullSelector_ThrowsArgumentNullException()
        {
            Func<float?, float?> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<float?>().Max(selector));
        }

        [Fact]
        public void Max_NullableFloat_WithSelectorAccessingProperty()
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
        public void Max_NullableDouble_NullSelector_ThrowsArgumentNullException()
        {
            Func<double?, double?> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<double?>().Max(selector));
        }

        [Fact]
        public void Max_NullableDouble_WithSelectorAccessingProperty()
        {
            var source = new []
            {
                new { name = "Tim", num = (double?)40.5},
                new { name = "John", num = default(double?)},
                new { name = "Bob", num = (double?)100.45}
            };
            Assert.Equal(100.45, source.Max(e => e.num));
        }

        [Fact]
        public void Max_NullableDecimal_NullSelector_ThrowsArgumentNullException()
        {
            Func<decimal?, decimal?> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<decimal?>().Max(selector));
        }

        [Fact]
        public void Max_NullableDecimal_WithSelectorAccessingProperty()
        {
            var source = new[]
            {
                new { name="Tim", num=(decimal?)420.5m },
                new { name="John", num=default(decimal?) },
                new { name="Bob", num=(decimal?)10.45m }
            };
            Assert.Equal(420.5m, source.Max(e => e.num));
        }

        [Fact]
        public void Max_NullableDateTime_EmptySourceWithSelector()
        {
            Assert.Null(Enumerable.Empty<DateTime?>().Max(x => x));
        }

        [Fact]
        public void Max_NullableDateTime_NullSelector_ThrowsArgumentNullException()
        {
            Func<DateTime?, DateTime?> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<DateTime?>().Max(selector));
        }

        [Fact]
        public void Max_String_NullSelector_ThrowsArgumentNullException()
        {
            Func<string, string> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<string>().Max(selector));
        }

        [Fact]
        public void Max_String_WithSelectorAccessingProperty()
        {
            var source = new[]
            {
                new { name="Tim", num=420.5m },
                new { name="John", num=900.25m },
                new { name="Bob", num=10.45m }
            };
            Assert.Equal("Tim", source.Max(e => e.name));
        }

        [Fact]
        public void Max_Boolean_EmptySource_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<bool>().Max());
        }
    }
}
