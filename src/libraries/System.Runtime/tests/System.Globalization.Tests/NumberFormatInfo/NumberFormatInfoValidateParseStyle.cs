// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoValidateParseStyle
    {
        [Theory]
        [InlineData(unchecked((NumberStyles)0xFFFFFC00), false)]
        [InlineData(NumberStyles.HexNumber | NumberStyles.Integer, false)]
        [InlineData(NumberStyles.AllowHexSpecifier, true)]
        [InlineData(NumberStyles.None, true)]
        public void ValidateParseStyle_Integer(NumberStyles style, bool valid)
        {
            if (!valid)
            {
                AssertExtensions.Throws<ArgumentException>("style", () => byte.Parse("0", style));
            }
            else
            {
                byte.Parse("0", style); // Should not throw
            }
        }

        [Theory]
        [InlineData(unchecked((NumberStyles)0xFFFFFC00), false)]
        [InlineData(NumberStyles.HexNumber | NumberStyles.Integer, false)]
        [InlineData(NumberStyles.AllowHexSpecifier, false)]
        [InlineData(NumberStyles.HexFloat, true)]
        [InlineData(NumberStyles.AllowBinarySpecifier, false)]
        [InlineData(NumberStyles.AllowHexSpecifier | NumberStyles.AllowExponent, true)]
        [InlineData(NumberStyles.None, true)]
        public void ValidateParseStyle_Float(NumberStyles style, bool valid)
        {
            // Use a value that's valid for hex float styles (which require "0x" prefix and "p" exponent).
            string value = (style & NumberStyles.AllowHexSpecifier) != 0 ? "0x0p0" : "0";
            if (!valid)
            {
                AssertExtensions.Throws<ArgumentException>("style", () => float.Parse(value, style));
            }
            else
            {
                float.Parse(value, style); // Should not throw
            }
        }

        [Theory]
        [InlineData(unchecked((NumberStyles)0xFFFFFC00), false)]
        [InlineData(NumberStyles.HexNumber | NumberStyles.Integer, false)]
        [InlineData(NumberStyles.AllowHexSpecifier, false)]
        [InlineData(NumberStyles.HexFloat, false)]
        [InlineData(NumberStyles.Float, true)]
        [InlineData(NumberStyles.AllowBinarySpecifier, false)]
        [InlineData(NumberStyles.AllowHexSpecifier | NumberStyles.AllowExponent, false)]
        [InlineData(NumberStyles.None, true)]
        public void ValidateParseStyle_Decimal(NumberStyles style, bool valid)
        {
            if (!valid)
            {
                AssertExtensions.Throws<ArgumentException>("style", () => decimal.Parse("0", style));
                AssertExtensions.Throws<ArgumentException>("style", () => decimal.Parse("0"u8, style));
            }
            else
            {
                decimal.Parse("0", style); // Should not throw
                decimal.Parse("0"u8, style); // Should not throw
            }
        }
    }
}
