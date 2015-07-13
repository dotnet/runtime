// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class StringInfoGetNextTextElement1
    {
        private const int c_MINI_STRING_LENGTH = 8;
        private const int c_MAX_STRING_LENGTH = 256;

        // PosTest1: The first text element is a letter
        [Fact]
        public void TestLetter()
        {
            char ch = TestLibrary.Generator.GetCharLetter(-55);
            string str = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            // If the random string's first letter is a NonSpacingMark the returned result will be ch + string's first letter (expected)
            // That scenario is hit in PosTest3. In order to avoid random failures, if the first character happens to be a NonSpacingMark
            // then append a random char letter on to the string
            if (CharUnicodeInfo.GetUnicodeCategory(str.ToCharArray()[0]) == UnicodeCategory.NonSpacingMark)
            {
                str = TestLibrary.Generator.GetCharLetter(-55).ToString() + str;
            }
            string result = StringInfo.GetNextTextElement(ch.ToString() + str);
            Assert.Equal(ch.ToString(), result);
        }

        // PosTest2: The first element is a surrogate pair
        [Fact]
        public void TestSurrogatePair()
        {
            string result = StringInfo.GetNextTextElement("\uDBFF\uDFFFabcde");
            Assert.Equal(2, result.Length);
        }

        // PosTest3: The element is a combining character or NonSpacingMark
        [Fact]
        public void TestNonSpacingMark()
        {
            string str = "a\u20D1";
            string result = StringInfo.GetNextTextElement("a\u20D1abcde");
            Assert.Equal(str, result);
        }

        // PosTest4: The element is a combination of base character and several combining characters
        [Fact]
        public void TestCharCombination()
        {
            string str = "z\uFE22\u20D1\u20EB";
            string result = StringInfo.GetNextTextElement("z\uFE22\u20D1\u20EBabcde");
            Assert.Equal(4, result.Length);
            Assert.Equal(str, result);
        }

        // NegTest1: The string is a null reference
        [Fact]
        public void TestNullReference()
        {
            string str = null;
            Assert.Throws<ArgumentNullException>(() =>
            {
                string result = StringInfo.GetNextTextElement(str);
            });
        }
    }
}
