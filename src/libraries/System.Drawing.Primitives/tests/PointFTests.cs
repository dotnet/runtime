// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Numerics;
using System.Reflection;
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

            PointF p2 = new PointF(new Vector2(x, y));
            Assert.Equal(p1, p2);
        }

        [Theory]
        [InlineData(float.MaxValue, float.MinValue)]
        [InlineData(float.MinValue, float.MinValue)]
        [InlineData(float.MaxValue, float.MaxValue)]
        [InlineData(float.MinValue, float.MaxValue)]
        [InlineData(0.0, 0.0)]
        public void ToFromVector(float x, float y)
        {
            PointF point1 = new PointF(x, y);
            Vector2 vector1 = new Vector2(x, y);

            Assert.Equal(vector1, point1.ToVector2());
            Assert.Equal(vector1, (Vector2)point1);
            Assert.Equal(point1, (PointF)vector1);
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

            p.X = 10;
            Assert.Equal(10, p.X);

            p.Y = -10.123f;
            Assert.Equal(-10.123, p.Y, 3);
        }

        [Theory]
        [InlineData(float.MaxValue, float.MinValue, int.MaxValue, int.MinValue)]
        [InlineData(float.MinValue, float.MaxValue, int.MinValue, int.MaxValue)]
        [InlineData(0, 0, 0, 0)]
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
        [InlineData(0, 0)]
        public void EqualityTest(float x, float y)
        {
            PointF pLeft = new PointF(x, y);
            PointF pRight = new PointF(y, x);

            if (x == y)
            {
                Assert.True(pLeft == pRight);
                Assert.False(pLeft != pRight);
                Assert.True(pLeft.Equals(pRight));
                Assert.True(pLeft.Equals((object)pRight));
                Assert.Equal(pLeft.GetHashCode(), pRight.GetHashCode());
                return;
            }

            Assert.True(pLeft != pRight);
            Assert.False(pLeft == pRight);
            Assert.False(pLeft.Equals(pRight));
            Assert.False(pLeft.Equals((object)pRight));
        }

        [Fact]
        public static void EqualityTest_NotPointF()
        {
            var point = new PointF(0, 0);
            Assert.False(point.Equals(null));
            Assert.False(point.Equals(0));

            // If PointF implements IEquatable<PointF> (e.g. in .NET Core), then classes that are implicitly
            // convertible to PointF can potentially be equal.
            // See https://github.com/dotnet/runtime/issues/16050.
            bool expectsImplicitCastToPointF = typeof(IEquatable<PointF>).IsAssignableFrom(point.GetType());
            Assert.Equal(expectsImplicitCastToPointF, point.Equals(new Point(0, 0)));

            Assert.False(point.Equals((object)new Point(0, 0))); // No implicit cast
        }

        [Fact]
        public static void GetHashCodeTest()
        {
            var point = new PointF(10, 10);
            Assert.Equal(point.GetHashCode(), new PointF(10, 10).GetHashCode());
            Assert.NotEqual(point.GetHashCode(), new PointF(20, 10).GetHashCode());
            Assert.NotEqual(point.GetHashCode(), new PointF(10, 20).GetHashCode());
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(5.1, -5.123)]
        public void ToStringTest(float x, float y)
        {
            PointF p = new PointF(x, y);
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, "{{X={0}, Y={1}}}", p.X, p.Y), p.ToString());
        }
    }
}
