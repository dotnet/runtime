// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace Windows.Foundation.Tests
{
    public class RectTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var rect = new Rect();
            Assert.Equal(0, rect.X);
            Assert.Equal(0, rect.Y);
            Assert.Equal(0, rect.Width);
            Assert.Equal(0, rect.Height);

            Assert.Equal(0, rect.Left);
            Assert.Equal(0, rect.Right);
            Assert.Equal(0, rect.Top);
            Assert.Equal(0, rect.Bottom);

            Assert.False(rect.IsEmpty);
        }

        [Theory]
        [InlineData(0, 0, 0, 0, 0, 0, 0, 0)]
        [InlineData(1, 2, 3, 4, 1, 2, 3, 4)]
        [InlineData(double.MaxValue, double.MaxValue, double.MaxValue, double.MaxValue, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity)]
        public void Ctor_X_Y_Width_Height(double x, double y, double width, double height, double expectedX, double expectedY, double expectedWidth, double expectedHeight)
        {
            var rect = new Rect(x, y, width, height);
            Assert.Equal(expectedX, rect.X);
            Assert.Equal(expectedY, rect.Y);
            Assert.Equal(expectedWidth, rect.Width);
            Assert.Equal(expectedHeight, rect.Height);

            Assert.Equal(expectedX, rect.Left);
            Assert.Equal(expectedX + expectedWidth, rect.Right);
            Assert.Equal(expectedY, rect.Top);
            Assert.Equal(expectedY + expectedHeight, rect.Bottom);

            Assert.False(rect.IsEmpty);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(double.NegativeInfinity)]
        [ActiveIssue(21704, TargetFrameworkMonikers.UapAot)]
        public void Ctor_NegativeWidth_ThrowsArgumentOutOfRangeException(double width)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("width", () => new Rect(1, 1, width, 1));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(double.NegativeInfinity)]
        [ActiveIssue(21704, TargetFrameworkMonikers.UapAot)]
        public void Ctor_NegativeHeight_ThrowsArgumentOutOfRangeException(double height)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("height", () => new Rect(1, 1, 1, height));
        }

        public static IEnumerable<object[]> Ctor_Point_Point_TestData()
        {
            yield return new object[] { new Point(1, 2), new Point(1, 2), 1, 2, 0, 0 };
            yield return new object[] { new Point(1, 2), new Point(3, 4), 1, 2, 2, 2 };
            yield return new object[] { new Point(3, 4), new Point(1, 2), 1, 2, 2, 2 };
        }

        [Theory]
        [MemberData(nameof(Ctor_Point_Point_TestData))]
        public void Ctor_Point_Point(Point point1, Point point2, double expectedX, double expectedY, double expectedWidth, double expectedHeight)
        {
            var rect = new Rect(point1, point2);
            Assert.Equal(expectedX, rect.X);
            Assert.Equal(expectedY, rect.Y);
            Assert.Equal(expectedWidth, rect.Width);
            Assert.Equal(expectedHeight, rect.Height);
        }

        public static IEnumerable<object[]> Ctor_Point_Size_TestData()
        {
            yield return new object[] { new Point(1, 2), Size.Empty, double.PositiveInfinity, double.PositiveInfinity, double.NegativeInfinity, double.NegativeInfinity };
            yield return new object[] { new Point(1, 2), new Size(0, 0), 1, 2, 0, 0 };
            yield return new object[] { new Point(1, 2), new Size(3, 4), 1, 2, 3, 4 };
            yield return new object[] { new Point(1, 2), new Size(double.MaxValue, double.MaxValue), 1, 2, double.PositiveInfinity, double.PositiveInfinity };
        }

        [Theory]
        [MemberData(nameof(Ctor_Point_Size_TestData))]
        public void Ctor_Point_Size(Point point, Size size, double expectedX, double expectedY, double expectedWidth, double expectedHeight)
        {
            var rect = new Rect(point, size);
            Assert.Equal(expectedX, rect.X);
            Assert.Equal(expectedY, rect.Y);
            Assert.Equal(expectedWidth, rect.Width);
            Assert.Equal(expectedHeight, rect.Height);
        }

        [Fact]
        public void Empty_Get_ReturnsExpected()
        {
            Rect rect = Rect.Empty;
            Assert.Equal(double.PositiveInfinity, rect.X);
            Assert.Equal(double.PositiveInfinity, rect.Y);
            Assert.Equal(double.NegativeInfinity, rect.Width);
            Assert.Equal(double.NegativeInfinity, rect.Height);

            Assert.Equal(double.PositiveInfinity, rect.Left);
            Assert.Equal(double.NegativeInfinity, rect.Right);
            Assert.Equal(double.PositiveInfinity, rect.Top);
            Assert.Equal(double.NegativeInfinity, rect.Bottom);

            Assert.True(rect.IsEmpty);
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
            var rect = new Rect { X = x };
            Assert.Equal(expectedX, rect.X);
        }

        [Theory]
        [MemberData(nameof(Coordinate_TestData))]
        public void Y_Set_GetReturnsExpected(double y, double expectedY)
        {
            var rect = new Rect { Y = y };
            Assert.Equal(expectedY, rect.Y);
        }

        public static IEnumerable<object[]> Size_TestData()
        {
            yield return new object[] { 0, 0 };
            yield return new object[] { 1, 1 };
            yield return new object[] { double.MaxValue, double.PositiveInfinity };
        }

        [Theory]
        [MemberData(nameof(Size_TestData))]
        public void Width_SetValid_GetReturnsExpected(double width, double expectedWidth)
        {
            var rect = new Rect { Width = width };
            Assert.Equal(expectedWidth, rect.Width);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(double.NegativeInfinity)]
        [ActiveIssue(21704, TargetFrameworkMonikers.UapAot)]
        public void Width_SetNegative_ThrowsArgumentOutOfRangeException(double width)
        {
            var rect = new Rect();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("Width", () => rect.Width = width);
        }

        [Theory]
        [MemberData(nameof(Size_TestData))]
        public void Height_SetValid_GetReturnsExpected(double height, double expectedHeight)
        {
            var rect = new Rect { Height = height };
            Assert.Equal(expectedHeight, rect.Height);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(double.NegativeInfinity)]
        [ActiveIssue(21704, TargetFrameworkMonikers.UapAot)]
        public void Height_SetNegative_ThrowsArgumentOutOfRangeException(double height)
        {
            var rect = new Rect();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("Height", () => rect.Height = height);
        }

        public static IEnumerable<object[]> Contains_TestData()
        {
            yield return new object[] { new Rect(1, 2, 3, 4), new Point(1, 2), true };
            yield return new object[] { new Rect(1, 2, 3, 4), new Point(3, 4), true };
            yield return new object[] { new Rect(1, 2, 3, 4), new Point(4, 6), true };
            yield return new object[] { new Rect(1, 2, 3, 4), new Point(5, 7), false };
            yield return new object[] { Rect.Empty, new Point(1, 2), false };
        }

        [Theory]
        [MemberData(nameof(Contains_TestData))]
        public void Contains_Point_ReturnsExpected(Rect rect, Point point, bool expected)
        {
            Assert.Equal(expected, rect.Contains(point));
        }

        public static IEnumerable<object[]> Intersect_TestData()
        {
            yield return new object[] { Rect.Empty, Rect.Empty, Rect.Empty };
            yield return new object[] { Rect.Empty, new Rect(1,2 , 3, 4), Rect.Empty };
            yield return new object[] { new Rect(1, 2, 3, 4), Rect.Empty, Rect.Empty };
            yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 3, 4), new Rect(1, 2, 3, 4) };
            yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 0, 0, 0), Rect.Empty };
            yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 2, 6, 6), new Rect(2, 2, 2, 4) };
            yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 2, 2, 2), new Rect(2, 2, 2, 2) };
            yield return new object[] { new Rect(1, 2, 3, 4), new Rect(-2, -2, 12, 12), new Rect(1, 2, 3, 4) };
        }

        [Theory]
        [MemberData(nameof(Intersect_TestData))]
        public void Intersect_Rect_ReturnsExpected(Rect rect, Rect other, Rect expected)
        {
            rect.Intersect(other);
            Assert.Equal(expected, rect);
        }

        public static IEnumerable<object[]> Union_Rect_TestData()
        {
            yield return new object[] { Rect.Empty, Rect.Empty, Rect.Empty };
            yield return new object[] { Rect.Empty, new Rect(1, 2, 3, 4), new Rect(1, 2, 3, 4) };
            yield return new object[] { new Rect(1, 2, 3, 4), Rect.Empty, new Rect(1, 2, 3, 4) };
            yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 3, 4), new Rect(1, 2, 3, 4) };
            yield return new object[] { new Rect(1, 2, 3, 4), new Rect(0, 0, 0, 0), new Rect(0, 0, 4, 6) };
            yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 2, 6, 6), new Rect(1, 2, 7, 6) };
            yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 2, 2, 2), new Rect(1, 2, 3, 4) };
            yield return new object[] { new Rect(1, 2, 3, 4), new Rect(-2, -2, 2, 2), new Rect(-2, -2, 6, 8) };
            yield return new object[] { new Rect(-1, -2, 3, 4), new Rect(2, 2, 2, 2), new Rect(-1, -2, 5, 6) };

            yield return new object[] { new Rect(1, 2, double.PositiveInfinity, double.PositiveInfinity), new Rect(-1, -2, 3, 4), new Rect(-1, -2, double.PositiveInfinity, double.PositiveInfinity)  };
            yield return new object[] { new Rect(-1, -2, 3, 4), new Rect(1, 2, double.PositiveInfinity, double.PositiveInfinity), new Rect(-1, -2, double.PositiveInfinity, double.PositiveInfinity) };
        }

        [Theory]
        [MemberData(nameof(Union_Rect_TestData))]
        public void Union_Rect_ReturnsExpected(Rect rect, Rect other, Rect expected)
        {
            rect.Union(other);
            Assert.Equal(expected, rect);
        }

        public static IEnumerable<object[]> Union_Point_TestData()
        {
            yield return new object[] { Rect.Empty, new Point(1, 2), new Rect(1, 2, 0, 0) };
            yield return new object[] { new Rect(2, 3, 4, 5), new Point(1, 2), new Rect(1, 2, 5, 6) };
            yield return new object[] { new Rect(2, 3, 4, 5), new Point(-1, -2), new Rect(-1, -2, 7, 10) };
            yield return new object[] { new Rect(2, 3, 4, 5), new Point(2, 3), new Rect(2, 3, 4, 5) };
        }

        [Theory]
        [MemberData(nameof(Union_Point_TestData))]
        public void Union_Point_ReturnsExpected(Rect rect, Point point, Rect expected)
        {
            rect.Union(point);
            Assert.Equal(expected, rect);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 3, 4), true };
            yield return new object[] { new Rect(1, 2, 3, 4), new Rect(2, 2, 3, 4), false };
            yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 3, 4), false };
            yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 2, 4, 4), false };
            yield return new object[] { new Rect(1, 2, 3, 4), new Rect(1, 3, 3, 5), false };
            yield return new object[] { new Rect(1, 2, 3, 4), Rect.Empty, false };

            yield return new object[] { Rect.Empty, Rect.Empty, true };
            yield return new object[] { Rect.Empty, new Rect(1, 2, 3, 4), false };

            yield return new object[] { new Rect(1, 2, 3, 4), new object(), false };
            yield return new object[] { new Rect(1, 2, 3, 4), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Other_ReturnsExpected(Rect rect, object other, bool expected)
        {
            Assert.Equal(expected, rect.Equals(other));
            if (other is Rect otherRect)
            {
                Assert.Equal(expected, rect == otherRect);
                Assert.Equal(!expected, rect != otherRect);
                Assert.Equal(expected, rect.Equals(otherRect));
                Assert.Equal(expected, rect.GetHashCode().Equals(other.GetHashCode()));
            }
        }
        
        public static IEnumerable<object[]> ToString_TestData()
        {
            yield return new object[] { new Rect(1, 2, 3, 4), null, null, "1,2,3,4" };
            yield return new object[] { new Rect(1, 2, 3, 4), null, CultureInfo.InvariantCulture, "1,2,3,4" };

            yield return new object[] { new Rect(1, 2, 3, 4), "", CultureInfo.InvariantCulture, "1,2,3,4" };
            yield return new object[] { new Rect(1, 2, 3, 4), "abc", null, "abc,abc,abc,abc" };
            yield return new object[] { new Rect(1, 2, 3, 4), "N4", CultureInfo.InvariantCulture, "1.0000,2.0000,3.0000,4.0000" };

            yield return new object[] { new Rect(1, 2, 3, 4), "", new NumberFormatInfo { NumberDecimalSeparator = "," }, "1;2;3;4" };
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public void ToString_Invoke_ReturnsExpected(Rect rect, string format, IFormatProvider formatProvider, string expected)
        {
            if (format == null)
            {
                if (formatProvider == null)
                {
                    Assert.Equal(expected, rect.ToString());
                }

                Assert.Equal(expected, rect.ToString(formatProvider));
            }

            Assert.Equal(expected, ((IFormattable)rect).ToString(format, formatProvider));
        }
    }
}
