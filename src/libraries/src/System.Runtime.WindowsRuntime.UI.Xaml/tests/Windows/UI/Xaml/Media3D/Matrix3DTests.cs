// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Windows.Foundation;
using Xunit;

namespace Windows.UI.Xaml.Media.Tests
{
    public class MatrixTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var matrix = new Matrix();
            Assert.Equal(0, matrix.M11);
            Assert.Equal(0, matrix.M12);
            Assert.Equal(0, matrix.M21);
            Assert.Equal(0, matrix.M22);
            Assert.Equal(0, matrix.OffsetX);
            Assert.Equal(0, matrix.OffsetY);

            Assert.False(matrix.IsIdentity);
        }

        [Theory]
        [InlineData(-1, -2, -3, -4, -5, -6, false)]
        [InlineData(0, 0, 0, 0, 0, 0, false)]
        [InlineData(1, 1, 1, 1, 1, 1, false)]
        [InlineData(1, 0, 0, 1, 0, 0, true)]
        [InlineData(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, false)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, false)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity, false)]
        public void Ctor_Values(double m11, double m12, double m21, double m22, double offsetX, double offsetY, bool expectedIsIdentity)
        {
            var matrix = new Matrix(m11, m12, m21, m22, offsetX, offsetY);
            Assert.Equal(m11, matrix.M11);
            Assert.Equal(m12, matrix.M12);
            Assert.Equal(m21, matrix.M21);
            Assert.Equal(m22, matrix.M22);
            Assert.Equal(offsetX, matrix.OffsetX);
            Assert.Equal(offsetY, matrix.OffsetY);

            Assert.Equal(expectedIsIdentity, matrix.IsIdentity);
        }

        [Fact]
        public void Identity_Get_ReturnsExpected()
        {
            Matrix matrix = Matrix.Identity;
            Assert.Equal(1, matrix.M11);
            Assert.Equal(0, matrix.M12);
            Assert.Equal(0, matrix.M21);
            Assert.Equal(1, matrix.M22);
            Assert.Equal(0, matrix.OffsetX);
            Assert.Equal(0, matrix.OffsetY);

            Assert.True(matrix.IsIdentity);
        }

        public static IEnumerable<object[]> Values_TestData()
        {
            yield return new object[] { -1 };
            yield return new object[] { 0 };
            yield return new object[] { 1 };
            yield return new object[] { float.NaN };
            yield return new object[] { float.PositiveInfinity };
            yield return new object[] { float.NegativeInfinity };
        }

        [Theory]
        [MemberData(nameof(Values_TestData))]
        public void M11_Set_GetReturnsExpected(double value)
        {
            var matrix = new Matrix { M11 = value };
            Assert.Equal(value, matrix.M11);
        }

        [Theory]
        [MemberData(nameof(Values_TestData))]
        public void M12_Set_GetReturnsExpected(double value)
        {
            var matrix = new Matrix { M12 = value };
            Assert.Equal(value, matrix.M12);
        }

        [Theory]
        [MemberData(nameof(Values_TestData))]
        public void M21_Set_GetReturnsExpected(double value)
        {
            var matrix = new Matrix { M21 = value };
            Assert.Equal(value, matrix.M21);
        }

        [Theory]
        [MemberData(nameof(Values_TestData))]
        public void M22_Set_GetReturnsExpected(double value)
        {
            var matrix = new Matrix { M22 = value };
            Assert.Equal(value, matrix.M22);
        }

        [Theory]
        [MemberData(nameof(Values_TestData))]
        public void OffsetX_Set_GetReturnsExpected(double value)
        {
            var matrix = new Matrix { OffsetX = value };
            Assert.Equal(value, matrix.OffsetX);
        }

        [Theory]
        [MemberData(nameof(Values_TestData))]
        public void OffsetY_Set_GetReturnsExpected(double value)
        {
            var matrix = new Matrix { OffsetY = value };
            Assert.Equal(value, matrix.OffsetY);
        }

        [Fact]
        public void Transform_Identity_ReturnsPoint()
        {
            Point transformedPoint = Matrix.Identity.Transform(new Point(1, 2));
            Assert.Equal(new Point(1, 2), transformedPoint);
        }

        [Fact]
        public void Transform_Point_ReturnsExpected()
        {
            var matrix = new Matrix(1, 2, 3, 4, 5, 6);
            var point = new Point(1, 2);

            Point transformedPoint = matrix.Transform(point);
            Assert.Equal(new Point(12, 16), transformedPoint);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { new Matrix(1, 2, 3, 4, 5, 6), new Matrix(1, 2, 3, 4, 5, 6), true };
            yield return new object[] { new Matrix(1, 2, 3, 4, 5, 6), new Matrix(2, 2, 3, 4, 5, 6), false };
            yield return new object[] { new Matrix(1, 2, 3, 4, 5, 6), new Matrix(1, 3, 3, 4, 5, 6), false };
            yield return new object[] { new Matrix(1, 2, 3, 4, 5, 6), new Matrix(1, 2, 4, 4, 5, 6), false };
            yield return new object[] { new Matrix(1, 2, 3, 4, 5, 6), new Matrix(1, 2, 3, 5, 5, 6), false };
            yield return new object[] { new Matrix(1, 2, 3, 4, 5, 6), new Matrix(1, 2, 3, 4, 6, 6), false };
            yield return new object[] { new Matrix(1, 2, 3, 4, 5, 6), new Matrix(1, 2, 3, 4, 5, 7), false };

            yield return new object[] { Matrix.Identity, Matrix.Identity, true };
            yield return new object[] { Matrix.Identity, new Matrix(1, 0, 0, 1, 0, 0), true };
            yield return new object[] { Matrix.Identity, new Matrix(1, 0, 0, 0, 0, 0), false };

            yield return new object[] { new Matrix(1, 2, 3, 4, 5, 6), new object(), false };
            yield return new object[] { new Matrix(1, 2, 3, 4, 5, 6), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Object_ReturnsExpected(Matrix matrix, object other, bool expected)
        {
            Assert.Equal(expected, matrix.Equals(other));
            if (other is Matrix otherMatrix)
            {
                Assert.Equal(expected, matrix.Equals(otherMatrix));
                Assert.Equal(expected, matrix == otherMatrix);
                Assert.Equal(!expected, matrix != otherMatrix);
                Assert.Equal(expected, matrix.GetHashCode().Equals(otherMatrix.GetHashCode()));
            }
        }

        public static IEnumerable<object[]> ToString_TestData()
        {
            string cultureSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            char decimalSeparator = cultureSeparator.Length > 0 && cultureSeparator[0] == ',' ? ';' : ',';

            yield return new object[] { Matrix.Identity, null, null, "Identity" };
            yield return new object[] { Matrix.Identity, "InvalidFormat", CultureInfo.CurrentCulture, "Identity" };
            yield return new object[] { new Matrix(1, 2, 3, 4, 5, 6), null, null, $"1{decimalSeparator}2{decimalSeparator}3{decimalSeparator}4{decimalSeparator}5{decimalSeparator}6" };

            var culture = new CultureInfo("en-US");
            culture.NumberFormat.NumberDecimalSeparator = "|";
            yield return new object[] { new Matrix(2.2, 2.2, 2.2, 2.2, 2.2, 2.2), "abc", culture, "abc,abc,abc,abc,abc,abc" };
            yield return new object[] { new Matrix(2.2, 2.2, 2.2, 2.2, 2.2, 2.2), "N4", culture, "2|2000,2|2000,2|2000,2|2000,2|2000,2|2000" };
            yield return new object[] { new Matrix(2.2, 2.2, 2.2, 2.2, 2.2, 2.2), null, culture, "2|2,2|2,2|2,2|2,2|2,2|2" };

            var commaCulture = new CultureInfo("en-US");
            commaCulture.NumberFormat.NumberDecimalSeparator = ",";
            yield return new object[] { new Matrix(2.2, 2.2, 2.2, 2.2, 2.2, 2.2), null, commaCulture, "2,2;2,2;2,2;2,2;2,2;2,2" };

            yield return new object[] { new Matrix(2.2, 2.2, 2.2, 2.2, 2.2, 2.2), null, null, $"{2.2.ToString()}{decimalSeparator}{2.2.ToString()}{decimalSeparator}{2.2.ToString()}{decimalSeparator}{2.2.ToString()}{decimalSeparator}{2.2.ToString()}{decimalSeparator}{2.2.ToString()}" };
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public void ToString_Invoke_ReturnsExpected(Matrix matrix, string format, IFormatProvider formatProvider, string expected)
        {
            if (format == null)
            {
                if (formatProvider == null)
                {
                    Assert.Equal(expected, matrix.ToString());
                }

                Assert.Equal(expected, matrix.ToString(formatProvider));
            }

            Assert.Equal(expected, ((IFormattable)matrix).ToString(format, formatProvider));
        }
    }
}