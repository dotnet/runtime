// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoNegativeInfinitySymbol
    {
        // PosTest1: Verify value of property NegativeInfinitySymbol for specific locales
        [Theory]
        [InlineData("en-US", "-Infinity", "-\u221E" )] //todo: Win10 has ICU semantics here so combine
        [InlineData("fr-FR", "-Infini", "-\u221E")] //todo: Win10 has ICU semantics here so combine
        public void PosTest1(string locale, string expectedWindows, string expectedIcu)
        {
            CultureInfo myTestCulture = new CultureInfo(locale);
            NumberFormatInfo nfi = myTestCulture.NumberFormat;

            string expected = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? expectedWindows : expectedIcu;

            Assert.Equal(expected, nfi.NegativeInfinitySymbol);
        }
    }
}
