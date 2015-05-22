// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    // Calculates the number of characters produced 
    // by decoding a sequence of bytes from the specified byte array.   
    // ASCIIEncoding.GetCharCount(byte[], int, int)
    public class ASCIIEncodingGetCharCount
    {
        private const int c_MIN_STRING_LENGTH = 2;
        private const int c_MAX_STRING_LENGTH = 260;

        // PosTest1: zero-length byte array.
        [Fact]
        public void PosTest1()
        {
            DoPosTest(new ASCIIEncoding(), new byte[0], 0, 0);
        }

        // PosTest2: random byte array.
        [Fact]
        public void PosTest2()
        {
            ASCIIEncoding ascii;
            byte[] bytes;
            int index, count;
            string source;

            ascii = new ASCIIEncoding();
            source = TestLibrary.Generator.GetString(-55, true, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);
            bytes = ascii.GetBytes(source);
            index = TestLibrary.Generator.GetInt32(-55) % bytes.Length;
            count = TestLibrary.Generator.GetInt32(-55) % (bytes.Length - index) + 1;

            // ensure that count <= bytes.Length
            if (count > bytes.Length)
            {
                count = bytes.Length;
            }
            DoPosTest(ascii, bytes, index, count);
        }

        private void DoPosTest(ASCIIEncoding ascii, byte[] bytes, int index, int count)
        {
            int actualValue;
            ascii = new ASCIIEncoding();
            actualValue = ascii.GetCharCount(bytes, index, count);
            Assert.Equal(count, actualValue);
        }

        // NegTest1: count of bytes is less than zero.
        [Fact]
        public void NegTest1()
        {
            ASCIIEncoding ascii;
            byte[] bytes;
            int index, count;

            ascii = new ASCIIEncoding();
            bytes = new byte[]
            {
             65,  83,  67,  73,  73,  32,  69,
            110,  99, 111, 100, 105, 110, 103,
             32,  69, 120,  97, 109, 112, 108
            };

            count = -1 * TestLibrary.Generator.GetInt32(-55) - 1;
            index = TestLibrary.Generator.GetInt32(-55) % bytes.Length;

            DoNegAOORTest(ascii, bytes, index, count);
        }

        // NegTest2: The start index of bytes is less than zero.
        [Fact]
        public void NegTest2()
        {
            ASCIIEncoding ascii;
            byte[] bytes;
            int index, count;

            ascii = new ASCIIEncoding();
            bytes = new byte[]
            {
             65,  83,  67,  73,  73,  32,  69,
            110,  99, 111, 100, 105, 110, 103,
             32,  69, 120,  97, 109, 112, 108
            };
            count = TestLibrary.Generator.GetInt32(-55) % bytes.Length + 1;
            index = -1 * TestLibrary.Generator.GetInt32(-55) - 1;

            DoNegAOORTest(ascii, bytes, index, count);
        }

        // NegTest3: count of bytes is too large.
        [Fact]
        public void NegTest3()
        {
            ASCIIEncoding ascii;
            byte[] bytes;
            int index, count;

            ascii = new ASCIIEncoding();
            bytes = new byte[]
            {
             65,  83,  67,  73,  73,  32,  69,
            110,  99, 111, 100, 105, 110, 103,
             32,  69, 120,  97, 109, 112, 108
            };
            index = TestLibrary.Generator.GetInt32(-55) % bytes.Length;
            count = bytes.Length - index + 1 +
                TestLibrary.Generator.GetInt32(-55) % (int.MaxValue - bytes.Length + index);

            DoNegAOORTest(ascii, bytes, index, count);
        }

        // NegTest4: The start index of bytes is too large.
        [Fact]
        public void NegTest4()
        {
            ASCIIEncoding ascii;
            byte[] bytes;
            int index, count;

            ascii = new ASCIIEncoding();
            bytes = new byte[]
            {
             65,  83,  67,  73,  73,  32,  69,
            110,  99, 111, 100, 105, 110, 103,
             32,  69, 120,  97, 109, 112, 108
            };
            count = TestLibrary.Generator.GetInt32(-55) % bytes.Length + 1;
            index = bytes.Length - count + 1 +
                TestLibrary.Generator.GetInt32(-55) % (int.MaxValue - bytes.Length + count);

            DoNegAOORTest(ascii, bytes, index, count);
        }

        // NegTest5: bytes is a null reference
        [Fact]
        public void NegTest5()
        {
            ASCIIEncoding ascii = new ASCIIEncoding();
            byte[] bytes = null;
            Assert.Throws<ArgumentNullException>(() =>
           {
               ascii.GetCharCount(bytes, 0, 0);
           });
        }

        private void DoNegAOORTest(ASCIIEncoding ascii, byte[] bytes, int index, int count)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                ascii.GetCharCount(bytes, index, count);
            });
        }

        private string GetByteArrayInfo(byte[] bytes, int startIndex, int count)
        {
            StringBuilder sb = new StringBuilder();
            if (null == bytes)
            {
                sb.Append("\nByte array: null reference");
            }
            else if (bytes.Length == 0)
            {
                sb.Append("\nByte array: zero-length array");
            }
            else
            {
                sb.Append("\nByte array: {");
                for (int i = 0; i < bytes.Length; ++i)
                {
                    if ((i % 8) == 0) sb.Append("\n\t");
                    //sb.AppendFormat("{0:x02},", bytes[i]);
                    sb.Append("," + bytes[i].ToString("X4"));
                }
                sb.Append("\n}");
            }
            sb.AppendFormat("\nStart index: {0}\nCount of bytes decoded: {1}", startIndex, count);

            return sb.ToString();
        }
    }
}
