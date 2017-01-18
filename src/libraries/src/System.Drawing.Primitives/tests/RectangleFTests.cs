// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        [InlineData(0, 0)]
        [InlineData(float.MaxValue, float.MinValue)]
        public static void LocationSetTest(float x, float y)
        {
            var point = new PointF(x, y);
            var rect = new RectangleF(10, 10, 10, 10);
            rect.Location = point;
            Assert.Equal(point, rect.Location);
            Assert.Equal(point.X, rect.X);
            Assert.Equal(point.Y, rect.Y);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(float.MaxValue, float.MinValue)]
        public static void SizeSetTest(float x, float y)
        {
            var size = new SizeF(x, y);
            var rect = new RectangleF(10, 10, 10, 10);
            rect.Size = size;
            Assert.Equal(size, rect.Size);
            Assert.Equal(size.Width, rect.Width);
            Assert.Equal(size.Height, rect.Height);
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

        [Fact]
        public static void EqualityTest_NotRectangleF()
        {
            var rectangle = new RectangleF(0, 0, 0, 0);
            Assert.False(rectangle.Equals(null));
            Assert.False(rectangle.Equals(0));
            Assert.False(rectangle.Equals(new Rectangle(0, 0, 0, 0)));
        }

        [Fact]
        public static void GetHashCodeTest()
        {
            var rect1 = new RectangleF(10, 10, 10, 10);
            var rect2 = new RectangleF(10, 10, 10, 10);
            Assert.Equal(rect1.GetHashCode(), rect2.GetHashCode());
            Assert.NotEqual(rect1.GetHashCode(), new RectangleF(20, 10, 10, 10).GetHashCode());
            Assert.NotEqual(rect1.GetHashCode(), new RectangleF(10, 20, 10, 10).GetHashCode());
            Assert.NotEqual(rect1.GetHashCode(), new RectangleF(10, 10, 20, 10).GetHashCode());
            Assert.NotEqual(rect1.GetHashCode(), new RectangleF(10, 10, 10, 20).GetHashCode());
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
        [InlineData(float.MaxValue / 2, float.MinValue / 2, float.MinValue / 2, float.MaxValue / 2)]
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
        [InlineData(float.MaxValue, float.MinValue, float.MaxValue / 2, float.MinValue / 2)]
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

        [Fact]
        public static void Intersect_IntersectingRects_Test()
        {
            var rect1 = new RectangleF(0, 0, 5, 5);
            var rect2 = new RectangleF(1, 1, 3, 3);
            var expected = new RectangleF(1, 1, 3, 3);

            Assert.Equal(expected, RectangleF.Intersect(rect1, rect2));
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

        [Theory]
        [InlineData(0, 0, 0, 0)]
        [InlineData(5, -5, 0.2, -1.3)]
        public void ToStringTest(float x, float y, float width, float height)
        {
            var r = new RectangleF(x, y, width, height);
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, "{{X={0},Y={1},Width={2},Height={3}}}", r.X, r.Y, r.Width, r.Height), r.ToString());
        }
    }
}
