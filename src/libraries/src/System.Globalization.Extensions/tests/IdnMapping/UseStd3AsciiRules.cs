// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Globalization;

namespace System.Globalization.Extensions.Tests
{
    /// <summary>
    /// According to the ToASCII algorithm, if the UseSTD3ASCIIRules flag is set, 
    /// then perform these checks: 
    ///
    ///	(a) Verify the absence of non-LDH ASCII code points; that is, the absence 
    ///      of 0..2C, 2E..2F, 3A..40, 5B..60, and 7B..7F.
    ///
    /// (b) Verify the absence of leading and trailing hyphen-minus; that is, the 
    ///      absence of U+002D at the beginning and end of the sequence.
    ///
    /// By default this flag should not be set.
    /// </summary>
    public class UseStd3AsciiRules
    {
        private void VerifyStd3AsciiRules(string unicode)
        {
            var idnStd3False = new IdnMapping { UseStd3AsciiRules = false };
            var idnStd3True = new IdnMapping { UseStd3AsciiRules = true };

            Assert.Equal(unicode, idnStd3False.GetAscii(unicode));
            Assert.Throws<ArgumentException>(() => idnStd3True.GetAscii(unicode));
        }

        [Fact]
        public void DefaultIsFalse()
        {
            Assert.False(new IdnMapping().UseStd3AsciiRules);
        }

        [Fact]
        public void SanityCheck()
        {
            VerifyStd3AsciiRules("\u0020\u0061\u0062");
            VerifyStd3AsciiRules("\u0061\u002F\u0062");
            VerifyStd3AsciiRules("\u0061\u0062\u003D");
            VerifyStd3AsciiRules("\u0061\u0062\u005D");
            VerifyStd3AsciiRules("\u007E\u0061\u0062");
            VerifyStd3AsciiRules("\u0020\u002E\u003D\u005D\u007E");
            VerifyStd3AsciiRules("\u007E\u002E\u0061");
            VerifyStd3AsciiRules("\u0061\u002E\u007E");
        }

        [Fact]
        public void LeadingHyphenMinus()
        {
            VerifyStd3AsciiRules("\u002D\u0061\u0062");
        }

        [Fact]
        public void LeadingHyphenMinusInFirstLabel()
        {
            VerifyStd3AsciiRules("\u002D\u0061\u0062\u002E\u0063\u0064");
        }

        [Fact]
        public void LeadingHyphenMinusInSecondLabel()
        {
            VerifyStd3AsciiRules("\u0061\u0062\u002E\u002D\u0063\u0064");
        }

        [Fact]
        public void TrailingHyphenMinus()
        {
            VerifyStd3AsciiRules("\u0061\u0062\u002D");
        }

        [Fact]
        public void TrailingHyphenMinusInFirstLabel()
        {
            VerifyStd3AsciiRules("\u0061\u0062\u002D\u002E\u0063\u0064");
        }

        [Fact]
        public void TrailingHyphenMinusInSecondLabel()
        {
            VerifyStd3AsciiRules("\u0061\u0062\u002E\u0063\u0064\u002D");
        }

        [Fact]
        public void LeadingAndTrailingHyphenMinus()
        {
            VerifyStd3AsciiRules("\u002D");
            VerifyStd3AsciiRules("\u002D\u0062\u002D");
        }

        [Fact]
        public void NonLDH_ASCII_Codepoint()
        {
            var idnStd3False = new IdnMapping { UseStd3AsciiRules = false };
            var unicode = "\u0030\u002D\u0045\u007A";

            Assert.Equal(unicode, idnStd3False.GetAscii(unicode));
        }
    }
}
