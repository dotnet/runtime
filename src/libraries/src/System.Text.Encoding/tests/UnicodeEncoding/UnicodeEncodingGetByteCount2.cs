// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    public class UnicodeEncodingGetByteCount2
    {
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        #region Positive Tests
        // PosTest1:Invoke the method with a empty String
        [Fact]
        public void PosTest1()
        {
            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int actualValue;
            actualValue = uEncoding.GetByteCount("");
            Assert.Equal(0, actualValue);
        }

        // PosTest2:Invoke the method with normal string
        [Fact]
        public void PosTest2()
        {
            String str = GetString(10);
            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int actualValue;

            String temp = ToString(str);
            actualValue = uEncoding.GetByteCount(str);
            Assert.Equal(20, actualValue);
        }

        // PosTest3:Invoke the method with one char String
        [Fact]
        public void PosTest3()
        {
            String str = GetString(1);
            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int actualValue;
            actualValue = uEncoding.GetByteCount(str);
            Assert.Equal(2, actualValue);
        }
        #endregion

        #region Negative Tests
        // NegTest1:Invoke the method with null
        [Fact]
        public void NegTest1()
        {
            String str = null;
            UnicodeEncoding uEncoding = new UnicodeEncoding();
            int actualValue;
            Assert.Throws<ArgumentNullException>(() =>
            {
                actualValue = uEncoding.GetByteCount(str);
            });
        }

        #endregion
        #region Helper Methods
        //Create a None-Surrogate-Char String.
        public String GetString(int length)
        {
            if (length <= 0) return "";

            String tempStr = null;

            int i = 0;
            while (i < length)
            {
                Char temp = _generator.GetChar(-55);
                if (!Char.IsSurrogate(temp))
                {
                    tempStr = tempStr + temp.ToString();
                    i++;
                }
            }
            return tempStr;
        }

        public String ToString(String myString)
        {
            String str = "{";
            Char[] chars = myString.ToCharArray();
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
