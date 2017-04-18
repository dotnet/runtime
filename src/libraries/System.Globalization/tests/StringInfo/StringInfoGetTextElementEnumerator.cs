// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class StringInfoGetTextElementEnumerator
    {
        public static IEnumerable<object[]> GetTextElementEnumerator_TestData()
        {
            yield return new object[] { "", 0, new string[0] }; // Empty string
            yield return new object[] { "Hello", 5, new string[0] }; // Index = string.Length

            // Surrogate pair
            yield return new object[] { "s\uDBFF\uDFFF$", 0, new string[] { "s", "\uDBFF\uDFFF", "$" } };
            yield return new object[] { "s\uDBFF\uDFFF$", 1, new string[] { "\uDBFF\uDFFF", "$" } };

            // Combining characters
            yield return new object[] { "13229^a\u20D1a", 6, new string[] { "a\u20D1", "a" } };
            yield return new object[] { "13229^a\u20D1a", 0, new string[] { "1", "3", "2", "2", "9", "^", "a\u20D1", "a" } };

            // Single base and combining character
            yield return new object[] { "a\u0300", 0, new string[] { "a\u0300" } };
            yield return new object[] { "a\u0300", 1, new string[] { "\u0300" } };

            // Lone combining character
            yield return new object[] { "\u0300\u0300", 0, new string[] { "\u0300", "\u0300" } };
        }

        [Theory]
        [MemberData(nameof(GetTextElementEnumerator_TestData))]
        public void GetTextElementEnumerator(string str, int index, string[] expected)
        {
            if (index == 0)
            {
                TextElementEnumerator basicEnumerator = StringInfo.GetTextElementEnumerator(str);
                int basicCounter = 0;
                while (basicEnumerator.MoveNext())
                {
                    Assert.Equal(expected[basicCounter], basicEnumerator.Current.ToString());
                    basicCounter++;
                }
                Assert.Equal(expected.Length, basicCounter);
            }
            TextElementEnumerator indexedEnumerator = StringInfo.GetTextElementEnumerator(str, index);
            int indexedCounter = 0;
            while (indexedEnumerator.MoveNext())
            {
                Assert.Equal(expected[indexedCounter], indexedEnumerator.Current.ToString());
                indexedCounter++;
            }
            Assert.Equal(expected.Length, indexedCounter);
        }

        [Fact]
        public void GetTextElementEnumerator_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("str", () => StringInfo.GetTextElementEnumerator(null)); // Str is null
            AssertExtensions.Throws<ArgumentNullException>("str", () => StringInfo.GetTextElementEnumerator(null, 0)); // Str is null

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => StringInfo.GetTextElementEnumerator("abc", -1)); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => StringInfo.GetTextElementEnumerator("abc", 4)); // Index > str.Length
        }
    }
}
