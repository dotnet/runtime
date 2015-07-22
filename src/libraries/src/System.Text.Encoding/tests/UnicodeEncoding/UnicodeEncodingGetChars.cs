// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    //System.Test.UnicodeEncoding.GetChars(System.Byte[],System.Int32,System.Int32,System.Char[],System.Int32)
    public class UnicodeEncodingGetChars
    {
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        #region Positive Tests
        // PosTest1:Invoke the method
        [Fact]
        public void PosTest1()
        {
            int actualValue;
            Char[] srcChars = GetCharArray(10);
            Char[] desChars = new Char[10];
            Byte[] bytes = new Byte[20];
            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int byteCount = uEncoding.GetBytes(srcChars, 0, 10, bytes, 0);

            actualValue = uEncoding.GetChars(bytes, 0, 20, desChars, 0);
            Assert.Equal(10, actualValue);
        }

        // PosTest2:Invoke the method with random char count
        [Fact]
        public void PosTest2()
        {
            int expectedValue = _generator.GetInt16(-55) % 10 + 1;
            int actualValue;

            Char[] srcChars = GetCharArray(10);
            Char[] desChars = new Char[10];
            Byte[] bytes = new Byte[20];
            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int byteCount = uEncoding.GetBytes(srcChars, 0, expectedValue, bytes, 0);

            actualValue = uEncoding.GetChars(bytes, 0, expectedValue * 2, desChars, 0);
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest3:Invoke the method and set charIndex as 10
        [Fact]
        public void PosTest3()
        {
            Char[] srcChars = GetCharArray(10);
            Char[] desChars = new Char[10];
            Byte[] bytes = new Byte[20];
            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int byteCount = uEncoding.GetBytes(srcChars, 0, 10, bytes, 0);
            int actualValue;
            actualValue = uEncoding.GetChars(bytes, 0, 0, desChars, 10);
            Assert.Equal(0, actualValue);
        }

        // PosTest4:Invoke the method and set byteIndex as 20
        [Fact]
        public void PosTest4()
        {
            Char[] srcChars = GetCharArray(10);
            Char[] desChars = new Char[10];
            Byte[] bytes = new Byte[20];

            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int byteCount = uEncoding.GetBytes(srcChars, 0, 10, bytes, 0);
            int actualValue;

            actualValue = uEncoding.GetChars(bytes, 20, 0, desChars, 10);
            Assert.Equal(0, actualValue);
        }
        #endregion

        #region Negative Tests
        // NegTest1:Invoke the method and set chars as null
        [Fact]
        public void NegTest1()
        {
            Char[] srcChars = GetCharArray(10);
            Char[] desChars = null;
            Byte[] bytes = new Byte[20];
            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int byteCount = uEncoding.GetBytes(srcChars, 0, 10, bytes, 0);
            int actualValue;

            Assert.Throws<ArgumentNullException>(() =>
            {
                actualValue = uEncoding.GetChars(bytes, 0, 0, desChars, 0);
            });
        }

        // NegTest2:Invoke the method and set bytes as null
        [Fact]
        public void NegTest2()
        {
            Char[] srcChars = GetCharArray(10);
            Char[] desChars = new Char[10];
            Byte[] bytes = null;
            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int actualValue;

            Assert.Throws<ArgumentNullException>(() =>
            {
                actualValue = uEncoding.GetChars(bytes, 0, 0, desChars, 0);
            });
        }

        // NegTest3:Invoke the method and the destination buffer is not enough
        [Fact]
        public void NegTest3()
        {
            Char[] srcChars = GetCharArray(10);
            Char[] desChars = new Char[5];
            Byte[] bytes = new Byte[20];
            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int byteCount = uEncoding.GetBytes(srcChars, 0, 10, bytes, 0);
            int actualValue;
            Assert.Throws<ArgumentException>(() =>
            {
                actualValue = uEncoding.GetChars(bytes, 0, 20, desChars, 0);
            });
        }

        // NegTest4:Invoke the method and the destination buffer is not enough
        [Fact]
        public void NegTest4()
        {
            Char[] srcChars = GetCharArray(10);
            Char[] desChars = new Char[10];
            Byte[] bytes = new Byte[20];
            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int byteCount = uEncoding.GetBytes(srcChars, 0, 10, bytes, 0);
            int actualValue;

            Assert.Throws<ArgumentException>(() =>
            {
                actualValue = uEncoding.GetChars(bytes, 0, 20, desChars, 5);
            });
        }

        // NegTest5:Invoke the method and set byteIndex as -1
        [Fact]
        public void NegTest5()
        {
            Char[] srcChars = GetCharArray(10);
            Char[] desChars = new Char[10];
            Byte[] bytes = new Byte[20];
            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int byteCount = uEncoding.GetBytes(srcChars, 0, 10, bytes, 0);
            int actualValue;

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = uEncoding.GetChars(bytes, -1, 0, desChars, 0);
            });
        }

        // NegTest6:Invoke the method and set byteIndex as 20
        [Fact]
        public void NegTest6()
        {
            Char[] srcChars = GetCharArray(10);
            Char[] desChars = new Char[10];
            Byte[] bytes = new Byte[20];
            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int byteCount = uEncoding.GetBytes(srcChars, 0, 10, bytes, 0);
            int actualValue;

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = uEncoding.GetChars(bytes, 20, 1, desChars, 10);
            });
        }

        // NegTest7:Invoke the method and set byteCount as -1
        [Fact]
        public void NegTest7()
        {
            Char[] srcChars = GetCharArray(10);
            Char[] desChars = new Char[10];
            Byte[] bytes = new Byte[20];
            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int byteCount = uEncoding.GetBytes(srcChars, 0, 10, bytes, 0);
            int actualValue;

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = uEncoding.GetChars(bytes, 0, -1, desChars, 0);
            });
        }

        // NegTest8:Invoke the method and set byteCount as 21
        [Fact]
        public void NegTest8()
        {
            Char[] srcChars = GetCharArray(10);
            Char[] desChars = new Char[10];
            Byte[] bytes = new Byte[20];
            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int byteCount = uEncoding.GetBytes(srcChars, 0, 10, bytes, 0);
            int actualValue;

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = uEncoding.GetChars(bytes, 0, 21, desChars, 0);
            });
        }

        // NegTest9:Invoke the method and set charIndex as -1
        [Fact]
        public void NegTest9()
        {
            Char[] srcChars = GetCharArray(10);
            Char[] desChars = new Char[10];
            Byte[] bytes = new Byte[20];
            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int byteCount = uEncoding.GetBytes(srcChars, 0, 10, bytes, 0);
            int actualValue;

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = uEncoding.GetChars(bytes, 0, 0, desChars, -1);
            });
        }

        // NegTest10:Invoke the method and set charIndex as 11
        [Fact]
        public void NegTest10()
        {
            Char[] srcChars = GetCharArray(10);
            Char[] desChars = new Char[10];
            Byte[] bytes = new Byte[20];
            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int byteCount = uEncoding.GetBytes(srcChars, 0, 10, bytes, 0);
            int actualValue;

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = uEncoding.GetChars(bytes, 0, 0, desChars, 11);
            });
        }
        #endregion
        #region Helper Methods
        //Create a None-Surrogate-Char Array.
        public Char[] GetCharArray(int length)
        {
            if (length <= 0) return new Char[] { };

            Char[] charArray = new Char[length];
            int i = 0;
            while (i < length)
            {
                Char temp = _generator.GetChar(-55);
                if (!Char.IsSurrogate(temp))
                {
                    charArray[i] = temp;
                    i++;
                }
            }
            return charArray;
        }

        //Convert Char Array to String
        public String ToString(Char[] chars)
        {
            String str = "{";
            for (int i = 0; i < chars.Length; i++)
            {
                str = str + @"\u" + String.Format("{0:X04}", (int)chars[i]);
                if (i != chars.Length - 1) str = str + ",";
            }
            str = str + "}";
            return str;
        }
        #endregion
    }
}

