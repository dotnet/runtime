// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class TextInfoToString
    {
        // PosTest1: Verify the en-US TextInfo
        [Fact]
        public void VerifyEnUSTextInfo()
        {
            TextInfo textInfoUS = new CultureInfo("en-US").TextInfo;
            String textinfoStr = "TextInfo - en-US";
            Assert.Equal(textinfoStr, textInfoUS.ToString());
        }

        // PosTest2: Verify the fr-FR CultureInfo's TextInfo
        [Fact]
        public void VerifyFrFRTextInfo()
        {
            TextInfo textInfoFrance = new CultureInfo("fr-FR").TextInfo;
            String textinfoStr = "TextInfo - fr-FR";
            Assert.Equal(textinfoStr, textInfoFrance.ToString());
        }
    }
}

