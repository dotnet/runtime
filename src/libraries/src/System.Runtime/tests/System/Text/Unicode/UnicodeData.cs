// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Sdk;

namespace System.Text.Unicode.Tests
{
    /*
     * This file contains helpers for parsing the Unicode data files, which we can then use to test
     * our Framework's implementations of various character & text processing routines.
     */

    public static class UnicodeData
    {
        private const string UnicodeDataFilename = "UnicodeData.11.0.txt";
        private const string CaseFoldingFilename = "CaseFolding-12.1.0.txt";
        private const string PropListFilename = "PropList-12.1.0.txt";

        private static readonly Dictionary<string, UnicodeCategory> _categoryCodeMap = new Dictionary<string, UnicodeCategory>()
        {
            { "Lu", UnicodeCategory.UppercaseLetter },
            { "Ll", UnicodeCategory.LowercaseLetter },
            { "Lt", UnicodeCategory.TitlecaseLetter },
            { "Lm", UnicodeCategory.ModifierLetter },
            { "Lo", UnicodeCategory.OtherLetter },
            { "Mn", UnicodeCategory.NonSpacingMark },
            { "Mc", UnicodeCategory.SpacingCombiningMark },
            { "Me", UnicodeCategory.EnclosingMark },
            { "Nd", UnicodeCategory.DecimalDigitNumber },
            { "Nl", UnicodeCategory.LetterNumber },
            { "No", UnicodeCategory.OtherNumber },
            { "Pc", UnicodeCategory.ConnectorPunctuation },
            { "Pd", UnicodeCategory.DashPunctuation },
            { "Ps", UnicodeCategory.OpenPunctuation },
            { "Pe", UnicodeCategory.ClosePunctuation },
            { "Pi", UnicodeCategory.InitialQuotePunctuation },
            { "Pf", UnicodeCategory.FinalQuotePunctuation },
            { "Po", UnicodeCategory.OtherPunctuation },
            { "Sm", UnicodeCategory.MathSymbol },
            { "Sc", UnicodeCategory.CurrencySymbol },
            { "Sk", UnicodeCategory.ModifierSymbol },
            { "So", UnicodeCategory.OtherSymbol },
            { "Zs", UnicodeCategory.SpaceSeparator },
            { "Zl", UnicodeCategory.LineSeparator },
            { "Zp", UnicodeCategory.ParagraphSeparator },
            { "Cc", UnicodeCategory.Control },
            { "Cf", UnicodeCategory.Format },
            { "Cs", UnicodeCategory.Surrogate },
            { "Co", UnicodeCategory.PrivateUse },
        };

        private static readonly Dictionary<uint, UnicodeCategory> _categoryMap = new Dictionary<uint, UnicodeCategory>();
        private static readonly Dictionary<uint, CharProperty> _propertyMap = new Dictionary<uint, CharProperty>();
        private static readonly Dictionary<uint, uint> _simpleCaseFoldMap = new Dictionary<uint, uint>();
        private static readonly Dictionary<uint, uint> _simpleLowerCaseMap = new Dictionary<uint, uint>();
        private static readonly Dictionary<uint, uint> _simpleTitleCaseMap = new Dictionary<uint, uint>();
        private static readonly Dictionary<uint, uint> _simpleUpperCaseMap = new Dictionary<uint, uint>();

        static UnicodeData()
        {
            static IEnumerable<string> ReadAllLines(string resourceName)
            {
                using (Stream stream = typeof(UnicodeData).Assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream is null)
                    {
                        throw new XunitException($"Could not locate resource stream {resourceName} in the assembly.");
                    }

                    List<string> allLines = new List<string>();

                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string nextLine;
                        while ((nextLine = reader.ReadLine()) != null)
                        {
                            yield return nextLine;
                        }
                    }
                }
            }

            // First, read the basic category, bidi data, and simple upper / lower / title mappings from UnicodeData.txt
            // File format is documented at https://www.unicode.org/reports/tr44/#UnicodeData.txt

            string nameOfCurrentRange = null;
            uint startOfCurrentRange = 0;

            foreach (string line in ReadAllLines(UnicodeDataFilename))
            {
                string[] splitLine = line.Split(';');
                Assert.Equal(15, splitLine.Length); // expected 15 segments resulting from the split

                uint currentCodePoint = uint.Parse(splitLine[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                string codePointName = splitLine[1];
                string category = splitLine[2];

                // Is this the start of a range?
                // If so, save this data and keep iterating.

                if (codePointName.EndsWith(", First>", StringComparison.Ordinal))
                {
                    startOfCurrentRange = currentCodePoint;
                    nameOfCurrentRange = codePointName[..^(", First>".Length)];
                    continue;
                }

                // If we didn't see a <..., First> range immediately before
                // this, assume it's a range of length 1.

                if (codePointName != nameOfCurrentRange + ", Last>")
                {
                    startOfCurrentRange = currentCodePoint;
                }

                uint.TryParse(splitLine[12], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint simpleUpperMapping);
                uint.TryParse(splitLine[13], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint simpleLowerMapping);
                uint.TryParse(splitLine[14], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint simpleTitleMapping);

                for (uint i = startOfCurrentRange; i <= currentCodePoint; i++)
                {
                    _categoryMap[i] = _categoryCodeMap[category];

                    if (simpleLowerMapping != 0)
                    {
                        _simpleLowerCaseMap[i] = simpleLowerMapping;
                    }

                    if (simpleUpperMapping != 0)
                    {
                        _simpleUpperCaseMap[i] = simpleUpperMapping;
                    }

                    if (simpleTitleMapping != 0)
                    {
                        _simpleTitleCaseMap[i] = simpleTitleMapping;
                    }
                }
            }

            // Then, read the property map from PropList.txt.
            // We're looking for lines of the form "XXXX[..YYYY] ; <prop> # <comment>"

            Regex propListRegex = new Regex(@"^\s*(?<firstCodePoint>[0-9A-F]{4,})(\.\.(?<lastCodePoint>[0-9A-F]{4,}))?\s*;\s*(?<propName>\w+)\s*#", RegexOptions.IgnoreCase);

            foreach (string line in ReadAllLines(PropListFilename))
            {
                Match match = propListRegex.Match(line);
                if (!match.Success)
                {
                    continue; // line was blank or was comment-only
                }

                uint firstCodePoint = uint.Parse(match.Groups["firstCodePoint"].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                if (!uint.TryParse(match.Groups["lastCodePoint"].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint lastCodePoint))
                {
                    lastCodePoint = firstCodePoint; // If not "XXXX..YYYY", assume "XXXX..XXXX"
                }
                CharProperty property = Enum.Parse<CharProperty>(match.Groups["propName"].Value);

                for (uint i = firstCodePoint; i <= lastCodePoint; i++)
                {
                    _propertyMap.TryGetValue(i, out CharProperty existingValue);
                    _propertyMap[i] = existingValue | property; // remember, this is flags
                }
            }

            // Finally, read the case fold map from CaseFolding.txt.
            // We're looking for lines of the form "<code>; <status>; <mapping>; # <name>", where <status> is 'C' or 'S'

            Regex caseFoldRegex = new Regex(@"^\s*(?<fromCodePoint>[0-9A-F]{4,}); [CS]; (?<toCodePoint>[0-9A-F]{4,});", RegexOptions.IgnoreCase);

            foreach (string line in ReadAllLines(CaseFoldingFilename))
            {
                Match match = caseFoldRegex.Match(line);
                if (!match.Success)
                {
                    continue; // line was blank, comment-only, or not simple / common
                }

                uint fromCodePoint = uint.Parse(match.Groups["fromCodePoint"].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                uint toCodePoint = uint.Parse(match.Groups["toCodePoint"].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);

                _simpleCaseFoldMap[fromCodePoint] = toCodePoint;
            }
        }

        public static UnicodeCategory GetUnicodeCategory(uint codePoint)
        {
            Assert.True(codePoint <= 0x10FFFF, "Invalid code point provided.");

            return _categoryMap.TryGetValue(codePoint, out UnicodeCategory category) ? category : UnicodeCategory.OtherNotAssigned;
        }

        public static bool IsLetter(uint codePoint)
        {
            switch (GetUnicodeCategory(codePoint))
            {
                case UnicodeCategory.UppercaseLetter:
                case UnicodeCategory.LowercaseLetter:
                case UnicodeCategory.TitlecaseLetter:
                case UnicodeCategory.ModifierLetter:
                case UnicodeCategory.OtherLetter:
                    return true;

                default:
                    return false;
            }
        }

        // Returns true iff code point is listed at https://unicode.org/cldr/utility/list-unicodeset.jsp?a=[:whitespace:]
        public static bool IsWhiteSpace(uint codePoint)
        {
            Assert.True(codePoint <= 0x10FFFF, "Invalid code point provided.");

            _propertyMap.TryGetValue(codePoint, out CharProperty property);
            return property.HasFlag(CharProperty.White_Space);
        }

        public static uint SimpleFoldCaseMap(uint codePoint)
        {
            Assert.True(codePoint <= 0x10FFFF, "Invalid code point provided.");

            // If an entry doesn't exist, the value maps to itself.
            return _simpleCaseFoldMap.TryGetValue(codePoint, out uint value) ? value : codePoint;
        }

        public static uint SimpleLowerCaseMap(uint codePoint)
        {
            Assert.True(codePoint <= 0x10FFFF, "Invalid code point provided.");

            // If an entry doesn't exist, the value maps to itself.
            return _simpleLowerCaseMap.TryGetValue(codePoint, out uint value) ? value : codePoint;
        }

        public static uint SimpleTitleCaseMap(uint codePoint)
        {
            Assert.True(codePoint <= 0x10FFFF, "Invalid code point provided.");

            // If an entry doesn't exist, the value maps to itself.
            return _simpleTitleCaseMap.TryGetValue(codePoint, out uint value) ? value : codePoint;
        }

        public static uint SimpleUpperCaseMap(uint codePoint)
        {
            Assert.True(codePoint <= 0x10FFFF, "Invalid code point provided.");

            // If an entry doesn't exist, the value maps to itself.
            return _simpleUpperCaseMap.TryGetValue(codePoint, out uint value) ? value : codePoint;
        }

        [Flags]
        private enum CharProperty : ulong
        {
            ASCII_Hex_Digit = 1ul << 0,
            Bidi_Control = 1ul << 1,
            Dash = 1ul << 2,
            Deprecated = 1ul << 3,
            Diacritic = 1ul << 4,
            Extender = 1ul << 5,
            Hex_Digit = 1ul << 6,
            Hyphen = 1ul << 7,
            Ideographic = 1ul << 8,
            IDS_Binary_Operator = 1ul << 9,
            IDS_Trinary_Operator = 1ul << 10,
            Join_Control = 1ul << 11,
            Logical_Order_Exception = 1ul << 12,
            Noncharacter_Code_Point = 1ul << 13,
            Other_Alphabetic = 1ul << 14,
            Other_Default_Ignorable_Code_Point = 1ul << 15,
            Other_Grapheme_Extend = 1ul << 16,
            Other_ID_Continue = 1ul << 17,
            Other_ID_Start = 1ul << 18,
            Other_Lowercase = 1ul << 19,
            Other_Math = 1ul << 20,
            Other_Uppercase = 1ul << 21,
            Pattern_Syntax = 1ul << 22,
            Pattern_White_Space = 1ul << 23,
            Prepended_Concatenation_Mark = 1ul << 24,
            Quotation_Mark = 1ul << 25,
            Radical = 1ul << 26,
            Regional_Indicator = 1ul << 27,
            Sentence_Terminal = 1ul << 28,
            Soft_Dotted = 1ul << 29,
            Terminal_Punctuation = 1ul << 30,
            Unified_Ideograph = 1ul << 31,
            Variation_Selector = 1ul << 32,
            White_Space = 1ul << 33,
        }
    }
}
