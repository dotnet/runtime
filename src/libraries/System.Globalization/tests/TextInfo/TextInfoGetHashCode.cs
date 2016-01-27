// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class TextInfoGetHashCode
    {
        // PosTest1: Verify the TextInfo equals original TextInfo
        [Fact]
        public void TestEnUSGetHashCode()
        {
            CultureInfo ci = new CultureInfo("en-US");
            CultureInfo ci2 = new CultureInfo("en-US");
            object textInfo = ci2.TextInfo;

            int originalHC = ci.TextInfo.GetHashCode();
            int clonedHC = (textInfo as TextInfo).GetHashCode();
            Assert.Equal(originalHC, clonedHC);
        }

        // PosTest2: Verify the TextInfo is not same  CultureInfo's
        [Fact]
        public void TestFrFRGetHashCode()
        {
            TextInfo textInfoFrance = new CultureInfo("fr-FR").TextInfo;
            TextInfo textInfoUS = new CultureInfo("en-US").TextInfo;

            int franceHashCode = textInfoFrance.GetHashCode();
            int usHashCode = textInfoUS.GetHashCode();
            Assert.False(franceHashCode == usHashCode);
        }
    }
}

