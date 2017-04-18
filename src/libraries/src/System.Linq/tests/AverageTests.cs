// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class AverageTests : EnumerableTests
    {
        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                    where x > int.MinValue
                    select x;

            Assert.Equal(q.Average(), q.Average());
        }

        [Fact]
        public void SameResultsRepeatCallsNullableLongQuery()
        {
            var q = from x in new long?[] { int.MaxValue, 0, 255, 127, 128, 1, 33, 99, null, int.MinValue }
                    select x;

            Assert.Equal(q.Average(), q.Average());
        }

        public static IEnumerable<object[]> NullableFloat_TestData()
        {
            yield return new object[] { new float?[0], null };
            yield return new object[] { new float?[] { float.MinValue }, float.MinValue };
            yield return new object[] { new float?[] { 0f, 0f, 0f, 0f, 0f }, 0f };

            yield return new object[] { new float?[] { 5.5f, 0, null, null, null, 15.5f, 40.5f, null, null, -23.5f }, 7.6f };

            yield return new object[] { new float?[] { null, null, null, null, 45f }, 45f };
            yield return new object[] { new float?[] { null, null, null, null, null }, null };
        }

        [Theory]
        [MemberData(nameof(NullableFloat_TestData))]
        public void NullableFoat(float?[] source, float? expected)
        {
            Assert.Equal(expected, source.Average());
            Assert.Equal(expected, source.Average(x => x));
        }

        [Theory, MemberData(nameof(NullableFloat_TestData))]
        public void NullableFoatRunOnce(float?[] source, float? expected)
        {
            Assert.Equal(expected, source.RunOnce().Average());
            Assert.Equal(expected, source.RunOnce().Average(x => x));
        }

        [Fact]
        public void NullableFloat_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<float?>)null).Average());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<float?>)null).Average(i => i));
        }

        [Fact]
        public void NullableFloat_NullSelector_ThrowsArgumentNullException()
        {
            Func<float?, float?> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<float?>().Average(selector));
        }
        
        [Fact]
        public void NullableFloat_WithSelector()
        {
            var source = new []
            {
                new { name = "Tim", num = (float?)5.5f },
                new { name = "John", num = (float?)15.5f },
                new { name = "Bob", num = default(float?) }
            };
            float? expected = 10.5f;

            Assert.Equal(expected, source.Average(e => e.num));
        }

        [Fact]
        public void Int_EmptySource_ThrowsInvalidOperationException()
        {
            int[] source = new int[0];
            
            Assert.Throws<InvalidOperationException>(() => source.Average());
            Assert.Throws<InvalidOperationException>(() => source.Average(i => i));
        }

        [Fact]
        public void Int_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).Average());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).Average(i => i));
        }

        [Fact]
        public void Int_NullSelector_ThrowsArgumentNullException()
        {
            Func<int, int> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<int>().Average(selector));
        }

        public static IEnumerable<object[]> Int_TestData()
        {
            yield return new object[] { new int[] { 5 }, 5 };
            yield return new object[] { new int[] { 0, 0, 0, 0, 0 }, 0 };
            yield return new object[] { new int[] { 5, -10, 15, 40, 28 }, 15.6 };
        }

        [Theory]
        [MemberData(nameof(Int_TestData))]
        public void Int(int[] source, double expected)
        {
            Assert.Equal(expected, source.Average());
            Assert.Equal(expected, source.Average(x => x));
        }

        [Theory, MemberData(nameof(Int_TestData))]
        public void IntRunOnce(int[] source, double expected)
        {
            Assert.Equal(expected, source.RunOnce().Average());
            Assert.Equal(expected, source.RunOnce().Average(x => x));
        }

        [Fact]
        public void Int_WithSelector()
        {
            var source = new []
            {
                new { name="Tim", num = 10 },
                new { name="John", num = -10 },
                new { name="Bob", num = 15 }
            };
            double expected = 5;

            Assert.Equal(expected, source.Average(e => e.num));
        }

        public static IEnumerable<object[]> NullableInt_TestData()
        {
            yield return new object[] { new int?[0], null };
            yield return new object[] { new int?[] { -5 }, -5.0 };
            yield return new object[] { new int?[] { 0, 0, 0, 0, 0 }, 0.0 };
            yield return new object[] { new int?[] { 5, -10, null, null, null, 15, 40, 28, null, null }, 15.6 };
            yield return new object[] { new int?[] { null, null, null, null, 50 }, 50.0 };
            yield return new object[] { new int?[] { null, null, null, null, null }, null };
        }

        [Theory]
        [MemberData(nameof(NullableInt_TestData))]
        public void NullableInt(int?[] source, double? expected)
        {
            Assert.Equal(expected, source.Average());
            Assert.Equal(expected, source.Average(x => x));
        }
        
        [Fact]
        public void NullableInt_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int?>)null).Average());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int?>)null).Average(i => i));
        }

        [Fact]
        public void NullableInt_NullSelector_ThrowsArgumentNullException()
        {
            Func<int?, int?> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<int?>().Average(selector));
        }
        
        [Fact]
        public void NullableInt_WithSelector()
        {
            var source = new []
            {
                new { name = "Tim", num  = (int?)10 },
                new { name = "John", num =  default(int?) },
                new { name = "Bob", num = (int?)10 }
            };
            double? expected = 10;

            Assert.Equal(expected, source.Average(e => e.num));
        }

        [Fact]
        public void Long_EmptySource_ThrowsInvalidOperationException()
        {
            long[] source = new long[0];

            Assert.Throws<InvalidOperationException>(() => source.Average());
            Assert.Throws<InvalidOperationException>(() => source.Average(i => i));
        }

        [Fact]
        public void Long_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<long>)null).Average());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<long>)null).Average(i => i));
        }

        [Fact]
        public void Long_NullSelector_ThrowsArgumentNullException()
        {
            Func<long, long> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<long>().Average(selector));
        }

        public static IEnumerable<object[]> Long_TestData()
        {
            yield return new object[] { new long[] { long.MaxValue }, long.MaxValue };
            yield return new object[] { new long[] { 0, 0, 0, 0, 0 }, 0 };
            yield return new object[] { new long[] { 5, -10, 15, 40, 28 }, 15.6 };
        }
        
        [Theory]
        [MemberData(nameof(Long_TestData))]
        public void Long(long[] source, double expected)
        {
            Assert.Equal(expected, source.Average());
            Assert.Equal(expected, source.Average(x => x));
        }

        [Fact]
        public void Long_FromSelector()
        {
            var source = new []
            {
                new { name = "Tim", num = 40L },
                new { name = "John", num = 50L },
                new { name = "Bob", num = 60L }
            };
            double expected = 50;

            Assert.Equal(expected, source.Average(e => e.num));
        }

        [Fact]
        public void Long_SumTooLarge_ThrowsOverflowException()
        {
            long[] source = new long[] { long.MaxValue, long.MaxValue };

            Assert.Throws<OverflowException>(() => source.Average());
        }

        public static IEnumerable<object[]> NullableLong_TestData()
        {
            yield return new object[] { new long?[0], null };
            yield return new object[] { new long?[] { long.MaxValue }, (double)long.MaxValue };
            yield return new object[] { new long?[] { 0, 0, 0, 0, 0 }, 0.0 };
            yield return new object[] { new long?[] { 5, -10, null, null, null, 15, 40, 28, null, null }, 15.6 };
            yield return new object[] { new long?[] { null, null, null, null, 50 }, 50.0 };
            yield return new object[] { new long?[] { null, null, null, null, null }, null };
        }

        [Theory]
        [MemberData(nameof(NullableLong_TestData))]
        public void NullableLong(long?[] source, double? expected)
        {
            Assert.Equal(expected, source.Average());
            Assert.Equal(expected, source.Average(x => x));
        }
        
        [Fact]
        public void NullableLong_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<long?>)null).Average());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<long?>)null).Average(i => i));
        }

        [Fact]
        public void NullableLong_NullSelector_ThrowsArgumentNullException()
        {
            Func<long?, long?> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<long?>().Average(selector));
        }
        
        [Fact]
        public void NullableLong_WithSelector()
        {
            var source = new []
            {
                new { name = "Tim", num = (long?)40L },
                new { name = "John", num = default(long?) },
                new { name = "Bob", num = (long?)30L }
            };
            double? expected = 35;

            Assert.Equal(expected, source.Average(e => e.num));
        }

        [Fact]
        public void Double_EmptySource_ThrowsInvalidOperationException()
        {
            double[] source = new double[0];

            Assert.Throws<InvalidOperationException>(() => source.Average());
            Assert.Throws<InvalidOperationException>(() => source.Average(i => i));
        }

        [Fact]
        public void Double_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<double>)null).Average());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<double>)null).Average(i => i));
        }

        [Fact]
        public void Double_NullSelector_ThrowsArgumentNullException()
        {
            Func<double, double> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<double>().Average(selector));
        }

        public static IEnumerable<object[]> Double_TestData()
        {
            yield return new object[] { new double[] { double.MaxValue }, double.MaxValue };
            yield return new object[] { new double[] { 0.0, 0.0, 0.0, 0.0, 0.0 }, 0 };
            yield return new object[] { new double[] { 5.5, -10, 15.5, 40.5, 28.5 }, 16 };
            yield return new object[] { new double[] { 5.58, Double.NaN, 30, 4.55, 19.38 }, double.NaN };
        }

        [Theory]
        [MemberData(nameof(Double_TestData))]
        public void Average_Double(double[] source, double expected)
        {
            Assert.Equal(expected, source.Average());
            Assert.Equal(expected, source.Average(x => x));
        }

        [Fact]
        public void Double_WithSelector()
        {
            var source = new []
            {
                new { name = "Tim", num = 5.5},
                new { name = "John", num = 15.5},
                new { name = "Bob", num = 3.0}
            };
            double expected = 8.0;

            Assert.Equal(expected, source.Average(e => e.num));
        }

        public static IEnumerable<object[]> NullableDouble_TestData()
        {
            yield return new object[] { new double?[0], null };
            yield return new object[] { new double?[] { double.MinValue }, double.MinValue };
            yield return new object[] { new double?[] { 0, 0, 0, 0, 0 }, 0.0 };
            yield return new object[] { new double?[] { 5.5, 0, null, null, null, 15.5, 40.5, null, null, -23.5 }, 7.6 };
            yield return new object[] { new double?[] { null, null, null, null, 45 }, 45.0 };
            yield return new object[] { new double?[] { -23.5, 0, double.NaN, 54.3, 0.56 }, double.NaN };
            yield return new object[] { new double?[] { null, null, null, null, null }, null };
        }

        [Theory]
        [MemberData(nameof(NullableDouble_TestData))]
        public void NullableDouble(double?[] source, double? expected)
        {
            Assert.Equal(expected, source.Average());
            Assert.Equal(expected, source.Average(x => x));
        }
        
        [Fact]
        public void NullableDouble_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<double?>)null).Average());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<double?>)null).Average(i => i));
        }

        [Fact]
        public void NullableDouble_NullSelector_ThrowsArgumentNullException()
        {
            Func<double?, double?> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<double?>().Average(selector));
        }
        
        [Fact]
        public void NullableDouble_WithSelector()
        {
            var source = new[]
            {
                new{ name = "Tim", num = (double?)5.5 },
                new{ name = "John", num = (double?)15.5 },
                new{ name = "Bob", num = default(double?) }
            };
            double? expected = 10.5;

            Assert.Equal(expected, source.Average(e => e.num));
        }

        [Fact]
        public void Decimal_EmptySource_ThrowsInvalidOperationException()
        {
            decimal[] source = new decimal[0];

            Assert.Throws<InvalidOperationException>(() => source.Average());
            Assert.Throws<InvalidOperationException>(() => source.Average(i => i));
        }

        [Fact]
        public void Decimal_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<decimal>)null).Average());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<decimal>)null).Average(i => i));
        }

        [Fact]
        public void Decimal_NullSelector_ThrowsArgumentNullException()
        {
            Func<decimal, decimal> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<decimal>().Average(selector));
        }

        public static IEnumerable<object[]> Decimal_TestData()
        {
            yield return new object[] { new decimal[] { decimal.MaxValue }, decimal.MaxValue };
            yield return new object[] { new decimal[] { 0.0m, 0.0m, 0.0m, 0.0m, 0.0m }, 0 };
            yield return new object[] { new decimal[] { 5.5m, -10m, 15.5m, 40.5m, 28.5m }, 16 };
        }

        [Theory]
        [MemberData(nameof(Decimal_TestData))]
        public void Decimal(decimal[] source, decimal expected)
        {
            Assert.Equal(expected, source.Average());
            Assert.Equal(expected, source.Average(x => x));
        }

        [Fact]
        public void Decimal_WithSelector()
        {
            var source = new[]
            {
                new{ name = "Tim", num = 5.5m},
                new{ name = "John", num = 15.5m},
                new{ name = "Bob", num = 3.0m}
            };
            decimal expected = 8.0m;

            Assert.Equal(expected, source.Average(e => e.num));
        }

        public static IEnumerable<object[]> NullableDecimal_TestData()
        {
            yield return new object[] { new decimal?[0], null };
            yield return new object[] { new decimal?[] { decimal.MinValue }, decimal.MinValue };
            yield return new object[] { new decimal?[] { 0m, 0m, 0m, 0m, 0m }, 0m };
            yield return new object[] { new decimal?[] { 5.5m, 0, null, null, null, 15.5m, 40.5m, null, null, -23.5m }, 7.6m };
            yield return new object[] { new decimal?[] { null, null, null, null, 45m }, 45m };
            yield return new object[] { new decimal?[] { null, null, null, null, null }, null };
        }

        [Theory]
        [MemberData(nameof(NullableDecimal_TestData))]
        public void NullableDecimal(decimal?[] source, decimal? expected)
        {
            Assert.Equal(expected, source.Average());
            Assert.Equal(expected, source.Average(x => x));
        }

        [Fact]
        public void NullableDecimal_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<decimal?>)null).Average());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<decimal?>)null).Average(i => i));
        }

        [Fact]
        public void NullableDecimal_NullSelector_ThrowsArgumentNullException()
        {
            Func<decimal?, decimal?> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<decimal?>().Average(selector));
        }

        [Fact]
        public void NullableDecimal_WithSelector()
        {
            var source = new[]
            {
                new{ name = "Tim", num = (decimal?)5.5m},
                new{ name = "John", num = (decimal?)15.5m},
                new{ name = "Bob", num = (decimal?)null}
            };
            decimal? expected = 10.5m;

            Assert.Equal(expected, source.Average(e => e.num));
        }

        [Fact]
        public void NullableDecimal_SumTooLarge_ThrowsOverflowException()
        {
            decimal?[] source = new decimal?[] { decimal.MaxValue, decimal.MaxValue };

            Assert.Throws<OverflowException>(() => source.Average());
        }

        [Fact]
        public void Float_EmptySource_ThrowsInvalidOperationException()
        {
            float[] source = new float[0];

            Assert.Throws<InvalidOperationException>(() => source.Average());
            Assert.Throws<InvalidOperationException>(() => source.Average(i => i));
        }

        [Fact]
        public void Float_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<float>)null).Average());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<float>)null).Average(i => i));
        }

        [Fact]
        public void Float_NullSelector_ThrowsArgumentNullException()
        {
            Func<float, float> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<float>().Average(selector));
        }

        public static IEnumerable<object[]> Float_TestData()
        {
            yield return new object[] { new float[] { float.MaxValue }, float.MaxValue };
            yield return new object[] { new float[] { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f }, 0f };
            yield return new object[] { new float[] { 5.5f, -10f, 15.5f, 40.5f, 28.5f }, 16f };
        }

        [Theory]
        [MemberData(nameof(Float_TestData))]
        public void Float(float[] source, float expected)
        {
            Assert.Equal(expected, source.Average());
            Assert.Equal(expected, source.Average(x => x));
        }

        [Fact]
        public void Float_WithSelector()
        {
            var source = new[]
            {
                new{ name = "Tim", num = 5.5f},
                new{ name = "John", num = 15.5f},
                new{ name = "Bob", num = 3.0f}
            };
            float expected = 8.0f;

            Assert.Equal(expected, source.Average(e => e.num));
        }
    }
}
