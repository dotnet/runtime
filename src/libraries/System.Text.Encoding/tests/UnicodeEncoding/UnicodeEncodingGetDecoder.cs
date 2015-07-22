// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    //System.Test.UnicodeEncoding.GetDecoder()
    public class UnicodeEncodingGetDecoder
    {
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        #region Positive Tests
        // PosTest1:Invoke the method
        [Fact]
        public void PosTest1()
        {
            Char[] srcChars = GetCharArray(10);
            Char[] desChars = new Char[10];
            Byte[] bytes = new Byte[20];
            int buffer;
            int outChars;
            bool completed;
            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int byteCount = uEncoding.GetBytes(srcChars, 0, 10, bytes, 0);
            bool expectedValue = true;
            bool actualValue = true;
            Decoder dC = uEncoding.GetDecoder();
            dC.Convert(bytes, 0, 20, desChars, 0, 10, true, out buffer, out outChars, out completed);
            if (completed)
            {
                for (int i = 0; i < 10; i++)
                {
                    actualValue = actualValue & (desChars[i] == srcChars[i]);
                }
            }
            Assert.Equal(expectedValue, actualValue);
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
