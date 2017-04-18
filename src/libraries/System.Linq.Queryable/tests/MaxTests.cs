// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq.Expressions;
using Xunit;

namespace System.Linq.Tests
{
    public class MaxTests : EnumerableBasedTests
    {
        [Fact]
        public void NullInt32Source()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<int>)null).Max());
        }

        [Fact]
        public void EmptyInt32()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<int>().AsQueryable().Max());
        }

        [Fact]
        public void Int32MaxRepeated()
        {
            int[] source = { -6, 0, -9, 0, -10, 0 };
            Assert.Equal(0, source.AsQueryable().Max());
        }

        [Fact]
        public void NullInt64Source()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<long>)null).Max());
        }

        [Fact]
        public void EmptyInt64()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<long>().AsQueryable().Max());
        }

        [Fact]
        public void Int64MaxRepeated()
        {
            long[] source = { 6, 50, 9, 50, 10, 50 };
            Assert.Equal(50, source.AsQueryable().Max());
        }

        [Fact]
        public void EmptySingle()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<float>().AsQueryable().Max());
        }

        [Fact]
        public void Single_MaxRepeated()
        {
            float[] source = { -5.5f, float.PositiveInfinity, 9.9f, float.PositiveInfinity };
            Assert.True(float.IsPositiveInfinity(source.AsQueryable().Max()));
        }

        [Fact]
        public void NullSingleSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<float>)null).Max());
        }

        [Fact]
        public void NullDoubleSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<double>)null).Max());
        }

        [Fact]
        public void EmptyDouble()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<double>().AsQueryable().Max());
        }

        [Fact]
        public void DoubleMaximumRepeated()
        {
            double[] source = { -5.5, double.PositiveInfinity, 9.9, double.PositiveInfinity };
            Assert.True(double.IsPositiveInfinity(source.AsQueryable().Max()));
        }

        [Fact]
        public void NullDecimalSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<decimal>)null).Max());
        }

        [Fact]
        public void EmptyDecimal()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<decimal>().AsQueryable().Max());
        }

        [Fact]
        public void DecimalMaximumRepeated()
        {
            decimal[] source = { -5.5m, 0m, 9.9m, -5.5m, 9.9m };
            Assert.Equal(9.9m, source.AsQueryable().Max());
        }

        [Fact]
        public void NullNullableInt32Source()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<int?>)null).Max());
        }

        [Fact]
        public void EmptyNullableInt32()
        {
            Assert.Null(Enumerable.Empty<int?>().AsQueryable().Max());
        }

        [Fact]
        public void NullableInt32MaxRepeated()
        {
            int?[] source = { 6, null, null, 100, 9, 100, 10, 100 };
            Assert.Equal(100, source.AsQueryable().Max());
        }

        [Fact]
        public void NullNullableInt64Source()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<long?>)null).Max());
        }

        [Fact]
        public void EmptyNullableInt64()
        {
            Assert.Null(Enumerable.Empty<long?>().AsQueryable().Max(x => x));
        }

        [Fact]
        public void NullableInt64MaximumRepeated()
        {
            long?[] source = { -6, null, null, 0, -9, 0, -10, -30 };
            Assert.Equal(0, source.AsQueryable().Max());
        }

        [Fact]
        public void EmptyNullableSingle()
        {
            Assert.Null(Enumerable.Empty<float?>().AsQueryable().Max());
        }

        [Fact]
        public void NullableSingleMaxRepeated()
        {
            float?[] source = { -6.4f, null, null, -0.5f, -9.4f, -0.5f, -10.9f, -0.5f };
            Assert.Equal(-0.5f, source.AsQueryable().Max());
        }

        [Fact]
        public void NullNullableDoubleSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<double?>)null).Max());
        }

        [Fact]
        public void EmptyNullableDouble()
        {
            Assert.Null(Enumerable.Empty<double?>().AsQueryable().Max());
        }

        [Fact]
        public void NullNullableDecimalSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<decimal?>)null).Max());
        }

        [Fact]
        public void EmptyNullableDecimal()
        {
            Assert.Null(Enumerable.Empty<decimal?>().AsQueryable().Max());
        }

        [Fact]
        public void NullableDecimalMaximumRepeated()
        {
            decimal?[] source = { 6.4m, null, null, decimal.MaxValue, 9.4m, decimal.MaxValue, 10.9m, decimal.MaxValue };
            Assert.Equal(decimal.MaxValue, source.AsQueryable().Max());
        }

        [Fact]
        public void EmptyDateTime()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<DateTime>().AsQueryable().Max());
        }

        [Fact]
        public void NullDateTimeSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<DateTime>)null).Max());
        }

        [Fact]
        public void NullStringSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<string>)null).Max());
        }

        [Fact]
        public void EmptyString()
        {
            Assert.Null(Enumerable.Empty<string>().AsQueryable().Max());
        }

        [Fact]
        public void StringMaximumRepeated()
        {
            string[] source = { "ooo", "ccc", "ccc", "ooo", "ooo", "nnn" };
            Assert.Equal("ooo", source.AsQueryable().Max());
        }

        [Fact]
        public void EmptyInt32WithSelector()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<int>().Max(x => x));
        }

        [Fact]
        public void NullInt32SourceWithSelector()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<int>)null).Max(i => i));
        }

        [Fact]
        public void Int32SourceWithNullSelector()
        {
            Expression<Func<int, int>> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<int>().AsQueryable().Max(selector));
        }

        [Fact]
        public void MaxInt32WithSelectorAccessingProperty()
        {
            var source = new[]{
                new { name="Tim", num=10 },
                new { name="John", num=-105 },
                new { name="Bob", num=30 }
            };

            Assert.Equal(30, source.AsQueryable().Max(e => e.num));
        }

        [Fact]
        public void EmptyInt64WithSelector()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<long>().AsQueryable().Max(x => x));
        }

        [Fact]
        public void NullInt64SourceWithSelector()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<long>)null).Max(i => i));
        }

        [Fact]
        public void Int64SourceWithNullSelector()
        {
            Expression<Func<long, long>> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<long>().AsQueryable().Max(selector));
        }

        [Fact]
        public void MaxInt64WithSelectorAccessingProperty()
        {
            var source = new[]{
                new { name="Tim", num=10L },
                new { name="John", num=-105L },
                new { name="Bob", num=long.MaxValue }
            };
            Assert.Equal(long.MaxValue, source.AsQueryable().Max(e => e.num));
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

            Assert.Equal(100.45f, source.AsQueryable().Max(e => e.num));
        }

        [Fact]
        public void NullSingleSourceWithSelector()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<float>)null).Max(i => i));
        }

        [Fact]
        public void SingleSourceWithNullSelector()
        {
            Expression<Func<float, float>> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<float>().AsQueryable().Max(selector));
        }

        [Fact]
        public void EmptySingleWithSelector()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<float>().AsQueryable().Max(x => x));
        }

        [Fact]
        public void EmptyDoubleWithSelector()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<double>().AsQueryable().Max(x => x));
        }

        [Fact]
        public void NullDoubleSourceWithSelector()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<double>)null).Max(i => i));
        }

        [Fact]
        public void DoubleSourceWithNullSelector()
        {
            Expression<Func<double, double>> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<double>().AsQueryable().Max(selector));
        }

        [Fact]
        public void MaxDoubleWithSelectorAccessingField()
        {
            var source = new[]{
                new { name="Tim", num=40.5 },
                new { name="John", num=-10.25 },
                new { name="Bob", num=100.45 }
            };
            Assert.Equal(100.45, source.AsQueryable().Max(e => e.num));
        }

        [Fact]
        public void EmptyDecimalWithSelector()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<decimal>().AsQueryable().Max(x => x));
        }

        [Fact]
        public void NullDecimalSourceWithSelector()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<decimal>)null).Max(i => i));
        }

        [Fact]
        public void DecimalSourceWithNullSelector()
        {
            Expression<Func<decimal, decimal>> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<decimal>().AsQueryable().Max(selector));
        }

        [Fact]
        public void MaxDecimalWithSelectorAccessingProperty()
        {
            var source = new[]{
                new { name="Tim", num=420.5m },
                new { name="John", num=900.25m },
                new { name="Bob", num=10.45m }
            };
            Assert.Equal(900.25m, source.AsQueryable().Max(e => e.num));
        }

        [Fact]
        public void EmptyNullableInt32WithSelector()
        {
            Assert.Null(Enumerable.Empty<int?>().Max(x => x));
        }

        [Fact]
        public void NullNullableInt32SourceWithSelector()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<int?>)null).Max(i => i));
        }

        [Fact]
        public void NullableInt32SourceWithNullSelector()
        {
            Expression<Func<int?, int?>> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<int?>().AsQueryable().Max(selector));
        }

        [Fact]
        public void MaxNullableInt32WithSelectorAccessingField()
        {
            var source = new[]{
                new { name="Tim", num=(int?)10 },
                new { name="John", num=(int?)-105 },
                new { name="Bob", num=(int?)null }
            };

            Assert.Equal(10, source.AsQueryable().Max(e => e.num));
        }

        [Fact]
        public void EmptyNullableInt64WithSelector()
        {
            Assert.Null(Enumerable.Empty<long?>().AsQueryable().Max(x => x));
        }

        [Fact]
        public void NullNullableInt64SourceWithSelector()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<long?>)null).Max(i => i));
        }

        [Fact]
        public void NullableInt64SourceWithNullSelector()
        {
            Expression<Func<long?, long?>> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<long?>().AsQueryable().Max(selector));
        }

        [Fact]
        public void MaxNullableInt64WithSelectorAccessingField()
        {
            var source = new[]{
                new {name="Tim", num=default(long?) },
                new {name="John", num=(long?)-105L },
                new {name="Bob", num=(long?)long.MaxValue }
            };
            Assert.Equal(long.MaxValue, source.AsQueryable().Max(e => e.num));
        }

        [Fact]
        public void EmptyNullableSingleWithSelector()
        {
            Assert.Null(Enumerable.Empty<float?>().AsQueryable().Max(x => x));
        }

        [Fact]
        public void NullNullableSingleSourceWithSelector()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<float?>)null).Max(i => i));
        }

        [Fact]
        public void NullableSingleSourceWithNullSelector()
        {
            Expression<Func<float?, float?>> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<float?>().AsQueryable().Max(selector));
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
            Assert.Equal(100.45f, source.AsQueryable().Max(e => e.num));
        }

        [Fact]
        public void EmptyNullableDoubleWithSelector()
        {
            Assert.Null(Enumerable.Empty<double?>().AsQueryable().Max(x => x));
        }

        [Fact]
        public void NullNullableDoubleSourceWithSelector()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<double?>)null).Max(i => i));
        }

        [Fact]
        public void NullableDoubleSourceWithNullSelector()
        {
            Expression<Func<double?, double?>> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<double?>().AsQueryable().Max(selector));
        }

        [Fact]
        public void MaxNullableDoubleWithSelectorAccessingProperty()
        {
            var source = new []{
                new { name = "Tim", num = (double?)40.5},
                new { name = "John", num = default(double?)},
                new { name = "Bob", num = (double?)100.45}
            };
            Assert.Equal(100.45, source.AsQueryable().Max(e => e.num));
        }

        [Fact]
        public void EmptyNullableDecimalWithSelector()
        {
            Assert.Null(Enumerable.Empty<decimal?>().AsQueryable().Max(x => x));
        }

        [Fact]
        public void NullNullableDecimalSourceWithSelector()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<decimal?>)null).Max(i => i));
        }

        [Fact]
        public void NullableDecimalSourceWithNullSelector()
        {
            Expression<Func<decimal?, decimal?>> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<decimal?>().AsQueryable().Max(selector));
        }

        [Fact]
        public void MaxNullableDecimalWithSelectorAccessingProperty()
        {
            var source = new[] {
                new { name="Tim", num=(decimal?)420.5m },
                new { name="John", num=default(decimal?) },
                new { name="Bob", num=(decimal?)10.45m }
            };
            Assert.Equal(420.5m, source.AsQueryable().Max(e => e.num));
        }

        [Fact]
        public void EmptyNullableDateTimeWithSelector()
        {
            Assert.Null(Enumerable.Empty<DateTime?>().AsQueryable().Max(x => x));
        }

        [Fact]
        public void NullNullableDateTimeSourceWithSelector()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<DateTime?>)null).Max(i => i));
        }

        [Fact]
        public void NullableDateTimeSourceWithNullSelector()
        {
            Expression<Func<DateTime?, DateTime?>> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<DateTime?>().AsQueryable().Max(selector));
        }

        public void EmptyStringSourceWithSelector()
        {
            Assert.Null(Enumerable.Empty<string>().AsQueryable().Max(x => x));
        }

        [Fact]
        public void NullStringSourceWithSelector()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<string>)null).Max(i => i));
        }

        [Fact]
        public void StringSourceWithNullSelector()
        {
            Expression<Func<string, string>> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<string>().AsQueryable().Max(selector));
        }

        [Fact]
        public void MaxStringWithSelectorAccessingProperty()
        {
            var source = new[]{
                new { name="Tim", num=420.5m },
                new { name="John", num=900.25m },
                new { name="Bob", num=10.45m }
            };
            Assert.Equal("Tim", source.AsQueryable().Max(e => e.name));
        }

        [Fact]
        public void EmptyBoolean()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<bool>().AsQueryable().Max());
        }

        [Fact]
        public void Max1()
        {
            var val = (new int[] { 0, 2, 1 }).AsQueryable().Max();
            Assert.Equal(2, val);
        }

        [Fact]
        public void Max2()
        {
            var val = (new int[] { 0, 2, 1 }).AsQueryable().Max(n => n);
            Assert.Equal(2, val);
        }
    }
}
