// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Xunit;

namespace System.Drawing.PrimitivesTests
{
    public class PointFTests
    {
        [Fact]
        public void DefaultConstructorTest()
        {
            Assert.Equal(PointF.Empty, new PointF());
        }

        [Theory]
        [InlineData(float.MaxValue, float.MinValue)]
        [InlineData(float.MinValue, float.MinValue)]
        [InlineData(float.MaxValue, float.MaxValue)]
        [InlineData(float.MinValue, float.MaxValue)]
        [InlineData(0.0, 0.0)]
        public void NonDefaultConstructorTest(float x, float y)
        {
            PointF p1 = new PointF(x, y);

            Assert.Equal(x, p1.X);
            Assert.Equal(y, p1.Y);
        }

        [Fact]
        public void IsEmptyDefaultsTest()
        {
            Assert.True(PointF.Empty.IsEmpty);
            Assert.True(new PointF().IsEmpty);
            Assert.True(new PointF(0, 0).IsEmpty);
        }

        [Theory]
        [InlineData(float.MaxValue, float.MinValue)]
        [InlineData(float.MinValue, float.MinValue)]
        [InlineData(float.MaxValue, float.MaxValue)]
        public void IsEmptyRandomTest(float x, float y)
        {
            Assert.False(new PointF(x, y).IsEmpty);
        }

        [Theory]
        [InlineData(float.MaxValue, float.MinValue)]
        [InlineData(float.MinValue, float.MinValue)]
        [InlineData(float.MaxValue, float.MaxValue)]
        [InlineData(0, 0)]
        public void CoordinatesTest(float x, float y)
        {
            PointF p = new PointF(x, y);
            Assert.Equal(x, p.X);
            Assert.Equal(y, p.Y);
        }

        [Theory]
        [InlineData(float.MaxValue, float.MinValue, int.MaxValue, int.MinValue)]
        [InlineData(float.MinValue, float.MaxValue, int.MinValue, int.MaxValue)]
        [InlineData(0,0,0,0)]
        public void ArithmeticTestWithSize(float x, float y, int x1, int y1)
        {
            PointF p = new PointF(x, y);
            Size s = new Size(x1, y1);

            PointF addExpected = new PointF(x + x1, y + y1);
            PointF subExpected = new PointF(x - x1, y - y1);
            Assert.Equal(addExpected, p + s);
            Assert.Equal(subExpected, p - s);
            Assert.Equal(addExpected, PointF.Add(p, s));
            Assert.Equal(subExpected, PointF.Subtract(p, s));
        }

        [Theory]
        [InlineData(float.MaxValue, float.MinValue)]
        [InlineData(float.MinValue, float.MaxValue)]
        [InlineData(0, 0)]
        public void ArithmeticTestWithSizeF(float x, float y)
        {
            PointF p = new PointF(x, y);
            SizeF s = new SizeF(y, x);

            PointF addExpected = new PointF(x + y, y + x);
            PointF subExpected = new PointF(x - y, y - x);
            Assert.Equal(addExpected, p + s);
            Assert.Equal(subExpected, p - s);
            Assert.Equal(addExpected, PointF.Add(p, s));
            Assert.Equal(subExpected, PointF.Subtract(p, s));
        }

        [Theory]
        [InlineData(float.MaxValue, float.MinValue)]
        [InlineData(float.MinValue, float.MaxValue)]
        [InlineData(float.MinValue, float.MinValue)]
        [InlineData(float.MaxValue, float.MaxValue)]
        [InlineData(0,0)]
        public void EqualityTest(float x, float y)
        {
            PointF pLeft = new PointF(x, y);
            PointF pRight = new PointF(y, x);

            if (x == y)
            {
                Assert.True(pLeft == pRight);
                Assert.False(pLeft != pRight);
                Assert.True(pLeft.Equals(pRight));
                Assert.Equal(pLeft.GetHashCode(), pRight.GetHashCode());
                return;
            }

            Assert.True(pLeft != pRight);
            Assert.False(pLeft == pRight);
            Assert.False(pLeft.Equals(pRight));
        }

        [Fact]
        public void ToStringTest()
        {
            PointF p = new PointF(0, 0);
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, "{{X={0}, Y={1}}}", p.X, p.Y), p.ToString());
        }
    }
}
