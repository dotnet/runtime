// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class StringInfoGetNextTextElement2
    {
        private const int c_MINI_STRING_LENGTH = 8;
        private const int c_MAX_STRING_LENGTH = 256;

        // PosTest1: Get the text element from a random index in a string
        [Fact]
        public void TestRandomIndex()
        {
            string str = TestLibrary.Generator.GetString(-55, true, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            int index = this.GetInt32(8, str.Length);
            string result = StringInfo.GetNextTextElement(str, index);
            Assert.Equal(str[index].ToString(), result);
        }

        // PosTest2: The first element is a surrogate pair
        [Fact]
        public void TestSurrogatePair()
        {
            string str = "\uDBFF\uDFFF";
            string result = StringInfo.GetNextTextElement("ef45-;\uDBFF\uDFFFabcde", 6);
            Assert.Equal(2, result.Length);
            Assert.Equal(str, result);
        }

        // PosTest3: The element is  a combining character
        [Fact]
        public void TestCombiningCharacter()
        {
            string str = "a\u20D1";
            string result = StringInfo.GetNextTextElement("13229^a\u20D1abcde", 6);
            Assert.Equal(str, result);
        }

        // PosTest4: The element is a combination of base character and several combining characters
        [Fact]
        public void TestCharacterCombination()
        {
            string str = "z\uFE22\u20D1\u20EB";
            string result = StringInfo.GetNextTextElement("az\uFE22\u20D1\u20EBabcde", 1);
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
                string result = StringInfo.GetNextTextElement(str, 0);
            });
        }

        // NegTest2: The index is out of the range of the string
        [Fact]
        public void TestOutOfRangeIndex()
        {
            string str = "abc";
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                string result = StringInfo.GetNextTextElement(str, -4);
            });
        }

        // NegTest3: The index is a negative number
        [Fact]
        public void TestNegativeIndex()
        {
            string str = "df8%^dk";
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                string result = StringInfo.GetNextTextElement(str, -1);
            });
        }

        private Int32 GetInt32(Int32 minValue, Int32 maxValue)
        {
            try
            {
                if (minValue == maxValue)
                {
                    return minValue;
                }
                if (minValue < maxValue)
                {
                    return minValue + TestLibrary.Generator.GetInt32(-55) % (maxValue - minValue);
                }
            }
            catch
            {
                throw;
            }

            return minValue;
        }
    }
}
