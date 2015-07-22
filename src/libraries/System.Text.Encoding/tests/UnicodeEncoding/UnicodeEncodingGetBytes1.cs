// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    //System.Test.UnicodeEncoding.GetBytes(System.Char[],System.Int32,System.Int32,System.Byte[],System.Int32)
    public class UnicodeEncodingGetBytes1
    {
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        #region Positive Tests
        // PosTest1:Invoke the method
        [Fact]
        public void PosTest1()
        {
            Char[] chars = GetCharArray(10);
            Byte[] bytes = new Byte[30];
            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int actualValue;

            actualValue = uEncoding.GetBytes(chars, 0, 10, bytes, 5);
            Assert.Equal(20, actualValue);
        }

        // PosTest2:Invoke the method and set charCount as 1 and byteIndex as 0
        [Fact]
        public void PosTest2()
        {
            Char[] chars = GetCharArray(10);
            Byte[] bytes = new Byte[30];
            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int actualValue;

            actualValue = uEncoding.GetBytes(chars, 0, 1, bytes, 0);
            Assert.Equal(2, actualValue);
        }

        // PosTest3:Invoke the method and set charIndex as 20
        [Fact]
        public void PosTest3()
        {
            Char[] chars = GetCharArray(10);
            Byte[] bytes = new Byte[20];
            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int actualValue;
            actualValue = uEncoding.GetBytes(chars, 0, 0, bytes, 20);
            Assert.Equal(0, actualValue);
        }
        #endregion

        #region Negative Tests
        // NegTest1:Invoke the method and set chars as null
        [Fact]
        public void NegTest1()
        {
            Char[] chars = null;
            Byte[] bytes = new Byte[30];
            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int actualValue;
            Assert.Throws<ArgumentNullException>(() =>
            {
                actualValue = uEncoding.GetBytes(chars, 0, 0, bytes, 0);
            });
        }

        // NegTest2:Invoke the method and set bytes as null
        [Fact]
        public void NegTest2()
        {
            Char[] chars = GetCharArray(10);
            Byte[] bytes = null;
            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int actualValue;
            Assert.Throws<ArgumentNullException>(() =>
            {
                actualValue = uEncoding.GetBytes(chars, 0, 0, bytes, 0);
            });
        }


        // NegTest3:Invoke the method and the destination buffer is not enough
        [Fact]
        public void NegTest3()
        {
            Char[] chars = GetCharArray(10);
            Byte[] bytes = new Byte[30];
            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int actualValue;
            Assert.Throws<ArgumentException>(() =>
            {
                actualValue = uEncoding.GetBytes(chars, 0, 10, bytes, 15);
            });
        }

        // NegTest4:Invoke the method and the destination buffer is not enough
        [Fact]
        public void NegTest4()
        {
            Char[] chars = GetCharArray(10);
            Byte[] bytes = new Byte[10];

            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int actualValue;
            Assert.Throws<ArgumentException>(() =>
            {
                actualValue = uEncoding.GetBytes(chars, 0, 10, bytes, 0);
            });
        }

        // NegTest5:Invoke the method and set charIndex as -1
        [Fact]
        public void NegTest5()
        {
            Char[] chars = GetCharArray(10);
            Byte[] bytes = new Byte[30];
            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = uEncoding.GetBytes(chars, -1, 1, bytes, 0);
            });
        }

        // NegTest6:Invoke the method and set charIndex as 15
        [Fact]
        public void NegTest6()
        {
            Char[] chars = GetCharArray(10);
            Byte[] bytes = new Byte[30];
            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = uEncoding.GetBytes(chars, 15, 1, bytes, 0);
            });
        }

        // NegTest7:Invoke the method and set charCount as -1
        [Fact]
        public void NegTest7()
        {
            Char[] chars = GetCharArray(10);
            Byte[] bytes = new Byte[30];
            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = uEncoding.GetBytes(chars, 0, -1, bytes, 0);
            });
        }

        // NegTest8:Invoke the method and set charCount as 12
        [Fact]
        public void NegTest8()
        {
            Char[] chars = GetCharArray(10);
            Byte[] bytes = new Byte[30];
            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = uEncoding.GetBytes(chars, 0, 12, bytes, 0);
            });
        }

        // NegTest9:Invoke the method and set byteIndex as -1
        [Fact]
        public void NegTest9()
        {
            Char[] chars = GetCharArray(10);
            Byte[] bytes = new Byte[30];
            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = uEncoding.GetBytes(chars, 0, 1, bytes, -1);
            });
        }

        // NegTest10:Invoke the method and set charIndex as 21
        [Fact]
        public void NegTest10()
        {
            Char[] chars = GetCharArray(10);
            Byte[] bytes = new Byte[20];
            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = uEncoding.GetBytes(chars, 0, 0, bytes, 21);
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
