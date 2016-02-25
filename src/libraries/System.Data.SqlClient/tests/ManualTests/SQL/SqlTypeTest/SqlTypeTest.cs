// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Globalization;
using System.Text;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public static class SqlTypeTest
    {
        private static string[] s_sampleString = new string[] { "In", "its", "first", "month", "on",  "the",  "market,",
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

        private static string[,] s_specialMatchingString = new string[4, 2] {{"Lorem ipsum dolor sit amet", "\uFF2C\uFF4F\uFF52\uFF45\uFF4D\u3000\uFF49\uFF50\uFF53\uFF55\uFF4D\u3000\uFF44\uFF4F\uFF4C\uFF4F\uFF52\u3000\uFF53\uFF49\uFF54\u3000\uFF41\uFF4D\uFF45\uFF54"},
                                                                         {"\u304B\u305F\u304B\u306A", "\u30AB\u30BF\u30AB\u30CA"},
                                                                         {"\uFF8C\uFF67\uFF7D\uFF9E\uFF65\uFF77\uFF9E\uFF80\uFF70", "\u30D5\u30A1\u30BA\u30FB\u30AE\u30BF\u30FC"},
                                                                         {"engine", "eNGine"}};


        private static SqlCompareOptions s_defaultCompareOption = SqlCompareOptions.IgnoreCase | SqlCompareOptions.IgnoreKanaType | SqlCompareOptions.IgnoreWidth;
        private static SqlCompareOptions[] s_compareOptions = new SqlCompareOptions[] { SqlCompareOptions.None,
                                                                                    SqlCompareOptions.BinarySort,
                                                                                    SqlCompareOptions.BinarySort2,
                                                                                    s_defaultCompareOption};

        private static int s_sampleStringCount = s_sampleString.Length - 1;

        private static CultureInfo[] s_cultureInfo =
        {
            new CultureInfo("ar-SA"),  // Arabic - Saudi Arabia
            new CultureInfo("ja-JP"),  // Japanese - Japan
            new CultureInfo("de-DE"),  // German - Germany
            new CultureInfo("hi-IN"),  // Hindi - India
            new CultureInfo("tr-TR"),  // Turkish - Turkey
            new CultureInfo("th-TH"),  // Thai - Thailand
            new CultureInfo("el-GR"),  // Greek - Greece
            new CultureInfo("ru-RU"),  // Russian - Russia
            new CultureInfo("he-IL"),  // Hebrew - Israel
            new CultureInfo("cs-CZ"),  // Czech - Czech Republic
            new CultureInfo("fr-CH"),  // French - Switzerland
            new CultureInfo("en-US")   // English - United States
        };

        private static int[] s_cultureLocaleIDs =
        {
            0x0401,  // Arabic - Saudi Arabia
            0x0411,  // Japanese - Japan
            0x0407,  // German - Germany
            0x0439,  // Hindi - India
            0x041f,  // Turkish - Turkey
            0x041e,  // Thai - Thailand
            0x0408,  // Greek - Greece
            0x0419,  // Russian - Russia
            0x040d,  // Hebrew - Israel
            0x0405,  // Czech - Czech Republic
            0x100c,  // French - Switzerland
            0x0409   // English - United States
        };

        private static UnicodeEncoding s_unicodeEncoding = new UnicodeEncoding(bigEndian: false, byteOrderMark: false, throwOnInvalidBytes: true);

        [Fact]
        public static void SqlStringValidComparisonTest()
        {
            for (int j = 0; j < s_cultureInfo.Length; ++j)
            {
                SqlStringDefaultCompareOptionTest(s_cultureLocaleIDs[j]);

                for (int i = 0; i < s_compareOptions.Length; ++i)
                {
                    SqlStringCompareTest(200, s_compareOptions[i], s_cultureInfo[j], s_cultureLocaleIDs[j]);
                }
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

            for (int i = 0; i < s_specialMatchingString.GetLength(0); ++i)
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
        //  NOTE: This comparison algorightm is different from BinraySory2. The algorithm is copied fro SqlString implementation
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
