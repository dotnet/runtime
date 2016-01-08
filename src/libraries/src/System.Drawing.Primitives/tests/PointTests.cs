// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            Point p2 = new Point((short)(x & 0xFFFF), (short)((x >> 16) & 0xFFFF));
            
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
            Point p = new Point(x, y);
            Size s = new Size(y, x);

            Point addExpected = new Point(x + y, y + x);
            Point subExpected = new Point(x - y, y - x);

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
            Point pCeiling = new Point((int)Math.Ceiling(x), (int)Math.Ceiling(y));
            Point pTruncate = new Point((int)x, (int)y);
            Point pRound = new Point((int)Math.Round(x), (int)Math.Round(y));

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

            Assert.Equal(p2.X + p2.Y, p1.X);
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

            Assert.Equal(p1.GetHashCode(), p3.GetHashCode());
        }

        [Fact]
        public void ToStringTest()
        {
            Point p = new Point(0, 0);
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, "{{X={0},Y={1}}}", p.X, p.Y), p.ToString());
        }
    }
}
