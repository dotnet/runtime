// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq.Expressions;
using Xunit;

namespace System.Linq.Tests
{
    public class AverageTests : EnumerableBasedTests
    {
        [Fact]
        public void NullNFloatSource()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IQueryable<float?>)null).Average());
        }

        [Fact]
        public void NullNFloatSourceWithFunc()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IQueryable<float?>)null).Average(i => i));
        }

        [Fact]
        public void NullNFloatFunc()
        {
            Expression<Func<float?, float?>> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<float?>().AsQueryable().Average(selector));
        }

        [Fact]
        public void MultipleNullableFloatSource()
        {
            float?[] source = { 5.5f, 0, null, null, null, 15.5f, 40.5f, null, null, -23.5f };
            float? expected = 7.6f;

            Assert.Equal(expected, source.AsQueryable().Average());
        }

        [Fact]
        public void NullableFloatFromSelector()
        {
            var source = new []
            {
                new { name = "Tim", num = (float?)5.5f },
                new { name = "John", num = (float?)15.5f },
                new { name = "Bob", num = default(float?) }
            };
            float? expected = 10.5f;

            Assert.Equal(expected, source.AsQueryable().Average(e => e.num));
        }

        [Fact]
        public void NullIntSource()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IQueryable<int>)null).Average());
        }

        [Fact]
        public void NullIntSourceWithFunc()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IQueryable<int>)null).Average(i => i));
        }

        [Fact]
        public void NullIntFunc()
        {
            Expression<Func <int, int>> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<int>().AsQueryable().Average(selector));
        }

        [Fact]
        public void MultipleIntSouce()
        {
            int[] source = { 5, -10, 15, 40, 28 };
            double expected = 15.6;

            Assert.Equal(expected, source.AsQueryable().Average());
        }

        [Fact]
        public void MultipleIntFromSelector()
        {
            var source = new []
            {
                new { name="Tim", num = 10 },
                new { name="John", num = -10 },
                new { name="Bob", num = 15 }
            };
            double expected = 5;

            Assert.Equal(expected, source.AsQueryable().Average(e => e.num));
        }

        [Fact]
        public void NullNIntSource()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IQueryable<int?>)null).Average());
        }

        [Fact]
        public void NullNIntSourceWithFunc()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IQueryable<int?>)null).Average(i => i));
        }

        [Fact]
        public void NullNIntFunc()
        {
            Expression<Func<int?, int?>> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<int?>().AsQueryable().Average(selector));
        }

        [Fact]
        public void MultipleNullableIntSource()
        {
            int?[] source = { 5, -10, null, null, null, 15, 40, 28, null, null };
            double? expected = 15.6;

            Assert.Equal(expected, source.AsQueryable().Average());
        }

        [Fact]
        public void NullableIntFromSelector()
        {
            var source = new []
            {
                new { name = "Tim", num  = (int?)10 },
                new { name = "John", num =  default(int?) },
                new { name = "Bob", num = (int?)10 }
            };
            double? expected = 10;

            Assert.Equal(expected, source.AsQueryable().Average(e => e.num));
        }

        [Fact]
        public void NullLongSource()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IQueryable<long>)null).Average());
        }

        [Fact]
        public void NullLongSourceWithFunc()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IQueryable<long>)null).Average(i => i));
        }

        [Fact]
        public void NullLongFunc()
        {
            Expression<Func<long, long>> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<long>().AsQueryable().Average(selector));
        }

        [Fact]
        public void MultipleLongValues()
        {
            long[] source = { 5, -10, 15, 40, 28 };
            double expected = 15.6;

            Assert.Equal(expected, source.AsQueryable().Average());
        }

        [Fact]
        public void MultipleLongFromSelector()
        {
            var source = new []
            {
                new { name = "Tim", num = 40L },
                new { name = "John", num = 50L },
                new { name = "Bob", num = 60L }
            };
            double expected = 50;

            Assert.Equal(expected, source.AsQueryable().Average(e => e.num));
        }

        [Fact]
        public void NullNLongSource()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IQueryable<long?>)null).Average());
        }

        [Fact]
        public void NullNLongSourceWithFunc()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IQueryable<long?>)null).Average(i => i));
        }

        [Fact]
        public void NullNLongFunc()
        {
            Expression<Func<long?, long?>> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<long?>().AsQueryable().Average(selector));
        }

        [Fact]
        public void MultipleNullableLongSource()
        {
            long?[] source = { 5, -10, null, null, null, 15, 40, 28, null, null };
            double? expected = 15.6;

            Assert.Equal(expected, source.AsQueryable().Average());
        }

        [Fact]
        public void NullableLongFromSelector()
        {
            var source = new []
            {
                new { name = "Tim", num = (long?)40L },
                new { name = "John", num = default(long?) },
                new { name = "Bob", num = (long?)30L }
            };
            double? expected = 35;

            Assert.Equal(expected, source.AsQueryable().Average(e => e.num));
        }

        [Fact]
        public void NullDoubleSource()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IQueryable<double>)null).Average());
        }

        [Fact]
        public void NullDoubleSourceWithFunc()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IQueryable<double>)null).Average(i => i));
        }

        [Fact]
        public void NullDoubleFunc()
        {
            Expression<Func<double, double>> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<double>().AsQueryable().Average(selector));
        }


        [Fact]
        public void MultipleDoubleValues()
        {
            double[] source = { 5.5, -10, 15.5, 40.5, 28.5 };
            double expected = 16;

            Assert.Equal(expected, source.AsQueryable().Average());
        }

        [Fact]
        public void MultipleDoubleFromSelector()
        {
            var source = new []
            {
                new { name = "Tim", num = 5.5},
                new { name = "John", num = 15.5},
                new { name = "Bob", num = 3.0}
            };
            double expected = 8.0;

            Assert.Equal(expected, source.AsQueryable().Average(e => e.num));
        }

        [Fact]
        public void NullNDoubleSource()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IQueryable<double?>)null).Average());
        }

        [Fact]
        public void NullNDoubleSourceWithFunc()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IQueryable<double?>)null).Average(i => i));
        }

        [Fact]
        public void NullNDoubleFunc()
        {
            Expression<Func<double?, double?>> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<double?>().AsQueryable().Average(selector));
        }

        [Fact]
        public void MultipleNullableDoubleSource()
        {
            double?[] source = { 5.5, 0, null, null, null, 15.5, 40.5, null, null, -23.5 };
            double? expected = 7.6;

            Assert.Equal(expected, source.AsQueryable().Average());
        }

        [Fact]
        public void NullableDoubleFromSelector()
        {
            var source = new[]
            {
                new{ name = "Tim", num = (double?)5.5 },
                new{ name = "John", num = (double?)15.5 },
                new{ name = "Bob", num = default(double?) }
            };
            double? expected = 10.5;

            Assert.Equal(expected, source.AsQueryable().Average(e => e.num));
        }

        [Fact]
        public void NullDecimalSource()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IQueryable<decimal>)null).Average());
        }

        [Fact]
        public void NullDecimalSourceWithFunc()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IQueryable<decimal>)null).Average(i => i));
        }

        [Fact]
        public void NullDecimalFunc()
        {
            Expression<Func<decimal, decimal>> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<decimal>().AsQueryable().Average(selector));
        }

        [Fact]
        public void MultipleDecimalValues()
        {
            decimal[] source = { 5.5m, -10m, 15.5m, 40.5m, 28.5m };
            decimal expected = 16m;

            Assert.Equal(expected, source.AsQueryable().Average());
        }

        [Fact]
        public void MultipleDecimalFromSelector()
        {
            var source = new[]
            {
                new{ name = "Tim", num = 5.5m},
                new{ name = "John", num = 15.5m},
                new{ name = "Bob", num = 3.0m}
            };
            decimal expected = 8.0m;

            Assert.Equal(expected, source.AsQueryable().Average(e => e.num));
        }

        [Fact]
        public void NullNDecimalSource()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IQueryable<decimal?>)null).Average());
        }

        [Fact]
        public void NullNDecimalSourceWithFunc()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IQueryable<decimal?>)null).Average(i => i));
        }

        [Fact]
        public void NullNDecimalFunc()
        {
            Expression<Func<decimal?, decimal?>> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<decimal?>().AsQueryable().Average(selector));
        }

        [Fact]
        public void MultipleNullableeDecimalSource()
        {
            decimal?[] source = { 5.5m, 0, null, null, null, 15.5m, 40.5m, null, null, -23.5m };
            decimal? expected = 7.6m;

            Assert.Equal(expected, source.AsQueryable().Average());
        }

        [Fact]
        public void NullableDecimalFromSelector()
        {
            var source = new[]
            {
                new{ name = "Tim", num = (decimal?)5.5m},
                new{ name = "John", num = (decimal?)15.5m},
                new{ name = "Bob", num = (decimal?)null}
            };
            decimal? expected = 10.5m;

            Assert.Equal(expected, source.AsQueryable().Average(e => e.num));
        }

        [Fact]
        public void NullFloatSource()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IQueryable<float>)null).Average());
        }

        [Fact]
        public void NullFloatSourceWithFunc()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IQueryable<float>)null).Average(i => i));
        }

        [Fact]
        public void NullFloatFunc()
        {
            Expression<Func<float, float>> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<float>().AsQueryable().Average(selector));
        }

        [Fact]
        public void MultipleFloatValues()
        {
            float[] source = { 5.5f, -10f, 15.5f, 40.5f, 28.5f };
            float expected = 16f;

            Assert.Equal(expected, source.AsQueryable().Average());
        }

        [Fact]
        public void MultipleFloatFromSelector()
        {
            var source = new[]
            {
                new{ name = "Tim", num = 5.5f},
                new{ name = "John", num = 15.5f},
                new{ name = "Bob", num = 3.0f}
            };
            float expected = 8.0f;

            Assert.Equal(expected, source.AsQueryable().Average(e => e.num));
        }

        [Fact]
        public void Average1()
        {
            var val = (new int[] { 0, 2, 1 }).AsQueryable().Average();
            Assert.Equal((double)1, val);
        }

        [Fact]
        public void Average2()
        {
            var val = (new int?[] { 0, 2, 1 }).AsQueryable().Average();
            Assert.Equal((double)1, val);
        }

        [Fact]
        public void Average3()
        {
            var val = (new long[] { 0, 2, 1 }).AsQueryable().Average();
            Assert.Equal((double)1, val);
        }

        [Fact]
        public void Average4()
        {
            var val = (new long?[] { 0, 2, 1 }).AsQueryable().Average();
            Assert.Equal((double)1, val);
        }

        [Fact]
        public void Average5()
        {
            var val = (new float[] { 0, 2, 1 }).AsQueryable().Average();
            Assert.Equal((float)1, val);
        }

        [Fact]
        public void Average6()
        {
            var val = (new float?[] { 0, 2, 1 }).AsQueryable().Average();
            Assert.Equal((float)1, val);
        }

        [Fact]
        public void Average7()
        {
            var val = (new double[] { 0, 2, 1 }).AsQueryable().Average();
            Assert.Equal((double)1, val);
        }

        [Fact]
        public void Average8()
        {
            var val = (new double?[] { 0, 2, 1 }).AsQueryable().Average();
            Assert.Equal((double)1, val);
        }

        [Fact]
        public void Average9()
        {
            var val = (new decimal[] { 0, 2, 1 }).AsQueryable().Average();
            Assert.Equal((decimal)1, val);
        }

        [Fact]
        public void Average10()
        {
            var val = (new decimal?[] { 0, 2, 1 }).AsQueryable().Average();
            Assert.Equal((decimal)1, val);
        }

        [Fact]
        public void Average11()
        {
            var val = (new int[] { 0, 2, 1 }).AsQueryable().Average(n => n);
            Assert.Equal((double)1, val);
        }

        [Fact]
        public void Average12()
        {
            var val = (new int?[] { 0, 2, 1 }).AsQueryable().Average(n => n);
            Assert.Equal((double)1, val);
        }

        [Fact]
        public void Average13()
        {
            var val = (new long[] { 0, 2, 1 }).AsQueryable().Average(n => n);
            Assert.Equal((double)1, val);
        }

        [Fact]
        public void Average14()
        {
            var val = (new long?[] { 0, 2, 1 }).AsQueryable().Average(n => n);
            Assert.Equal((double)1, val);
        }

        [Fact]
        public void Average15()
        {
            var val = (new float[] { 0, 2, 1 }).AsQueryable().Average(n => n);
            Assert.Equal((float)1, val);
        }

        [Fact]
        public void Average16()
        {
            var val = (new float?[] { 0, 2, 1 }).AsQueryable().Average(n => n);
            Assert.Equal((float)1, val);
        }

        [Fact]
        public void Average17()
        {
            var val = (new double[] { 0, 2, 1 }).AsQueryable().Average(n => n);
            Assert.Equal((double)1, val);
        }

        [Fact]
        public void Average18()
        {
            var val = (new double?[] { 0, 2, 1 }).AsQueryable().Average(n => n);
            Assert.Equal((double)1, val);
        }

        [Fact]
        public void Average19()
        {
            var val = (new decimal[] { 0, 2, 1 }).AsQueryable().Average(n => n);
            Assert.Equal((decimal)1, val);
        }

        [Fact]
        public void Average20()
        {
            var val = (new decimal?[] { 0, 2, 1 }).AsQueryable().Average(n => n);
            Assert.Equal((decimal)1, val);
        }
    }
}
