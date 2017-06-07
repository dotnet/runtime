// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Xunit;

namespace System.Drawing.PrimitivesTest
{
    public partial class SizeFTests
    {
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
        [InlineData(1.0f, float.MinValue)]
        [InlineData(-1.0f, float.MinValue)]
        [InlineData(-1.0f, float.MinValue)]
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
