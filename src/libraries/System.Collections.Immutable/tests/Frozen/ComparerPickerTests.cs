// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace System.Collections.Frozen.Tests
{
    public static class ComparerPickerTests
    {
        private static StringComparerBase NewPicker(string[] values, bool ignoreCase)
        {
            StringComparerBase c = ComparerPicker.Pick(values, ignoreCase, out int minLen, out int maxLenDiff);

            foreach (string s in values)
            {
                Assert.True(s.Length >= minLen);
                Assert.True(s.Length <= minLen + maxLenDiff);
            }

            return c;
        }

        [Fact]
        public static void LeftHand()
        {
            StringComparerBase c = NewPicker(new[] { "K0", "K20", "K300" }, false);
            Assert.IsType<LeftJustifiedSingleCharComparer>(c);
            Assert.Equal(1, ((SubstringComparerBase)c).Index);
            Assert.Equal(1, ((SubstringComparerBase)c).Count);

            c = NewPicker(new[] { "S1" }, false);
            Assert.IsType<LeftJustifiedSingleCharComparer>(c);
            Assert.Equal(0, ((SubstringComparerBase)c).Index);
            Assert.Equal(1, ((SubstringComparerBase)c).Count);

            c = NewPicker(new[] { "S1", "T1" }, false);
            Assert.IsType<LeftJustifiedSingleCharComparer>(c);
            Assert.Equal(0, ((SubstringComparerBase)c).Index);
            Assert.Equal(1, ((SubstringComparerBase)c).Count);

            c = NewPicker(new[] { "SA1", "TA1", "SB1" }, false);
            Assert.IsType<LeftJustifiedSubstringComparer>(c);
            Assert.Equal(0, ((SubstringComparerBase)c).Index);
            Assert.Equal(2, ((SubstringComparerBase)c).Count);
        }

        [Fact]
        public static void LeftHandCaseInsensitive()
        {
            StringComparerBase c = NewPicker(new[] { "É1" }, true);
            Assert.IsType<LeftJustifiedCaseInsensitiveSubstringComparer>(c);
            Assert.Equal(0, ((SubstringComparerBase)c).Index);
            Assert.Equal(1, ((SubstringComparerBase)c).Count);

            c = NewPicker(new[] { "É1", "T1" }, true);
            Assert.IsType<LeftJustifiedCaseInsensitiveSubstringComparer>(c);
            Assert.Equal(0, ((SubstringComparerBase)c).Index);
            Assert.Equal(1, ((SubstringComparerBase)c).Count);

            c = NewPicker(new[] { "ÉA1", "TA1", "ÉB1" }, true);
            Assert.IsType<LeftJustifiedCaseInsensitiveSubstringComparer>(c);
            Assert.Equal(0, ((SubstringComparerBase)c).Index);
            Assert.Equal(2, ((SubstringComparerBase)c).Count);

            c = NewPicker(new[] { "ABCDEÉ1ABCDEF", "ABCDETA1ABCDEF", "ABCDESB1ABCDEF" }, true);
            Assert.IsType<LeftJustifiedCaseInsensitiveSubstringComparer>(c);
            Assert.Equal(5, ((SubstringComparerBase)c).Index);
            Assert.Equal(1, ((SubstringComparerBase)c).Count);

            c = NewPicker(new[] { "ABCDEFÉ1ABCDEF", "ABCDEFTA1ABCDEF", "ABCDEFSB1ABCDEF" }, true);
            Assert.IsType<LeftJustifiedCaseInsensitiveSubstringComparer>(c);
            Assert.Equal(6, ((SubstringComparerBase)c).Index);
            Assert.Equal(1, ((SubstringComparerBase)c).Count);

            c = NewPicker(new[] { "ABCÉDEFÉ1ABCDEF", "ABCÉDEFTA1ABCDEF", "ABCÉDEFSB1ABCDEF" }, true);
            Assert.IsType<LeftJustifiedCaseInsensitiveSubstringComparer>(c);
            Assert.Equal(7, ((SubstringComparerBase)c).Index);
            Assert.Equal(1, ((SubstringComparerBase)c).Count);
        }

        [Fact]
        public static void LeftHandCaseInsensitiveAscii()
        {
            StringComparerBase c = NewPicker(new[] { "S1" }, true);
            Assert.IsType<LeftJustifiedCaseInsensitiveAsciiSubstringComparer>(c);
            Assert.Equal(0, ((SubstringComparerBase)c).Index);
            Assert.Equal(1, ((SubstringComparerBase)c).Count);

            c = NewPicker(new[] { "S1", "T1" }, true);
            Assert.IsType<LeftJustifiedCaseInsensitiveAsciiSubstringComparer>(c);
            Assert.Equal(0, ((SubstringComparerBase)c).Index);
            Assert.Equal(1, ((SubstringComparerBase)c).Count);

            c = NewPicker(new[] { "SA1", "TA1", "SB1" }, true);
            Assert.IsType<LeftJustifiedCaseInsensitiveAsciiSubstringComparer>(c);
            Assert.Equal(0, ((SubstringComparerBase)c).Index);
            Assert.Equal(2, ((SubstringComparerBase)c).Count);
        }

        [Fact]
        public static void RightHand()
        {
            StringComparerBase c = NewPicker(new[] { "1S", "1T" }, false);
            Assert.IsType<RightJustifiedSingleCharComparer>(c);
            Assert.Equal(-1, ((SubstringComparerBase)c).Index);
            Assert.Equal(1, ((SubstringComparerBase)c).Count);

            c = NewPicker(new[] { "1AS", "1AT", "1BS" }, false);
            Assert.IsType<RightJustifiedSubstringComparer>(c);
            Assert.Equal(-2, ((SubstringComparerBase)c).Index);
            Assert.Equal(2, ((SubstringComparerBase)c).Count);
        }

        [Fact]
        public static void RightHandCaseInsensitive()
        {
            StringComparerBase c = NewPicker(new[] { "1É", "1T" }, true);
            Assert.IsType<RightJustifiedCaseInsensitiveSubstringComparer>(c);
            Assert.Equal(-1, ((SubstringComparerBase)c).Index);
            Assert.Equal(1, ((SubstringComparerBase)c).Count);

            c = NewPicker(new[] { "1AÉ", "1AT", "1BÉ" }, true);
            Assert.IsType<RightJustifiedCaseInsensitiveSubstringComparer>(c);
            Assert.Equal(-2, ((SubstringComparerBase)c).Index);
            Assert.Equal(2, ((SubstringComparerBase)c).Count);
        }

        [Fact]
        public static void RightHandCaseInsensitiveAscii()
        {
            StringComparerBase c = NewPicker(new[] { "1S", "1T" }, true);
            Assert.IsType<RightJustifiedCaseInsensitiveAsciiSubstringComparer>(c);
            Assert.Equal(-1, ((SubstringComparerBase)c).Index);
            Assert.Equal(1, ((SubstringComparerBase)c).Count);

            c = NewPicker(new[] { "1AS", "1AT", "1BS" }, true);
            Assert.IsType<RightJustifiedCaseInsensitiveAsciiSubstringComparer>(c);
            Assert.Equal(-2, ((SubstringComparerBase)c).Index);
            Assert.Equal(2, ((SubstringComparerBase)c).Count);
        }

        [Fact]
        public static void Full()
        {
            StringComparerBase c = NewPicker(new[] { "ABC", "DBC", "ADC", "ABD", "ABDABD" }, false);
            Assert.IsType<FullStringComparer>(c);
        }

        [Fact]
        public static void FullCaseInsensitive()
        {
            StringComparerBase c = NewPicker(new[] { "æbc", "DBC", "æDC", "æbd", "æbdæbd" }, true);
            Assert.IsType<FullCaseInsensitiveStringComparer>(c);
        }

        [Fact]
        public static void FullCaseInsensitiveAscii()
        {
            StringComparerBase c = NewPicker(new[] { "abc", "DBC", "aDC", "abd", "abdabd" }, true);
            Assert.IsType<FullCaseInsensitiveAsciiStringComparer>(c);
        }

        [Fact]
        public static void IsAllAscii()
        {
            Assert.True(ComparerPicker.IsAllAscii("abc".AsSpan()));
            Assert.True(ComparerPicker.IsAllAscii("abcdefghij".AsSpan()));
            Assert.False(ComparerPicker.IsAllAscii("abcdéfghij".AsSpan()));
        }
    }
}
