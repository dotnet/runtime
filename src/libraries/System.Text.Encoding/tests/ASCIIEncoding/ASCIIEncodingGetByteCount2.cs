// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    // Calculates the number of bytes produced 
    // by encoding the characters in the specified character array. 
    public class ASCIIEncodingGetByteCount2
    {
        private const int c_MAX_ARRAY_LENGTH = 260;

        // PosTest1: The specified character array is zero-length array.
        [Fact]
        public void PosTest1()
        {
            DoPosTest(new char[0], 0, 0, 0);
        }

        // PosTest2: The specified character array, start index and count of encoding character are all random.
        [Fact]
        public void PosTest2()
        {
            char[] chars;
            int startIndex, count;
            int expectedValue;

            InitializeCharacterArray(out chars);
            startIndex = TestLibrary.Generator.GetInt32(-55) % chars.Length;
            count = TestLibrary.Generator.GetInt32(-55) % (chars.Length - startIndex) + 1;
            expectedValue = count;
            DoPosTest(chars, startIndex, count,
                            expectedValue);
        }

        private void DoPosTest(char[] chars, int startIndex, int count, int expectedValue)
        {
            ASCIIEncoding ascii;
            int actualValue;

            ascii = new ASCIIEncoding();
            actualValue = ascii.GetByteCount(chars, startIndex, count);
            Assert.Equal(expectedValue, actualValue);
        }

        private string GetCharArrayInfo(char[] chars, int startIndex, int count)
        {
            StringBuilder sb = new StringBuilder();

            if (null == chars) return string.Empty;
            if (0 == chars.Length)
            {
                sb.Append("\nThe character array is zero-length array: {}");
            }
            else
            {
                sb.Append("\nThe character array is: {");
                for (int i = 0; i < chars.Length; ++i)
                {
                    if (0 == (i & 0xf)) sb.Append("\n");
                    sb.AppendFormat("\t\\u{0:X04}, ", (int)chars[i]);
                }
                sb.Append("}");
            }
            sb.AppendFormat("\nThe length of character array: {0}", chars.Length);
            sb.AppendFormat("\nStart index for encoding: {0}\nCount of character encoded: {1}",
                            startIndex, count);

            return sb.ToString();
        }

        // NegTest1: character array is a null reference (Nothing in Visual Basic).
        [Fact]
        public void NegTest1()
        {
            ASCIIEncoding ascii;
            char[] chars = null;

            ascii = new ASCIIEncoding();
            Assert.Throws<ArgumentNullException>(() =>
            {
                ascii.GetByteCount(chars, 0, 0);
            });
        }

        // NegTest2: Start index is less than zero.
        [Fact]
        public void NegTest2()
        {
            char[] chars;
            int startIndex, count;

            InitializeCharacterArray(out chars);
            startIndex = -1 * TestLibrary.Generator.GetInt32(-55) - 1;
            count = TestLibrary.Generator.GetInt32(-55) % chars.Length + 1;

            DoNegAOORTest(chars, startIndex, count);
        }

        // NegTest3: Count of character encoded is less than zero.
        [Fact]
        public void NegTest3()
        {
            char[] chars;
            int startIndex, count;

            InitializeCharacterArray(out chars);
            startIndex = TestLibrary.Generator.GetInt32(-55) % chars.Length;
            count = -1 * TestLibrary.Generator.GetInt32(-55) - 1;

            DoNegAOORTest(chars, startIndex, count);
        }

        // NegTest4: index is greater than or equal the length of character array.
        [Fact]
        public void NegTest4()
        {
            char[] chars;
            int startIndex, count;

            InitializeCharacterArray(out chars);
            startIndex = chars.Length + TestLibrary.Generator.GetInt32(-55) % (Int32.MaxValue - chars.Length);
            count = TestLibrary.Generator.GetInt32(-55) % chars.Length + 1;

            DoNegAOORTest(chars, startIndex, count);
        }

        // NegTest5: count is greater than the length of character array.
        [Fact]
        public void NegTest5()
        {
            char[] chars;
            int startIndex, count;

            InitializeCharacterArray(out chars);
            startIndex = TestLibrary.Generator.GetInt32(-55) % chars.Length;
            count = chars.Length + 1 + TestLibrary.Generator.GetInt32(-55) % (Int32.MaxValue - chars.Length);

            DoNegAOORTest(chars, startIndex, count);
        }

        private void DoNegAOORTest(char[] chars, int startIndex, int count)
        {
            ASCIIEncoding ascii = new ASCIIEncoding();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
           {
               ascii.GetByteCount(chars, startIndex, count);
           });
        }

        //Initialize the character array using random values
        private void InitializeCharacterArray(out char[] chars)
        {
            //Get a character array whose length is beween 1 and c_MAX_ARRAY_LENGTH
            int length = TestLibrary.Generator.GetInt32(-55) % c_MAX_ARRAY_LENGTH + 1;
            chars = new char[length];
            for (int i = 0; i < chars.Length; ++i)
            {
                chars[i] = TestLibrary.Generator.GetChar(-55);
            }
        }
    }
}
