// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
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

        private static void AssertEqual<T>(T expected, T actual, string methodName, CodePoint codePoint)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new AssertActualExpectedException(
                    expected: expected,
                    actual: actual,
                    userMessage: FormattableString.Invariant($"CharUnicodeInfo.{methodName}({codePoint}) returned unexpected value."));
            }
        }
    }
}
