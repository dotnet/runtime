// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class UnicodeCategoryTests
    {
        [Theory]
        [InlineData(UnicodeCategory.UppercaseLetter, 0)]
        [InlineData(UnicodeCategory.LowercaseLetter, 1)]
        [InlineData(UnicodeCategory.TitlecaseLetter, 2)]
        [InlineData(UnicodeCategory.ModifierLetter, 3)]
        [InlineData(UnicodeCategory.OtherLetter, 4)]
        [InlineData(UnicodeCategory.NonSpacingMark, 5)]
        [InlineData(UnicodeCategory.SpacingCombiningMark, 6)]
        [InlineData(UnicodeCategory.EnclosingMark, 7)]
        [InlineData(UnicodeCategory.DecimalDigitNumber, 8)]
        [InlineData(UnicodeCategory.LetterNumber, 9)]
        [InlineData(UnicodeCategory.OtherNumber, 10)]
        [InlineData(UnicodeCategory.SpaceSeparator, 11)]
        [InlineData(UnicodeCategory.LineSeparator, 12)]
        [InlineData(UnicodeCategory.ParagraphSeparator, 13)]
        [InlineData(UnicodeCategory.Control, 14)]
        [InlineData(UnicodeCategory.Format, 15)]
        [InlineData(UnicodeCategory.Surrogate, 16)]
        [InlineData(UnicodeCategory.PrivateUse, 17)]
        [InlineData(UnicodeCategory.ConnectorPunctuation, 18)]
        [InlineData(UnicodeCategory.DashPunctuation, 19)]
        [InlineData(UnicodeCategory.OpenPunctuation, 20)]
        [InlineData(UnicodeCategory.ClosePunctuation, 21)]
        [InlineData(UnicodeCategory.InitialQuotePunctuation, 22)]
        [InlineData(UnicodeCategory.FinalQuotePunctuation, 23)]
        [InlineData(UnicodeCategory.OtherPunctuation, 24)]
        [InlineData(UnicodeCategory.MathSymbol, 25)]
        [InlineData(UnicodeCategory.CurrencySymbol, 26)]
        [InlineData(UnicodeCategory.ModifierSymbol, 27)]
        [InlineData(UnicodeCategory.OtherSymbol, 28)]
        [InlineData(UnicodeCategory.OtherNotAssigned, 29)]
        public void Cases(UnicodeCategory category, int expected)
        {
            Assert.Equal(expected, (int)category);
        }
    }
}
