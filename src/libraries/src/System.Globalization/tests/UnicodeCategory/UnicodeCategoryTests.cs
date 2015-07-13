// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class UnicodeCategoryTests
    {
        [Fact]
        public void UppercaseLetter()
        {
            Assert.Equal(0, (int)UnicodeCategory.UppercaseLetter);
        }

        [Fact]
        public void LowercaseLetter()
        {
            Assert.Equal(1, (int)UnicodeCategory.LowercaseLetter);
        }

        [Fact]
        public void TitlecaseLetter()
        {
            Assert.Equal(2, (int)UnicodeCategory.TitlecaseLetter);
        }

        [Fact]
        public void ModifierLetter()
        {
            Assert.Equal(3, (int)UnicodeCategory.ModifierLetter);
        }

        [Fact]
        public void OtherLetter()
        {
            Assert.Equal(4, (int)UnicodeCategory.OtherLetter);
        }

        [Fact]
        public void NonSpacingMark()
        {
            Assert.Equal(5, (int)UnicodeCategory.NonSpacingMark);
        }

        [Fact]
        public void SpacingCombiningMark()
        {
            Assert.Equal(6, (int)UnicodeCategory.SpacingCombiningMark);
        }

        [Fact]
        public void EnclosingMark()
        {
            Assert.Equal(7, (int)UnicodeCategory.EnclosingMark);
        }

        [Fact]
        public void DecimalDigitNumber()
        {
            Assert.Equal(8, (int)UnicodeCategory.DecimalDigitNumber);
        }

        [Fact]
        public void LetterNumber()
        {
            Assert.Equal(9, (int)UnicodeCategory.LetterNumber);
        }

        [Fact]
        public void OtherNumber()
        {
            Assert.Equal(10, (int)UnicodeCategory.OtherNumber);
        }

        [Fact]
        public void SpaceSeparator()
        {
            Assert.Equal(11, (int)UnicodeCategory.SpaceSeparator);
        }

        [Fact]
        public void LineSeparator()
        {
            Assert.Equal(12, (int)UnicodeCategory.LineSeparator);
        }

        [Fact]
        public void ParagraphSeparator()
        {
            Assert.Equal(13, (int)UnicodeCategory.ParagraphSeparator);
        }

        [Fact]
        public void Control()
        {
            Assert.Equal(14, (int)UnicodeCategory.Control);
        }

        [Fact]
        public void Format()
        {
            Assert.Equal(15, (int)UnicodeCategory.Format);
        }

        [Fact]
        public void Surrogate()
        {
            Assert.Equal(16, (int)UnicodeCategory.Surrogate);
        }

        [Fact]
        public void PrivateUse()
        {
            Assert.Equal(17, (int)UnicodeCategory.PrivateUse);
        }

        [Fact]
        public void ConnectorPunctuation()
        {
            Assert.Equal(18, (int)UnicodeCategory.ConnectorPunctuation);
        }

        [Fact]
        public void DashPunctuation()
        {
            Assert.Equal(19, (int)UnicodeCategory.DashPunctuation);
        }

        [Fact]
        public void OpenPunctuation()
        {
            Assert.Equal(20, (int)UnicodeCategory.OpenPunctuation);
        }

        [Fact]
        public void ClosePunctuation()
        {
            Assert.Equal(21, (int)UnicodeCategory.ClosePunctuation);
        }

        [Fact]
        public void InitialQuotePunctuation()
        {
            Assert.Equal(22, (int)UnicodeCategory.InitialQuotePunctuation);
        }

        [Fact]
        public void FinalQuotePunctuation()
        {
            Assert.Equal(23, (int)UnicodeCategory.FinalQuotePunctuation);
        }

        [Fact]
        public void OtherPunctuation()
        {
            Assert.Equal(24, (int)UnicodeCategory.OtherPunctuation);
        }

        [Fact]
        public void MathSymbol()
        {
            Assert.Equal(25, (int)UnicodeCategory.MathSymbol);
        }

        [Fact]
        public void CurrencySymbol()
        {
            Assert.Equal(26, (int)UnicodeCategory.CurrencySymbol);
        }

        [Fact]
        public void ModifierSymbol()
        {
            Assert.Equal(27, (int)UnicodeCategory.ModifierSymbol);
        }

        [Fact]
        public void OtherSymbol()
        {
            Assert.Equal(28, (int)UnicodeCategory.OtherSymbol);
        }

        [Fact]
        public void OtherNotAssigned()
        {
            Assert.Equal(29, (int)UnicodeCategory.OtherNotAssigned);
        }
    }
}