// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Xunit;

namespace System.Drawing.PrimitivesTests
{
    public partial class SizeTests
    {
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