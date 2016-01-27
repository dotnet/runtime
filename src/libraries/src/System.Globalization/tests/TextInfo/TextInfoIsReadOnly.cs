// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class TextInfoIsReadOnly
    {
        // PosTest1: Verify the new TextInfo is not readOnly
        [Fact]
        public void TestEnUSTextInfo()
        {
            CultureInfo ci = new CultureInfo("en-US");
            TextInfo textInfoUS = ci.TextInfo;

            Assert.False(textInfoUS.IsReadOnly);
        }

        // PosTest2: Verify the fr-FR CultureInfo's TextInfo
        [Fact]
        public void TestFrFRTextInfo()
        {
            CultureInfo ci = new CultureInfo("fr-FR");
            TextInfo textInfoFrance = ci.TextInfo;

            Assert.False(textInfoFrance.IsReadOnly);
        }
    }
}

