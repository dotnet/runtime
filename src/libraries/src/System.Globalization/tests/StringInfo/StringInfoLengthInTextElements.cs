// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class StringInfoLengthInTextElements
    {
        // PosTest1: The mothod should return the number of base character in the current stringInfo object
        [Fact]
        public void PosTest1()
        {
            VerificationHelper("\u4f00\u302a\ud800\udc00\u4f01", 3);
            VerificationHelper("abcdefgh", 8);
            VerificationHelper("zj\uDBFF\uDFFFlk", 5);
            VerificationHelper("!@#$%^&", 7);
            VerificationHelper("!\u20D1bo\uFE22\u20D1\u20EB|", 4);
            VerificationHelper("1\uDBFF\uDFFF@\uFE22\u20D1\u20EB9", 4);
        }

        // PosTest2: The string in stringinfo is white space or empty string
        [Fact]
        public void PosTest2()
        {
            VerificationHelper("   ", 3);
            VerificationHelper(string.Empty, 0);
        }

        private void VerificationHelper(string str, int expected)
        {
            StringInfo stringInfo = new StringInfo(str);
            int result = stringInfo.LengthInTextElements;
            Assert.Equal(expected, result);
        }
    }
}
