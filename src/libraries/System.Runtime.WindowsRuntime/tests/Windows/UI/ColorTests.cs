// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace Windows.UI.Tests
{
    public class ColorTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var color = new Color();
            Assert.Equal(0, color.A);
            Assert.Equal(0, color.R);
            Assert.Equal(0, color.G);
            Assert.Equal(0, color.B);
        }

        [Theory]
        [InlineData(0, 0, 0, 0)]
        [InlineData(1, 2, 3, 4)]
        [InlineData(255, 255, 255, 255)]
        public void FromArgb_Invoke_ReturnsExpectedColor(byte a, byte r, byte g, byte b)
        {
            Color color = Color.FromArgb(a, r, g, b);
            Assert.Equal(a, color.A);
            Assert.Equal(r, color.R);
            Assert.Equal(g, color.G);
            Assert.Equal(b, color.B);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { Color.FromArgb(1, 2, 3, 4), Color.FromArgb(1, 2, 3, 4), true };
            yield return new object[] { Color.FromArgb(1, 2, 3, 4), Color.FromArgb(2, 2, 3, 4), false };
            yield return new object[] { Color.FromArgb(1, 2, 3, 4), Color.FromArgb(1, 3, 3, 4), false };
            yield return new object[] { Color.FromArgb(1, 2, 3, 4), Color.FromArgb(1, 2, 4, 4), false };
            yield return new object[] { Color.FromArgb(1, 2, 3, 4), Color.FromArgb(1, 2, 3, 5), false };

            yield return new object[] { Color.FromArgb(1, 2, 3, 4), new object(), false };
            yield return new object[] { Color.FromArgb(1, 2, 3, 4), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Other_ReturnsExpected(Color color, object other, bool expected)
        {
            Assert.Equal(expected, color.Equals(other));
            if (other is Color otherColor)
            {
                Assert.Equal(expected, color == otherColor);
                Assert.Equal(!expected, color != otherColor);
                Assert.Equal(expected, color.Equals(otherColor));
                Assert.Equal(expected, color.GetHashCode().Equals(other.GetHashCode()));
            }
        }
        
        public static IEnumerable<object[]> ToString_TestData()
        {
            yield return new object[] { Color.FromArgb(1, 2, 3, 4), null, null, "#01020304" };
            yield return new object[] { Color.FromArgb(1, 2, 3, 4), null, CultureInfo.InvariantCulture, "#01020304" };

            yield return new object[] { Color.FromArgb(1, 2, 3, 4), "", CultureInfo.InvariantCulture, "sc#1, 2, 3, 4" };
            yield return new object[] { Color.FromArgb(1, 2, 3, 4), "abc", null, "sc#abc, abc, abc, abc" };
            yield return new object[] { Color.FromArgb(1, 2, 3, 4), "D4", CultureInfo.InvariantCulture, "sc#0001, 0002, 0003, 0004" };

            yield return new object[] { Color.FromArgb(1, 2, 3, 4), "", new NumberFormatInfo { NumberDecimalSeparator = "," }, "sc#1; 2; 3; 4" };
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public void ToString_Invoke_ReturnsExpected(Color color, string format, IFormatProvider formatProvider, string expected)
        {
            if (format == null)
            {
                if (formatProvider == null)
                {
                    Assert.Equal(expected, color.ToString());
                }

                Assert.Equal(expected, color.ToString(formatProvider));
            }

            Assert.Equal(expected, ((IFormattable)color).ToString(format, formatProvider));
        }
    }
}
