// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoNegativeInfinitySymbol
    {
        // PosTest1: Verify value of property NegativeInfinitySymbol for specific locales
        [Theory]
        [InlineData("en-US")]
        [InlineData("fr-FR")]
        public void PosTest1(string locale)
        {
            CultureInfo myTestCulture = new CultureInfo(locale);
            NumberFormatInfo nfi = myTestCulture.NumberFormat;
            string actual = nfi.NegativeInfinitySymbol;
            string expected = NumberFormatInfoData.GetNegativeInfinitySymbol(myTestCulture);

            Assert.Equal(expected, actual);
        }
    }
}
