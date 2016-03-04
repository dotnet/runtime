// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class StringInfoGetTextElementEnumerator
    {
        private const int MinStringLength = 8;
        private const int MaxStringLength = 256;
        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();
        private static readonly Random s_Random = new Random(-55);

        public static IEnumerable<object[]> GetTextElementEnumerator_TestData()
        {
            yield return new object[] { "", 0, new string[0] }; // Empty string
            yield return new object[] { "Hello", 5, new string[0] }; // Index = string.Length

            yield return new object[] { "s\uDBFF\uDFFF$", 0, new string[] { "s", "\uDBFF\uDFFF", "$" } }; // Surrogate pair
            yield return new object[] { "13229^a\u20D1a", 0, new string[] { "1", "3", "2", "2", "9", "^", "a\u20D1", "a" } }; // Combining characters

            string randomString = s_randomDataGenerator.GetString(-55, true, MinStringLength, MaxStringLength);
            string[] randomExpected = new string[randomString.Length];
            for (int i = 0; i < randomExpected.Length; i++)
            {
                randomExpected[i] = randomString[i].ToString();
            }
            yield return new object[] { randomString, 0, randomExpected };

            yield return new object[] { "s\uDBFF\uDFFF$", 1, new string[] { "\uDBFF\uDFFF", "$" } }; // Surrogate pair
            yield return new object[] { "13229^a\u20D1a", 6, new string[] { "a\u20D1", "a" } }; // Combining characters

            randomString = s_randomDataGenerator.GetString(-55, true, MinStringLength, MaxStringLength);
            int randomIndex = s_Random.Next(MinStringLength, randomString.Length);
            randomExpected = new string[randomString.Length - randomIndex];
            for (int i = 0; i < randomExpected.Length; i++)
            {
                randomExpected[i] = randomString[i + randomIndex].ToString();
            }
            yield return new object[] { randomString, randomIndex, randomExpected };
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
            Assert.Throws<ArgumentNullException>("str", () => StringInfo.GetTextElementEnumerator(null)); // Str is null
            Assert.Throws<ArgumentNullException>("str", () => StringInfo.GetTextElementEnumerator(null, 0)); // Str is null

            Assert.Throws<ArgumentOutOfRangeException>("index", () => StringInfo.GetTextElementEnumerator("abc", -1)); // Index < 0
            Assert.Throws<ArgumentOutOfRangeException>("index", () => StringInfo.GetTextElementEnumerator("abc", 4)); // Index > str.Length
        }
    }
}
