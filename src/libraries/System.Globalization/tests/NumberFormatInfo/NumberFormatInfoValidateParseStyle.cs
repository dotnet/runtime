// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
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
                Assert.Throws<ArgumentException>(() => byte.Parse("0", style));
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
        [InlineData(NumberStyles.None, true)]
        public void ValidateParseStyle_Float(NumberStyles style, bool valid)
        {
            if (!valid)
            {
                Assert.Throws<ArgumentException>(() => float.Parse("0", style));
            }
            else
            {
                float.Parse("0", style); // Should not throw
            }
        }
    }
}
