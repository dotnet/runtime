// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using System.Text.Unicode;
using Xunit;
using Xunit.Sdk;

namespace System.Globalization.Tests
{
    public partial class CharUnicodeInfoTests
    {
        [Fact]
        public void GetDecimalDigitValue_Char()
        {
            for (int i = 0; i <= char.MaxValue; i++)
            {
                char ch = (char)i;

                CodePoint knownGoodData = UnicodeData.GetData(ch);
                int actualValue = CharUnicodeInfo.GetDecimalDigitValue(ch);

                AssertEqual(knownGoodData.DecimalDigitValue, actualValue, nameof(CharUnicodeInfo.GetDecimalDigitValue), knownGoodData);
            }
        }

        [Fact]
        public void GetDigitValue_Char()
        {
            for (int i = 0; i <= char.MaxValue; i++)
            {
                char ch = (char)i;

                CodePoint knownGoodData = UnicodeData.GetData(ch);
                int actualValue = CharUnicodeInfo.GetDigitValue(ch);

                AssertEqual(knownGoodData.DigitValue, actualValue, nameof(CharUnicodeInfo.GetDigitValue), knownGoodData);
            }
        }

        [Fact]
        public void GetNumericValue_Char()
        {
            for (int i = 0; i <= char.MaxValue; i++)
            {
                char ch = (char)i;

                CodePoint knownGoodData = UnicodeData.GetData(ch);
                double actualValue = CharUnicodeInfo.GetNumericValue(ch);

                AssertEqual(knownGoodData.NumericValue, actualValue, nameof(CharUnicodeInfo.GetNumericValue), knownGoodData);
            }
        }

        [Fact]
        public void GetUnicodeCategory_Char()
        {
            for (int i = 0; i <= char.MaxValue; i++)
            {
                char ch = (char)i;

                CodePoint knownGoodData = UnicodeData.GetData(ch);
                UnicodeCategory actualCategory = CharUnicodeInfo.GetUnicodeCategory(ch);

                AssertEqual(knownGoodData.GeneralCategory, actualCategory, nameof(CharUnicodeInfo.GetUnicodeCategory), knownGoodData);
            }
        }

        [Fact]
        public void GetUnicodeCategory_Int32()
        {
            for (int i = 0; i <= HIGHEST_CODE_POINT; i++)
            {
                CodePoint knownGoodData = UnicodeData.GetData(i);
                UnicodeCategory actualCategory = CharUnicodeInfo.GetUnicodeCategory(i);

                AssertEqual(knownGoodData.GeneralCategory, actualCategory, nameof(CharUnicodeInfo.GetUnicodeCategory), knownGoodData);
            }
        }

        [Fact]
        public void TestCasing()
        {
            Func<uint, uint> toUpperUInt = (Func<uint, uint>) typeof(CharUnicodeInfo)
                                                                .GetMethod("ToUpper", BindingFlags.NonPublic | System.Reflection.BindingFlags.Static, new Type[] { typeof(uint) })
                                                                .CreateDelegate(typeof(Func<uint, uint>));
            Func<char, char> toUpperChar = (Func<char, char>) typeof(CharUnicodeInfo)
                                                                .GetMethod("ToUpper", BindingFlags.NonPublic | System.Reflection.BindingFlags.Static, new Type[] { typeof(char) })
                                                                .CreateDelegate(typeof(Func<char, char>));
            Func<uint, uint> toLowerUInt = (Func<uint, uint>) typeof(CharUnicodeInfo)
                                                                .GetMethod("ToLower", BindingFlags.NonPublic | System.Reflection.BindingFlags.Static, new Type[] { typeof(uint) })
                                                                .CreateDelegate(typeof(Func<uint, uint>));
            Func<char, char> toLowerChar = (Func<char, char>) typeof(CharUnicodeInfo)
                                                                .GetMethod("ToLower", BindingFlags.NonPublic | System.Reflection.BindingFlags.Static, new Type[] { typeof(char) })
                                                                .CreateDelegate(typeof(Func<char, char>));

            for (int i = 0; i <= 0xFFFF; i++)
            {
                if (i == 0x0130 || // We special case Turkish uppercase i
                    i == 0x0131 || // and Turkish lowercase i
                    i == 0x017f)   // and LATIN SMALL LETTER LONG S
                {
                    continue;
                }

                CodePoint codePoint = UnicodeData.GetData(i);

                Assert.True(codePoint.SimpleUppercaseMapping == (int)toUpperUInt((uint)i),
                            $"CharUnicodeInfo.ToUpper({i:x4}) returned unexpected value. Expected: {codePoint.SimpleUppercaseMapping:x4}, Actual: {toUpperUInt((uint)i):x4}");

                Assert.True(codePoint.SimpleUppercaseMapping == (int)toUpperChar((char)i),
                            $"CharUnicodeInfo.ToUpper({i:x4}) returned unexpected value. Expected: {codePoint.SimpleUppercaseMapping:x4}, Actual: {(int)toUpperChar((char)i):x4}");

                Assert.True(codePoint.SimpleLowercaseMapping == (int)toLowerUInt((uint)i),
                            $"CharUnicodeInfo.ToLower({i:x4}) returned unexpected value. Expected: {codePoint.SimpleLowercaseMapping:x4}, Actual: {toLowerUInt((uint)i):x4}");

                Assert.True(codePoint.SimpleLowercaseMapping == (int)toLowerChar((char)i),
                            $"CharUnicodeInfo.ToLower({i:x4}) returned unexpected value. Expected: {codePoint.SimpleLowercaseMapping:x4}, Actual: {(int)toLowerChar((char)i):x4}");
            }

            for (int i = 0x10000; i <= HIGHEST_CODE_POINT; i++)
            {
                CodePoint codePoint = UnicodeData.GetData(i);

                Assert.True(codePoint.SimpleUppercaseMapping == (int)toUpperUInt((uint)i),
                            $"CharUnicodeInfo.ToUpper({i:x4}) returned unexpected value. Expected: {codePoint.SimpleUppercaseMapping:x4}, Actual: {toUpperUInt((uint)i):x4}");

                Assert.True(codePoint.SimpleLowercaseMapping == (int)toLowerUInt((uint)i),
                            $"CharUnicodeInfo.ToLower({i:x4}) returned unexpected value. Expected: {codePoint.SimpleLowercaseMapping:x4}, Actual: {toLowerUInt((uint)i):x4}");
            }
        }

        private static void AssertEqual<T>(T expected, T actual, string methodName, CodePoint codePoint)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw EqualException.ForMismatchedValues(
                    expected: expected,
                    actual: actual,
                    banner: FormattableString.Invariant($"CharUnicodeInfo.{methodName}({codePoint}) returned unexpected value."));
            }
        }
    }
}
