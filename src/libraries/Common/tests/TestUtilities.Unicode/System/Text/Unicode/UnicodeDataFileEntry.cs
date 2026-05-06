// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace System.Text.Unicode
{
    // Represents an entry from UnicodeData.txt.
    internal sealed class UnicodeDataFileEntry
    {
        private static readonly Dictionary<string, UnicodeCategory> UnicodeCategoryMap = new Dictionary<string, UnicodeCategory>
        {
            ["Lu"] = UnicodeCategory.UppercaseLetter,
            ["Ll"] = UnicodeCategory.LowercaseLetter,
            ["Lt"] = UnicodeCategory.TitlecaseLetter,
            ["Lm"] = UnicodeCategory.ModifierLetter,
            ["Lo"] = UnicodeCategory.OtherLetter,
            ["Mn"] = UnicodeCategory.NonSpacingMark,
            ["Mc"] = UnicodeCategory.SpacingCombiningMark,
            ["Me"] = UnicodeCategory.EnclosingMark,
            ["Nd"] = UnicodeCategory.DecimalDigitNumber,
            ["Nl"] = UnicodeCategory.LetterNumber,
            ["No"] = UnicodeCategory.OtherNumber,
            ["Zs"] = UnicodeCategory.SpaceSeparator,
            ["Zl"] = UnicodeCategory.LineSeparator,
            ["Zp"] = UnicodeCategory.ParagraphSeparator,
            ["Cc"] = UnicodeCategory.Control,
            ["Cf"] = UnicodeCategory.Format,
            ["Cs"] = UnicodeCategory.Surrogate,
            ["Co"] = UnicodeCategory.PrivateUse,
            ["Pc"] = UnicodeCategory.ConnectorPunctuation,
            ["Pd"] = UnicodeCategory.DashPunctuation,
            ["Ps"] = UnicodeCategory.OpenPunctuation,
            ["Pe"] = UnicodeCategory.ClosePunctuation,
            ["Pi"] = UnicodeCategory.InitialQuotePunctuation,
            ["Pf"] = UnicodeCategory.FinalQuotePunctuation,
            ["Po"] = UnicodeCategory.OtherPunctuation,
            ["Sm"] = UnicodeCategory.MathSymbol,
            ["Sc"] = UnicodeCategory.CurrencySymbol,
            ["Sk"] = UnicodeCategory.ModifierSymbol,
            ["So"] = UnicodeCategory.OtherSymbol,
            ["Cn"] = UnicodeCategory.OtherNotAssigned,
        };

        public readonly int CodePoint;
        public readonly string Name;
        public readonly UnicodeCategory GeneralCategory;
        public readonly int DecimalDigitValue;
        public readonly int DigitValue;
        public readonly double NumericValue;
        public readonly int SimpleUppercaseMapping;
        public readonly int SimpleLowercaseMapping;
        public readonly int SimpleTitlecaseMapping;

        // ctor used when UnicodeData.txt contains a range
        public UnicodeDataFileEntry(int codePoint, string baseName, UnicodeCategory generalCategory)
        {
            CodePoint = codePoint;
            Name = baseName;
            GeneralCategory = generalCategory;

            DecimalDigitValue = -1;
            DigitValue = -1;
            NumericValue = -1;

            SimpleUppercaseMapping = codePoint;
            SimpleLowercaseMapping = codePoint;
            SimpleTitlecaseMapping = codePoint;
        }

        public UnicodeDataFileEntry(string line)
        {
            // The format of each line is listed at https://www.unicode.org/reports/tr44/#UnicodeData.txt.
            // ';' is used as a separator, and we should have exactly 15 entries per line.

            string[] split = line.Split(';');
            Assert.Equal(15, split.Length);

            CodePoint = (int)uint.Parse(split[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            Name = split[1];
            GeneralCategory = UnicodeCategoryMap[split[2]];

            if (!int.TryParse(split[6], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out DecimalDigitValue))
            {
                DecimalDigitValue = -1;
            }

            if (!int.TryParse(split[7], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out DigitValue))
            {
                DigitValue = -1;
            }

            NumericValue = -1;
            if (!string.IsNullOrEmpty(split[8]))
            {
                // Data is in the format "[-]M[/N]"

                string numericValue = split[8];
                int indexOfSlash = numericValue.IndexOf('/');

                if (indexOfSlash < 0)
                {
                    NumericValue = double.Parse(numericValue, NumberStyles.Integer, CultureInfo.InvariantCulture);
                }
                else
                {
                    double numerator = double.Parse(numericValue.AsSpan(0, indexOfSlash), NumberStyles.Integer, CultureInfo.InvariantCulture);
                    double denominator = double.Parse(numericValue.AsSpan(indexOfSlash + 1), NumberStyles.Integer, CultureInfo.InvariantCulture);
                    NumericValue = numerator / denominator;
                }
            }

            if (!int.TryParse(split[12], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out SimpleUppercaseMapping))
            {
                SimpleUppercaseMapping = CodePoint;
            }

            if (!int.TryParse(split[13], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out SimpleLowercaseMapping))
            {
                SimpleLowercaseMapping = CodePoint;
            }

            if (!int.TryParse(split[14], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out SimpleTitlecaseMapping))
            {
                SimpleTitlecaseMapping = CodePoint;
            }
        }
    }
}
