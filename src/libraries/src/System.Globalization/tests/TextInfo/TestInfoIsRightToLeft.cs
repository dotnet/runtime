// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class TestInfoIsRightToLeft
    {
        // NegTest1: Verify the en-US TextInfo
        [Fact]
        public void NegTest1()
        {
            CultureInfo ci = new CultureInfo("en-US");
            TextInfo textInfoUS = ci.TextInfo;
            bool value = textInfoUS.IsRightToLeft;
            Assert.Equal(false, value);
        }

        // PosTest1: Verify the ar (Arabic) TextInfo
        [Fact]
        public void PostTest1()
        {
            CultureInfo ci = new CultureInfo("ar");
            TextInfo textInfoUS = ci.TextInfo;
            bool value = textInfoUS.IsRightToLeft;
            Assert.Equal(true, value);
        }
    }
}
