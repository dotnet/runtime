// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class CultureInfoEnglishName
    {
        [Fact]
        public void PosTest1()
        {
            CultureInfo ci = CultureInfo.CurrentCulture;

            string inFactenglish = ci.EnglishName;
            string excepectedName = new CultureInfo(ci.Name).EnglishName;
            Assert.Equal(excepectedName, inFactenglish);
        }

        [Theory]
        [InlineData("en-US", "English (United States)")]
        [InlineData("fr-FR", "French (France)")]
        public void TestEnglishNameLocale(string locale, string expected)
        {
            CultureInfo myTestCulture = new CultureInfo(locale);
            Assert.Equal(expected, myTestCulture.EnglishName);
        }
    }
}
