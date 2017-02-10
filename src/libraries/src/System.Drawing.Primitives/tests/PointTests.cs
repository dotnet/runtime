// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Xunit;

namespace System.Drawing.PrimitivesTests
{
    public class PointTests
    {
        [Fact]
        public void DefaultConstructorTest()
        {
            Assert.Equal(Point.Empty, new Point());
        }

        [Theory]
        [InlineData(int.MaxValue, int.MinValue)]
        [InlineData(int.MinValue, int.MinValue)]
        [InlineData(int.MaxValue, int.MaxValue)]
        [InlineData(0, 0)]
        public void NonDefaultConstructorTest(int x, int y)
        {
            Point p1 = new Point(x, y);
            Point p2 = new Point(new Size(x, y));

            Assert.Equal(p1, p2);
        }

        [Theory]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        [InlineData(0)]
        public void SingleIntConstructorTest(int x)
        {
            Point p1 = new Point(x);
            Point p2 = new Point(unchecked((short)(x & 0xFFFF)), unchecked((short)((x >> 16) & 0xFFFF)));

            Assert.Equal(p1, p2);
        }

        [Fact]
        public void IsEmptyDefaultsTest()
        {
            Assert.True(Point.Empty.IsEmpty);
            Assert.True(new Point().IsEmpty);
            Assert.True(new Point(0, 0).IsEmpty);
        }

        [Theory]
        [InlineData(int.MaxValue, int.MinValue)]
        [InlineData(int.MinValue, int.MinValue)]
        [InlineData(int.MaxValue, int.MaxValue)]
        public void IsEmptyRandomTest(int x, int y)
        {
            Assert.False(new Point(x, y).IsEmpty);
        }

        [Theory]
        [InlineData(int.MaxValue, int.MinValue)]
        [InlineData(int.MinValue, int.MinValue)]
        [InlineData(int.MaxValue, int.MaxValue)]
        [InlineData(0, 0)]
        public void CoordinatesTest(int x, int y)
        {
            Point p = new Point(x, y);
            Assert.Equal(x, p.X);
            Assert.Equal(y, p.Y);
        }

        [Theory]
        [InlineData(int.MaxValue, int.MinValue)]
        [InlineData(int.MinValue, int.MinValue)]
        [InlineData(int.MaxValue, int.MaxValue)]
        [InlineData(0, 0)]
        public void PointFConversionTest(int x, int y)
        {
            PointF p = new Point(x, y);
            Assert.Equal(new PointF(x, y), p);
        }

        [Theory]
        [InlineData(int.MaxValue, int.MinValue)]
        [InlineData(int.MinValue, int.MinValue)]
        [InlineData(int.MaxValue, int.MaxValue)]
        [InlineData(0, 0)]
        public void SizeConversionTest(int x, int y)
        {
            Size sz = (Size)new Point(x, y);
            Assert.Equal(new Size(x, y), sz);
        }

        [Theory]
        [InlineData(int.MaxValue, int.MinValue)]
        [InlineData(int.MinValue, int.MinValue)]
        [InlineData(int.MaxValue, int.MaxValue)]
        [InlineData(0, 0)]
        public void ArithmeticTest(int x, int y)
        {
            Point addExpected, subExpected, p = new Point(x, y);
            Size s = new Size(y, x);

            unchecked
            {
                addExpected = new Point(x + y, y + x);
                subExpected = new Point(x - y, y - x);
            }

            Assert.Equal(addExpected, p + s);
            Assert.Equal(subExpected, p - s);
            Assert.Equal(addExpected, Point.Add(p, s));
            Assert.Equal(subExpected, Point.Subtract(p, s));
        }

        [Theory]
        [InlineData(float.MaxValue, float.MinValue)]
        [InlineData(float.MinValue, float.MinValue)]
        [InlineData(float.MaxValue, float.MaxValue)]
        [InlineData(0, 0)]
        public void PointFMathematicalTest(float x, float y)
        {
            PointF pf = new PointF(x, y);
            Point pCeiling, pTruncate, pRound;

            unchecked
            {
                pCeiling = new Point((int)Math.Ceiling(x), (int)Math.Ceiling(y));
                pTruncate = new Point((int)x, (int)y);
                pRound = new Point((int)Math.Round(x), (int)Math.Round(y));
            }

            Assert.Equal(pCeiling, Point.Ceiling(pf));
            Assert.Equal(pRound, Point.Round(pf));
            Assert.Equal(pTruncate, Point.Truncate(pf));
        }

        [Theory]
        [InlineData(int.MaxValue, int.MinValue)]
        [InlineData(int.MinValue, int.MinValue)]
        [InlineData(int.MaxValue, int.MaxValue)]
        [InlineData(0, 0)]
        public void OffsetTest(int x, int y)
        {
            Point p1 = new Point(x, y);
            Point p2 = new Point(y, x);

            p1.Offset(p2);

            Assert.Equal(unchecked(p2.X + p2.Y), p1.X);
            Assert.Equal(p1.X, p1.Y);

            p2.Offset(x, y);
            Assert.Equal(p1, p2);
        }

        [Theory]
        [InlineData(int.MaxValue, int.MinValue)]
        [InlineData(int.MinValue, int.MinValue)]
        [InlineData(int.MaxValue, int.MaxValue)]
        [InlineData(0, 0)]
        public void EqualityTest(int x, int y)
        {
            Point p1 = new Point(x, y);
            Point p2 = new Point(x / 2 - 1, y / 2 - 1);
            Point p3 = new Point(x, y);

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
        public static void EqualityTest_NotPoint()
        {
            var point = new Point(0, 0);
            Assert.False(point.Equals(null));
            Assert.False(point.Equals(0));
            Assert.False(point.Equals(new PointF(0, 0)));
        }

        [Fact]
        public static void GetHashCodeTest()
        {
            var point = new Point(10, 10);
            Assert.Equal(point.GetHashCode(), new Point(10, 10).GetHashCode());
            Assert.NotEqual(point.GetHashCode(), new Point(20, 10).GetHashCode());
            Assert.NotEqual(point.GetHashCode(), new Point(10, 20).GetHashCode());
        }

        [Theory]
        [InlineData(0, 0, 0, 0)]
        [InlineData(1, -2, 3, -4)]
        public void ConversionTest(int x, int y, int width, int height)
        {
            Rectangle rect = new Rectangle(x, y, width, height);
            RectangleF rectF = rect;
            Assert.Equal(x, rectF.X);
            Assert.Equal(y, rectF.Y);
            Assert.Equal(width, rectF.Width);
            Assert.Equal(height, rectF.Height);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(5, -5)]
        public void ToStringTest(int x, int y)
        {
            Point p = new Point(x, y);
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, "{{X={0},Y={1}}}", p.X, p.Y), p.ToString());
        }
    }
}
