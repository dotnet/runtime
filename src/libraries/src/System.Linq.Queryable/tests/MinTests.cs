// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq.Expressions;
using Xunit;

namespace System.Linq.Tests
{
    public class MinTests : EnumerableBasedTests
    {
        [Fact]
        public void EmptyInt32Source()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<int>().AsQueryable().Min());
        }

        [Fact]
        public void NullInt32Source()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<int>)null).Min());
        }

        [Fact]
        public void Int32MinimumRepeated()
        {
            int[] source = { 6, 0, 9, 0, 10, 0 };
            Assert.Equal(0, source.AsQueryable().Min());
        }

        [Fact]
        public void EmptyInt64Source()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<long>().AsQueryable().Min());
        }

        [Fact]
        public void NullInt64Source()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<long>)null).Min());
        }

        [Fact]
        public void Int64MinimumRepeated()
        {
            long[] source = { 6, -5, 9, -5, 10, -5 };
            Assert.Equal(-5, source.AsQueryable().Min());
        }

        [Fact]
        public void NullSingleSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<float>)null).Min());
        }

        [Fact]
        public void EmptySingle()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<float>().AsQueryable().Min());
        }

        [Fact]
        public void SingleMinimumRepeated()
        {
            float[] source = { -5.5f, float.NegativeInfinity, 9.9f, float.NegativeInfinity };
            Assert.True(float.IsNegativeInfinity(source.AsQueryable().Min()));
        }

        [Fact]
        public void EmptyDoubleSource()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<double>().AsQueryable().Min());
        }

        [Fact]
        public void NullDoubleSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<double>)null).Min());
        }

        [Fact]
        public void DoubleMinimumRepeated()
        {
            double[] source = { -5.5, double.NegativeInfinity, 9.9, double.NegativeInfinity };
            Assert.True(double.IsNegativeInfinity(source.AsQueryable().Min()));
        }

        [Fact]
        public void EmptyDecimalSource()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<decimal>().AsQueryable().Min());
        }

        [Fact]
        public void NullDecimalSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<decimal>)null).Min());
        }

        [Fact]
        public void DecimalMinimumRepeated()
        {
            decimal[] source = { -5.5m, 0m, 9.9m, -5.5m, 5m };
            Assert.Equal(-5.5m, source.AsQueryable().Min());
        }

        [Fact]
        public void EmptyNullableInt32Source()
        {
            Assert.Null(Enumerable.Empty<int?>().AsQueryable().Min());
        }

        [Fact]
        public void NullableInt32MinimumRepeated()
        {
            int?[] source = { 6, null, null, 0, 9, 0, 10, 0 };
            Assert.Equal(0, source.AsQueryable().Min());
        }

        [Fact]
        public void EmptyNullableInt64Source()
        {
            Assert.Null(Enumerable.Empty<long?>().AsQueryable().Min());
        }

        [Fact]
        public void NullNullableInt64Source()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<long?>)null).Min());
        }

        [Fact]
        public void NullableInt64MinimumRepeated()
        {
            long?[] source = { 6, null, null, 0, 9, 0, 10, 0 };
            Assert.Equal(0, source.AsQueryable().Min());
        }

        [Fact]
        public void EmptyNullableSingleSource()
        {
            Assert.Null(Enumerable.Empty<float?>().AsQueryable().Min());
        }

        [Fact]
        public void NullableSingleMinimumRepated()
        {
            float?[] source = { 6.4f, null, null, -0.5f, 9.4f, -0.5f, 10.9f, -0.5f };
            Assert.Equal(-0.5f, source.AsQueryable().Min());
        }

        [Fact]
        public void EmptyNullableDoubleSource()
        {
            Assert.Null(Enumerable.Empty<double?>().AsQueryable().Min());
        }

        [Fact]
        public void NullNullableDoubleSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<double?>)null).Min());
        }

        [Fact]
        public void NullableDoubleMinimumRepeated()
        {
            double?[] source = { 6.4, null, null, -0.5, 9.4, -0.5, 10.9, -0.5 };
            Assert.Equal(-0.5, source.AsQueryable().Min());
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
        public void EmptyNullableDecimalSource()
        {
            Assert.Null(Enumerable.Empty<decimal?>().AsQueryable().Min());
        }

        [Fact]
        public void NullNullableDecimalSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<decimal?>)null).Min());
        }

        [Fact]
        public void NullableDecimalMinimumRepeated()
        {
            decimal?[] source = { 6.4m, null, null, decimal.MinValue, 9.4m, decimal.MinValue, 10.9m, decimal.MinValue };
            Assert.Equal(decimal.MinValue, source.AsQueryable().Min());
        }

        [Fact]
        public void EmptyDateTimeSource()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<DateTime>().AsQueryable().Min());
        }

        [Fact]
        public void NullNullableDateTimeSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<DateTime>)null).Min());
        }

        [Fact]
        public void EmptyStringSource()
        {
            Assert.Null(Enumerable.Empty<string>().AsQueryable().Min());
        }

        [Fact]
        public void NullStringSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<string>)null).Min());
        }

        [Fact]
        public void StringMinimumRepeated()
        {
            string[] source = { "ooo", "www", "www", "ooo", "ooo", "ppp" };
            Assert.Equal("ooo", source.AsQueryable().Min());
        }

        [Fact]
        public void MinInt32WithSelectorAccessingProperty()
        {
            var source = new[]{
                new { name="Tim", num=10 },
                new { name="John", num=-105 },
                new { name="Bob", num=-30 }
            };
            Assert.Equal(-105, source.AsQueryable().Min(e => e.num));
        }

        [Fact]
        public void EmptyInt32WithSelector()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<int>().AsQueryable().Min(x => x));
        }

        [Fact]
        public void NullInt32SourceWithSelector()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<int>)null).Min(i => i));
        }

        [Fact]
        public void Int32SourceWithNullSelector()
        {
            Expression<Func<int, int>> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<int>().AsQueryable().Min(selector));
        }

        [Fact]
        public void MinInt64WithSelectorAccessingProperty()
        {
            var source = new[]{
                new { name="Tim", num=10L },
                new { name="John", num=long.MinValue },
                new { name="Bob", num=-10L }
            };

            Assert.Equal(long.MinValue, source.AsQueryable().Min(e => e.num));
        }

        [Fact]
        public void EmptyInt64WithSelector()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<long>().AsQueryable().Min(x => x));
        }

        [Fact]
        public void NullInt64SourceWithSelector()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<long>)null).Min(i => i));
        }

        [Fact]
        public void Int64SourceWithNullSelector()
        {
            Expression<Func<long, long>> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<long>().AsQueryable().Min(selector));
        }

        [Fact]
        public void EmptySingleWithSelector()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<float>().AsQueryable().Min(x => x));
        }

        [Fact]
        public void NullSingleSourceWithSelector()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<float>)null).Min(i => i));
        }

        [Fact]
        public void MinSingleWithSelectorAccessingProperty()
        {
            var source = new []{
                new { name="Tim", num=-45.5f },
                new { name="John", num=-132.5f },
                new { name="Bob", num=20.45f }
            };
            Assert.Equal(-132.5f, source.AsQueryable().Min(e => e.num));
        }

        [Fact]
        public void MinDoubleWithSelectorAccessingProperty()
        {
            var source = new[]{
                new { name="Tim", num=-45.5 },
                new { name="John", num=-132.5 },
                new { name="Bob", num=20.45 }
            };
            Assert.Equal(-132.5, source.AsQueryable().Min(e => e.num));
        }

        [Fact]
        public void EmptyDoubleWithSelector()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<double>().AsQueryable().Min(x => x));
        }

        [Fact]
        public void NullDoubleSourceWithSelector()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<double>)null).Min(i => i));
        }

        [Fact]
        public void DoubleSourceWithNullSelector()
        {
            Expression<Func<double, double>> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<double>().AsQueryable().Min(selector));
        }

        [Fact]
        public void MinDecimalWithSelectorAccessingProperty()
        {
            var source = new[]{
                new {name="Tim", num=100.45m},
                new {name="John", num=10.5m},
                new {name="Bob", num=0.05m}
            };
            Assert.Equal(0.05m, source.AsQueryable().Min(e => e.num));
        }

        [Fact]
        public void EmptyDecimalWithSelector()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<decimal>().AsQueryable().Min(x => x));
        }

        [Fact]
        public void NullDecimalSourceWithSelector()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<decimal>)null).Min(i => i));
        }

        [Fact]
        public void DecimalSourceWithNullSelector()
        {
            Expression<Func<decimal, decimal>> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<decimal>().AsQueryable().Min(selector));
        }

        [Fact]
        public void MinNullableInt32WithSelectorAccessingProperty()
        {
            var source = new[]{
                new { name="Tim", num=(int?)10 },
                new { name="John", num=default(int?) },
                new { name="Bob", num=(int?)-30 }
            };
            Assert.Equal(-30, source.AsQueryable().Min(e => e.num));
        }

        [Fact]
        public void NullNullableInt32SourceWithSelector()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<int?>)null).Min(i => i));
        }

        [Fact]
        public void NullableInt32SourceWithNullSelector()
        {
            Expression<Func<int?, int?>> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<int?>().AsQueryable().Min(selector));
        }

        [Fact]
        public void MinNullableInt64WithSelectorAccessingProperty()
        {
            var source = new[]{
                new { name="Tim", num=default(long?) },
                new { name="John", num=(long?)long.MinValue },
                new { name="Bob", num=(long?)-10L }
            };
            Assert.Equal(long.MinValue, source.AsQueryable().Min(e => e.num));
        }

        [Fact]
        public void NullNullableInt64SourceWithSelector()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<long?>)null).Min(i => i));
        }

        [Fact]
        public void NullableInt64SourceWithNullSelector()
        {
            Expression<Func<long?, long?>> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<long?>().AsQueryable().Min(selector));
        }

        [Fact]
        public void MinNullableSingleWithSelectorAccessingProperty()
        {
            var source = new[]{
                new {name="Tim", num=(float?)-45.5f},
                new {name="John", num=(float?)-132.5f},
                new {name="Bob", num=default(float?)}
            };

            Assert.Equal(-132.5f, source.AsQueryable().Min(e => e.num));
        }

        [Fact]
        public void NullNullableSingleSourceWithSelector()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<float?>)null).Min(i => i));
        }

        [Fact]
        public void NullableSingleSourceWithNullSelector()
        {
            Expression<Func<float?, float?>> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<float?>().AsQueryable().Min(selector));
        }

        [Fact]
        public void MinNullableDoubleWithSelectorAccessingProperty()
        {
            var source = new[] {
                new { name="Tim", num=(double?)-45.5 },
                new { name="John", num=(double?)-132.5 },
                new { name="Bob", num=default(double?) }
            };
            Assert.Equal(-132.5, source.AsQueryable().Min(e => e.num));
        }
        [Fact]
        public void NullNullableDoubleSourceWithSelector()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<double?>)null).Min(i => i));
        }

        [Fact]
        public void NullableDoubleSourceWithNullSelector()
        {
            Expression<Func<double?, double?>> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<double?>().AsQueryable().Min(selector));
        }

        [Fact]
        public void MinNullableDecimalWithSelectorAccessingProperty()
        {
            var source = new[]{
                new { name="Tim", num=(decimal?)100.45m },
                new { name="John", num=(decimal?)10.5m },
                new { name="Bob", num=default(decimal?) }
            };
            Assert.Equal(10.5m, source.AsQueryable().Min(e => e.num));
        }

        [Fact]
        public void NullNullableDecimalSourceWithSelector()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<decimal?>)null).Min(i => i));
        }

        [Fact]
        public void NullableDecimalSourceWithNullSelector()
        {
            Expression<Func<decimal?, decimal?>> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<decimal?>().AsQueryable().Min(selector));
        }

        [Fact]
        public void EmptyDateTimeWithSelector()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<DateTime>().AsQueryable().Min(x => x));
        }

        [Fact]
        public void NullDateTimeSourceWithSelector()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<DateTime>)null).Min(i => i));
        }

        [Fact]
        public void DateTimeSourceWithNullSelector()
        {
            Expression<Func<DateTime, DateTime>> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<DateTime>().AsQueryable().Min(selector));
        }

        [Fact]
        public void MinStringWitSelectorAccessingProperty()
        {
            var source = new[]{
                new { name="Tim", num=100.45m },
                new { name="John", num=10.5m },
                new { name="Bob", num=0.05m }
            };
            Assert.Equal("Bob", source.AsQueryable().Min(e => e.name));
        }

        [Fact]
        public void NullStringSourceWithSelector()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<string>)null).Min(i => i));
        }

        [Fact]
        public void StringSourceWithNullSelector()
        {
            Expression<Func<string, string>> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<string>().AsQueryable().Min(selector));
        }

        [Fact]
        public void EmptyBooleanSource()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<bool>().AsQueryable().Min());
        }

        [Fact]
        public void Min1()
        {
            var val = (new int[] { 0, 2, 1 }).AsQueryable().Min();
            Assert.Equal(0, val);
        }

        [Fact]
        public void Min2()
        {
            var val = (new int[] { 0, 2, 1 }).AsQueryable().Min(n => n);
            Assert.Equal(0, val);
        }
    }
}
