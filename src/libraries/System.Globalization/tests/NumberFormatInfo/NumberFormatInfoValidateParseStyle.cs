// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoValidateParseStyle
    {
        [Theory]
        [InlineData(unchecked((NumberStyles)0xFFFFFC00), false, "style")]
        [InlineData(NumberStyles.HexNumber | NumberStyles.Integer, false, null)]
        [InlineData(NumberStyles.AllowHexSpecifier, true, null)]
        [InlineData(NumberStyles.None, true, null)]
        public void ValidateParseStyle_Integer(NumberStyles style, bool valid, string paramName)
        {
            if (!valid)
            {
                AssertExtensions.Throws<ArgumentException>(paramName, () => byte.Parse("0", style));
            }
            else
            {
                byte.Parse("0", style); // Should not throw
            }
        }

        [Theory]
        [InlineData(unchecked((NumberStyles)0xFFFFFC00), false, "style")]
        [InlineData(NumberStyles.HexNumber | NumberStyles.Integer, false, null)]
        [InlineData(NumberStyles.AllowHexSpecifier, false, null)]
        [InlineData(NumberStyles.None, true, null)]
        public void ValidateParseStyle_Float(NumberStyles style, bool valid, string paramName)
        {
            if (!valid)
            {
                AssertExtensions.Throws<ArgumentException>(paramName, () => float.Parse("0", style));
            }
            else
            {
                float.Parse("0", style); // Should not throw
            }
        }
    }
}
