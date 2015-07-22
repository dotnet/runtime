// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    //System.Test.UnicodeEncoding.GetString(System.Byte[],System.Int32,System.Int32)
    public class UnicodeEncodingGetString
    {
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        #region Positive Test Logic
        // PosTest1:Invoke the method
        [Fact]
        public void PosTest1()
        {
            Char[] srcChars = GetCharArray(10);
            Char[] desChars = new Char[10];
            Byte[] bytes = new Byte[20];
            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int byteCount = uEncoding.GetBytes(srcChars, 0, 10, bytes, 0);
            bool expectedValue = true;
            bool actualValue = true;

            String desString = uEncoding.GetString(bytes, 0, 20);
            desChars = desString.ToCharArray();
            for (int i = 0; i < 10; i++)
            {
                actualValue = actualValue & (desChars[i] == srcChars[i]);
            }
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest2:Invoke the method,convert 1 char
        [Fact]
        public void PosTest2()
        {
            Char[] srcChars = GetCharArray(10);
            Char[] desChars = new Char[10];
            Byte[] bytes = new Byte[20];

            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int byteCount = uEncoding.GetBytes(srcChars, 0, 10, bytes, 0);

            bool expectedValue = true;
            bool actualValue = true;

            String desString = uEncoding.GetString(bytes, 0, 2);
            desChars = desString.ToCharArray();
            for (int i = 0; i < 1; i++)
            {
                actualValue = actualValue & (desChars[i] == srcChars[i]);
            }
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest3:Invoke the method and set byteIndex as 0 and byteCount as 0
        [Fact]
        public void PosTest3()
        {
            Char[] srcChars = GetCharArray(10);
            Char[] desChars = new Char[10];
            Byte[] bytes = new Byte[20];
            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int byteCount = uEncoding.GetBytes(srcChars, 0, 10, bytes, 0);
            String expectedValue = "";
            String actualValue;

            actualValue = uEncoding.GetString(bytes, 0, 0);
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest4:Invoke the method and set byteIndex out of range
        [Fact]
        public void PosTest4()
        {
            Char[] srcChars = GetCharArray(10);
            Char[] desChars = new Char[10];
            Byte[] bytes = new Byte[20];

            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int byteCount = uEncoding.GetBytes(srcChars, 0, 10, bytes, 0);

            String expectedValue = "";
            String actualValue;

            actualValue = uEncoding.GetString(bytes, 20, 0);
            Assert.Equal(expectedValue, actualValue);
        }
        #endregion

        #region Negative Tests
        // NegTest1:Invoke the method and set chars as null
        [Fact]
        public void NegTest1()
        {
            Char[] srcChars = GetCharArray(10);
            Char[] desChars = new Char[10];
            Byte[] bytes = new Byte[20];

            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int byteCount = uEncoding.GetBytes(srcChars, 0, 10, bytes, 0);

            String actualValue;

            Assert.Throws<ArgumentNullException>(() =>
            {
                actualValue = uEncoding.GetString(null, 0, 0);
            });
        }

        // NegTest2:Invoke the method and set byteIndex out of range
        [Fact]
        public void NegTest2()
        {
            Char[] srcChars = GetCharArray(10);
            Char[] desChars = new Char[10];
            Byte[] bytes = new Byte[20];

            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int byteCount = uEncoding.GetBytes(srcChars, 0, 10, bytes, 0);

            String actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = uEncoding.GetString(bytes, 21, 0);
            });
        }

        // NegTest3:Invoke the method and set byteIndex out of range
        [Fact]
        public void NegTest3()
        {
            Char[] srcChars = GetCharArray(10);
            Char[] desChars = new Char[10];
            Byte[] bytes = new Byte[20];

            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int byteCount = uEncoding.GetBytes(srcChars, 0, 10, bytes, 0);

            String actualValue;

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = uEncoding.GetString(bytes, -1, 0);
            });
        }

        // NegTest4:Invoke the method and set byteCount out of range
        [Fact]
        public void NegTest4()
        {
            Char[] srcChars = GetCharArray(10);
            Char[] desChars = new Char[10];
            Byte[] bytes = new Byte[20];

            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int byteCount = uEncoding.GetBytes(srcChars, 0, 10, bytes, 0);

            String actualValue;

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = uEncoding.GetString(bytes, 0, -1);
            });
        }

        // NegTest5:Invoke the method and set byteCount out of range
        [Fact]
        public void NegTest5()
        {
            Char[] srcChars = GetCharArray(10);
            Char[] desChars = new Char[10];
            Byte[] bytes = new Byte[20];

            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int byteCount = uEncoding.GetBytes(srcChars, 0, 10, bytes, 0);

            String actualValue;

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = uEncoding.GetString(bytes, 0, 21);
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
