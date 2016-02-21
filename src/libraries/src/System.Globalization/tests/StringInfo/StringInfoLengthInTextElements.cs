// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class StringInfoLengthInTextElements
    {
        public static IEnumerable<object[]> LengthInTextElements_TestData()
        {
            yield return new object[] { "\u4f00\u302a\ud800\udc00\u4f01", 3 };
            yield return new object[] { "abcdefgh", 8 };
            yield return new object[] { "zj\uDBFF\uDFFFlk", 5 };
            yield return new object[] { "!@#$%^&", 7 };
            yield return new object[] { "!\u20D1bo\uFE22\u20D1\u20EB|", 4 };
            yield return new object[] { "1\uDBFF\uDFFF@\uFE22\u20D1\u20EB9", 4 };
            yield return new object[] { "   ", 3 };
            yield return new object[] { "", 0 };
        }

        [Theory]
        [MemberData("LengthInTextElements_TestData")]
        public void LengthInTextElements(string value, int expected)
        {
            Assert.Equal(expected, new StringInfo(value).LengthInTextElements);
        }
    }
}
