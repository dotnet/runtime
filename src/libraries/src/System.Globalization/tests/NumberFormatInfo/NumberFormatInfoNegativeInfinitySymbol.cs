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
        [InlineData("en-US", "-Infinity", "-\u221E" )]
        [InlineData("fr-FR", "-Infini", "-\u221E")]
        public void PosTest1(string locale, string expectedWindows, string expectedIcu)
        {
            CultureInfo myTestCulture = new CultureInfo(locale);
            NumberFormatInfo nfi = myTestCulture.NumberFormat;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows 10 uses ICU data here, this should be cleaned up as part of #3243
                Assert.True(nfi.NegativeInfinitySymbol == expectedWindows || nfi.NegativeInfinitySymbol == expectedIcu);
            }
            else
            {
                Assert.Equal(expectedIcu, nfi.NegativeInfinitySymbol);
            }
        }
    }
}
