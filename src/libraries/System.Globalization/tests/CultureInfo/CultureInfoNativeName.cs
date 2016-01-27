// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class CultureInfoNativeName
    {
        [Fact]
        public void PosTest1()
        {
            CultureInfo ci = CultureInfo.CurrentCulture;
            {
                string inFactName = ci.NativeName;
                string excepectedName = new CultureInfo(ci.Name).NativeName;
                Assert.Equal(excepectedName, inFactName);
            }
        }

        [Theory]
        [InlineData("en-US", "English (United States)")]
        [InlineData("en-CA", "English (Canada)")]
        public void TestNativeNameLocale(string locale, string expected)
        {
            CultureInfo myTestCulture = new CultureInfo(locale);
            Assert.Equal(expected, myTestCulture.NativeName);
        }
    }
}
