// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Data.SqlTypes.Tests
{
    public static class SqlStringSortingTest
    {
        private static readonly string[] s_sampleString = new string[] { "In", "its", "first", "month", "on",  "the",  "market,",
            "Microsoft\u2019s", "new", "search", "engine", "Bing", "Yahoo\u2019s",
            "Wednesday", "from", "tracker", "comScore", "8.4% of queries",
            "Earlier, Microsoft said that unique visitors to Bing",
            "rose 8% in June compared to the previous month. ",
            "The company also touted the search engine\u2019s success with advertisers, saying electronics",
            "retailer TigerDirect increased its marketing spend on",
            "Bing \u201Cby twofold.\u201D",
            "\u3072\u3089\u304C\u306A", "\u304B\u305F\u304B\u306A", "\u30AB\u30BF\u30AB\u30CA", "\uFF2C\uFF4F\uFF52\uFF45\uFF4D\u3000\uFF49\uFF50\uFF53\uFF55\uFF4D\u3000\uFF44\uFF4F\uFF4C\uFF4F\uFF52\u3000\uFF53\uFF49\uFF54\u3000\uFF41\uFF4D\uFF45\uFF54",
            "\uFF8C\uFF67\uFF7D\uFF9E\uFF65\uFF77\uFF9E\uFF80\uFF70", "\u30D5\u30A1\u30BA\u30FB\u30AE\u30BF\u30FC", "eNGine",
            new string(new char[] {'I', 'n', '\uD800', '\uDC00', 'z'}),     // surrogate pair
            new string(new char[] {'\uD800', '\uDC00', '\uD800', '\uDCCC', '\uDBFF', '\uDFCC', '\uDBFF', '\uDFFF'})      // surrogate pairs
        };

        private static readonly string[,] s_specialMatchingString = new string[4, 2] {{"Lorem ipsum dolor sit amet", "\uFF2C\uFF4F\uFF52\uFF45\uFF4D\u3000\uFF49\uFF50\uFF53\uFF55\uFF4D\u3000\uFF44\uFF4F\uFF4C\uFF4F\uFF52\u3000\uFF53\uFF49\uFF54\u3000\uFF41\uFF4D\uFF45\uFF54"},
                                                                         {"\u304B\u305F\u304B\u306A", "\u30AB\u30BF\u30AB\u30CA"},
                                                                         {"engine", "eNGine"},

                                                                         // Keep the following item at the end of the array as some tests need to exclude it.
                                                                         {"\uFF8C\uFF67\uFF7D\uFF9E\uFF65\uFF77\uFF9E\uFF80\uFF70", "\u30D5\u30A1\u30BA\u30FB\u30AE\u30BF\u30FC"}
                                                                         };

        private static readonly int s_sampleStringCount = s_sampleString.Length - 1;

        private static readonly UnicodeEncoding s_unicodeEncoding = new UnicodeEncoding(bigEndian: false, byteOrderMark: false, throwOnInvalidBytes: true);

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInvariantGlobalization))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/95195", typeof(PlatformDetection), nameof(PlatformDetection.IsHybridGlobalizationOnApplePlatform))]
        [InlineData("ja-JP", 0x0411)] // Japanese - Japan
        [InlineData("ar-SA", 0x0401)] // Arabic - Saudi Arabia
        [InlineData("de-DE", 0x0407)] // German - Germany
        [InlineData("hi-IN", 0x0439)] // Hindi - India
        [InlineData("tr-TR", 0x041f)] // Turkish - Turkey
        [InlineData("th-TH", 0x041e)] // Thai - Thailand
        [InlineData("el-GR", 0x0408)] // Greek - Greece
        [InlineData("ru-RU", 0x0419)] // Russian - Russia
        [InlineData("he-IL", 0x040d)] // Hebrew - Israel
        [InlineData("cs-CZ", 0x0405)] // Czech - Czech Republic
        [InlineData("fr-CH", 0x100c)] // French - Switzerland
        [InlineData("en-US", 0x0409)] // English - United States
        public static void SqlStringValidComparisonTest(string cultureName, int localeId)
        {
            if (PlatformDetection.IsIcuGlobalization && cultureName == "ja-JP" && localeId == 0x0411)
            {
                // TODO: Remove this once: https://github.com/dotnet/runtime/issues/18912 is fixed on ICU.
                throw new SkipTestException($"PlatformDetection.IsIcuGlobalization and cultureName == ja-JP");
            }

            var culture = new CultureInfo(cultureName);

            const SqlCompareOptions DefaultCompareOption = SqlCompareOptions.IgnoreCase | SqlCompareOptions.IgnoreKanaType | SqlCompareOptions.IgnoreWidth;

            SqlStringDefaultCompareOptionTest(localeId);
            foreach (SqlCompareOptions option in new[] { SqlCompareOptions.None, SqlCompareOptions.BinarySort, SqlCompareOptions.BinarySort2, DefaultCompareOption })
            {
                SqlStringCompareTest(200, option, culture, localeId);
            }
        }

        [Fact]
        public static void SqlStringNullComparisonTest()
        {
            SqlString nullSqlString = new SqlString(null);
            SqlString nonNullSqlString = new SqlString("abc   ");

            Assert.True(
                (bool)(nullSqlString < nonNullSqlString
                || nonNullSqlString >= nullSqlString
                || nullSqlString.CompareTo(nonNullSqlString) < 0
                || nonNullSqlString.CompareTo(nullSqlString) >= 0),
                "FAILED: (SqlString Null Comparison): Null SqlString not equal to null");

            Assert.True((nullSqlString == null && nullSqlString.CompareTo(null) == 0).IsNull, "FAILED: (SqlString Null Comparison): Null SqlString not equal to null");
        }

        // Special characters matching test for default option (SqlCompareOptions.IgnoreCase | SqlCompareOptions.IgnoreKanaType | SqlCompareOptions.IgnoreWidth)
        private static void SqlStringDefaultCompareOptionTest(int localeID)
        {
            SqlString str1;
            SqlString str2;

            int count = s_specialMatchingString.GetLength(0);

            // Some of Windows versions have a regression, so ignore last entry in the s_specialMatchingString if this is the case.
            if (PlatformDetection.IsWindows10Version1903OrGreater &&
                CultureInfo.InvariantCulture.CompareInfo.Compare("\u3060", "\uFF80\uFF9E", CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase) != 0)
            {
                count--;
            }

            for (int i = 0; i < count; ++i)
            {
                // SqlString(string) creates instance with the default comparison options
                str1 = new SqlString(s_specialMatchingString[i, 0], localeID);
                str2 = new SqlString(s_specialMatchingString[i, 1], localeID);

                // Per default option, each set contains two string which should be matched as equal per default option
                Assert.True((bool)(str1 == str2), string.Format("Error (Default Comparison Option with Operator): {0} and {1} should be equal", s_specialMatchingString[i, 0], s_specialMatchingString[i, 1]));
                Assert.True(str1.CompareTo(str2) == 0, string.Format("FAILED: (Default Comparison Option with CompareTo): {0} and {1} should be equal", s_specialMatchingString[i, 0], s_specialMatchingString[i, 1]));
            }
        }

        private static void SqlStringCompareTest(int numberOfItems, SqlCompareOptions compareOption, CultureInfo cInfo, int localeID)
        {
            SortedList<SqlString, SqlString> items = CreateSortedSqlStringList(numberOfItems, compareOption, cInfo, localeID);
            VerifySortedSqlStringList(items, compareOption, cInfo);
        }

        private static SortedList<SqlString, SqlString> CreateSortedSqlStringList(int numberOfItems, SqlCompareOptions compareOption, CultureInfo cInfo, int localeID)
        {
            SortedList<SqlString, SqlString> items = new SortedList<SqlString, SqlString>(numberOfItems);

            //
            // Generate list of SqlString
            //

            Random rand = new Random(500);
            int numberOfWords;

            StringBuilder builder = new StringBuilder();
            SqlString word;

            for (int i = 0; i < numberOfItems; ++i)
            {
                do
                {
                    builder.Clear();
                    numberOfWords = rand.Next(10) + 1;

                    for (int j = 0; j < numberOfWords; ++j)
                    {
                        builder.Append(s_sampleString[rand.Next(s_sampleStringCount)]);
                        builder.Append(" ");
                    }

                    if (numberOfWords % 2 == 1)
                    {
                        for (int k = 0; k < rand.Next(100); ++k)
                        {
                            builder.Append(' ');
                        }
                    }
                    word = new SqlString(builder.ToString(), localeID, compareOption);
                } while (items.ContainsKey(word));

                items.Add(word, word);
            }

            return items;
        }

        private static void VerifySortedSqlStringList(SortedList<SqlString, SqlString> items, SqlCompareOptions compareOption, CultureInfo cInfo)
        {
            //
            // Verify the list is in order
            //

            IList<SqlString> keyList = items.Keys;
            for (int i = 0; i < items.Count - 1; ++i)
            {
                SqlString currentString = keyList[i];
                SqlString nextString = keyList[i + 1];

                Assert.True((bool)((currentString < nextString) && (nextString >= currentString)), "FAILED: (SqlString Operator Comparison): SqlStrings are out of order");
                Assert.True((currentString.CompareTo(nextString) < 0) && (nextString.CompareTo(currentString) > 0), "FAILED: (SqlString.CompareTo): SqlStrings are out of order");

                switch (compareOption)
                {
                    case SqlCompareOptions.BinarySort:
                        Assert.True(CompareBinary(currentString.Value, nextString.Value) < 0, "FAILED: (SqlString BinarySort Comparison): SqlStrings are out of order");
                        break;
                    case SqlCompareOptions.BinarySort2:
                        Assert.True(string.CompareOrdinal(currentString.Value.TrimEnd(), nextString.Value.TrimEnd()) < 0, "FAILED: (SqlString BinarySort2 Comparison): SqlStrings are out of order");

                        break;
                    default:
                        CompareInfo cmpInfo = cInfo.CompareInfo;
                        CompareOptions cmpOptions = SqlString.CompareOptionsFromSqlCompareOptions(nextString.SqlCompareOptions);

                        Assert.True(cmpInfo.Compare(currentString.Value.TrimEnd(), nextString.Value.TrimEnd(), cmpOptions) < 0, "FAILED: (SqlString Comparison): SqlStrings are out of order");
                        break;
                }
            }
        }

        //  Wide-character string comparison for Binary Unicode Collation (for SqlCompareOptions.BinarySort)
        //  Return values:
        //      -1 : wstr1 < wstr2
        //      0  : wstr1 = wstr2
        //      1  : wstr1 > wstr2
        //
        //  Does a memory comparison.
        //  NOTE: This comparison algorithm is different from BinraySory2. The algorithm is copied fro SqlString implementation
        private static int CompareBinary(string x, string y)
        {
            byte[] rgDataX = s_unicodeEncoding.GetBytes(x);
            byte[] rgDataY = s_unicodeEncoding.GetBytes(y);
            int cbX = rgDataX.Length;
            int cbY = rgDataY.Length;
            int cbMin = cbX < cbY ? cbX : cbY;
            int i;

            for (i = 0; i < cbMin; i++)
            {
                if (rgDataX[i] < rgDataY[i])
                    return -1;
                else if (rgDataX[i] > rgDataY[i])
                    return 1;
            }

            i = cbMin;

            int iCh;
            int iSpace = (int)' ';

            if (cbX < cbY)
            {
                for (; i < cbY; i += 2)
                {
                    iCh = ((int)rgDataY[i + 1]) << 8 + rgDataY[i];
                    if (iCh != iSpace)
                        return (iSpace > iCh) ? 1 : -1;
                }
            }
            else
            {
                for (; i < cbX; i += 2)
                {
                    iCh = ((int)rgDataX[i + 1]) << 8 + rgDataX[i];
                    if (iCh != iSpace)
                        return (iCh > iSpace) ? 1 : -1;
                }
            }

            return 0;
        }
    }
}
