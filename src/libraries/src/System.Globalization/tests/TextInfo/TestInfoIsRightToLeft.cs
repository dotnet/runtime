// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        [ActiveIssue(846, PlatformID.AnyUnix)]
        public void PostTest1()
        {
            CultureInfo ci = new CultureInfo("ar");
            TextInfo textInfoUS = ci.TextInfo;
            bool value = textInfoUS.IsRightToLeft;
            Assert.Equal(true, value);
        }
    }
}
