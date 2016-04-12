// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace System.Globalization.Tests
{
    public static class CharUnicodeInfoTestData
    {
        private static readonly Lazy<List<CharUnicodeInfoTestCase>> s_testCases = new Lazy<List<CharUnicodeInfoTestCase>>(() =>
        {
            List<CharUnicodeInfoTestCase> testCases = new List<CharUnicodeInfoTestCase>();
            string fileName = "UnicodeData.8.0.txt";
            Stream stream = typeof(CharUnicodeInfoGetUnicodeCategoryTests).GetTypeInfo().Assembly.GetManifestResourceStream(fileName);
            using (StreamReader reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    testCases.Add(Parse(reader.ReadLine()));
                }
            }
            return testCases;
        });

        public static List<CharUnicodeInfoTestCase> TestCases => s_testCases.Value;

        private static CharUnicodeInfoTestCase Parse(string line)
        {
            // Data is in the format: 
            // code-value;
            // character-name; (ignored)
            // general-category;
            // canonical-combining-classes; (ignored)
            // bidirecional-category; (ignored)
            // character-decomposition-mapping; (ignored)
            // decimal-digit-value; (ignored)
            // digit-value; (ignoed)
            // number-value;
            string[] data = line.Split(';');
            string charValueString = data[0];
            string charCategoryString = data[2];
            string numericValueString = data[8];

            int codeValue = int.Parse(charValueString, NumberStyles.HexNumber);
            string codeValueRepresentation = codeValue > char.MaxValue ? char.ConvertFromUtf32(codeValue) : ((char)codeValue).ToString();

            double numericValue = ParseNumericValueString(numericValueString);

            UnicodeCategory generalCategory;

            switch (charCategoryString)
            {
                case "Pe":
                    generalCategory = UnicodeCategory.ClosePunctuation;
                    break;
                case "Pc":
                    generalCategory = UnicodeCategory.ConnectorPunctuation;
                    break;
                case "Cc":
                    generalCategory = UnicodeCategory.Control;
                    break;
                case "Sc":
                    generalCategory = UnicodeCategory.CurrencySymbol;
                    break;
                case "Pd":
                    generalCategory = UnicodeCategory.DashPunctuation;
                    break;
                case "Nd":
                    generalCategory = UnicodeCategory.DecimalDigitNumber;
                    break;
                case "Me":
                    generalCategory = UnicodeCategory.EnclosingMark;
                    break;
                case "Pf":
                    generalCategory = UnicodeCategory.FinalQuotePunctuation;
                    break;
                case "Cf":
                    generalCategory = UnicodeCategory.Format;
                    break;
                case "Pi":
                    generalCategory = UnicodeCategory.InitialQuotePunctuation;
                    break;
                case "Nl":
                    generalCategory = UnicodeCategory.LetterNumber;
                    break;
                case "Zl":
                    generalCategory = UnicodeCategory.LineSeparator;
                    break;
                case "Ll":
                    generalCategory = UnicodeCategory.LowercaseLetter;
                    break;
                case "Sm":
                    generalCategory = UnicodeCategory.MathSymbol;
                    break;
                case "Lm":
                    generalCategory = UnicodeCategory.ModifierLetter;
                    break;
                case "Sk":
                    generalCategory = UnicodeCategory.ModifierSymbol;
                    break;
                case "Mn":
                    generalCategory = UnicodeCategory.NonSpacingMark;
                    break;
                case "Ps":
                    generalCategory = UnicodeCategory.OpenPunctuation;
                    break;
                case "Lo":
                    generalCategory = UnicodeCategory.OtherLetter;
                    break;
                case "Cn":
                    generalCategory = UnicodeCategory.OtherNotAssigned;
                    break;
                case "No":
                    generalCategory = UnicodeCategory.OtherNumber;
                    break;
                case "Po":
                    generalCategory = UnicodeCategory.OtherPunctuation;
                    break;
                case "So":
                    generalCategory = UnicodeCategory.OtherSymbol;
                    break;
                case "Zp":
                    generalCategory = UnicodeCategory.ParagraphSeparator;
                    break;
                case "Co":
                    generalCategory = UnicodeCategory.PrivateUse;
                    break;
                case "Zs":
                    generalCategory = UnicodeCategory.SpaceSeparator;
                    break;
                case "Mc":
                    generalCategory = UnicodeCategory.SpacingCombiningMark;
                    break;
                case "Cs":
                    generalCategory = UnicodeCategory.Surrogate;
                    break;
                case "Lt":
                    generalCategory = UnicodeCategory.TitlecaseLetter;
                    break;
                case "Lu":
                    generalCategory = UnicodeCategory.UppercaseLetter;
                    break;
                default:
                    throw new InvalidOperationException("No such UnicodeCharCategory. Check the test data.");
            }

            return new CharUnicodeInfoTestCase()
            {
                Utf32CodeValue = codeValueRepresentation,
                GeneralCategory = generalCategory,
                NumericValue = numericValue
            };
        }

        private static double ParseNumericValueString(string numericValueString)
        {
            if (numericValueString.Length == 0)
            {
                // Parsing empty string (no numeric value)
                return -1;
            }

            int fractionDelimeterIndex = numericValueString.IndexOf("/", StringComparison.Ordinal);
            if (fractionDelimeterIndex == -1)
            {
                // Parsing basic number
                return double.Parse(numericValueString);
            }

            // Unicode datasets display fractions (e.g. 1/4 instead of 0.25), so we should parse them as such
            string numeratorString = numericValueString.Substring(0, fractionDelimeterIndex);
            double numerator = double.Parse(numeratorString);

            string denominatorString = numericValueString.Substring(fractionDelimeterIndex + 1);
            double denominator = double.Parse(denominatorString);

            return numerator / denominator;
        }
    }

    public class CharUnicodeInfoTestCase
    {
        public string Utf32CodeValue { get; set; }
        public UnicodeCategory GeneralCategory { get; set; }
        public double NumericValue { get; set; }
    }
}
