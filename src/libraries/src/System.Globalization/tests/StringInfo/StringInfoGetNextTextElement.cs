// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class StringInfoGetNextTextElement
    {
        public static IEnumerable<object[]> GetNextTextElement_TestData()
        {
            yield return new object[] { "", 0, "" }; // Empty string
            yield return new object[] { "Hello", 5, "" }; // Index = string.Length

            // Surrogate pair
            yield return new object[] { "\uDBFF\uDFFFabcde", 0, "\uDBFF\uDFFF" };
            yield return new object[] { "ef45-;\uDBFF\uDFFFabcde", 6, "\uDBFF\uDFFF" };

            yield return new object[] { "a\u20D1abcde", 0, "a\u20D1" }; // Combining character or non spacing mark

            // Base character with several combining characters
            yield return new object[] { "z\uFE22\u20D1\u20EBabcde", 0, "z\uFE22\u20D1\u20EB" };
            yield return new object[] { "az\uFE22\u20D1\u20EBabcde", 1, "z\uFE22\u20D1\u20EB" };

            yield return new object[] { "13229^a\u20D1abcde", 6, "a\u20D1" }; // Combining characters

            // Single base and combining character
            yield return new object[] { "a\u0300", 0, "a\u0300" };
            yield return new object[] { "a\u0300", 1, "\u0300" };

            // Lone combining character
            yield return new object[] { "\u0300\u0300", 0, "\u0300" };
            yield return new object[] { "\u0300\u0300", 1, "\u0300" };
        }
        
        [Theory]
        [MemberData(nameof(GetNextTextElement_TestData))]
        public void GetNextTextElement(string str, int index, string expected)
        {
            if (index == 0)
            {
                Assert.Equal(expected, StringInfo.GetNextTextElement(str));
            }
            Assert.Equal(expected, StringInfo.GetNextTextElement(str, index));
        }
        
        [Fact]
        public void GetNextTextElement_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("str", () => StringInfo.GetNextTextElement(null)); // Str is null
            AssertExtensions.Throws<ArgumentNullException>("str", () => StringInfo.GetNextTextElement(null, 0)); // Str is null

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => StringInfo.GetNextTextElement("abc", -1)); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => StringInfo.GetNextTextElement("abc", 4)); // Index > str.Length
        }
    }
}
