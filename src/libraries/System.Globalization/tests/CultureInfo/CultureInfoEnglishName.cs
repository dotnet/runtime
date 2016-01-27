// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
