// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class StringInfoParseCombiningCharacters
    {
        public static IEnumerable<object[]> ParseCombiningCharacters_TestData()
        {
            yield return new object[] { "\u4f00\u302a\ud800\udc00\u4f01", new int[] { 0, 2, 4 } };
            yield return new object[] { "abcdefgh", new int[] { 0, 1, 2, 3, 4, 5, 6, 7 } };
            yield return new object[] { "!@#$%^&", new int[] { 0, 1, 2, 3, 4, 5, 6 } };
            yield return new object[] { "!\u20D1bo\uFE22\u20D1\u20EB|", new int[] { 0, 2, 3, 7 } };
            yield return new object[] { "1\uDBFF\uDFFF@\uFE22\u20D1\u20EB9", new int[] { 0, 1, 3, 7 } };
            yield return new object[] { "   ", new int[] { 0, 1, 2 } };
            yield return new object[] { "", new int[0] };
        }

        [Theory]
        [MemberData("ParseCombiningCharacters_TestData")]
        public void ParseCombiningCharacters(string str, int[] expected)
        {
            Assert.Equal(expected, StringInfo.ParseCombiningCharacters(str));
        }
        
        [Fact]
        public void ParseCombiningCharacters_Invalid()
        {
            Assert.Throws<ArgumentNullException>(() => StringInfo.ParseCombiningCharacters(null)); // Str is null
        }
    }
}
