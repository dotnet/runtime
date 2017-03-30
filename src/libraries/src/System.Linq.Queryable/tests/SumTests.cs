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
            Assert.Throws<ArgumentNullException>("source", () => sourceInt.Sum());
            Assert.Throws<ArgumentNullException>("source", () => sourceInt.Sum(x => x));
        }

        [Fact]
        public void SumOfNullableOfInt_SourceIsNull_ArgumentNullExceptionThrown()
        {
            IQueryable<int?> sourceNullableInt = null;
            Assert.Throws<ArgumentNullException>("source", () => sourceNullableInt.Sum());
            Assert.Throws<ArgumentNullException>("source", () => sourceNullableInt.Sum(x => x));
        }

        [Fact]
        public void SumOfLong_SourceIsNull_ArgumentNullExceptionThrown()
        {
            IQueryable<long> sourceLong = null;
            Assert.Throws<ArgumentNullException>("source", () => sourceLong.Sum());
            Assert.Throws<ArgumentNullException>("source", () => sourceLong.Sum(x => x));
        }

        [Fact]
        public void SumOfNullableOfLong_SourceIsNull_ArgumentNullExceptionThrown()
        {
            IQueryable<long?> sourceNullableLong = null;
            Assert.Throws<ArgumentNullException>("source", () => sourceNullableLong.Sum());
            Assert.Throws<ArgumentNullException>("source", () => sourceNullableLong.Sum(x => x));
        }

        [Fact]
        public void SumOfFloat_SourceIsNull_ArgumentNullExceptionThrown()
        {
            IQueryable<float> sourceFloat = null;
            Assert.Throws<ArgumentNullException>("source", () => sourceFloat.Sum());
            Assert.Throws<ArgumentNullException>("source", () => sourceFloat.Sum(x => x));
        }

        [Fact]
        public void SumOfNullableOfFloat_SourceIsNull_ArgumentNullExceptionThrown()
        {
            IQueryable<float?> sourceNullableFloat = null;
            Assert.Throws<ArgumentNullException>("source", () => sourceNullableFloat.Sum());
            Assert.Throws<ArgumentNullException>("source", () => sourceNullableFloat.Sum(x => x));
        }

        [Fact]
        public void SumOfDouble_SourceIsNull_ArgumentNullExceptionThrown()
        {
            IQueryable<double> sourceDouble = null;
            Assert.Throws<ArgumentNullException>("source", () => sourceDouble.Sum());
            Assert.Throws<ArgumentNullException>("source", () => sourceDouble.Sum(x => x));
        }

        [Fact]
        public void SumOfNullableOfDouble_SourceIsNull_ArgumentNullExceptionThrown()
        {
            IQueryable<double?> sourceNullableDouble = null;
            Assert.Throws<ArgumentNullException>("source", () => sourceNullableDouble.Sum());
            Assert.Throws<ArgumentNullException>("source", () => sourceNullableDouble.Sum(x => x));
        }

        [Fact]
        public void SumOfDecimal_SourceIsNull_ArgumentNullExceptionThrown()
        {
            IQueryable<decimal> sourceDecimal = null;
            Assert.Throws<ArgumentNullException>("source", () => sourceDecimal.Sum());
            Assert.Throws<ArgumentNullException>("source", () => sourceDecimal.Sum(x => x));
        }

        [Fact]
        public void SumOfNullableOfDecimal_SourceIsNull_ArgumentNullExceptionThrown()
        {
            IQueryable<decimal?> sourceNullableDecimal = null;
            Assert.Throws<ArgumentNullException>("source", () => sourceNullableDecimal.Sum());
            Assert.Throws<ArgumentNullException>("source", () => sourceNullableDecimal.Sum(x => x));
        }

        [Fact]
        public void SumOfInt_SelectorIsNull_ArgumentNullExceptionThrown()
        {
            IQueryable<int> sourceInt = Enumerable.Empty<int>().AsQueryable();
            Expression<Func<int, int>> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => sourceInt.Sum(selector));
        }

        [Fact]
        public void SumOfNullableOfInt_SelectorIsNull_ArgumentNullExceptionThrown()
        {
            IQueryable<int?> sourceNullableInt = Enumerable.Empty<int?>().AsQueryable();
            Expression<Func<int?, int?>> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => sourceNullableInt.Sum(selector));
        }

        [Fact]
        public void SumOfLong_SelectorIsNull_ArgumentNullExceptionThrown()
        {
            IQueryable<long> sourceLong = Enumerable.Empty<long>().AsQueryable();
            Expression<Func<long, long>> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => sourceLong.Sum(selector));
        }

        [Fact]
        public void SumOfNullableOfLong_SelectorIsNull_ArgumentNullExceptionThrown()
        {
            IQueryable<long?> sourceNullableLong = Enumerable.Empty<long?>().AsQueryable();
            Expression<Func<long?, long?>> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => sourceNullableLong.Sum(selector));
        }

        [Fact]
        public void SumOfFloat_SelectorIsNull_ArgumentNullExceptionThrown()
        {
            IQueryable<float> sourceFloat = Enumerable.Empty<float>().AsQueryable();
            Expression<Func<float, float>> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => sourceFloat.Sum(selector));
        }

        [Fact]
        public void SumOfNullableOfFloat_SelectorIsNull_ArgumentNullExceptionThrown()
        {
            IQueryable<float?> sourceNullableFloat = Enumerable.Empty<float?>().AsQueryable();
            Expression<Func<float?, float?>> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => sourceNullableFloat.Sum(selector));
        }

        [Fact]
        public void SumOfDouble_SelectorIsNull_ArgumentNullExceptionThrown()
        {
            IQueryable<double> sourceDouble = Enumerable.Empty<double>().AsQueryable();
            Expression<Func<double, double>> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => sourceDouble.Sum(selector));
        }

        [Fact]
        public void SumOfNullableOfDouble_SelectorIsNull_ArgumentNullExceptionThrown()
        {
            IQueryable<double?> sourceNullableDouble = Enumerable.Empty<double?>().AsQueryable();
            Expression<Func<double?, double?>> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => sourceNullableDouble.Sum(selector));
        }

        [Fact]
        public void SumOfDecimal_SelectorIsNull_ArgumentNullExceptionThrown()
        {
            IQueryable<decimal> sourceDecimal = Enumerable.Empty<decimal>().AsQueryable();
            Expression<Func<decimal, decimal>> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => sourceDecimal.Sum(selector));
        }

        [Fact]
        public void SumOfNullableOfDecimal_SelectorIsNull_ArgumentNullExceptionThrown()
        {
            IQueryable<decimal?> sourceNullableDecimal = Enumerable.Empty<decimal?>().AsQueryable();
            Expression<Func<decimal?, decimal?>> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => sourceNullableDecimal.Sum(selector));
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
