// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Numerics;
using Xunit;

namespace System.Drawing.PrimitivesTest
{
    public class SizeFTests
    {
        [Fact]
        public void DefaultConstructorTest()
        {
            Assert.Equal(SizeF.Empty, new SizeF());
        }

        [Theory]
        [InlineData(float.MaxValue, float.MinValue)]
        [InlineData(float.MinValue, float.MinValue)]
        [InlineData(float.MaxValue, float.MaxValue)]
        [InlineData(0, 0)]
        public void NonDefaultConstructorAndDimensionsTest(float width, float height)
        {
            SizeF s1 = new SizeF(width, height);
            PointF p1 = new PointF(width, height);
            SizeF s2 = new SizeF(s1);

            Assert.Equal(s1, s2);
            Assert.Equal(s1, new SizeF(p1));
            Assert.Equal(s2, new SizeF(p1));
            Assert.Equal(s1, new SizeF(new Vector2(width, height)));

            Assert.Equal(width, s1.Width);
            Assert.Equal(height, s1.Height);

            s1.Width = 10;
            Assert.Equal(10, s1.Width);

            s1.Height = -10.123f;
            Assert.Equal(-10.123, s1.Height, 3);
        }

        [Theory]
        [InlineData(float.MaxValue, float.MinValue)]
        [InlineData(float.MinValue, float.MinValue)]
        [InlineData(float.MaxValue, float.MaxValue)]
        [InlineData(float.MinValue, float.MaxValue)]
        [InlineData(0.0, 0.0)]
        public void ToFromVector(float x, float y)
        {
            SizeF size1 = new SizeF(x, y);
            Vector2 vector1 = new Vector2(x, y);

            Assert.Equal(vector1, size1.ToVector2());
            Assert.Equal(vector1, (Vector2)size1);
            Assert.Equal(size1, (SizeF)vector1);
        }

        [Fact]
        public void IsEmptyDefaultsTest()
        {
            Assert.True(SizeF.Empty.IsEmpty);
            Assert.True(new SizeF().IsEmpty);
            Assert.True(new SizeF(0, 0).IsEmpty);
        }

        [Theory]
        [InlineData(float.MaxValue, float.MinValue)]
        [InlineData(float.MinValue, float.MinValue)]
        [InlineData(float.MaxValue, float.MaxValue)]
        public void IsEmptyRandomTest(float width, float height)
        {
            Assert.False(new SizeF(width, height).IsEmpty);
        }

        [Theory]
        [InlineData(float.MaxValue, float.MinValue)]
        [InlineData(float.MinValue, float.MinValue)]
        [InlineData(float.MaxValue, float.MaxValue)]
        [InlineData(0, 0)]
        public void ArithmeticTest(float width, float height)
        {
            SizeF s1 = new SizeF(width, height);
            SizeF s2 = new SizeF(height, width);
            SizeF addExpected = new SizeF(width + height, width + height);
            SizeF subExpected = new SizeF(width - height, height - width);

            Assert.Equal(addExpected, s1 + s2);
            Assert.Equal(addExpected, SizeF.Add(s1, s2));

            Assert.Equal(subExpected, s1 - s2);
            Assert.Equal(subExpected, SizeF.Subtract(s1, s2));
        }

        [Theory]
        [InlineData(float.MaxValue, float.MinValue)]
        [InlineData(float.MinValue, float.MinValue)]
        [InlineData(float.MaxValue, float.MaxValue)]
        [InlineData(0, 0)]
        public void EqualityTest(float width, float height)
        {
            SizeF sLeft = new SizeF(width, height);
            SizeF sRight = new SizeF(height, width);

            if (width == height)
            {
                Assert.True(sLeft == sRight);
                Assert.False(sLeft != sRight);
                Assert.True(sLeft.Equals(sRight));
                Assert.True(sLeft.Equals((object)sRight));
                Assert.Equal(sLeft.GetHashCode(), sRight.GetHashCode());
                return;
            }

            Assert.True(sLeft != sRight);
            Assert.False(sLeft == sRight);
            Assert.False(sLeft.Equals(sRight));
            Assert.False(sLeft.Equals((object)sRight));
        }

        [Fact]
        public static void EqualityTest_NotSizeF()
        {
            var size = new SizeF(0, 0);
            Assert.False(size.Equals(null));
            Assert.False(size.Equals(0));

            // If SizeF implements IEquatable<SizeF> (e.g in .NET Core), then classes that are implicitly
            // convertible to SizeF can potentially be equal.
            // See https://github.com/dotnet/runtime/issues/16050.
            bool expectsImplicitCastToSizeF = typeof(IEquatable<SizeF>).IsAssignableFrom(size.GetType());
            Assert.Equal(expectsImplicitCastToSizeF, size.Equals(new Size(0, 0)));

            Assert.False(size.Equals((object)new Size(0, 0))); // No implicit cast
        }

        [Fact]
        public static void GetHashCodeTest()
        {
            var size = new SizeF(10, 10);
            Assert.Equal(size.GetHashCode(), new SizeF(10, 10).GetHashCode());
            Assert.NotEqual(size.GetHashCode(), new SizeF(20, 10).GetHashCode());
            Assert.NotEqual(size.GetHashCode(), new SizeF(10, 20).GetHashCode());
        }

        [Theory]
        [InlineData(float.MaxValue, float.MinValue)]
        [InlineData(float.MinValue, float.MinValue)]
        [InlineData(float.MaxValue, float.MaxValue)]
        [InlineData(0, 0)]
        public void ConversionTest(float width, float height)
        {
            SizeF s1 = new SizeF(width, height);
            PointF p1 = (PointF)s1;
            Size s2 = new Size(unchecked((int)width), unchecked((int)height));

            Assert.Equal(new PointF(width, height), p1);
            Assert.Equal(p1, s1.ToPointF());
            Assert.Equal(s2, s1.ToSize());
        }

        [Fact]
        public void ToStringTest()
        {
            SizeF s = new SizeF(0, 0);
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, "{{Width={0}, Height={1}}}", s.Width, s.Height), s.ToString());
        }

        [Theory]
        [InlineData(1000.234f, 0.0f)]
        [InlineData(1000.234f, 1.0f)]
        [InlineData(1000.234f, 2400.933f)]
        [InlineData(1000.234f, float.MaxValue)]
        [InlineData(1000.234f, -1.0f)]
        [InlineData(1000.234f, -2400.933f)]
        [InlineData(1000.234f, float.MinValue)]
        [InlineData(float.MaxValue, 0.0f)]
        [InlineData(float.MaxValue, 1.0f)]
        [InlineData(float.MaxValue, 2400.933f)]
        [InlineData(float.MaxValue, float.MaxValue)]
        [InlineData(float.MaxValue, -1.0f)]
        [InlineData(float.MaxValue, -2400.933f)]
        [InlineData(float.MaxValue, float.MinValue)]
        [InlineData(float.MinValue, 0.0f)]
        [InlineData(float.MinValue, 1.0f)]
        [InlineData(float.MinValue, 2400.933f)]
        [InlineData(float.MinValue, float.MaxValue)]
        [InlineData(float.MinValue, -1.0f)]
        [InlineData(float.MinValue, -2400.933f)]
        [InlineData(float.MinValue, float.MinValue)]
        public void MultiplicationTest(float dimension, float multiplier)
        {
            SizeF sz1 = new SizeF(dimension, dimension);
            SizeF mulExpected;

            mulExpected = new SizeF(dimension * multiplier, dimension * multiplier);

            Assert.Equal(mulExpected, sz1 * multiplier);
            Assert.Equal(mulExpected, multiplier * sz1);
        }

        [Theory]
        [InlineData(1111.1111f, 2222.2222f, 3333.3333f)]
        public void MultiplicationTestWidthHeightMultiplier(float width, float height, float multiplier)
        {
            SizeF sz1 = new SizeF(width, height);
            SizeF mulExpected;

            mulExpected = new SizeF(width * multiplier, height * multiplier);

            Assert.Equal(mulExpected, sz1 * multiplier);
            Assert.Equal(mulExpected, multiplier * sz1);
        }

        [Theory]
        [InlineData(0.0f, 1.0f)]
        [InlineData(1.0f, 1.0f)]
        [InlineData(-1.0f, 1.0f)]
        [InlineData(1.0f, -1.0f)]
        [InlineData(-1.0f, -1.0f)]
        [InlineData(float.MaxValue, float.MaxValue)]
        [InlineData(float.MaxValue, float.MinValue)]
        [InlineData(float.MinValue, float.MaxValue)]
        [InlineData(float.MinValue, float.MinValue)]
        [InlineData(float.MaxValue, 1.0f)]
        [InlineData(float.MinValue, 1.0f)]
        [InlineData(float.MaxValue, -1.0f)]
        [InlineData(float.MinValue, -1.0f)]
        [InlineData(float.MinValue, 0.0f)]
        [InlineData(1.0f, float.MinValue)]
        [InlineData(1.0f, float.MaxValue)]
        [InlineData(-1.0f, float.MinValue)]
        [InlineData(-1.0f, float.MaxValue)]
        public void DivideTestSizeFloat(float dimension, float divisor)
        {
            SizeF size = new SizeF(dimension, dimension);
            SizeF expected = new SizeF(dimension / divisor, dimension / divisor);
            Assert.Equal(expected, size / divisor);
        }

        [Theory]
        [InlineData(-111.111f, 222.222f, 333.333f)]
        public void DivideTestSizeFloatWidthHeightDivisor(float width, float height, float divisor)
        {
            SizeF size = new SizeF(width, height);
            SizeF expected = new SizeF(width / divisor, height / divisor);
            Assert.Equal(expected, size / divisor);
        }
    }
}
