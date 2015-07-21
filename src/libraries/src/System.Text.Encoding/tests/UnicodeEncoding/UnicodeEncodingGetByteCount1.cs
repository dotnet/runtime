// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    public class UnicodeEncodingGetByteCount1
    {
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        #region Positive Tests
        // PosTest1:Invoke the method with a empty char array.
        [Fact]
        public void PosTest1()
        {
            Char[] chars = new Char[] { };
            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int actualValue;
            actualValue = uEncoding.GetByteCount(chars, 0, 0);
            Assert.Equal(0, actualValue);
        }

        // PosTest2:Invoke the method with max length of the char array
        [Fact]
        public void PosTest2()
        {
            Char[] chars = GetCharArray(10);
            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int actualValue;

            actualValue = uEncoding.GetByteCount(chars, 0, chars.Length);
            Assert.Equal(20, actualValue);
        }

        // PosTest3:Invoke the method with one char array
        [Fact]
        public void PosTest3()
        {
            Char[] chars = GetCharArray(1);
            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int actualValue;

            actualValue = uEncoding.GetByteCount(chars, 0, 1);
            Assert.Equal(2, actualValue);
        }
        #endregion

        #region Negative Tests
        // NegTest1:Invoke the method with null
        [Fact]
        public void NegTest1()
        {
            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int actualValue;
            Assert.Throws<ArgumentNullException>(() =>
            {
                actualValue = uEncoding.GetByteCount(null, 0, 0);
            });
        }

        // NegTest2:Invoke the method with index out of range
        [Fact]
        public void NegTest2()
        {
            Char[] chars = GetCharArray(10);
            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = uEncoding.GetByteCount(chars, 10, 1);
            });
        }

        // NegTest3:Invoke the method with count out of range
        [Fact]
        public void NegTest3()
        {
            Char[] chars = GetCharArray(10);
            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = uEncoding.GetByteCount(chars, 5, -1);
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
