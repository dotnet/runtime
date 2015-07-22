// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    //System.Test.UnicodeEncoding.GetCharCount(System.Byte[],System.Int32,System.Int32) [v-zuolan]
    public class UnicodeEncodingGetCharCount
    {
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        #region Positive Tests
        // PosTest1:Invoke the method.
        [Fact]
        public void PosTest1()
        {
            Char[] chars = GetCharArray(10);
            Byte[] bytes = new Byte[20];
            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int byteCount = uEncoding.GetBytes(chars, 0, 10, bytes, 0);
            int actualValue;

            actualValue = uEncoding.GetCharCount(bytes, 0, 20);
            Assert.Equal(10, actualValue);
        }

        // PosTest2:Invoke the method and set byteCount as 0.
        [Fact]
        public void PosTest2()
        {
            Char[] chars = GetCharArray(10);
            Byte[] bytes = new Byte[20];

            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int byteCount = uEncoding.GetBytes(chars, 0, 10, bytes, 0);
            int actualValue;

            actualValue = uEncoding.GetCharCount(bytes, 5, 0);
            Assert.Equal(0, actualValue);
        }

        // PosTest3:Invoke the method and set byteCount as 2
        [Fact]
        public void PosTest3()
        {
            Char[] chars = GetCharArray(10);
            Byte[] bytes = new Byte[20];
            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int byteCount = uEncoding.GetBytes(chars, 0, 10, bytes, 0);
            int actualValue;

            actualValue = uEncoding.GetCharCount(bytes, 0, 2);
            Assert.Equal(1, actualValue);
        }

        // PosTest4:Invoke the method and set byteIndex out of right range
        [Fact]
        public void PosTest4()
        {
            Char[] chars = GetCharArray(10);
            Byte[] bytes = new Byte[20];
            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int byteCount = uEncoding.GetBytes(chars, 0, 10, bytes, 0);
            int actualValue;

            actualValue = uEncoding.GetCharCount(bytes, 20, 0);
            Assert.Equal(0, actualValue);
        }
        #endregion

        #region Negative Tests
        // NegTest1:Invoke the method and set bytes as null
        [Fact]
        public void NegTest1()
        {
            Char[] chars = GetCharArray(10);
            Byte[] bytes = new Byte[20];
            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int byteCount = uEncoding.GetBytes(chars, 0, 10, bytes, 0);
            int actualValue;
            Assert.Throws<ArgumentNullException>(() =>
            {
                actualValue = uEncoding.GetCharCount(null, 0, 0);
            });
        }

        // NegTest2:Invoke the method and set byteIndex as -1
        [Fact]
        public void NegTest2()
        {
            Char[] chars = GetCharArray(10);
            Byte[] bytes = new Byte[20];
            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int byteCount = uEncoding.GetBytes(chars, 0, 10, bytes, 0);
            int actualValue;

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = uEncoding.GetCharCount(bytes, -1, 2);
            });
        }

        // NegTest3:Invoke the method and set byteIndex out of right range
        [Fact]
        public void NegTest3()
        {
            Char[] chars = GetCharArray(10);
            Byte[] bytes = new Byte[20];
            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int byteCount = uEncoding.GetBytes(chars, 0, 10, bytes, 0);
            int actualValue;

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = uEncoding.GetCharCount(bytes, 21, 0);
            });
        }

        // NegTest4:Invoke the method and set byteCount as -1
        [Fact]
        public void NegTest4()
        {
            Char[] chars = GetCharArray(10);
            Byte[] bytes = new Byte[20];
            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int byteCount = uEncoding.GetBytes(chars, 0, 10, bytes, 0);
            int actualValue;

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = uEncoding.GetCharCount(bytes, 0, -1);
            });
        }

        // NegTest5:Invoke the method and set byteCount as 21
        [Fact]
        public void NegTest5()
        {
            Char[] chars = GetCharArray(10);
            Byte[] bytes = new Byte[20];
            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int byteCount = uEncoding.GetBytes(chars, 0, 10, bytes, 0);
            int actualValue;

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = uEncoding.GetCharCount(bytes, 0, 21);
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
