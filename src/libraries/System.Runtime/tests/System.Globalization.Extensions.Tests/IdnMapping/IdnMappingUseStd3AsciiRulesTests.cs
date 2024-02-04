// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Globalization.Tests
{
    /// <summary>
    /// According to the ToASCII algorithm, if the UseSTD3ASCIIRules flag is set,
    /// then perform these checks:
    ///
    ///    (a) Verify the absence of non-LDH ASCII code points; that is, the absence
    ///      of 0..2C, 2E..2F, 3A..40, 5B..60, and 7B..7F.
    ///
    /// (b) Verify the absence of leading and trailing hyphen-minus; that is, the
    ///      absence of U+002D at the beginning and end of the sequence.
    ///
    /// By default this flag should not be set.
    /// </summary>
    public class UseStd3AsciiRules
    {
        [Fact]
        public void UseStd3AsciiRules_IsFalseByDefault()
        {
            Assert.False(new IdnMapping().UseStd3AsciiRules);
        }

        [Theory]
        [InlineData("\u0020\u0061\u0062", false)]
        [InlineData("\u0061\u002F\u0062", false)]
        [InlineData("\u0061\u0062\u003D", false)]
        [InlineData("\u0061\u0062\u005D", false)]
        [InlineData("\u007E\u0061\u0062", false)]
        [InlineData("\u0020\u002E\u003D\u005D\u007E", false)]
        [InlineData("\u007E\u002E\u0061", false)]
        [InlineData("\u0061\u002E\u007E", false)]
        [InlineData("\u002D\u0061\u0062", true)] // Leading hyphen minus
        [InlineData("\u002D\u0061\u0062\u002E\u0063\u0064", true)] // Leading hyphen minus in first label
        [InlineData("\u0061\u0062\u002E\u002D\u0063\u0064", true)] // Leading hyphen minus in second label
        [InlineData("\u0061\u0062\u002D", true)] // Trailing hyphen minus
        [InlineData("\u0061\u0062\u002D\u002E\u0063\u0064", true)] // Trailing hyphen minus in first label
        [InlineData("\u0061\u0062\u002E\u0063\u0064\u002D", true)] // Trailing hyphen minus in second label
        [InlineData("\u002D", true)] // Leading and trailing hyphen minus
        [InlineData("\u002D\u0062\u002D", true)] // Leading and trailing hyphen minus
        public void UseStd3AsciiRules_ChangesGetAsciiBehavior(string unicode, bool containsInvalidHyphen)
        {
            var idnStd3False = new IdnMapping { UseStd3AsciiRules = false };
            var idnStd3True = new IdnMapping { UseStd3AsciiRules = true };

            if (containsInvalidHyphen && PlatformDetection.IsIcuGlobalization)
            {
                // ICU always fails on leading/trailing hyphens regardless of the Std3 rules option.
                AssertExtensions.Throws<ArgumentException>("unicode", () => idnStd3False.GetAscii(unicode));
            }
            else
            {
                Assert.Equal(unicode, idnStd3False.GetAscii(unicode));
            }

            AssertExtensions.Throws<ArgumentException>("unicode", () => idnStd3True.GetAscii(unicode));
        }

        [Fact]
        public void UseStd3AsciiRules_NonLDH_ASCII_Codepoint()
        {
            var idnStd3False = new IdnMapping { UseStd3AsciiRules = false };
            string unicode = "\u0030\u002D\u0045\u007A";

            Assert.Equal(unicode, idnStd3False.GetAscii(unicode), ignoreCase: true);
        }
    }
}
