// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoPercentNegativePattern
    {
        public static IEnumerable<object[]> PercentNegativePattern_TestData()
        {
            yield return new object[] { CultureInfo.GetCultureInfo("en-US").NumberFormat, 1 };
            yield return new object[] { CultureInfo.GetCultureInfo("en-MY").NumberFormat, 1 };
            yield return new object[] { CultureInfo.GetCultureInfo("tr").NumberFormat, 2 };
            if (PlatformDetection.IsHybridGlobalizationOnBrowser)
            {
                yield return new object[] { CultureInfo.GetCultureInfo("ar-SA").NumberFormat, 1 };
                yield return new object[] { CultureInfo.GetCultureInfo("cs-CZ").NumberFormat, 0 };
                yield return new object[] { CultureInfo.GetCultureInfo("de-CH").NumberFormat, 1 };
                yield return new object[] { CultureInfo.GetCultureInfo("de-DE").NumberFormat, 0 };
                yield return new object[] { CultureInfo.GetCultureInfo("de-LI").NumberFormat, 1 };
                yield return new object[] { CultureInfo.GetCultureInfo("de-LU").NumberFormat, 0 };
                yield return new object[] { CultureInfo.GetCultureInfo("el-CY").NumberFormat, 1 };
                yield return new object[] { CultureInfo.GetCultureInfo("en-AT").NumberFormat, 0 };
                yield return new object[] { CultureInfo.GetCultureInfo("en-AU").NumberFormat, 1 };
                yield return new object[] { CultureInfo.GetCultureInfo("en-DE").NumberFormat, 0 };
                yield return new object[] { CultureInfo.GetCultureInfo("en-DM").NumberFormat, 1 };
                yield return new object[] { CultureInfo.GetCultureInfo("en-FI").NumberFormat, 0 };
                yield return new object[] { CultureInfo.GetCultureInfo("en-FJ").NumberFormat, 1 };
                yield return new object[] { CultureInfo.GetCultureInfo("en-SE").NumberFormat, 0 };
                yield return new object[] { CultureInfo.GetCultureInfo("en-SG").NumberFormat, 1 };
                yield return new object[] { CultureInfo.GetCultureInfo("es-ES").NumberFormat, 0 };
                yield return new object[] { CultureInfo.GetCultureInfo("et-EE").NumberFormat, 1 };
                yield return new object[] { CultureInfo.GetCultureInfo("fi-FI").NumberFormat, 0 };
                yield return new object[] { CultureInfo.GetCultureInfo("fil-PH").NumberFormat, 1 };
                yield return new object[] { CultureInfo.GetCultureInfo("fr-BE").NumberFormat, 0 };
                yield return new object[] { CultureInfo.GetCultureInfo("fr-CH").NumberFormat, 1 };
                yield return new object[] { CultureInfo.GetCultureInfo("fr-FR").NumberFormat, 0 };
                yield return new object[] { CultureInfo.GetCultureInfo("gu-IN").NumberFormat, 1 };
                yield return new object[] { CultureInfo.GetCultureInfo("hr-BA").NumberFormat, 0 };
                yield return new object[] { CultureInfo.GetCultureInfo("hu-HU").NumberFormat, 1 };
                yield return new object[] { CultureInfo.GetCultureInfo("lt-LT").NumberFormat, 0 };
                yield return new object[] { CultureInfo.GetCultureInfo("lv-LV").NumberFormat, 1 };
                yield return new object[] { CultureInfo.GetCultureInfo("nb-NO").NumberFormat, 0 };
                yield return new object[] { CultureInfo.GetCultureInfo("nl-AW").NumberFormat, 1 };
                yield return new object[] { CultureInfo.GetCultureInfo("ro-RO").NumberFormat, 0 };
                yield return new object[] { CultureInfo.GetCultureInfo("sr-Cyrl-RS").NumberFormat, 1 };
                yield return new object[] { CultureInfo.GetCultureInfo("sv-AX").NumberFormat, 0 };
                yield return new object[] { CultureInfo.GetCultureInfo("sw-CD").NumberFormat, 1 };
            }
        }

        /// <summary>
        /// Not testing for NLS as the culture data can change
        /// https://blogs.msdn.microsoft.com/shawnste/2005/04/05/culture-data-shouldnt-be-considered-stable-except-for-invariant/
        /// In the CultureInfoAll test class we are testing the expected behavior
        /// for NLS by enumerating all locales on the system and then test them.
        /// </summary>
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsIcuGlobalization))]
        [MemberData(nameof(PercentNegativePattern_TestData))]
        public void PercentNegativePattern_Get_ReturnsExpected_ICU(NumberFormatInfo format, int expected)
        {
            Assert.Equal(expected, format.PercentNegativePattern);
        }

        [Fact]
        public void PercentNegativePattern_GetInvariant_ReturnsExpected()
        {
            Assert.Equal(0, NumberFormatInfo.InvariantInfo.PercentNegativePattern);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(11)]
        public void PercentNegativePattern_Set_GetReturnsExpected(int newPercentNegativePattern)
        {
            NumberFormatInfo format = new NumberFormatInfo();
            format.PercentNegativePattern = newPercentNegativePattern;
            Assert.Equal(newPercentNegativePattern, format.PercentNegativePattern);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(12)]
        public void PercentNegativePattern_SetInvalid_ThrowsArgumentOutOfRangeException(int value)
        {
            var format = new NumberFormatInfo();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", "PercentNegativePattern", () => format.PercentNegativePattern = value);
        }

        [Fact]
        public void PercentNegativePattern_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => NumberFormatInfo.InvariantInfo.PercentNegativePattern = 1);
        }
    }
}
