// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoGetInstance
    {
        public static IEnumerable<object[]> GetInstance_TestData()
        {
            CultureInfo frFRCulture = CultureInfo.GetCultureInfo("fr-FR");
            yield return new object[] { frFRCulture, frFRCulture.NumberFormat };
            yield return new object[] { frFRCulture.NumberFormat, frFRCulture.NumberFormat };
            yield return new object[] { new CustomFormatProvider(), CustomFormatProvider.CustomFormat };

            yield return new object[] { new InvalidFormatProvider(), NumberFormatInfo.CurrentInfo };
            yield return new object[] { null, NumberFormatInfo.CurrentInfo };
        }

        [Theory]
        [MemberData(nameof(GetInstance_TestData))]
        public void GetInstance(IFormatProvider formatProvider, NumberFormatInfo expected)
        {
            NumberFormatInfo nfi = NumberFormatInfo.GetInstance(formatProvider);

            Assert.Equal(expected.CurrencyDecimalDigits, nfi.CurrencyDecimalDigits);
            Assert.Equal(expected.CurrencyDecimalSeparator, nfi.CurrencyDecimalSeparator);
            Assert.Equal(expected.CurrencyGroupSeparator, nfi.CurrencyGroupSeparator);
            Assert.Equal(expected.CurrencyGroupSizes, nfi.CurrencyGroupSizes);
            Assert.Equal(expected.CurrencyNegativePattern, nfi.CurrencyNegativePattern);
            Assert.Equal(expected.CurrencyPositivePattern, nfi.CurrencyPositivePattern);
            Assert.Equal(expected.CurrencySymbol, nfi.CurrencySymbol);
            Assert.Equal(expected.NaNSymbol, nfi.NaNSymbol);
            Assert.Equal(expected.NegativeInfinitySymbol, nfi.NegativeInfinitySymbol);
            Assert.Equal(expected.NegativeSign, nfi.NegativeSign);
            Assert.Equal(expected.NumberDecimalDigits, nfi.NumberDecimalDigits);
            Assert.Equal(expected.NumberDecimalSeparator, nfi.NumberDecimalSeparator);
            Assert.Equal(expected.NumberGroupSeparator, nfi.NumberGroupSeparator);
            Assert.Equal(expected.NumberGroupSizes, nfi.NumberGroupSizes);
            Assert.Equal(expected.NumberNegativePattern, nfi.NumberNegativePattern);
            Assert.Equal(expected.PercentDecimalDigits, nfi.PercentDecimalDigits);
            Assert.Equal(expected.PercentDecimalSeparator, nfi.PercentDecimalSeparator);
            Assert.Equal(expected.PercentGroupSeparator, nfi.PercentGroupSeparator);
            Assert.Equal(expected.PercentGroupSizes, nfi.PercentGroupSizes);
            Assert.Equal(expected.PercentNegativePattern, nfi.PercentNegativePattern);
            Assert.Equal(expected.PercentPositivePattern, nfi.PercentPositivePattern);
            Assert.Equal(expected.PercentSymbol, nfi.PercentSymbol);
            Assert.Equal(expected.PositiveInfinitySymbol, nfi.PositiveInfinitySymbol);
            Assert.Equal(expected.PerMilleSymbol, nfi.PerMilleSymbol);
            Assert.Equal(expected.PositiveSign, nfi.PositiveSign);
        }

        private class CustomFormatProvider : IFormatProvider
        {
            public static NumberFormatInfo CustomFormat { get; } = CultureInfo.GetCultureInfo("fr-FR").NumberFormat;

            public object GetFormat(Type formatType) => CustomFormat;
        }

        private class InvalidFormatProvider : IFormatProvider
        {
            public object GetFormat(Type formatType) => "hello";
        }
    }
}
