// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq.Expressions;
using Xunit;

namespace System.Linq.Tests
{
    public class SumTests : EnumerableBasedTests
    {
        [Fact]
        public void SumOfInt_SourceIsNull_ArgumentNullExceptionThrown()
        {
            IQueryable<int> sourceInt = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => sourceInt.Sum());
            AssertExtensions.Throws<ArgumentNullException>("source", () => sourceInt.Sum(x => x));
        }

        [Fact]
        public void SumOfNullableOfInt_SourceIsNull_ArgumentNullExceptionThrown()
        {
            IQueryable<int?> sourceNullableInt = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => sourceNullableInt.Sum());
            AssertExtensions.Throws<ArgumentNullException>("source", () => sourceNullableInt.Sum(x => x));
        }

        [Fact]
        public void SumOfLong_SourceIsNull_ArgumentNullExceptionThrown()
        {
            IQueryable<long> sourceLong = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => sourceLong.Sum());
            AssertExtensions.Throws<ArgumentNullException>("source", () => sourceLong.Sum(x => x));
        }

        [Fact]
        public void SumOfNullableOfLong_SourceIsNull_ArgumentNullExceptionThrown()
        {
            IQueryable<long?> sourceNullableLong = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => sourceNullableLong.Sum());
            AssertExtensions.Throws<ArgumentNullException>("source", () => sourceNullableLong.Sum(x => x));
        }

        [Fact]
        public void SumOfFloat_SourceIsNull_ArgumentNullExceptionThrown()
        {
            IQueryable<float> sourceFloat = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => sourceFloat.Sum());
            AssertExtensions.Throws<ArgumentNullException>("source", () => sourceFloat.Sum(x => x));
        }

        [Fact]
        public void SumOfNullableOfFloat_SourceIsNull_ArgumentNullExceptionThrown()
        {
            IQueryable<float?> sourceNullableFloat = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => sourceNullableFloat.Sum());
            AssertExtensions.Throws<ArgumentNullException>("source", () => sourceNullableFloat.Sum(x => x));
        }

        [Fact]
        public void SumOfDouble_SourceIsNull_ArgumentNullExceptionThrown()
        {
            IQueryable<double> sourceDouble = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => sourceDouble.Sum());
            AssertExtensions.Throws<ArgumentNullException>("source", () => sourceDouble.Sum(x => x));
        }

        [Fact]
        public void SumOfNullableOfDouble_SourceIsNull_ArgumentNullExceptionThrown()
        {
            IQueryable<double?> sourceNullableDouble = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => sourceNullableDouble.Sum());
            AssertExtensions.Throws<ArgumentNullException>("source", () => sourceNullableDouble.Sum(x => x));
        }

        [Fact]
        public void SumOfDecimal_SourceIsNull_ArgumentNullExceptionThrown()
        {
            IQueryable<decimal> sourceDecimal = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => sourceDecimal.Sum());
            AssertExtensions.Throws<ArgumentNullException>("source", () => sourceDecimal.Sum(x => x));
        }

        [Fact]
        public void SumOfNullableOfDecimal_SourceIsNull_ArgumentNullExceptionThrown()
        {
            IQueryable<decimal?> sourceNullableDecimal = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => sourceNullableDecimal.Sum());
            AssertExtensions.Throws<ArgumentNullException>("source", () => sourceNullableDecimal.Sum(x => x));
        }

        [Fact]
        public void SumOfInt_SelectorIsNull_ArgumentNullExceptionThrown()
        {
            IQueryable<int> sourceInt = Enumerable.Empty<int>().AsQueryable();
            Expression<Func<int, int>> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => sourceInt.Sum(selector));
        }

        [Fact]
        public void SumOfNullableOfInt_SelectorIsNull_ArgumentNullExceptionThrown()
        {
            IQueryable<int?> sourceNullableInt = Enumerable.Empty<int?>().AsQueryable();
            Expression<Func<int?, int?>> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => sourceNullableInt.Sum(selector));
        }

        [Fact]
        public void SumOfLong_SelectorIsNull_ArgumentNullExceptionThrown()
        {
            IQueryable<long> sourceLong = Enumerable.Empty<long>().AsQueryable();
            Expression<Func<long, long>> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => sourceLong.Sum(selector));
        }

        [Fact]
        public void SumOfNullableOfLong_SelectorIsNull_ArgumentNullExceptionThrown()
        {
            IQueryable<long?> sourceNullableLong = Enumerable.Empty<long?>().AsQueryable();
            Expression<Func<long?, long?>> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => sourceNullableLong.Sum(selector));
        }

        [Fact]
        public void SumOfFloat_SelectorIsNull_ArgumentNullExceptionThrown()
        {
            IQueryable<float> sourceFloat = Enumerable.Empty<float>().AsQueryable();
            Expression<Func<float, float>> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => sourceFloat.Sum(selector));
        }

        [Fact]
        public void SumOfNullableOfFloat_SelectorIsNull_ArgumentNullExceptionThrown()
        {
            IQueryable<float?> sourceNullableFloat = Enumerable.Empty<float?>().AsQueryable();
            Expression<Func<float?, float?>> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => sourceNullableFloat.Sum(selector));
        }

        [Fact]
        public void SumOfDouble_SelectorIsNull_ArgumentNullExceptionThrown()
        {
            IQueryable<double> sourceDouble = Enumerable.Empty<double>().AsQueryable();
            Expression<Func<double, double>> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => sourceDouble.Sum(selector));
        }

        [Fact]
        public void SumOfNullableOfDouble_SelectorIsNull_ArgumentNullExceptionThrown()
        {
            IQueryable<double?> sourceNullableDouble = Enumerable.Empty<double?>().AsQueryable();
            Expression<Func<double?, double?>> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => sourceNullableDouble.Sum(selector));
        }

        [Fact]
        public void SumOfDecimal_SelectorIsNull_ArgumentNullExceptionThrown()
        {
            IQueryable<decimal> sourceDecimal = Enumerable.Empty<decimal>().AsQueryable();
            Expression<Func<decimal, decimal>> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => sourceDecimal.Sum(selector));
        }

        [Fact]
        public void SumOfNullableOfDecimal_SelectorIsNull_ArgumentNullExceptionThrown()
        {
            IQueryable<decimal?> sourceNullableDecimal = Enumerable.Empty<decimal?>().AsQueryable();
            Expression<Func<decimal?, decimal?>> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => sourceNullableDecimal.Sum(selector));
        }

        [Fact]
        public void SumOfInt_SourceIsEmptyCollection_ZeroReturned()
        {
            IQueryable<int> sourceInt = Enumerable.Empty<int>().AsQueryable();
            Assert.Equal(0, sourceInt.Sum());
            Assert.Equal(0, sourceInt.Sum(x => x));
        }

        [Fact]
        public void SumOfNullableOfInt_SourceIsEmptyCollection_ZeroReturned()
        {
            IQueryable<int?> sourceNullableInt = Enumerable.Empty<int?>().AsQueryable();
            Assert.Equal(0, sourceNullableInt.Sum());
            Assert.Equal(0, sourceNullableInt.Sum(x => x));
        }

        [Fact]
        public void SumOfLong_SourceIsEmptyCollection_ZeroReturned()
        {
            IQueryable<long> sourceLong = Enumerable.Empty<long>().AsQueryable();
            Assert.Equal(0L, sourceLong.Sum());
            Assert.Equal(0L, sourceLong.Sum(x => x));
        }

        [Fact]
        public void SumOfNullableOfLong_SourceIsEmptyCollection_ZeroReturned()
        {
            IQueryable<long?> sourceNullableLong = Enumerable.Empty<long?>().AsQueryable();
            Assert.Equal(0L, sourceNullableLong.Sum());
            Assert.Equal(0L, sourceNullableLong.Sum(x => x));
        }

        [Fact]
        public void SumOfFloat_SourceIsEmptyCollection_ZeroReturned()
        {
            IQueryable<float> sourceFloat = Enumerable.Empty<float>().AsQueryable();
            Assert.Equal(0f, sourceFloat.Sum());
            Assert.Equal(0f, sourceFloat.Sum(x => x));
        }

        [Fact]
        public void SumOfNullableOfFloat_SourceIsEmptyCollection_ZeroReturned()
        {
            IQueryable<float?> sourceNullableFloat = Enumerable.Empty<float?>().AsQueryable();
            Assert.Equal(0f, sourceNullableFloat.Sum());
            Assert.Equal(0f, sourceNullableFloat.Sum(x => x));
        }

        [Fact]
        public void SumOfDouble_SourceIsEmptyCollection_ZeroReturned()
        {
            IQueryable<double> sourceDouble = Enumerable.Empty<double>().AsQueryable();
            Assert.Equal(0d, sourceDouble.Sum());
            Assert.Equal(0d, sourceDouble.Sum(x => x));
        }

        [Fact]
        public void SumOfNullableOfDouble_SourceIsEmptyCollection_ZeroReturned()
        {
            IQueryable<double?> sourceNullableDouble = Enumerable.Empty<double?>().AsQueryable();
            Assert.Equal(0d, sourceNullableDouble.Sum());
            Assert.Equal(0d, sourceNullableDouble.Sum(x => x));
        }

        [Fact]
        public void SumOfDecimal_SourceIsEmptyCollection_ZeroReturned()
        {
            IQueryable<decimal> sourceDecimal = Enumerable.Empty<decimal>().AsQueryable();
            Assert.Equal(0m, sourceDecimal.Sum());
            Assert.Equal(0m, sourceDecimal.Sum(x => x));
        }

        [Fact]
        public void SumOfNullableOfDecimal_SourceIsEmptyCollection_ZeroReturned()
        {
            IQueryable<decimal?> sourceNullableDecimal = Enumerable.Empty<decimal?>().AsQueryable();
            Assert.Equal(0m, sourceNullableDecimal.Sum());
            Assert.Equal(0m, sourceNullableDecimal.Sum(x => x));
        }

        [Fact]
        public void Sum1()
        {
            var val = (new int[] { 0, 2, 1 }).AsQueryable().Sum();
            Assert.Equal((int)3, val);
        }

        [Fact]
        public void Sum2()
        {
            var val = (new int?[] { 0, 2, 1 }).AsQueryable().Sum();
            Assert.Equal((int)3, val);
        }

        [Fact]
        public void Sum3()
        {
            var val = (new long[] { 0, 2, 1 }).AsQueryable().Sum();
            Assert.Equal((long)3, val);
        }

        [Fact]
        public void Sum4()
        {
            var val = (new long?[] { 0, 2, 1 }).AsQueryable().Sum();
            Assert.Equal((long)3, val);
        }

        [Fact]
        public void Sum5()
        {
            var val = (new float[] { 0, 2, 1 }).AsQueryable().Sum();
            Assert.Equal((float)3, val);
        }

        [Fact]
        public void Sum6()
        {
            var val = (new float?[] { 0, 2, 1 }).AsQueryable().Sum();
            Assert.Equal((float)3, val);
        }

        [Fact]
        public void Sum7()
        {
            var val = (new double[] { 0, 2, 1 }).AsQueryable().Sum();
            Assert.Equal((double)3, val);
        }

        [Fact]
        public void Sum8()
        {
            var val = (new double?[] { 0, 2, 1 }).AsQueryable().Sum();
            Assert.Equal((double)3, val);
        }

        [Fact]
        public void Sum9()
        {
            var val = (new decimal[] { 0, 2, 1 }).AsQueryable().Sum();
            Assert.Equal((decimal)3, val);
        }

        [Fact]
        public void Sum10()
        {
            var val = (new decimal?[] { 0, 2, 1 }).AsQueryable().Sum();
            Assert.Equal((decimal)3, val);
        }

        [Fact]
        public void Sum11()
        {
            var val = (new int[] { 0, 2, 1 }).AsQueryable().Sum(n => n);
            Assert.Equal((int)3, val);
        }

        [Fact]
        public void Sum12()
        {
            var val = (new int?[] { 0, 2, 1 }).AsQueryable().Sum(n => n);
            Assert.Equal((int)3, val);
        }

        [Fact]
        public void Sum13()
        {
            var val = (new long[] { 0, 2, 1 }).AsQueryable().Sum(n => n);
            Assert.Equal((long)3, val);
        }

        [Fact]
        public void Sum14()
        {
            var val = (new long?[] { 0, 2, 1 }).AsQueryable().Sum(n => n);
            Assert.Equal((long)3, val);
        }

        [Fact]
        public void Sum15()
        {
            var val = (new float[] { 0, 2, 1 }).AsQueryable().Sum(n => n);
            Assert.Equal((float)3, val);
        }

        [Fact]
        public void Sum16()
        {
            var val = (new float?[] { 0, 2, 1 }).AsQueryable().Sum(n => n);
            Assert.Equal((float)3, val);
        }

        [Fact]
        public void Sum17()
        {
            var val = (new double[] { 0, 2, 1 }).AsQueryable().Sum(n => n);
            Assert.Equal((double)3, val);
        }

        [Fact]
        public void Sum18()
        {
            var val = (new double?[] { 0, 2, 1 }).AsQueryable().Sum(n => n);
            Assert.Equal((double)3, val);
        }

        [Fact]
        public void Sum19()
        {
            var val = (new decimal[] { 0, 2, 1 }).AsQueryable().Sum(n => n);
            Assert.Equal((decimal)3, val);
        }

        [Fact]
        public void Sum20()
        {
            var val = (new decimal?[] { 0, 2, 1 }).AsQueryable().Sum(n => n);
            Assert.Equal((decimal)3, val);
        }
    }
}
