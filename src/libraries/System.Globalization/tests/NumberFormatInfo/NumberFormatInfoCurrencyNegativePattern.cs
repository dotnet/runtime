// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            yield return new object[] { CultureInfo.GetCultureInfo("bg-BG").NumberFormat, new int[] { 8 } };
        }

        [Theory]
        [MemberData(nameof(CurrencyNegativePattern_TestData))]
        public void CurrencyNegativePattern_Get(NumberFormatInfo format, int[] acceptablePatterns)
        {
            Assert.Contains(format.CurrencyNegativePattern, acceptablePatterns);
        }

        [Theory]
        [InlineData("en-US")]
        [InlineData("en-CA")]
        [InlineData("fa-IR")]
        [InlineData("fr-CD")]
        [InlineData("as")]
        [InlineData("es-BO")]
        [InlineData("fr-CA")]
        public void CurrencyNegativePattern_Get(string locale)
        {
            CultureInfo culture; 
            try
            {
                culture = CultureInfo.GetCultureInfo(locale);
            }
            catch(CultureNotFoundException)
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
        public void CurrencyNegativePattern_Set(int newCurrencyNegativePattern)
        {
            NumberFormatInfo format = new NumberFormatInfo();
            format.CurrencyNegativePattern = newCurrencyNegativePattern;
            Assert.Equal(newCurrencyNegativePattern, format.CurrencyNegativePattern);
        }

        [Fact]
        public void CurrencyNegativePattern_Set_Invalid()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("CurrencyNegativePattern", () => new NumberFormatInfo().CurrencyNegativePattern = -1);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("CurrencyNegativePattern", () => new NumberFormatInfo().CurrencyNegativePattern = 16);
            Assert.Throws<InvalidOperationException>(() => NumberFormatInfo.InvariantInfo.CurrencyNegativePattern = 1);
        }
    }
}
