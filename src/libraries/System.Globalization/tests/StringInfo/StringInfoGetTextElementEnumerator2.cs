// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class StringInfoGetTextElementEnumerator2
    {
        private const int c_MINI_STRING_LENGTH = 8;
        private const int c_MAX_STRING_LENGTH = 256;

        // PosTest1: The argument is a random string,and start at a random index
        [Fact]
        public void TestRandomIndex()
        {
            string str = TestLibrary.Generator.GetString(-55, true, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            int len = str.Length;
            int index = this.GetInt32(8, len);
            TextElementEnumerator TextElementEnumerator = StringInfo.GetTextElementEnumerator(str, index);
            TextElementEnumerator.MoveNext();
            for (int i = 0; i < len - index; i++)
            {
                Assert.Equal(str[i + index].ToString(), TextElementEnumerator.Current.ToString());
                TextElementEnumerator.MoveNext();
            }
        }

        // PosTest2: The string has a surrogate pair
        [Fact]
        public void TestSurrogatePair()
        {
            string str = "\uDBFF\uDFFF";
            TextElementEnumerator TextElementEnumerator = StringInfo.GetTextElementEnumerator("s\uDBFF\uDFFF$", 1);
            TextElementEnumerator.MoveNext();
            Assert.Equal(str, TextElementEnumerator.Current.ToString());
            TextElementEnumerator.MoveNext();
            Assert.Equal("$", TextElementEnumerator.Current.ToString());
        }

        // PosTest3: The string has a combining character
        [Fact]
        public void TestCombiningCharacter()
        {
            string str = "a\u20D1";
            TextElementEnumerator TextElementEnumerator = StringInfo.GetTextElementEnumerator("13229^a\u20D1a", 6);
            TextElementEnumerator.MoveNext();
            Assert.Equal(str, TextElementEnumerator.Current.ToString());
            TextElementEnumerator.MoveNext();
            Assert.Equal("a", TextElementEnumerator.Current.ToString());
            Assert.False(TextElementEnumerator.MoveNext());
        }

        // NegTest1: The string is a null reference
        [Fact]
        public void TestNullReference()
        {
            string str = null;
            Assert.Throws<ArgumentNullException>(() =>
            {
                TextElementEnumerator TextElementEnumerator = StringInfo.GetTextElementEnumerator(str, 0);
            });
        }

        // NegTest2: The index is out of the range of the string
        [Fact]
        public void TestIndexOutOfRange()
        {
            string str = "dur8&p!";
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                TextElementEnumerator TextElementEnumerator = StringInfo.GetTextElementEnumerator(str, 10);
            });
        }

        // NegTest3: The index is a negative number
        [Fact]
        public void TestNegativeIndex()
        {
            string str = "dur8&p!";
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                TextElementEnumerator TextElementEnumerator = StringInfo.GetTextElementEnumerator(str, -10);
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
