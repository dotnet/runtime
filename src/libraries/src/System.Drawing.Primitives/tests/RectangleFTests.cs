// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Xunit;

namespace System.Drawing.PrimitivesTest
{
    public class RectangleFTests
    {
        [Fact]
        public void DefaultConstructorTest()
        {
            Assert.Equal(RectangleF.Empty, new RectangleF());
        }

        [Theory]
        [InlineData(0, 0, 0, 0)]
        [InlineData(float.MaxValue, float.MinValue, float.MinValue, float.MaxValue)]
        [InlineData(float.MaxValue, 0, 0, float.MaxValue)]
        [InlineData(0, float.MinValue, float.MaxValue, 0)]
        public void NonDefaultConstructorTest(float x, float y, float width, float height)
        {
            RectangleF rect1 = new RectangleF(x, y, width, height);
            PointF p = new PointF(x, y);
            SizeF s = new SizeF(width, height);
            RectangleF rect2 = new RectangleF(p, s);

            Assert.Equal(rect1, rect2);
        }

        [Theory]
        [InlineData(0, 0, 0, 0)]
        [InlineData(float.MaxValue, float.MinValue, float.MinValue, float.MaxValue)]
        [InlineData(float.MaxValue, 0, 0, float.MaxValue)]
        [InlineData(0, float.MinValue, float.MaxValue, 0)]
        public void FromLTRBTest(float left, float top, float right, float bottom)
        {
            RectangleF expected = new RectangleF(left, top, right - left, bottom - top);
            RectangleF actual = RectangleF.FromLTRB(left, top, right, bottom);

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(0, 0, 0, 0)]
        [InlineData(float.MaxValue, float.MinValue, float.MinValue, float.MaxValue)]
        [InlineData(float.MaxValue, 0, 0, float.MaxValue)]
        [InlineData(0, float.MinValue, float.MaxValue, 0)]
        public void DimensionsTest(float x, float y, float width, float height)
        {
            RectangleF rect = new RectangleF(x, y, width, height);
            PointF p = new PointF(x, y);
            SizeF s = new SizeF(width, height);

            Assert.Equal(p, rect.Location);
            Assert.Equal(s, rect.Size);
            Assert.Equal(x, rect.X);
            Assert.Equal(y, rect.Y);
            Assert.Equal(width, rect.Width);
            Assert.Equal(height, rect.Height);
            Assert.Equal(x, rect.Left);
            Assert.Equal(y, rect.Top);
            Assert.Equal(x + width, rect.Right);
            Assert.Equal(y + height, rect.Bottom);
        }

        [Fact]
        public void IsEmptyTest()
        {
            Assert.True(RectangleF.Empty.IsEmpty);
            Assert.True(new RectangleF().IsEmpty);
            Assert.True(new RectangleF(1, -2, -10, 10).IsEmpty);
            Assert.True(new RectangleF(1, -2, 10, -10).IsEmpty);
            Assert.True(new RectangleF(1, -2, 0, 0).IsEmpty);

            Assert.False(new RectangleF(0, 0, 10, 10).IsEmpty);
        }

        [Theory]
        [InlineData(float.MaxValue, float.MinValue, float.MinValue, float.MaxValue)]
        [InlineData(float.MaxValue, 0, 0, float.MaxValue)]
        [InlineData(0, float.MinValue, float.MaxValue, 0)]
        public void EqualityTest(float x, float y, float width, float height)
        {
            RectangleF rect1 = new RectangleF(x, y, width, height);
            RectangleF rect2 = new RectangleF(width, height, x, y);

            Assert.True(rect1 != rect2);
            Assert.False(rect1 == rect2);
            Assert.False(rect1.Equals(rect2));
        }

        [Theory]
        [InlineData(float.MaxValue, float.MinValue, float.MinValue, float.MaxValue)]
        [InlineData(0, float.MinValue, float.MaxValue, 0)]
        public void ContainsTest(float x, float y, float width, float height)
        {
            RectangleF rect = new RectangleF(x, y, width, height);
            float X = (x + width) / 2;
            float Y = (y + height) / 2;
            PointF p = new PointF(X, Y);
            RectangleF r = new RectangleF(X, Y, width / 2, height / 2);

            Assert.False(rect.Contains(X, Y));
            Assert.False(rect.Contains(p));
            Assert.False(rect.Contains(r));
        }

        [Theory]
        [InlineData(0, 0, 0, 0)]
        [InlineData(float.MaxValue/2, float.MinValue/2, float.MinValue/2, float.MaxValue/2)]
        [InlineData(0, float.MinValue, float.MaxValue, 0)]
        public void InflateTest(float x, float y, float width, float height)
        {
            RectangleF rect = new RectangleF(x, y, width, height);
            RectangleF inflatedRect = new RectangleF(x - width, y - height, width + 2 * width, height + 2 * height);

            rect.Inflate(width, height);
            Assert.Equal(inflatedRect, rect);

            SizeF s = new SizeF(x, y);
            inflatedRect = RectangleF.Inflate(rect, x, y);

            rect.Inflate(s);
            Assert.Equal(inflatedRect, rect);
        }

        [Theory]
        [InlineData(float.MaxValue, float.MinValue, float.MaxValue/2, float.MinValue/2)]
        [InlineData(0, float.MinValue, float.MaxValue, 0)]
        public void IntersectTest(float x, float y, float width, float height)
        {
            RectangleF rect1 = new RectangleF(x, y, width, height);
            RectangleF rect2 = new RectangleF(y, x, width, height);
            RectangleF expectedRect = RectangleF.Intersect(rect1, rect2);
            rect1.Intersect(rect2);
            Assert.Equal(expectedRect, rect1);
            Assert.False(rect1.IntersectsWith(expectedRect));
        }

        [Theory]
        [InlineData(0, 0, 0, 0)]
        [InlineData(float.MaxValue, float.MinValue, float.MinValue, float.MaxValue)]
        [InlineData(float.MaxValue, 0, 0, float.MaxValue)]
        [InlineData(0, float.MinValue, float.MaxValue, 0)]
        public void UnionTest(float x, float y, float width, float height)
        {
            RectangleF a = new RectangleF(x, y, width, height);
            RectangleF b = new RectangleF(width, height, x, y);

            float x1 = Math.Min(a.X, b.X);
            float x2 = Math.Max(a.X + a.Width, b.X + b.Width);
            float y1 = Math.Min(a.Y, b.Y);
            float y2 = Math.Max(a.Y + a.Height, b.Y + b.Height);

            RectangleF expectedRectangle = new RectangleF(x1, y1, x2 - x1, y2 - y1);

            Assert.Equal(expectedRectangle, RectangleF.Union(a, b));
        }

        [Theory]
        [InlineData(0, 0, 0, 0)]
        [InlineData(float.MaxValue, float.MinValue, float.MinValue, float.MaxValue)]
        [InlineData(float.MaxValue, 0, 0, float.MaxValue)]
        [InlineData(0, float.MinValue, float.MaxValue, 0)]
        public void OffsetTest(float x, float y, float width, float height)
        {
            RectangleF r1 = new RectangleF(x, y, width, height);
            RectangleF expectedRect = new RectangleF(x + width, y + height, width, height);
            PointF p = new PointF(width, height);

            r1.Offset(p);
            Assert.Equal(expectedRect, r1);

            expectedRect.Offset(p);
            r1.Offset(width, height);
            Assert.Equal(expectedRect, r1);
        }

        [Fact]
        public void ToStringTest()
        {
            string expected = "{X=0,Y=0,Width=0,Height=0}";
            Assert.Equal(expected, RectangleF.Empty.ToString());
        }
    }
}
