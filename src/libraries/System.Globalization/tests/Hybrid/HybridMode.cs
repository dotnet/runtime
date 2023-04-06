// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class HybridModeTests
    {
        public static IEnumerable<object[]> EnglishName_TestData()
        {
            yield return new object[] { "en-US", "English (United States)" };
            yield return new object[] { "fr-FR", "French (France)" };
        }

        public static IEnumerable<object[]> NativeName_TestData()
        {
            yield return new object[] { "en-US", "English (United States)" };
            yield return new object[] { "fr-FR", "français (France)" };
            yield return new object[] { "en-CA", "English (Canada)" };
        }

        [Theory]
        [MemberData(nameof(EnglishName_TestData))]
        public void TestEnglishName(string cultureName, string expected)
        {
            CultureInfo myTestCulture = new CultureInfo(cultureName);
            Assert.Equal(expected, myTestCulture.EnglishName);
        }

        [Theory]
        [MemberData(nameof(NativeName_TestData))]
        public void TestNativeName(string cultureName, string expected)
        {
            CultureInfo myTestCulture = new CultureInfo(cultureName);
            Assert.Equal(expected, myTestCulture.NativeName);
        }

        [Theory]
        [InlineData("de-DE", "de")]
        [InlineData("en-US", "en")]
        public void TwoLetterISOLanguageName(string name, string expected)
        {
            Assert.Equal(expected, new CultureInfo(name).TwoLetterISOLanguageName);
        }

        public static IEnumerable<object[]> FirstDayOfWeek_Get_TestData()
        {
            yield return new object[] { DateTimeFormatInfo.InvariantInfo, DayOfWeek.Sunday };
            yield return new object[] { new CultureInfo("en-US", false).DateTimeFormat, DayOfWeek.Sunday };
            yield return new object[] { new CultureInfo("fr-FR", false).DateTimeFormat, DayOfWeek.Monday };
        }

        [Theory]
        [MemberData(nameof(FirstDayOfWeek_Get_TestData))]
        public void FirstDayOfWeek(DateTimeFormatInfo format, DayOfWeek expected)
        {
            Assert.Equal(expected, format.FirstDayOfWeek);
        }

        public static IEnumerable<object[]> CurrencyNegativePatternTestLocales()
        {
            yield return new object[] { "en-US" };
            yield return new object[] { "en-CA" };
            yield return new object[] { "fa-IR" };
            yield return new object[] { "fr-CD" };
            yield return new object[] { "fr-CA" };
        }

        [Theory]
        [MemberData(nameof(CurrencyNegativePatternTestLocales))]
        public void CurrencyNegativePattern_Get_ReturnsExpected_ByLocale(string locale)
        {
            CultureInfo culture = new CultureInfo(locale);

            NumberFormatInfo format = culture.NumberFormat;
            Assert.Contains(format.CurrencyNegativePattern, GetCurrencyNegativePatterns(locale));
        }

        internal static int[] GetCurrencyNegativePatterns(string localeName)
        {
            // CentOS uses an older ICU than Ubuntu, which means the "Linux" values need to allow for
            // multiple values, since we can't tell which version of ICU we are using, or whether we are
            // on CentOS or Ubuntu.
            // When multiple values are returned, the "older" ICU value is returned last.

            switch (localeName)
            {
                case "en-US":
                    return new int[] { 1, 0 };
                case "en-CA":
                    return new int[] { 1, 0 };
                case "fa-IR":
                        return new int[] { 1, 0 };
                case "fr-CD":
                        return new int[] { 8, 15 };
                case "as":
                    return new int[] { 9 };
                case "fr-CA":
                    return new int[] { 8, 15 };
            }
            
            return new int[] { 0 };
        }
    }
}
