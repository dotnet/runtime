// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace Windows.Foundation.Tests
{
    public class PointTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var point = new Point();
            Assert.Equal(0, point.X);
            Assert.Equal(0, point.Y);
        }

        [Theory]
        [InlineData(double.MinValue, double.MinValue, double.NegativeInfinity, double.NegativeInfinity)]
        [InlineData(-1, -2, -1 , -2)]
        [InlineData(0, 0, 0, 0)]
        [InlineData(1, 2, 1, 2)]
        [InlineData(double.MaxValue, double.MaxValue, double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.NaN, double.NaN, double.NaN, double.NaN)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity)]
        public void Ctor_X_Y(double x, double y, double expectedX, double expectedY)
        {
            var point = new Point(x, y);    
            Assert.Equal(expectedX, point.X);
            Assert.Equal(expectedY, point.Y);
        }

        public static IEnumerable<object[]> Coordinate_TestData()
        {
            yield return new object[] { double.MinValue, double.NegativeInfinity };
            yield return new object[] { -1, -1 };
            yield return new object[] { 0, 0 };
            yield return new object[] { 1, 1 };
            yield return new object[] { double.MaxValue, double.PositiveInfinity };
            yield return new object[] { double.NaN, double.NaN };
            yield return new object[] { double.NegativeInfinity, double.NegativeInfinity };
        }

        [Theory]
        [MemberData(nameof(Coordinate_TestData))]
        public void X_Set_GetReturnsExpected(double x, double expectedX)
        {
            var point = new Point { X = x };
            Assert.Equal(expectedX, point.X);
        }

        [Theory]
        [MemberData(nameof(Coordinate_TestData))]
        public void Y_Set_GetReturnsExpected(double y, double expectedY)
        {
            var point = new Point { Y = y };
            Assert.Equal(expectedY, point.Y);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { new Point(1, 2), new Point(1, 2), true };
            yield return new object[] { new Point(1, 2), new Point(2, 2), false };
            yield return new object[] { new Point(1, 2), new Point(1, 3), false };

            yield return new object[] { new Point(1, 2), new object(), false };
            yield return new object[] { new Point(1, 2), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Other_ReturnsExpected(Point point, object other, bool expected)
        {
            Assert.Equal(expected, point.Equals(other));
            if (other is Point otherPoint)
            {
                Assert.Equal(expected, point == otherPoint);
                Assert.Equal(!expected, point != otherPoint);
                Assert.Equal(expected, point.Equals(otherPoint));
                Assert.Equal(expected, point.GetHashCode().Equals(other.GetHashCode()));
            }
        }
        
        public static IEnumerable<object[]> ToString_TestData()
        {
            yield return new object[] { new Point(1, 2), null, null, "1,2" };
            yield return new object[] { new Point(1, 2), null, CultureInfo.InvariantCulture, "1,2" };

            yield return new object[] { new Point(1, 2), "", CultureInfo.InvariantCulture, "1,2" };
            yield return new object[] { new Point(1, 2), "abc", null, "abc,abc" };
            yield return new object[] { new Point(1, 2), "N4", CultureInfo.InvariantCulture, "1.0000,2.0000" };

            yield return new object[] { new Point(1, 2), "", new NumberFormatInfo { NumberDecimalSeparator = "," }, "1;2" };
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public void ToString_Invoke_ReturnsExpected(Point point, string format, IFormatProvider formatProvider, string expected)
        {
            if (format == null)
            {
                if (formatProvider == null)
                {
                    Assert.Equal(expected, point.ToString());
                }

                Assert.Equal(expected, point.ToString(formatProvider));
            }

            Assert.Equal(expected, ((IFormattable)point).ToString(format, formatProvider));
        }
    }
}
