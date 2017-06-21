// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace Windows.Foundation.Tests
{
    public class SizeTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var size = new Size();
            Assert.Equal(0, size.Width);
            Assert.Equal(0, size.Height);

            Assert.False(size.IsEmpty);
        }

        [Theory]
        [InlineData(0, 0, 0, 0)]
        [InlineData(1, 2, 1, 2)]
        [InlineData(double.NaN, double.NaN, double.NaN, double.NaN)]
        [InlineData(double.MaxValue, double.MaxValue, double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity)]
        public void Ctor_Width_Height(double width, double height, double expectedWidth, double expectedHeight)
        {
            var size = new Size(width, height);
            Assert.Equal(expectedWidth, size.Width);
            Assert.Equal(expectedHeight, size.Height);

            Assert.False(size.IsEmpty);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(double.NegativeInfinity)]
        public void Ctor_NegativeWidth_ThrowsArgumentOutOfRangeException(double width)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("width", () => new Size(width, 1));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(double.NegativeInfinity)]
        public void Ctor_NegativeHeight_ThrowsArgumentOutOfRangeException(double height)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("height", () => new Size(1, height));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.NaN)]
        public void Width_SetValid_GetReturnsExpected(double width)
        {
            var size = new Size { Width = width };
            Assert.Equal(width, size.Width);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(double.NegativeInfinity)]
        public void Width_SetNegative_ThrowsArgumentOutOfRangeException(double width)
        {
            var size = new Size();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("Width", () => size.Width = width);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.NaN)]
        public void Height_SetValid_GetReturnsExpected(double height)
        {
            var size = new Size { Height = height };
            Assert.Equal(height, size.Height);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(double.NegativeInfinity)]
        public void Height_SetNegative_ThrowsArgumentOutOfRangeException(double height)
        {
            var size = new Size();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("Height", () => size.Height = height);
        }

        [Fact]
        public void Empty_Get_ReturnsExpected()
        {
            Size size = Size.Empty;
            Assert.Equal(double.NegativeInfinity, size.Width);
            Assert.Equal(double.NegativeInfinity, size.Height);
            Assert.True(size.IsEmpty);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { new Size(1, 2), new Size(1, 2), true };
            yield return new object[] { new Size(1, 2), new Size(2, 2), false };
            yield return new object[] { new Size(1, 2), new Size(1, 3), false };
            yield return new object[] { new Size(1, 2), Size.Empty, false };

            yield return new object[] { Size.Empty, Size.Empty, true };
            yield return new object[] { Size.Empty, new Size(1, 2), false };

            yield return new object[] { new Size(1, 2), new object(), false };
            yield return new object[] { new Size(1, 2), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Other_ReturnsExpected(Size size, object other, bool expected)
        {
            Assert.Equal(expected, size.Equals(other));
            if (other is Size otherSize)
            {
                Assert.Equal(expected, size == otherSize);
                Assert.Equal(!expected, size != otherSize);
                Assert.Equal(expected, size.Equals(otherSize));
                Assert.Equal(expected, size.GetHashCode().Equals(other.GetHashCode()));
            }
        }
        
        public static IEnumerable<object[]> ToString_TestData()
        {
            yield return new object[] { Size.Empty, "Empty" };
            yield return new object[] { new Size(0, 0), "0,0" };
            yield return new object[] { new Size(1, 2), "1,2" };
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public void ToString_Invoke_ReturnsExpected(Size size, string expected)
        {
            Assert.Equal(expected, size.ToString());
        }
    }
}
