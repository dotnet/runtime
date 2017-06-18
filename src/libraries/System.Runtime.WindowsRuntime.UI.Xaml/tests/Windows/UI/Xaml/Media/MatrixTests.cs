// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Windows.Foundation;
using Xunit;

namespace Windows.UI.Xaml.Media.Media3D.Tests
{
    public class Matrix3DTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var matrix = new Matrix3D();
            Assert.Equal(0, matrix.M11);
            Assert.Equal(0, matrix.M12);
            Assert.Equal(0, matrix.M13);
            Assert.Equal(0, matrix.M14);
            Assert.Equal(0, matrix.M21);
            Assert.Equal(0, matrix.M22);
            Assert.Equal(0, matrix.M23);
            Assert.Equal(0, matrix.M24);
            Assert.Equal(0, matrix.M31);
            Assert.Equal(0, matrix.M32);
            Assert.Equal(0, matrix.M33);
            Assert.Equal(0, matrix.M34);
            Assert.Equal(0, matrix.OffsetX);
            Assert.Equal(0, matrix.OffsetY);
            Assert.Equal(0, matrix.OffsetZ);
            Assert.Equal(0, matrix.M44);

            Assert.False(matrix.IsIdentity);
        }

        [Theory]
        [InlineData(-1, -2, -3, -4, -5, -6, -7, -8, -9, -10, -11, -12, -13, -14, -15, -16, false, false)]
        [InlineData(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, false, false)]
        [InlineData(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, false, false)]
        [InlineData(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, true, true)]
        [InlineData(1, 2, 0, 0, 0, 1, 2, 0, 4, 0, 1, 2, 0, 4, 0, 1, false, true)]
        [InlineData(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, false, true)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, false, true)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity, false, true)]
        public void Ctor_Values(double m11, double m12, double m13, double m14,
                                double m21, double m22, double m23, double m24,
                                double m31, double m32, double m33, double m34, 
                                double offsetX, double offsetY, double offsetZ, double m44, bool expectedIsIdentity, bool expectedHasInverse)
        {
            var matrix = new Matrix3D(m11, m12, m13, m14, m21, m22, m23, m24, m31, m32, m33, m34, offsetX, offsetY, offsetZ, m44);
            Assert.Equal(m11, matrix.M11);
            Assert.Equal(m12, matrix.M12);
            Assert.Equal(m13, matrix.M13);
            Assert.Equal(m14, matrix.M14);
            Assert.Equal(m21, matrix.M21);
            Assert.Equal(m22, matrix.M22);
            Assert.Equal(m23, matrix.M23);
            Assert.Equal(m24, matrix.M24);
            Assert.Equal(m31, matrix.M31);
            Assert.Equal(m32, matrix.M32);
            Assert.Equal(m33, matrix.M33);
            Assert.Equal(m34, matrix.M34);
            Assert.Equal(offsetX, matrix.OffsetX);
            Assert.Equal(offsetY, matrix.OffsetY);
            Assert.Equal(offsetZ, matrix.OffsetZ);
            Assert.Equal(m44, matrix.M44);

            Assert.Equal(expectedIsIdentity, matrix.IsIdentity);
            Assert.Equal(expectedHasInverse, matrix.HasInverse);
        }

        [Fact]
        public void Identity_Get_ReturnsExpected()
        {
            Matrix3D matrix = Matrix3D.Identity;
            Assert.Equal(1, matrix.M11);
            Assert.Equal(0, matrix.M12);
            Assert.Equal(0, matrix.M13);
            Assert.Equal(0, matrix.M14);
            Assert.Equal(0, matrix.M21);
            Assert.Equal(1, matrix.M22);
            Assert.Equal(0, matrix.M23);
            Assert.Equal(0, matrix.M24);
            Assert.Equal(0, matrix.M31);
            Assert.Equal(0, matrix.M32);
            Assert.Equal(1, matrix.M33);
            Assert.Equal(0, matrix.M34);
            Assert.Equal(0, matrix.OffsetX);
            Assert.Equal(0, matrix.OffsetY);
            Assert.Equal(0, matrix.OffsetZ);
            Assert.Equal(1, matrix.M44);

            Assert.True(matrix.IsIdentity);
            Assert.True(matrix.HasInverse);
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
            var matrix = new Matrix3D { M11 = value };
            Assert.Equal(value, matrix.M11);
        }

        [Theory]
        [MemberData(nameof(Values_TestData))]
        public void M12_Set_GetReturnsExpected(double value)
        {
            var matrix = new Matrix3D { M12 = value };
            Assert.Equal(value, matrix.M12);
        }

        [Theory]
        [MemberData(nameof(Values_TestData))]
        public void M13_Set_GetReturnsExpected(double value)
        {
            var matrix = new Matrix3D { M13 = value };
            Assert.Equal(value, matrix.M13);
        }

        [Theory]
        [MemberData(nameof(Values_TestData))]
        public void M14_Set_GetReturnsExpected(double value)
        {
            var matrix = new Matrix3D { M14 = value };
            Assert.Equal(value, matrix.M14);
        }

        [Theory]
        [MemberData(nameof(Values_TestData))]
        public void M21_Set_GetReturnsExpected(double value)
        {
            var matrix = new Matrix3D { M21 = value };
            Assert.Equal(value, matrix.M21);
        }

        [Theory]
        [MemberData(nameof(Values_TestData))]
        public void M22_Set_GetReturnsExpected(double value)
        {
            var matrix = new Matrix3D { M22 = value };
            Assert.Equal(value, matrix.M22);
        }

        [Theory]
        [MemberData(nameof(Values_TestData))]
        public void M23_Set_GetReturnsExpected(double value)
        {
            var matrix = new Matrix3D { M23 = value };
            Assert.Equal(value, matrix.M23);
        }

        [Theory]
        [MemberData(nameof(Values_TestData))]
        public void M24_Set_GetReturnsExpected(double value)
        {
            var matrix = new Matrix3D { M24 = value };
            Assert.Equal(value, matrix.M24);
        }
        [Theory]
        [MemberData(nameof(Values_TestData))]
        public void M31_Set_GetReturnsExpected(double value)
        {
            var matrix = new Matrix3D { M31 = value };
            Assert.Equal(value, matrix.M31);
        }

        [Theory]
        [MemberData(nameof(Values_TestData))]
        public void M32_Set_GetReturnsExpected(double value)
        {
            var matrix = new Matrix3D { M32 = value };
            Assert.Equal(value, matrix.M32);
        }

        [Theory]
        [MemberData(nameof(Values_TestData))]
        public void M33_Set_GetReturnsExpected(double value)
        {
            var matrix = new Matrix3D { M33 = value };
            Assert.Equal(value, matrix.M33);
        }

        [Theory]
        [MemberData(nameof(Values_TestData))]
        public void M34_Set_GetReturnsExpected(double value)
        {
            var matrix = new Matrix3D { M34 = value };
            Assert.Equal(value, matrix.M34);
        }

        [Theory]
        [MemberData(nameof(Values_TestData))]
        public void OffsetX_Set_GetReturnsExpected(double value)
        {
            var matrix = new Matrix3D { OffsetX = value };
            Assert.Equal(value, matrix.OffsetX);
        }

        [Theory]
        [MemberData(nameof(Values_TestData))]
        public void OffsetY_Set_GetReturnsExpected(double value)
        {
            var matrix = new Matrix3D { OffsetY = value };
            Assert.Equal(value, matrix.OffsetY);
        }

        [Theory]
        [MemberData(nameof(Values_TestData))]
        public void OffsetZ_Set_GetReturnsExpected(double value)
        {
            var matrix = new Matrix3D { OffsetZ = value };
            Assert.Equal(value, matrix.OffsetZ);
        }

        [Theory]
        [MemberData(nameof(Values_TestData))]
        public void M44_Set_GetReturnsExpected(double value)
        {
            var matrix = new Matrix3D { M44 = value };
            Assert.Equal(value, matrix.M44);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            var matrix = new Matrix3D(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);

            yield return new object[] { matrix, new Matrix3D(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16), true };
            yield return new object[] { matrix, new Matrix3D(2, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16), false };
            yield return new object[] { matrix, new Matrix3D(1, 3, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16), false };
            yield return new object[] { matrix, new Matrix3D(1, 2, 4, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16), false };
            yield return new object[] { matrix, new Matrix3D(1, 2, 3, 5, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16), false };
            yield return new object[] { matrix, new Matrix3D(1, 2, 3, 4, 6, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16), false };
            yield return new object[] { matrix, new Matrix3D(1, 2, 3, 4, 5, 7, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16), false };
            yield return new object[] { matrix, new Matrix3D(1, 2, 3, 4, 5, 6, 8, 8, 9, 10, 11, 12, 13, 14, 15, 16), false };
            yield return new object[] { matrix, new Matrix3D(1, 2, 3, 4, 5, 6, 7, 9, 9, 10, 11, 12, 13, 14, 15, 16), false };
            yield return new object[] { matrix, new Matrix3D(1, 2, 3, 4, 5, 6, 7, 8, 10, 10, 11, 12, 13, 14, 15, 16), false };
            yield return new object[] { matrix, new Matrix3D(1, 2, 3, 4, 5, 6, 7, 8, 9, 11, 11, 12, 13, 14, 15, 16), false };
            yield return new object[] { matrix, new Matrix3D(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 12, 12, 13, 14, 15, 16), false };
            yield return new object[] { matrix, new Matrix3D(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 13, 14, 15, 16), false };
            yield return new object[] { matrix, new Matrix3D(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 14, 14, 15, 16), false };
            yield return new object[] { matrix, new Matrix3D(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 15, 15, 16), false };
            yield return new object[] { matrix, new Matrix3D(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 16, 16), false };
            yield return new object[] { matrix, new Matrix3D(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 16, 17), false };

            yield return new object[] { Matrix3D.Identity, Matrix3D.Identity, true };
            yield return new object[] { Matrix3D.Identity, new Matrix3D(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1), true };
            yield return new object[] { Matrix3D.Identity, new Matrix3D(1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1), false };

            yield return new object[] { Matrix3D.Identity, new object(), false };
            yield return new object[] { Matrix3D.Identity, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Object_ReturnsExpected(Matrix3D matrix, object other, bool expected)
        {
            Assert.Equal(expected, matrix.Equals(other));
            if (other is Matrix3D otherMatrix)
            {
                Assert.Equal(expected, matrix.Equals(otherMatrix));
                Assert.Equal(expected, matrix == otherMatrix);
                Assert.Equal(!expected, matrix != otherMatrix);
                Assert.Equal(expected, matrix.GetHashCode().Equals(otherMatrix.GetHashCode()));
            }
        }

        [Fact]
        public void Multiply_Matrices_ReturnsExpected()
        {
            var matrix1 = new Matrix3D(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            var matrix2 = new Matrix3D(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);

            Matrix3D result = matrix1 * matrix2;
            Assert.Equal(new Matrix3D(90, 100, 110, 120, 202, 228, 254, 280, 314, 356, 398, 440, 426, 484, 542, 600), result);

            Assert.False(result.IsIdentity);
            Assert.False(result.HasInverse);
        }

        [Fact]
        public void Invert_Affine_ReturnsExpected()
        {
            var matrix = new Matrix3D(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);
            matrix.Invert();

            Assert.Equal(new Matrix3D(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1), matrix);
        }

        [Fact]
        public void Invert_NonAffine_ReturnsExpected()
        {
            var matrix = new Matrix3D(1, 2, 0, 0, 0, 1, 2, 0, 4, 0, 1, 2, 0, 4, 0, 1);
            matrix.Invert();

            string expected = ((IFormattable)new Matrix3D(0.515151515151515, -0.0606060606060606, 0.121212121212121, -0.242424242424242, 0.242424242424242, 0.0303030303030303, -0.0606060606060606, 0.121212121212121, -0.121212121212121, 0.484848484848485, 0.0303030303030303, -0.0606060606060606, -0.96969696969697, -0.121212121212121, 0.242424242424242, 0.515151515151515)).ToString("N2", null);
            Assert.Equal(expected, ((IFormattable)matrix).ToString("N2", null));
        }

        [Fact]
        public void Invert_NotInvertible_ThrowsInvalidOperationException()
        {
            var matrix = new Matrix3D(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            Assert.Throws<InvalidOperationException>(() => matrix.Invert());
        }

        [Fact]
        public void Invert_AffineNotInvertible_ThrowsInvalidOperationException()
        {
            var matrix = new Matrix3D(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1);
            Assert.Throws<InvalidOperationException>(() => matrix.Invert());
        }

        public static IEnumerable<object[]> ToString_TestData()
        {
            string cultureSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            char decimalSeparator = cultureSeparator.Length > 0 && cultureSeparator[0] == ',' ? ';' : ',';

            yield return new object[] { Matrix3D.Identity, null, null, "Identity" };
            yield return new object[] { Matrix3D.Identity, "InvalidFormat", CultureInfo.CurrentCulture, "Identity" };
            yield return new object[] { new Matrix3D(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16), null, null, $"1{decimalSeparator}2{decimalSeparator}3{decimalSeparator}4{decimalSeparator}5{decimalSeparator}6{decimalSeparator}7{decimalSeparator}8{decimalSeparator}9{decimalSeparator}10{decimalSeparator}11{decimalSeparator}12{decimalSeparator}13{decimalSeparator}14{decimalSeparator}15{decimalSeparator}16" };

            var matrix = new Matrix3D(2.2, 2.2, 2.2, 2.2, 2.2, 2.2, 2.2, 2.2, 2.2, 2.2, 2.2, 2.2, 2.2, 2.2, 2.2, 2.2);

            var culture = new CultureInfo("en-US");
            culture.NumberFormat.NumberDecimalSeparator = "|";
            yield return new object[] { matrix, "abc", culture, "abc,abc,abc,abc,abc,abc,abc,abc,abc,abc,abc,abc,abc,abc,abc,abc" };
            yield return new object[] { matrix, "N4", culture, "2|2000,2|2000,2|2000,2|2000,2|2000,2|2000,2|2000,2|2000,2|2000,2|2000,2|2000,2|2000,2|2000,2|2000,2|2000,2|2000" };
            yield return new object[] { matrix, null, culture, "2|2,2|2,2|2,2|2,2|2,2|2,2|2,2|2,2|2,2|2,2|2,2|2,2|2,2|2,2|2,2|2" };

            var commaCulture = new CultureInfo("en-US");
            commaCulture.NumberFormat.NumberDecimalSeparator = ",";
            yield return new object[] { matrix, null, commaCulture, "2,2;2,2;2,2;2,2;2,2;2,2;2,2;2,2;2,2;2,2;2,2;2,2;2,2;2,2;2,2;2,2" };

            yield return new object[] { matrix, null, null, $"{2.2.ToString()}{decimalSeparator}{2.2.ToString()}{decimalSeparator}{2.2.ToString()}{decimalSeparator}{2.2.ToString()}{decimalSeparator}{2.2.ToString()}{decimalSeparator}{2.2.ToString()}{decimalSeparator}{2.2.ToString()}{decimalSeparator}{2.2.ToString()}{decimalSeparator}{2.2.ToString()}{decimalSeparator}{2.2.ToString()}{decimalSeparator}{2.2.ToString()}{decimalSeparator}{2.2.ToString()}{decimalSeparator}{2.2.ToString()}{decimalSeparator}{2.2.ToString()}{decimalSeparator}{2.2.ToString()}{decimalSeparator}{2.2.ToString()}" };
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public void ToString_Invoke_ReturnsExpected(Matrix3D matrix, string format, IFormatProvider formatProvider, string expected)
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