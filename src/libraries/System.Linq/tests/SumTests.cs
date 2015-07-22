// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class SumTests
    {
        #region SourceIsNull - ArgumentNullExceptionThrown

        [Fact]
        public void SumOfInt_SourceIsNull_ArgumentNullExceptionThrown()
        {
            IEnumerable<int> sourceInt = null;
            Assert.Throws<ArgumentNullException>(() => sourceInt.Sum());
            Assert.Throws<ArgumentNullException>(() => sourceInt.Sum(x => x));
        }

        [Fact]
        public void SumOfNullableOfInt_SourceIsNull_ArgumentNullExceptionThrown()
        {
            IEnumerable<int?> sourceNullableInt = null;
            Assert.Throws<ArgumentNullException>(() => sourceNullableInt.Sum());
            Assert.Throws<ArgumentNullException>(() => sourceNullableInt.Sum(x => x));
        }

        [Fact]
        public void SumOfLong_SourceIsNull_ArgumentNullExceptionThrown()
        {
            IEnumerable<long> sourceLong = null;
            Assert.Throws<ArgumentNullException>(() => sourceLong.Sum());
            Assert.Throws<ArgumentNullException>(() => sourceLong.Sum(x => x));
        }

        [Fact]
        public void SumOfNullableOfLong_SourceIsNull_ArgumentNullExceptionThrown()
        {
            IEnumerable<long?> sourceNullableLong = null;
            Assert.Throws<ArgumentNullException>(() => sourceNullableLong.Sum());
            Assert.Throws<ArgumentNullException>(() => sourceNullableLong.Sum(x => x));
        }

        [Fact]
        public void SumOfFloat_SourceIsNull_ArgumentNullExceptionThrown()
        {
            IEnumerable<float> sourceFloat = null;
            Assert.Throws<ArgumentNullException>(() => sourceFloat.Sum());
            Assert.Throws<ArgumentNullException>(() => sourceFloat.Sum(x => x));
        }

        [Fact]
        public void SumOfNullableOfFloat_SourceIsNull_ArgumentNullExceptionThrown()
        {
            IEnumerable<float?> sourceNullableFloat = null;
            Assert.Throws<ArgumentNullException>(() => sourceNullableFloat.Sum());
            Assert.Throws<ArgumentNullException>(() => sourceNullableFloat.Sum(x => x));
        }

        [Fact]
        public void SumOfDouble_SourceIsNull_ArgumentNullExceptionThrown()
        {
            IEnumerable<double> sourceDouble = null;
            Assert.Throws<ArgumentNullException>(() => sourceDouble.Sum());
            Assert.Throws<ArgumentNullException>(() => sourceDouble.Sum(x => x));
        }

        [Fact]
        public void SumOfNullableOfDouble_SourceIsNull_ArgumentNullExceptionThrown()
        {
            IEnumerable<double?> sourceNullableDouble = null;
            Assert.Throws<ArgumentNullException>(() => sourceNullableDouble.Sum());
            Assert.Throws<ArgumentNullException>(() => sourceNullableDouble.Sum(x => x));
        }

        [Fact]
        public void SumOfDecimal_SourceIsNull_ArgumentNullExceptionThrown()
        {
            IEnumerable<decimal> sourceDecimal = null;
            Assert.Throws<ArgumentNullException>(() => sourceDecimal.Sum());
            Assert.Throws<ArgumentNullException>(() => sourceDecimal.Sum(x => x));
        }

        [Fact]
        public void SumOfNullableOfDecimal_SourceIsNull_ArgumentNullExceptionThrown()
        {
            IEnumerable<decimal?> sourceNullableDecimal = null;
            Assert.Throws<ArgumentNullException>(() => sourceNullableDecimal.Sum());
            Assert.Throws<ArgumentNullException>(() => sourceNullableDecimal.Sum(x => x));
        }

        #endregion

        #region SelectionIsNull - ArgumentNullExceptionThrown

        [Fact]
        public void SumOfInt_SelectorIsNull_ArgumentNullExceptionThrown()
        {
            IEnumerable<int> sourceInt = Enumerable.Empty<int>();
            Func<int, int> selector = null;
            Assert.Throws<ArgumentNullException>(() => sourceInt.Sum(selector));
        }

        [Fact]
        public void SumOfNullableOfInt_SelectorIsNull_ArgumentNullExceptionThrown()
        {

            IEnumerable<int?> sourceNullableInt = Enumerable.Empty<int?>();
            Func<int?, int?> selector = null;
            Assert.Throws<ArgumentNullException>(() => sourceNullableInt.Sum(selector));
        }

        [Fact]
        public void SumOfLong_SelectorIsNull_ArgumentNullExceptionThrown()
        {
            IEnumerable<long> sourceLong = Enumerable.Empty<long>();
            Func<long, long> selector = null;
            Assert.Throws<ArgumentNullException>(() => sourceLong.Sum(selector));
        }

        [Fact]
        public void SumOfNullableOfLong_SelectorIsNull_ArgumentNullExceptionThrown()
        {

            IEnumerable<long?> sourceNullableLong = Enumerable.Empty<long?>();
            Func<long?, long?> selector = null;
            Assert.Throws<ArgumentNullException>(() => sourceNullableLong.Sum(selector));
        }

        [Fact]
        public void SumOfFloat_SelectorIsNull_ArgumentNullExceptionThrown()
        {
            IEnumerable<float> sourceFloat = Enumerable.Empty<float>();
            Func<float, float> selector = null;
            Assert.Throws<ArgumentNullException>(() => sourceFloat.Sum(selector));
        }

        [Fact]
        public void SumOfNullableOfFloat_SelectorIsNull_ArgumentNullExceptionThrown()
        {

            IEnumerable<float?> sourceNullableFloat = Enumerable.Empty<float?>();
            Func<float?, float?> selector = null;
            Assert.Throws<ArgumentNullException>(() => sourceNullableFloat.Sum(selector));
        }

        [Fact]
        public void SumOfDouble_SelectorIsNull_ArgumentNullExceptionThrown()
        {
            IEnumerable<double> sourceDouble = Enumerable.Empty<double>();
            Func<double, double> selector = null;
            Assert.Throws<ArgumentNullException>(() => sourceDouble.Sum(selector));
        }

        [Fact]
        public void SumOfNullableOfDouble_SelectorIsNull_ArgumentNullExceptionThrown()
        {

            IEnumerable<double?> sourceNullableDouble = Enumerable.Empty<double?>();
            Func<double?, double?> selector = null;
            Assert.Throws<ArgumentNullException>(() => sourceNullableDouble.Sum(selector));
        }

        [Fact]
        public void SumOfDecimal_SelectorIsNull_ArgumentNullExceptionThrown()
        {
            IEnumerable<decimal> sourceDecimal = Enumerable.Empty<decimal>();
            Func<decimal, decimal> selector = null;
            Assert.Throws<ArgumentNullException>(() => sourceDecimal.Sum(selector));
        }

        [Fact]
        public void SumOfNullableOfDecimal_SelectorIsNull_ArgumentNullExceptionThrown()
        {

            IEnumerable<decimal?> sourceNullableDecimal = Enumerable.Empty<decimal?>();
            Func<decimal?, decimal?> selector = null;
            Assert.Throws<ArgumentNullException>(() => sourceNullableDecimal.Sum(selector));
        }

        #endregion

        #region SourceIsEmptyCollection - ZeroReturned

        [Fact]
        public void SumOfInt_SourceIsEmptyCollection_ZeroReturned()
        {
            IEnumerable<int> sourceInt = Enumerable.Empty<int>();
            Assert.Equal(0, sourceInt.Sum());
            Assert.Equal(0, sourceInt.Sum(x => x));
        }

        [Fact]
        public void SumOfNullableOfInt_SourceIsEmptyCollection_ZeroReturned()
        {

            IEnumerable<int?> sourceNullableInt = Enumerable.Empty<int?>();
            Assert.Equal(0, sourceNullableInt.Sum());
            Assert.Equal(0, sourceNullableInt.Sum(x => x));
        }

        [Fact]
        public void SumOfLong_SourceIsEmptyCollection_ZeroReturned()
        {
            IEnumerable<long> sourceLong = Enumerable.Empty<long>();
            Assert.Equal(0L, sourceLong.Sum());
            Assert.Equal(0L, sourceLong.Sum(x => x));
        }

        [Fact]
        public void SumOfNullableOfLong_SourceIsEmptyCollection_ZeroReturned()
        {

            IEnumerable<long?> sourceNullableLong = Enumerable.Empty<long?>();
            Assert.Equal(0L, sourceNullableLong.Sum());
            Assert.Equal(0L, sourceNullableLong.Sum(x => x));
        }

        [Fact]
        public void SumOfFloat_SourceIsEmptyCollection_ZeroReturned()
        {
            IEnumerable<float> sourceFloat = Enumerable.Empty<float>();
            Assert.Equal(0f, sourceFloat.Sum());
            Assert.Equal(0f, sourceFloat.Sum(x => x));
        }

        [Fact]
        public void SumOfNullableOfFloat_SourceIsEmptyCollection_ZeroReturned()
        {

            IEnumerable<float?> sourceNullableFloat = Enumerable.Empty<float?>();
            Assert.Equal(0f, sourceNullableFloat.Sum());
            Assert.Equal(0f, sourceNullableFloat.Sum(x => x));
        }

        [Fact]
        public void SumOfDouble_SourceIsEmptyCollection_ZeroReturned()
        {
            IEnumerable<double> sourceDouble = Enumerable.Empty<double>();
            Assert.Equal(0d, sourceDouble.Sum());
            Assert.Equal(0d, sourceDouble.Sum(x => x));
        }

        [Fact]
        public void SumOfNullableOfDouble_SourceIsEmptyCollection_ZeroReturned()
        {

            IEnumerable<double?> sourceNullableDouble = Enumerable.Empty<double?>();
            Assert.Equal(0d, sourceNullableDouble.Sum());
            Assert.Equal(0d, sourceNullableDouble.Sum(x => x));
        }

        [Fact]
        public void SumOfDecimal_SourceIsEmptyCollection_ZeroReturned()
        {
            IEnumerable<decimal> sourceDecimal = Enumerable.Empty<decimal>();
            Assert.Equal(0m, sourceDecimal.Sum());
            Assert.Equal(0m, sourceDecimal.Sum(x => x));
        }

        [Fact]
        public void SumOfNullableOfDecimal_SourceIsEmptyCollection_ZeroReturned()
        {

            IEnumerable<decimal?> sourceNullableDecimal = Enumerable.Empty<decimal?>();
            Assert.Equal(0m, sourceNullableDecimal.Sum());
            Assert.Equal(0m, sourceNullableDecimal.Sum(x => x));
        }

        #endregion

        #region SourceIsNotEmpty - ProperSumReturned

        [Fact]
        public void SumOfInt_SourceIsNotEmpty_ProperSumReturned()
        {
            IEnumerable<int> sourceInt = new int[] { 1, -2, 3, -4 };
            Assert.Equal(-2, sourceInt.Sum());
            Assert.Equal(-2, sourceInt.Sum(x => x));
        }

        [Fact]
        public void SumOfNullableOfInt_SourceIsNotEmpty_ProperSumReturned()
        {

            IEnumerable<int?> sourceNullableInt = new int?[] { 1, -2, null, 3, -4, null };
            Assert.Equal(-2, sourceNullableInt.Sum());
            Assert.Equal(-2, sourceNullableInt.Sum(x => x));
        }

        [Fact]
        public void SumOfLong_SourceIsNotEmpty_ProperSumReturned()
        {
            IEnumerable<long> sourceLong = new long[] { 1L, -2L, 3L, -4L };
            Assert.Equal(-2L, sourceLong.Sum());
            Assert.Equal(-2L, sourceLong.Sum(x => x));
        }

        [Fact]
        public void SumOfNullableOfLong_SourceIsNotEmpty_ProperSumReturned()
        {

            IEnumerable<long?> sourceNullableLong = new long?[] { 1L, -2L, null, 3L, -4L, null };
            Assert.Equal(-2L, sourceNullableLong.Sum());
            Assert.Equal(-2L, sourceNullableLong.Sum(x => x));
        }

        [Fact]
        public void SumOfFloat_SourceIsNotEmpty_ProperSumReturned()
        {
            IEnumerable<float> sourceFloat = new float[] { 1f, 0.5f, -1f, 0.5f };
            Assert.Equal(1f, sourceFloat.Sum());
            Assert.Equal(1f, sourceFloat.Sum(x => x));
        }

        [Fact]
        public void SumOfNullableOfFloat_SourceIsNotEmpty_ProperSumReturned()
        {

            IEnumerable<float?> sourceNullableFloat = new float?[] { 1f, 0.5f, null, -1f, 0.5f, null };
            Assert.Equal(1f, sourceNullableFloat.Sum());
            Assert.Equal(1f, sourceNullableFloat.Sum(x => x));
        }

        [Fact]
        public void SumOfDouble_SourceIsNotEmpty_ProperSumReturned()
        {
            IEnumerable<double> sourceDouble = new double[] { 1d, 0.5d, -1d, 0.5d };
            Assert.Equal(1d, sourceDouble.Sum());
            Assert.Equal(1d, sourceDouble.Sum(x => x));
        }

        [Fact]
        public void SumOfNullableOfDouble_SourceIsNotEmpty_ProperSumReturned()
        {

            IEnumerable<double?> sourceNullableDouble = new double?[] { 1d, 0.5d, null, -1d, 0.5d, null };
            Assert.Equal(1d, sourceNullableDouble.Sum());
            Assert.Equal(1d, sourceNullableDouble.Sum(x => x));
        }

        [Fact]
        public void SumOfDecimal_SourceIsNotEmpty_ProperSumReturned()
        {
            IEnumerable<decimal> sourceDecimal = new decimal[] { 1m, 0.5m, -1m, 0.5m };
            Assert.Equal(1m, sourceDecimal.Sum());
            Assert.Equal(1m, sourceDecimal.Sum(x => x));
        }

        [Fact]
        public void SumOfNullableOfDecimal_SourceIsNotEmpty_ProperSumReturned()
        {

            IEnumerable<decimal?> sourceNullableDecimal = new decimal?[] { 1m, 0.5m, null, -1m, 0.5m, null };
            Assert.Equal(1m, sourceNullableDecimal.Sum());
            Assert.Equal(1m, sourceNullableDecimal.Sum(x => x));
        }

        #endregion

        #region SourceSumsToOverflow - OverflowExceptionThrown or Infinity returned

        [Fact]
        public void SumOfInt_SourceSumsToOverflow_OverflowExceptionThrown()
        {
            IEnumerable<int> sourceInt = new int[] { int.MaxValue, 1 };
            Assert.Throws<OverflowException>(() => sourceInt.Sum());
            Assert.Throws<OverflowException>(() => sourceInt.Sum(x => x));
        }

        [Fact]
        public void SumOfNullableOfInt_SourceSumsToOverflow_OverflowExceptionThrown()
        {
            IEnumerable<int?> sourceNullableInt = new int?[] { int.MaxValue, null, 1 };
            Assert.Throws<OverflowException>(() => sourceNullableInt.Sum());
            Assert.Throws<OverflowException>(() => sourceNullableInt.Sum(x => x));
        }

        [Fact]
        public void SumOfLong_SourceSumsToOverflow_OverflowExceptionThrown()
        {
            IEnumerable<long> sourceLong = new long[] { long.MaxValue, 1L };
            Assert.Throws<OverflowException>(() => sourceLong.Sum());
            Assert.Throws<OverflowException>(() => sourceLong.Sum(x => x));
        }

        [Fact]
        public void SumOfNullableOfLong_SourceSumsToOverflow_OverflowExceptionThrown()
        {
            IEnumerable<long?> sourceNullableLong = new long?[] { long.MaxValue, null, 1 };
            Assert.Throws<OverflowException>(() => sourceNullableLong.Sum());
            Assert.Throws<OverflowException>(() => sourceNullableLong.Sum(x => x));
        }

        [Fact]
        public void SumOfFloat_SourceSumsToOverflow_InfinityReturned()
        {
            IEnumerable<float> sourceFloat = new float[] { float.MaxValue, float.MaxValue };
            Assert.True(float.IsInfinity(sourceFloat.Sum()));
            Assert.True(float.IsInfinity(sourceFloat.Sum(x => x)));
        }

        [Fact]
        public void SumOfNullableOfFloat_SourceSumsToOverflow_InfinityReturned()
        {
            IEnumerable<float?> sourceNullableFloat = new float?[] { float.MaxValue, null, float.MaxValue };
            Assert.True(float.IsInfinity(sourceNullableFloat.Sum().Value));
            Assert.True(float.IsInfinity(sourceNullableFloat.Sum(x => x).Value));
        }

        [Fact]
        public void SumOfDouble_SourceSumsToOverflow_InfinityReturned()
        {
            IEnumerable<double> sourceDouble = new double[] { double.MaxValue, double.MaxValue };
            Assert.True(double.IsInfinity(sourceDouble.Sum()));
            Assert.True(double.IsInfinity(sourceDouble.Sum(x => x)));
        }

        [Fact]
        public void SumOfNullableOfDouble_SourceSumsToOverflow_InfinityReturned()
        {
            IEnumerable<double?> sourceNullableDouble = new double?[] { double.MaxValue, null, double.MaxValue };
            Assert.True(double.IsInfinity(sourceNullableDouble.Sum().Value));
            Assert.True(double.IsInfinity(sourceNullableDouble.Sum(x => x).Value));
        }

        [Fact]
        public void SumOfDecimal_SourceSumsToOverflow_OverflowExceptionThrown()
        {
            IEnumerable<decimal> sourceDecimal = new decimal[] { decimal.MaxValue, 1m };
            Assert.Throws<OverflowException>(() => sourceDecimal.Sum());
            Assert.Throws<OverflowException>(() => sourceDecimal.Sum(x => x));
        }

        [Fact]
        public void SumOfNullableOfDecimal_SourceSumsToOverflow_OverflowExceptionThrown()
        {
            IEnumerable<decimal?> sourceNullableDecimal = new decimal?[] { decimal.MaxValue, null, 1m };
            Assert.Throws<OverflowException>(() => sourceNullableDecimal.Sum());
            Assert.Throws<OverflowException>(() => sourceNullableDecimal.Sum(x => x));
        }

        #endregion
    }
}
