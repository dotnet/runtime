// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoCurrencyNegativePattern
    {
        public static IEnumerable<object[]> CurrencyNegativePattern_TestData()
        {
            yield return new object[] { NumberFormatInfo.InvariantInfo, new int[] { 0 } };
            yield return new object[] { CultureInfo.GetCultureInfo("bg-BG").NumberFormat, new int[] { 0, 8 } };
            if (PlatformDetection.IsHybridGlobalizationOnBrowser)
            {
                yield return new object[] { CultureInfo.GetCultureInfo("ar-SA").NumberFormat, new int[] { 8 } };
                yield return new object[] { CultureInfo.GetCultureInfo("am-ET").NumberFormat, new int[] { 1 } };
                yield return new object[] { CultureInfo.GetCultureInfo("bn-BD").NumberFormat, new int[] { 5 } };
                yield return new object[] { CultureInfo.GetCultureInfo("bn-IN").NumberFormat, new int[] { 5 } };
                yield return new object[] { CultureInfo.GetCultureInfo("ca-AD").NumberFormat, new int[] { 8 } }; // ca-ES, cs-CZ, da-DK, de-BE, de-DE, de-IT, de-LU, el-CY, el-GY
                yield return new object[] { CultureInfo.GetCultureInfo("de-AT").NumberFormat, new int[] { 9 } }; // de-LI
                yield return new object[] { CultureInfo.GetCultureInfo("de-CH").NumberFormat, new int[] { 2 } }; // all *-CH are 2
                yield return new object[] { CultureInfo.GetCultureInfo("en-AE").NumberFormat, new int[] { 1 } }; // all other en-* are 1
                yield return new object[] { CultureInfo.GetCultureInfo("en-CH").NumberFormat, new int[] { 2 } };
                yield return new object[] { CultureInfo.GetCultureInfo("en-DK").NumberFormat, new int[] { 8 } };
                yield return new object[] { CultureInfo.GetCultureInfo("en-SE").NumberFormat, new int[] { 8 } };
                yield return new object[] { CultureInfo.GetCultureInfo("es-ES").NumberFormat, new int[] { 8 } };
                yield return new object[] { CultureInfo.GetCultureInfo("es-MX").NumberFormat, new int[] { 1 } };
                yield return new object[] { CultureInfo.GetCultureInfo("fa-IR").NumberFormat, new int[] { 1 } };
                yield return new object[] { CultureInfo.GetCultureInfo("fi-FI").NumberFormat, new int[] { 8 } };
                yield return new object[] { CultureInfo.GetCultureInfo("fil-PH").NumberFormat, new int[] { 1 } };
                yield return new object[] { CultureInfo.GetCultureInfo("fr-BE").NumberFormat, new int[] { 8 } }; // all fr-* are 8
                yield return new object[] { CultureInfo.GetCultureInfo("no-NO").NumberFormat, new int[] { 9 } };
                yield return new object[] { CultureInfo.GetCultureInfo("nl-NL").NumberFormat, new int[] { 12 } }; // all nl-* are 12
                yield return new object[] { CultureInfo.GetCultureInfo("ru-RU").NumberFormat, new int[] { 8 } };
                yield return new object[] { CultureInfo.GetCultureInfo("sv-SE").NumberFormat, new int[] { 8 } };
                yield return new object[] { CultureInfo.GetCultureInfo("sw-UG").NumberFormat, new int[] { 9 } }; // sw-*
                yield return new object[] { CultureInfo.GetCultureInfo("ta-SG").NumberFormat, new int[] { 9 } }; // ta-*,
                yield return new object[] { CultureInfo.GetCultureInfo("uk-UA").NumberFormat, new int[] { 8 } };
                yield return new object[] { CultureInfo.GetCultureInfo("vi-VN").NumberFormat, new int[] { 8 } };
            }
        }

        [Theory]
        [MemberData(nameof(CurrencyNegativePattern_TestData))]
        public void CurrencyNegativePattern_Get_ReturnsExpected(NumberFormatInfo format, int[] acceptablePatterns)
        {
            Assert.Contains(format.CurrencyNegativePattern, acceptablePatterns);
        }

        public static IEnumerable<object[]> CurrencyNegativePatternTestLocales()
        {
            yield return new object[] { "en-US" };
            yield return new object[] { "en-CA" };
            yield return new object[] { "fa-IR" };
            yield return new object[] { "fr-CD" };
            yield return new object[] { "fr-CA" };

            if (PlatformDetection.IsNotUsingLimitedCultures)
            {
                // ICU for mobile / browser do not contain these locales
                yield return new object[] { "as" };
                yield return new object[] { "es-BO" };
            }
        }

        [Theory]
        [MemberData(nameof(CurrencyNegativePatternTestLocales))]
        public void CurrencyNegativePattern_Get_ReturnsExpected_ByLocale(string locale)
        {
            CultureInfo culture;
            try
            {
                culture = CultureInfo.GetCultureInfo(locale);
            }
            catch (CultureNotFoundException)
            {
                return; // ignore unsupported culture
            }

            NumberFormatInfo format = culture.NumberFormat;
            Assert.Contains(format.CurrencyNegativePattern, NumberFormatInfoData.GetCurrencyNegativePatterns(locale));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(15)]
        [InlineData(16)]
        public void CurrencyNegativePattern_Set_GetReturnsExpected(int newCurrencyNegativePattern)
        {
            NumberFormatInfo format = new NumberFormatInfo();
            format.CurrencyNegativePattern = newCurrencyNegativePattern;
            Assert.Equal(newCurrencyNegativePattern, format.CurrencyNegativePattern);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(17)]
        public void CurrencyNegativePattern_SetInvalid_ThrowsArgumentOutOfRangeException(int value)
        {
            var format = new NumberFormatInfo();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", "CurrencyNegativePattern", () => format.CurrencyNegativePattern = value);
        }

        [Fact]
        public void CurrencyNegativePattern_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => NumberFormatInfo.InvariantInfo.CurrencyNegativePattern = 1);
        }
    }
}
