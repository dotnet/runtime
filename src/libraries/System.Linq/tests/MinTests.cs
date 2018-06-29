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
                    where x > int.MinValue
                    select x;

            Assert.Equal(q.Min(), q.Min());
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", string.Empty }
                    where !string.IsNullOrEmpty(x)
                    select x;

            Assert.Equal(q.Min(), q.Min());
        }

        public static IEnumerable<object[]> Min_Int_TestData()
        {
            yield return new object[] { Enumerable.Repeat(42, 1), 42 };
            yield return new object[] { Enumerable.Range(1, 10).ToArray(), 1 };
            yield return new object[] { new int[] { -1, -10, 10, 200, 1000 }, -10 };
            yield return new object[] { new int[] { 3000, 100, 200, 1000 }, 100 };
            yield return new object[] { new int[] { 3000, 100, 200, 1000 }.Concat(Enumerable.Repeat(int.MinValue, 1)), int.MinValue };

            yield return new object[] { Enumerable.Repeat(20, 1), 20 };
            yield return new object[] { Enumerable.Repeat(-2, 5), -2 };
            yield return new object[] { Enumerable.Range(1, 10).ToArray(), 1 };
            yield return new object[] { new int[] { 6, 9, 10, 7, 8 }, 6 };
            yield return new object[] { new int[] { 6, 9, 10, 0, -5 }, -5 };
            yield return new object[] { new int[] { 6, 0, 9, 0, 10, 0 }, 0 };
        }
        
        [Theory]
        [MemberData(nameof(Min_Int_TestData))]
        public void Min_Int(IEnumerable<int> source, int expected)
        {
            Assert.Equal(expected, source.Min());
            Assert.Equal(expected, source.Min(x => x));
        }

        [Fact]
        public void Min_Int_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).Min());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).Min(x => x));
        }

        [Fact]
        public void Min_Int_EmptySource_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<int>().Min());
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<int>().Min(x => x));
        }

        public static IEnumerable<object[]> Min_Long_TestData()
        {
            yield return new object[] { Enumerable.Repeat(42L, 1), 42L };
            yield return new object[] { Enumerable.Range(1, 10).Select(i => (long)i).ToArray(), 1L };
            yield return new object[] { new long[] { -1, -10, 10, 200, 1000 }, -10L };
            yield return new object[] { new long[] { 3000, 100, 200, 1000 }, 100L };
            yield return new object[] { new long[] { 3000, 100, 200, 1000 }.Concat(Enumerable.Repeat(long.MinValue, 1)), long.MinValue };

            yield return new object[] { Enumerable.Repeat(int.MaxValue + 10L, 1), int.MaxValue + 10L };
            yield return new object[] { Enumerable.Repeat(500L, 5), 500L };
            yield return new object[] { new long[] { -250, 49, 130, 47, 28 }, -250L };
            yield return new object[] { new long[] { 6, 9, 10, 0, -int.MaxValue - 50L }, -int.MaxValue - 50L };
            yield return new object[] { new long[] { 6, -5, 9, -5, 10, -5 }, -5 };
        }

        [Theory]
        [MemberData(nameof(Min_Long_TestData))]
        public void Min_Long(IEnumerable<long> source, long expected)
        {
            Assert.Equal(expected, source.Min());
            Assert.Equal(expected, source.Min(x => x));
        }

        [Fact]
        public void Min_Long_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<long>)null).Min());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<long>)null).Min(x => x));
        }

        [Fact]
        public void Min_Long_EmptySource_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<long>().Min());
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<long>().Min(x => x));
        }

        public static IEnumerable<object[]> Min_Float_TestData()
        {
            yield return new object[] { Enumerable.Repeat(42f, 1), 42f };
            yield return new object[] { Enumerable.Range(1, 10).Select(i => (float)i).ToArray(), 1f };
            yield return new object[] { new float[] { -1, -10, 10, 200, 1000 }, -10f };
            yield return new object[] { new float[] { 3000, 100, 200, 1000 }, 100 };
            yield return new object[] { new float[] { 3000, 100, 200, 1000 }.Concat(Enumerable.Repeat(float.MinValue, 1)), float.MinValue };

            yield return new object[] { Enumerable.Repeat(5.5f, 1), 5.5f };
            yield return new object[] { Enumerable.Repeat(float.NaN, 5), float.NaN };
            yield return new object[] { new float[] { -2.5f, 4.9f, 130f, 4.7f, 28f }, -2.5f};
            yield return new object[] { new float[] { 6.8f, 9.4f, 10f, 0, -5.6f }, -5.6f };
            yield return new object[] { new float[] { -5.5f, float.NegativeInfinity, 9.9f, float.NegativeInfinity }, float.NegativeInfinity };

            yield return new object[] { new float[] { float.NaN, 6.8f, 9.4f, 10f, 0, -5.6f }, float.NaN };
            yield return new object[] { new float[] { 6.8f, 9.4f, 10f, 0, -5.6f, float.NaN }, float.NaN };
            yield return new object[] { new float[] { float.NaN, float.NegativeInfinity }, float.NaN };
            yield return new object[] { new float[] { float.NegativeInfinity, float.NaN }, float.NaN };

            // In .NET Core, Enumerable.Min shortcircuits if it finds any float.NaN in the array,
            // as nothing can be less than float.NaN. See https://github.com/dotnet/corefx/pull/2426.
            // Without this optimization, we would iterate through int.MaxValue elements, which takes
            // a long time.
            if (!PlatformDetection.IsFullFramework)
            {
                yield return new object[] { Enumerable.Repeat(float.NaN, int.MaxValue), float.NaN };
            }
            yield return new object[] { Enumerable.Repeat(float.NaN, 3), float.NaN };

            // Normally NaN < anything is false, as is anything < NaN
            // However, this leads to some irksome outcomes in Min and Max.
            // If we use those semantics then Min(NaN, 5.0) is NaN, but
            // Min(5.0, NaN) is 5.0!  To fix this, we impose a total
            // ordering where NaN is smaller than every value, including
            // negative infinity.
            yield return new object[] { Enumerable.Range(1, 10).Select(i => (float)i).Concat(Enumerable.Repeat(float.NaN, 1)).ToArray(), float.NaN };
            yield return new object[] { new float[] { -1F, -10, float.NaN, 10, 200, 1000 }, float.NaN };
            yield return new object[] { new float[] { float.MinValue, 3000F, 100, 200, float.NaN, 1000 }, float.NaN };
        }

        [Theory]
        [MemberData(nameof(Min_Float_TestData))]
        public void Min_Float(IEnumerable<float> source, float expected)
        {
            Assert.Equal(expected, source.Min());
            Assert.Equal(expected, source.Min(x => x));
        }

        [Fact]
        public void Min_Float_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<float>)null).Min());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<float>)null).Min(x => x));
        }

        [Fact]
        public void Min_Float_EmptySource_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<float>().Min());
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<float>().Min(x => x));
        }

        public static IEnumerable<object[]> Min_Double_TestData()
        {
            yield return new object[] { Enumerable.Repeat(42.0, 1), 42.0 };
            yield return new object[] { Enumerable.Range(1, 10).Select(i => (double)i).ToArray(), 1.0 };
            yield return new object[] { new double[] { -1, -10, 10, 200, 1000 }, -10.0 };
            yield return new object[] { new double[] { 3000, 100, 200, 1000 }, 100.0 };
            yield return new object[] { new double[] { 3000, 100, 200, 1000 }.Concat(Enumerable.Repeat(double.MinValue, 1)), double.MinValue };

            yield return new object[] { Enumerable.Repeat(5.5, 1), 5.5 };
            yield return new object[] { new double[] { -2.5, 4.9, 130, 4.7, 28 }, -2.5 };
            yield return new object[] { new double[] { 6.8, 9.4, 10, 0, -5.6 }, -5.6 };
            yield return new object[] { new double[] { -5.5, double.NegativeInfinity, 9.9, double.NegativeInfinity }, double.NegativeInfinity };

            // In .NET Core, Enumerable.Min shortcircuits if it finds any double.NaN in the array,
            // as nothing can be less than double.NaN. See https://github.com/dotnet/corefx/pull/2426.
            // Without this optimization, we would iterate through int.MaxValue elements, which takes
            // a long time.
            if (!PlatformDetection.IsFullFramework)
            {
                yield return new object[] { Enumerable.Repeat(double.NaN, int.MaxValue), double.NaN };
            }
            yield return new object[] { Enumerable.Repeat(double.NaN, 3), double.NaN };

            yield return new object[] { new double[] { double.NaN, 6.8, 9.4, 10, 0, -5.6 }, double.NaN };
            yield return new object[] { new double[] { 6.8, 9.4, 10, 0, -5.6, double.NaN }, double.NaN };
            yield return new object[] { new double[] { double.NaN, double.NegativeInfinity }, double.NaN };
            yield return new object[] { new double[] { double.NegativeInfinity, double.NaN }, double.NaN };

            // Normally NaN < anything is false, as is anything < NaN
            // However, this leads to some irksome outcomes in Min and Max.
            // If we use those semantics then Min(NaN, 5.0) is NaN, but
            // Min(5.0, NaN) is 5.0!  To fix this, we impose a total
            // ordering where NaN is smaller than every value, including
            // negative infinity.
            yield return new object[] { Enumerable.Range(1, 10).Select(i => (double)i).Concat(Enumerable.Repeat(double.NaN, 1)).ToArray(), double.NaN };
            yield return new object[] { new double[] { -1, -10, double.NaN, 10, 200, 1000 }, double.NaN };
            yield return new object[] { new double[] { double.MinValue, 3000F, 100, 200, double.NaN, 1000 }, double.NaN };
        }

        [Theory]
        [MemberData(nameof(Min_Double_TestData))]
        public void Min_Double(IEnumerable<double> source, double expected)
        {
            Assert.Equal(expected, source.Min());
            Assert.Equal(expected, source.Min(x => x));
        }

        [Fact]
        public void Min_Double_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<double>)null).Min());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<double>)null).Min(x => x));
        }

        [Fact]
        public void Min_Double_EmptySource_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<double>().Min());
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<double>().Min(x => x));
        }

        public static IEnumerable<object[]> Min_Decimal_TestData()
        {
            yield return new object[] { Enumerable.Repeat(42m, 1), 42m };
            yield return new object[] { Enumerable.Range(1, 10).Select(i => (decimal)i).ToArray(), 1m };
            yield return new object[] { new decimal[] { -1, -10, 10, 200, 1000 }, -10m };
            yield return new object[] { new decimal[] { 3000, 100, 200, 1000 }, 100m };
            yield return new object[] { new decimal[] { 3000, 100, 200, 1000 }.Concat(Enumerable.Repeat(decimal.MinValue, 1)), decimal.MinValue };

            yield return new object[] { Enumerable.Repeat(5.5m, 1), 5.5m };
            yield return new object[] { Enumerable.Repeat(-3.4m, 5), -3.4m };
            yield return new object[] { new decimal[] { -2.5m, 4.9m, 130m, 4.7m, 28m }, -2.5m };
            yield return new object[] { new decimal[] { 6.8m, 9.4m, 10m, 0m, 0m, decimal.MinValue }, decimal.MinValue };
            yield return new object[] { new decimal[] { -5.5m, 0m, 9.9m, -5.5m, 5m }, -5.5m };
        }

        [Theory]
        [MemberData(nameof(Min_Decimal_TestData))]
        public void Min_Decimal(IEnumerable<decimal> source, decimal expected)
        {
            Assert.Equal(expected, source.Min());
            Assert.Equal(expected, source.Min(x => x));
        }

        [Fact]
        public void Min_Decimal_EmptySource_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<decimal>().Min());
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<decimal>().Min(x => x));
        }

        [Fact]
        public void Min_Decimal_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<decimal>)null).Min());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<decimal>)null).Min(x => x));
        }

        public static IEnumerable<object[]> Min_NullableInt_TestData()
        {
            yield return new object[] { Enumerable.Range(1, 10).Select(i => (int?)i).ToArray(), 1 };
            yield return new object[] { new int?[] { null, -1, -10, 10, 200, 1000 }, -10 };
            yield return new object[] { new int?[] { null, 3000, 100, 200, 1000 }, 100 };
            yield return new object[] { new int?[] { null, 3000, 100, 200, 1000 }.Concat(Enumerable.Repeat((int?)int.MinValue, 1)), int.MinValue };
            yield return new object[] { Enumerable.Repeat(default(int?), 100), null };
            yield return new object[] { Enumerable.Repeat((int?)42, 1), 42 };

            yield return new object[] { Enumerable.Empty<int?>(), null };
            yield return new object[] { Enumerable.Repeat((int?)20, 1), 20 };
            yield return new object[] { Enumerable.Repeat(default(int?), 5), null };
            yield return new object[] { new int?[] { 6, null, 9, 10, null, 7, 8 }, 6 };
            yield return new object[] { new int?[] { null, null, null, null, null, -5 }, -5 };
            yield return new object[] { new int?[] { 6, null, null, 0, 9, 0, 10, 0 }, 0 };
        }

        [Theory]
        [MemberData(nameof(Min_NullableInt_TestData))]
        public void Min_NullableInt(IEnumerable<int?> source, int? expected)
        {
            Assert.Equal(expected, source.Min());
        }

        [Theory, MemberData(nameof(Min_NullableInt_TestData))]
        public void Min_NullableIntRunOnce(IEnumerable<int?> source, int? expected)
        {
            Assert.Equal(expected, source.RunOnce().Min());
        }

        [Fact]
        public void Min_NullableInt_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int?>)null).Min());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int?>)null).Min(x => x));
        }

        public static IEnumerable<object[]> Min_NullableLong_TestData()
        {
            yield return new object[] { Enumerable.Range(1, 10).Select(i => (long?)i).ToArray(), 1L };
            yield return new object[] { new long?[] { null, -1, -10, 10, 200, 1000 }, -10L };
            yield return new object[] { new long?[] { null, 3000, 100, 200, 1000 }, 100L };
            yield return new object[] { new long?[] { null, 3000, 100, 200, 1000 }.Concat(Enumerable.Repeat((long?)long.MinValue, 1)), long.MinValue };
            yield return new object[] { Enumerable.Repeat(default(long?), 100), null };
            yield return new object[] { Enumerable.Repeat((long?)42, 1), 42L };

            yield return new object[] { Enumerable.Empty<long?>(), null };
            yield return new object[] { Enumerable.Repeat((long?)long.MaxValue, 1), long.MaxValue };
            yield return new object[] { Enumerable.Repeat(default(long?), 5), null };
            yield return new object[] { new long?[] { long.MinValue, null, 9, 10, null, 7, 8 }, long.MinValue };
            yield return new object[] { new long?[] { null, null, null, null, null, -long.MaxValue }, -long.MaxValue };
            yield return new object[] { new long?[] { 6, null, null, 0, 9, 0, 10, 0 }, 0L };
        }

        [Theory]
        [MemberData(nameof(Min_NullableLong_TestData))]
        public void Min_NullableLong(IEnumerable<long?> source, long? expected)
        {
            Assert.Equal(expected, source.Min());
        }

        [Fact]
        public void Min_NullableLong_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<long?>)null).Min());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<long?>)null).Min(x => x));
        }

        public static IEnumerable<object[]> Min_NullableFloat_TestData()
        {
            yield return new object[] { Enumerable.Range(1, 10).Select(i => (float?)i).ToArray(), 1f };
            yield return new object[] { new float?[] { null, -1, -10, 10, 200, 1000 }, -10f };
            yield return new object[] { new float?[] { null, 3000, 100, 200, 1000 }, 100f };
            yield return new object[] { new float?[] { null, 3000, 100, 200, 1000 }.Concat(Enumerable.Repeat((float?)float.MinValue, 1)), float.MinValue };
            yield return new object[] { Enumerable.Repeat(default(float?), 100), null };
            yield return new object[] { Enumerable.Repeat((float?)42, 1), 42f };

            yield return new object[] { Enumerable.Empty<float?>(), null };
            yield return new object[] { Enumerable.Repeat((float?)float.MinValue, 1), float.MinValue };
            yield return new object[] { Enumerable.Repeat(default(float?), 100), null };
            yield return new object[] { new float?[] { -4.50f, null, 10.98f, null, 7.5f, 8.6f }, -4.5f };
            yield return new object[] { new float?[] { null, null, null, null, null, 0f }, 0f };
            yield return new object[] { new float?[] { 6.4f, null, null, -0.5f, 9.4f, -0.5f, 10.9f, -0.5f }, -0.5f };

            yield return new object[] { new float?[] { float.NaN, 6.8f, 9.4f, 10f, 0, null, -5.6f }, float.NaN };
            yield return new object[] { new float?[] { 6.8f, 9.4f, 10f, 0, null, -5.6f, float.NaN }, float.NaN };
            yield return new object[] { new float?[] { float.NaN, float.NegativeInfinity }, float.NaN };
            yield return new object[] { new float?[] { float.NegativeInfinity, float.NaN }, float.NaN };
            yield return new object[] { new float?[] { float.NaN, null, null, null }, float.NaN };
            yield return new object[] { new float?[] { null, null, null, float.NaN }, float.NaN };
            yield return new object[] { new float?[] { null, float.NaN, null }, float.NaN };

            // In .NET Core, Enumerable.Min shortcircuits if it finds any float.NaN in the array,
            // as nothing can be less than float.NaN. See https://github.com/dotnet/corefx/pull/2426.
            // Without this optimization, we would iterate through int.MaxValue elements, which takes
            // a long time.
            if (!PlatformDetection.IsFullFramework)
            {
                yield return new object[] { Enumerable.Repeat((float?)float.NaN, int.MaxValue), float.NaN };
            }
            yield return new object[] { Enumerable.Repeat((float?)float.NaN, 3), float.NaN };
        }

        [Theory]
        [MemberData(nameof(Min_NullableFloat_TestData))]
        public void Min_NullableFloat(IEnumerable<float?> source, float? expected)
        {
            Assert.Equal(expected, source.Min());
            Assert.Equal(expected, source.Min(x => x));
        }

        [Fact]
        public void Min_NullableFloat_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<float?>)null).Min());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<float?>)null).Min(x => x));
        }

        public static IEnumerable<object[]> Min_NullableDouble_TestData()
        {
            yield return new object[] { Enumerable.Range(1, 10).Select(i => (double?)i).ToArray(), 1.0 };
            yield return new object[] { new double?[] { null, -1, -10, 10, 200, 1000 }, -10.0 };
            yield return new object[] { new double?[] { null, 3000, 100, 200, 1000 }, 100.0 };
            yield return new object[] { new double?[] { null, 3000, 100, 200, 1000 }.Concat(Enumerable.Repeat((double?)double.MinValue, 1)), double.MinValue };
            yield return new object[] { Enumerable.Repeat(default(double?), 100), null };
            yield return new object[] { Enumerable.Repeat((double?)42, 1), 42.0 };

            yield return new object[] { Enumerable.Empty<double?>(), null };
            yield return new object[] { Enumerable.Repeat((double?)double.MinValue, 1), double.MinValue };
            yield return new object[] { Enumerable.Repeat(default(double?), 5), null };
            yield return new object[] { new double?[] { -4.50, null, 10.98, null, 7.5, 8.6 }, -4.5 };
            yield return new object[] { new double?[] { null, null, null, null, null, 0 }, 0.0 };
            yield return new object[] { new double?[] { 6.4, null, null, -0.5, 9.4, -0.5, 10.9, -0.5 }, -0.5 };

            yield return new object[] { new double?[] { double.NaN, 6.8, 9.4, 10.0, 0.0, null, -5.6 }, double.NaN };
            yield return new object[] { new double?[] { 6.8, 9.4, 10, 0.0, null, -5.6f, double.NaN }, double.NaN };
            yield return new object[] { new double?[] { double.NaN, double.NegativeInfinity }, double.NaN };
            yield return new object[] { new double?[] { double.NegativeInfinity, double.NaN }, double.NaN };
            yield return new object[] { new double?[] { double.NaN, null, null, null }, double.NaN };
            yield return new object[] { new double?[] { null, null, null, double.NaN }, double.NaN };
            yield return new object[] { new double?[] { null, double.NaN, null }, double.NaN };

            // In .NET Core, Enumerable.Min shortcircuits if it finds any double.NaN in the array,
            // as nothing can be less than double.NaN. See https://github.com/dotnet/corefx/pull/2426.
            // Without this optimization, we would iterate through int.MaxValue elements, which takes
            // a long time.
            if (!PlatformDetection.IsFullFramework)
            {
                yield return new object[] { Enumerable.Repeat((double?)double.NaN, int.MaxValue), double.NaN };
            }
            yield return new object[] { Enumerable.Repeat((double?)double.NaN, 3), double.NaN };
        }

        [Theory]
        [MemberData(nameof(Min_NullableDouble_TestData))]
        public void Min_NullableDouble(IEnumerable<double?> source, double? expected)
        {
            Assert.Equal(expected, source.Min());
            Assert.Equal(expected, source.Min(x => x));
        }

        [Fact]
        public void Min_NullableDouble_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<double?>)null).Min());
        }

        public static IEnumerable<object[]> Min_NullableDecimal_TestData()
        {
            yield return new object[] { Enumerable.Range(1, 10).Select(i => (decimal?)i).ToArray(), 1m };
            yield return new object[] { new decimal?[] { null, -1, -10, 10, 200, 1000 }, -10m };
            yield return new object[] { new decimal?[] { null, 3000, 100, 200, 1000 }, 100m };
            yield return new object[] { new decimal?[] { null, 3000, 100, 200, 1000 }.Concat(Enumerable.Repeat((decimal?)decimal.MinValue, 1)), decimal.MinValue };
            yield return new object[] { Enumerable.Repeat(default(decimal?), 100), null };
            yield return new object[] { Enumerable.Repeat((decimal?)42, 1), 42m };

            yield return new object[] { Enumerable.Empty<decimal?>(), null };
            yield return new object[] { Enumerable.Repeat((decimal?)decimal.MaxValue, 1), decimal.MaxValue };
            yield return new object[] { Enumerable.Repeat(default(decimal?), 5), null };
            yield return new object[] { new decimal?[] { -4.50m, null, null, 10.98m, null, 7.5m, 8.6m }, -4.5m };
            yield return new object[] { new decimal?[] { null, null, null, null, null, 0m }, 0m };
            yield return new object[] { new decimal?[] { 6.4m, null, null, decimal.MinValue, 9.4m, decimal.MinValue, 10.9m, decimal.MinValue }, decimal.MinValue };
        }

        [Theory]
        [MemberData(nameof(Min_NullableDecimal_TestData))]
        public void Min_NullableDecimal(IEnumerable<decimal?> source, decimal? expected)
        {
            Assert.Equal(expected, source.Min());
            Assert.Equal(expected, source.Min(x => x));
        }

        [Fact]
        public void Min_NullableDecimal_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<decimal?>)null).Min());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<decimal?>)null).Min(x => x));
        }
        
        public static IEnumerable<object[]> Min_DateTime_TestData()
        {
            yield return new object[] { Enumerable.Range(1, 10).Select(i => new DateTime(2000, 1, i)).ToArray(), new DateTime(2000, 1, 1) };
            yield return new object[] { new DateTime[] { new DateTime(2000, 12, 1), new DateTime(2000, 1, 1), new DateTime(2000, 1, 12) }, new DateTime(2000, 1, 1) };

            DateTime[] hundred = new DateTime[]
            {
                new DateTime(3000, 1, 1),
                new DateTime(100, 1, 1),
                new DateTime(200, 1, 1),
                new DateTime(1000, 1, 1)
            };
            yield return new object[] { hundred, new DateTime(100, 1, 1) };
            yield return new object[] { hundred.Concat(Enumerable.Repeat(DateTime.MinValue, 1)), DateTime.MinValue };
        }

        [Theory]
        [MemberData(nameof(Min_DateTime_TestData))]
        public void Min_DateTime(IEnumerable<DateTime> source, DateTime expected)
        {
            Assert.Equal(expected, source.Min());
            Assert.Equal(expected, source.Min(x => x));
        }

        [Fact]
        public void Min_DateTime_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<DateTime>)null).Min());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<DateTime>)null).Min(x => x));
        }

        [Fact]
        public void Min_DateTime_EmptySource_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<DateTime>().Min());
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<DateTime>().Min(x => x));
        }

        public static IEnumerable<object[]> Min_String_TestData()
        {
            yield return new object[] { Enumerable.Range(1, 10).Select(i => i.ToString()).ToArray(), "1" };
            yield return new object[] { new string[] { "Alice", "Bob", "Charlie", "Eve", "Mallory", "Trent", "Victor" }, "Alice" };
            yield return new object[] { new string[] { null, "Charlie", null, "Victor", "Trent", null, "Eve", "Alice", "Mallory", "Bob" }, "Alice" };

            yield return new object[] { Enumerable.Empty<string>(), null };
            yield return new object[] { Enumerable.Repeat("Hello", 1), "Hello" };
            yield return new object[] { Enumerable.Repeat("hi", 5), "hi" };
            yield return new object[] { new string[] { "aaa", "abcd", "bark", "temp", "cat" }, "aaa" };
            yield return new object[] { new string[] { null, null, null, null, "aAa" }, "aAa" };
            yield return new object[] { new string[] { "ooo", "www", "www", "ooo", "ooo", "ppp" }, "ooo" };
            yield return new object[] { Enumerable.Repeat(default(string), 5), null };
        }

        [Theory]
        [MemberData(nameof(Min_String_TestData))]
        public void Min_String(IEnumerable<string> source, string expected)
        {
            Assert.Equal(expected, source.Min());
            Assert.Equal(expected, source.Min(x => x));
        }

        [Theory, MemberData(nameof(Min_String_TestData))]
        public void Min_StringRunOnce(IEnumerable<string> source, string expected)
        {
            Assert.Equal(expected, source.RunOnce().Min());
            Assert.Equal(expected, source.RunOnce().Min(x => x));
        }

        [Fact]
        public void Min_String_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<string>)null).Min());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<string>)null).Min(x => x));
        }
        
        [Fact]
        public void Min_Int_WithSelectorAccessingProperty()
        {
            var source = new[]
            {
                new { name="Tim", num=10 },
                new { name="John", num=-105 },
                new { name="Bob", num=-30 }
            };
            Assert.Equal(-105, source.Min(e => e.num));
        }
        
        [Fact]
        public void Min_Int_NullSelector_ThrowsArgumentNullException()
        {
            Func<int, int> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<int>().Min(selector));
        }

        [Fact]
        public void Min_Long_WithSelectorAccessingProperty()
        {
            var source = new[]
            {
                new { name="Tim", num=10L },
                new { name="John", num=long.MinValue },
                new { name="Bob", num=-10L }
            };

            Assert.Equal(long.MinValue, source.Min(e => e.num));
        }
        
        [Fact]
        public void Min_Long_NullSelector_ThrowsArgumentNullException()
        {
            Func<long, long> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<long>().Min(selector));
        }

        [Fact]
        public void Min_Float_WithSelectorAccessingProperty()
        {
            var source = new[]
            {
                new { name="Tim", num=-45.5f },
                new { name="John", num=-132.5f },
                new { name="Bob", num=20.45f }
            };
            Assert.Equal(-132.5f, source.Min(e => e.num));
        }

        [Fact]
        public void Min_Float_NullSelector_ThrowsArgumentNullException()
        {
            Func<float, float> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<float>().Min(selector));
        }

        [Fact]
        public void Min_Double_WithSelectorAccessingProperty()
        {
            var source = new[]
            {
                new { name="Tim", num=-45.5 },
                new { name="John", num=-132.5 },
                new { name="Bob", num=20.45 }
            };
            Assert.Equal(-132.5, source.Min(e => e.num));
        }

        [Fact]
        public void Min_Double_NullSelector_ThrowsArgumentNullException()
        {
            Func<double, double> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<double>().Min(selector));
        }

        [Fact]
        public void Min_Decimal_WithSelectorAccessingProperty()
        {
            var source = new[]
            {
                new {name="Tim", num=100.45m},
                new {name="John", num=10.5m},
                new {name="Bob", num=0.05m}
            };
            Assert.Equal(0.05m, source.Min(e => e.num));
        }

        [Fact]
        public void Min_Decimal_NullSelector_ThrowsArgumentNullException()
        {
            Func<decimal, decimal> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<decimal>().Min(selector));
        }

        [Fact]
        public void Min_NullableInt_WithSelectorAccessingProperty()
        {
            var source = new[]
            {
                new { name="Tim", num=(int?)10 },
                new { name="John", num=default(int?) },
                new { name="Bob", num=(int?)-30 }
            };
            Assert.Equal(-30, source.Min(e => e.num));
        }

        [Fact]
        public void Min_NullableInt_NullSelector_ThrowsArgumentNullException()
        {
            Func<int?, int?> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<int?>().Min(selector));
        }

        [Fact]
        public void Min_NullableLong_WithSelectorAccessingProperty()
        {
            var source = new[]
            {
                new { name="Tim", num=default(long?) },
                new { name="John", num=(long?)long.MinValue },
                new { name="Bob", num=(long?)-10L }
            };
            Assert.Equal(long.MinValue, source.Min(e => e.num));
        }

        [Fact]
        public void Min_NullableLong_NullSelector_ThrowsArgumentNullException()
        {
            Func<long?, long?> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<long?>().Min(selector));
        }

        [Fact]
        public void Min_NullableFloat_WithSelectorAccessingProperty()
        {
            var source = new[]
            {
                new {name="Tim", num=(float?)-45.5f},
                new {name="John", num=(float?)-132.5f},
                new {name="Bob", num=default(float?)}
            };

            Assert.Equal(-132.5f, source.Min(e => e.num));
        }

        [Fact]
        public void Min_NullableFloat_NullSelector_ThrowsArgumentNullException()
        {
            Func<float?, float?> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<float?>().Min(selector));
        }
        
        [Fact]
        public void Min_NullableDouble_WithSelectorAccessingProperty()
        {
            var source = new[]
            {
                new { name="Tim", num=(double?)-45.5 },
                new { name="John", num=(double?)-132.5 },
                new { name="Bob", num=default(double?) }
            };
            Assert.Equal(-132.5, source.Min(e => e.num));
        }

        [Fact]
        public void Min_NullableDouble_NullSelector_ThrowsArgumentNullException()
        {
            Func<double?, double?> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<double?>().Min(selector));
        }

        [Fact]
        public void Min_NullableDecimal_WithSelectorAccessingProperty()
        {
            var source = new[]
            {
                new { name="Tim", num=(decimal?)100.45m },
                new { name="John", num=(decimal?)10.5m },
                new { name="Bob", num=default(decimal?) }
            };
            Assert.Equal(10.5m, source.Min(e => e.num));
        }
        
        [Fact]
        public void Min_NullableDecimal_NullSelector_ThrowsArgumentNullException()
        {
            Func<decimal?, decimal?> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<decimal?>().Min(selector));
        }
                
        [Fact]
        public void Min_DateTime_NullSelector_ThrowsArgumentNullException()
        {
            Func<DateTime, DateTime> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<DateTime>().Min(selector));
        }
        
        [Fact]
        public void Min_String_WithSelectorAccessingProperty()
        {
            var source = new[]
            {
                new { name="Tim", num=100.45m },
                new { name="John", num=10.5m },
                new { name="Bob", num=0.05m }
            };
            Assert.Equal("Bob", source.Min(e => e.name));
        }
        
        [Fact]
        public void Min_String_NullSelector_ThrowsArgumentNullException()
        {
            Func<string, string> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<string>().Min(selector));
        }

        [Fact]
        public void Min_Bool_EmptySource_ThrowsInvalodOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<bool>().Min());
        }
    }
}
