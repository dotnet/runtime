// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Xunit;

namespace System.Drawing.PrimitivesTests
{
    public class SizeTests
    {
        [Fact]
        public void DefaultConstructorTest()
        {
            Assert.Equal(Size.Empty, new Size());
        }

        [Theory]
        [InlineData(int.MaxValue, int.MinValue)]
        [InlineData(int.MinValue, int.MinValue)]
        [InlineData(int.MaxValue, int.MaxValue)]
        [InlineData(0, 0)]
        public void NonDefaultConstructorTest(int width, int height)
        {
            Size s1 = new Size(width, height);
            Size s2 = new Size(new Point(width, height));

            Assert.Equal(s1, s2);

            s1.Width = 10;
            Assert.Equal(10, s1.Width);

            s1.Height = -10;
            Assert.Equal(-10, s1.Height);
        }

        [Fact]
        public void IsEmptyDefaultsTest()
        {
            Assert.True(Size.Empty.IsEmpty);
            Assert.True(new Size().IsEmpty);
            Assert.True(new Size(0, 0).IsEmpty);
        }

        [Theory]
        [InlineData(int.MaxValue, int.MinValue)]
        [InlineData(int.MinValue, int.MinValue)]
        [InlineData(int.MaxValue, int.MaxValue)]
        public void IsEmptyRandomTest(int width, int height)
        {
            Assert.False(new Size(width, height).IsEmpty);
        }

        [Theory]
        [InlineData(int.MaxValue, int.MinValue)]
        [InlineData(int.MinValue, int.MinValue)]
        [InlineData(int.MaxValue, int.MaxValue)]
        [InlineData(0, 0)]
        public void DimensionsTest(int width, int height)
        {
            Size p = new Size(width, height);
            Assert.Equal(width, p.Width);
            Assert.Equal(height, p.Height);
        }

        [Theory]
        [InlineData(int.MaxValue, int.MinValue)]
        [InlineData(int.MinValue, int.MinValue)]
        [InlineData(int.MaxValue, int.MaxValue)]
        [InlineData(0, 0)]
        public void PointFConversionTest(int width, int height)
        {
            SizeF sz = new Size(width, height);
            Assert.Equal(new SizeF(width, height), sz);
        }

        [Theory]
        [InlineData(int.MaxValue, int.MinValue)]
        [InlineData(int.MinValue, int.MinValue)]
        [InlineData(int.MaxValue, int.MaxValue)]
        [InlineData(0, 0)]
        public void SizeConversionTest(int width, int height)
        {
            Point sz = (Point)new Size(width, height);
            Assert.Equal(new Point(width, height), sz);
        }

        [Theory]
        [InlineData(int.MaxValue, int.MinValue)]
        [InlineData(int.MinValue, int.MinValue)]
        [InlineData(int.MaxValue, int.MaxValue)]
        [InlineData(0, 0)]
        public void ArithmeticTest(int width, int height)
        {
            Size sz1 = new Size(width, height);
            Size sz2 = new Size(height, width);
            Size addExpected, subExpected;

            unchecked
            {
                addExpected = new Size(width + height, height + width);
                subExpected = new Size(width - height, height - width);
            }

            Assert.Equal(addExpected, sz1 + sz2);
            Assert.Equal(subExpected, sz1 - sz2);
            Assert.Equal(addExpected, Size.Add(sz1, sz2));
            Assert.Equal(subExpected, Size.Subtract(sz1, sz2));
        }

        [Theory]
        [InlineData(float.MaxValue, float.MinValue)]
        [InlineData(float.MinValue, float.MinValue)]
        [InlineData(float.MaxValue, float.MaxValue)]
        [InlineData(0, 0)]
        public void PointFMathematicalTest(float width, float height)
        {
            SizeF szF = new SizeF(width, height);
            Size pCeiling, pTruncate, pRound;

            unchecked
            {
                pCeiling = new Size((int)Math.Ceiling(width), (int)Math.Ceiling(height));
                pTruncate = new Size((int)width, (int)height);
                pRound = new Size((int)Math.Round(width), (int)Math.Round(height));
            }

            Assert.Equal(pCeiling, Size.Ceiling(szF));
            Assert.Equal(pRound, Size.Round(szF));
            Assert.Equal(pTruncate, Size.Truncate(szF));
        }

        [Theory]
        [InlineData(int.MaxValue, int.MinValue)]
        [InlineData(int.MinValue, int.MinValue)]
        [InlineData(int.MaxValue, int.MaxValue)]
        [InlineData(0, 0)]
        public void EqualityTest(int width, int height)
        {
            Size p1 = new Size(width, height);
            Size p2 = new Size(unchecked(width - 1), unchecked(height - 1));
            Size p3 = new Size(width, height);

            Assert.True(p1 == p3);
            Assert.True(p1 != p2);
            Assert.True(p2 != p3);

            Assert.True(p1.Equals(p3));
            Assert.False(p1.Equals(p2));
            Assert.False(p2.Equals(p3));

            Assert.True(p1.Equals((object)p3));
            Assert.False(p1.Equals((object)p2));
            Assert.False(p2.Equals((object)p3));

            Assert.Equal(p1.GetHashCode(), p3.GetHashCode());
        }

        [Fact]
        public static void EqualityTest_NotSize()
        {
            var size = new Size(0, 0);
            Assert.False(size.Equals(null));
            Assert.False(size.Equals(0));
            Assert.False(size.Equals(new SizeF(0, 0)));
        }

        [Fact]
        public static void GetHashCodeTest()
        {
            var size = new Size(10, 10);
            Assert.Equal(size.GetHashCode(), new Size(10, 10).GetHashCode());
            Assert.NotEqual(size.GetHashCode(), new Size(20, 10).GetHashCode());
            Assert.NotEqual(size.GetHashCode(), new Size(10, 20).GetHashCode());
        }

        [Fact]
        public void ToStringTest()
        {
            Size sz = new Size(0, 0);
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, "{{Width={0}, Height={1}}}", sz.Width, sz.Height), sz.ToString());
        }

        [Theory]
        [InlineData(1000, 0)]
        [InlineData(1000, 1)]
        [InlineData(1000, 2400)]
        [InlineData(1000, int.MaxValue)]
        [InlineData(1000, -1)]
        [InlineData(1000, -2400)]
        [InlineData(1000, int.MinValue)]
        [InlineData(int.MaxValue, 0)]
        [InlineData(int.MaxValue, 1)]
        [InlineData(int.MaxValue, 2400)]
        [InlineData(int.MaxValue, int.MaxValue)]
        [InlineData(int.MaxValue, -1)]
        [InlineData(int.MaxValue, -2400)]
        [InlineData(int.MaxValue, int.MinValue)]
        [InlineData(int.MinValue, 0)]
        [InlineData(int.MinValue, 1)]
        [InlineData(int.MinValue, 2400)]
        [InlineData(int.MinValue, int.MaxValue)]
        [InlineData(int.MinValue, -1)]
        [InlineData(int.MinValue, -2400)]
        [InlineData(int.MinValue, int.MinValue)]
        public void MultiplicationTestSizeInt(int dimension, int multiplier)
        {
            Size sz1 = new Size(dimension, dimension);
            Size mulExpected;

            unchecked
            {
                mulExpected = new Size(dimension * multiplier, dimension * multiplier);
            }

            Assert.Equal(mulExpected, sz1 * multiplier);
            Assert.Equal(mulExpected, multiplier * sz1);
        }

        [Theory]
        [InlineData(1000, 2000, 3000)]
        public void MultiplicationTestSizeIntWidthHeightMultiplier(int width, int height, int multiplier)
        {
            Size sz1 = new Size(width, height);
            Size mulExpected;

            unchecked
            {
                mulExpected = new Size(width * multiplier, height * multiplier);
            }

            Assert.Equal(mulExpected, sz1 * multiplier);
            Assert.Equal(mulExpected, multiplier * sz1);
        }


        [Theory]
        [InlineData(1000, 0.0f)]
        [InlineData(1000, 1.0f)]
        [InlineData(1000, 2400.933f)]
        [InlineData(1000, float.MaxValue)]
        [InlineData(1000, -1.0f)]
        [InlineData(1000, -2400.933f)]
        [InlineData(1000, float.MinValue)]
        [InlineData(int.MaxValue, 0.0f)]
        [InlineData(int.MaxValue, 1.0f)]
        [InlineData(int.MaxValue, 2400.933f)]
        [InlineData(int.MaxValue, float.MaxValue)]
        [InlineData(int.MaxValue, -1.0f)]
        [InlineData(int.MaxValue, -2400.933f)]
        [InlineData(int.MaxValue, float.MinValue)]
        [InlineData(int.MinValue, 0.0f)]
        [InlineData(int.MinValue, 1.0f)]
        [InlineData(int.MinValue, 2400.933f)]
        [InlineData(int.MinValue, float.MaxValue)]
        [InlineData(int.MinValue, -1.0f)]
        [InlineData(int.MinValue, -2400.933f)]
        [InlineData(int.MinValue, float.MinValue)]
        public void MultiplicationTestSizeFloat(int dimension, float multiplier)
        {
            Size sz1 = new Size(dimension, dimension);
            SizeF mulExpected;

            mulExpected = new SizeF(dimension * multiplier, dimension * multiplier);

            Assert.Equal(mulExpected, sz1 * multiplier);
            Assert.Equal(mulExpected, multiplier * sz1);
        }

        [Theory]
        [InlineData(1000, 2000, 30.33f)]
        public void MultiplicationTestSizeFloatWidthHeightMultiplier(int width, int height, float multiplier)
        {
            Size sz1 = new Size(width, height);
            SizeF mulExpected;

            mulExpected = new SizeF(width * multiplier, height * multiplier);

            Assert.Equal(mulExpected, sz1 * multiplier);
            Assert.Equal(mulExpected, multiplier * sz1);
        }


        [Fact]
        public void DivideByZeroChecks()
        {
            Size size = new Size(100, 100);
            Assert.Throws<DivideByZeroException>(() => size / 0);

            SizeF expectedSizeF = new SizeF(float.PositiveInfinity, float.PositiveInfinity);
            Assert.Equal(expectedSizeF, size / 0.0f);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(1, 1)]
        [InlineData(-1, 1)]
        [InlineData(1, -1)]
        [InlineData(-1, -1)]
        [InlineData(int.MaxValue, int.MaxValue)]
        [InlineData(int.MaxValue, int.MinValue)]
        [InlineData(int.MinValue, int.MaxValue)]
        [InlineData(int.MinValue, int.MinValue)]
        [InlineData(int.MaxValue, 1)]
        [InlineData(int.MinValue, 1)]
        [InlineData(int.MaxValue, -1)]
        public void DivideTestSizeInt(int dimension, int divisor)
        {
            Size size = new Size(dimension, dimension);
            Size expected;

            expected = new Size(dimension / divisor, dimension / divisor);

            Assert.Equal(expected, size / divisor);
        }

        [Theory]
        [InlineData(1111, 2222, 3333)]
        public void DivideTestSizeIntWidthHeightDivisor(int width, int height, int divisor)
        {
            Size size = new Size(width, height);
            Size expected;

            expected = new Size(width / divisor, height / divisor);

            Assert.Equal(expected, size / divisor);
        }

        [Theory]
        [InlineData(0, 1.0f)]
        [InlineData(1, 1.0f)]
        [InlineData(-1, 1.0f)]
        [InlineData(1, -1.0f)]
        [InlineData(-1, -1.0f)]
        [InlineData(int.MaxValue, float.MaxValue)]
        [InlineData(int.MaxValue, float.MinValue)]
        [InlineData(int.MinValue, float.MaxValue)]
        [InlineData(int.MinValue, float.MinValue)]
        [InlineData(int.MaxValue, 1.0f)]
        [InlineData(int.MinValue, 1.0f)]
        [InlineData(int.MaxValue, -1.0f)]
        [InlineData(int.MinValue, -1.0f)]
        public void DivideTestSizeFloat(int dimension, float divisor)
        {
            SizeF size = new SizeF(dimension, dimension);
            SizeF expected;

            expected = new SizeF(dimension / divisor, dimension / divisor);
            Assert.Equal(expected, size / divisor);
        }

        [Theory]
        [InlineData(1111, 2222, -333.33f)]
        public void DivideTestSizeFloatWidthHeightDivisor(int width, int height, float divisor)
        {
            SizeF size = new SizeF(width, height);
            SizeF expected;

            expected = new SizeF(width / divisor, height / divisor);
            Assert.Equal(expected, size / divisor);
        }
    }
}
