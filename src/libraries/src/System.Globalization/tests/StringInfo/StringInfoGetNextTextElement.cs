// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class StringInfoGetNextTextElement
    {
        private const int MinStringLength = 8;
        private const int MaxStringLength = 256;
        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();
        private static readonly Random s_random = new Random(-55);

        public static IEnumerable<object[]> GetNextTextElement_TestData()
        {
            yield return new object[] { "", 0, "" }; // Empty string
            yield return new object[] { "Hello", 5, "" }; // Index = string.Length

            yield return new object[] { "\uDBFF\uDFFFabcde", 0, "\uDBFF\uDFFF" }; // Surrogate pair
            yield return new object[] { "a\u20D1abcde", 0, "a\u20D1" }; // Combining character or non spacing mark
            yield return new object[] { "z\uFE22\u20D1\u20EBabcde", 0, "z\uFE22\u20D1\u20EB" }; // Base character with several combining characters

            char randomChar = s_randomDataGenerator.GetCharLetter(-55);
            string randomString = s_randomDataGenerator.GetString(-55, false, MinStringLength, MaxStringLength);
            // If the random string's first letter is a NonSpacingMark the returned result will be ch + string's first letter (expected)
            // That scenario is hit in PosTest3. In order to avoid random failures, if the first character happens to be a NonSpacingMark
            // then append a random char letter on to the string
            if (CharUnicodeInfo.GetUnicodeCategory(randomString.ToCharArray()[0]) == UnicodeCategory.NonSpacingMark)
            {
                randomString = s_randomDataGenerator.GetCharLetter(-55).ToString() + randomString;
            }
            yield return new object[] { randomChar.ToString() + randomString, 0, randomChar.ToString() };

            yield return new object[] { "ef45-;\uDBFF\uDFFFabcde", 6, "\uDBFF\uDFFF" }; // Surrogate pair
            yield return new object[] { "az\uFE22\u20D1\u20EBabcde", 1, "z\uFE22\u20D1\u20EB" }; // Base character with several combining characters
            yield return new object[] { "13229^a\u20D1abcde", 6, "a\u20D1" }; // Combining characters

            randomString = s_randomDataGenerator.GetString(-55, true, MinStringLength, MaxStringLength);
            int randomIndex = s_random.Next(MinStringLength, randomString.Length);
            yield return new object[] { randomString, randomIndex, randomString[randomIndex].ToString() };
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
            Assert.Throws<ArgumentNullException>("str", () => StringInfo.GetNextTextElement(null)); // Str is null
            Assert.Throws<ArgumentNullException>("str", () => StringInfo.GetNextTextElement(null, 0)); // Str is null

            Assert.Throws<ArgumentOutOfRangeException>("index", () => StringInfo.GetNextTextElement("abc", -1)); // Index < 0
            Assert.Throws<ArgumentOutOfRangeException>("index", () => StringInfo.GetNextTextElement("abc", 4)); // Index > str.Length
        }
    }
}
