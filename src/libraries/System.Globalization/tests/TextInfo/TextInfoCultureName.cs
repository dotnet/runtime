// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class TextInfoCultureName
    {
        // PosTest1: Verify the en-US TextInfo
        [Fact]
        public void TestEnUSTextInfo()
        {
            CultureInfo ci = new CultureInfo("en-US");
            TextInfo textInfoUS = ci.TextInfo;
            String cultureName = ci.Name;
            Assert.Equal(cultureName, textInfoUS.CultureName);
        }

        // PosTest2: Verify the fr-FR CultureInfo's TextInfo
        [Fact]
        public void TestFrFRTextInfo()
        {
            CultureInfo ci = new CultureInfo("fr-FR");
            TextInfo textInfoFrance = ci.TextInfo;
            String cultureName = ci.Name;

            Assert.Equal(cultureName, textInfoFrance.CultureName);
        }
    }
}

