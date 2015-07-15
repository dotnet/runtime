// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class StringInfoGetTextElementEnumerator1
    {
        private const int c_MINI_STRING_LENGTH = 8;
        private const int c_MAX_STRING_LENGTH = 256;
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        // PosTest1: The argument is a random string
        [Fact]
        public void TestRandomString()
        {
            string str = _generator.GetString(-55, true, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            TextElementEnumerator TextElementEnumerator = StringInfo.GetTextElementEnumerator(str);
            int len = str.Length;
            TextElementEnumerator.MoveNext();
            for (int i = 0; i < len; i++)
            {
                Assert.Equal(str[i].ToString(), TextElementEnumerator.Current.ToString());
                TextElementEnumerator.MoveNext();
            }
        }

        // PosTest2: The string has a surrogate pair
        [Fact]
        public void TestSurrogatePair()
        {
            string str = "\uDBFF\uDFFF";
            TextElementEnumerator TextElementEnumerator = StringInfo.GetTextElementEnumerator("s\uDBFF\uDFFF$");
            TextElementEnumerator.MoveNext();
            Assert.Equal("s", TextElementEnumerator.Current.ToString());

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
            TextElementEnumerator TextElementEnumerator = StringInfo.GetTextElementEnumerator("13229^a\u20D1a");
            for (int i = 0; i < 7; i++)
            {
                TextElementEnumerator.MoveNext();
            }
            Assert.Equal(str, TextElementEnumerator.Current.ToString());
            TextElementEnumerator.MoveNext();

            Assert.False(TextElementEnumerator.MoveNext());
        }

        // NegTest1: The string is a null reference
        [Fact]
        public void TestNullReference()
        {
            string str = null;
            Assert.Throws<ArgumentNullException>(() =>
            {
                TextElementEnumerator TextElementEnumerator = StringInfo.GetTextElementEnumerator(str);
            });
        }
    }
}
