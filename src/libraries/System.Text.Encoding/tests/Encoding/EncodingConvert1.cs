// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    // Convert(System.Text.Encoding,System.Text.Encoding,System.Byte[])
    public class EncodingConvert1
    {
        #region Positive Test Cases
        // PosTest1: Verify method Convert
        [Fact]
        public void PosTest1()
        {
            string unicodeStr = "test string .";
            Encoding ascii = Encoding.UTF8;
            Encoding unicode = Encoding.Unicode;

            byte[] unicodeBytes = unicode.GetBytes(unicodeStr);

            byte[] asciiBytes = Encoding.Convert(unicode, ascii, unicodeBytes);

            char[] asciiChars = new char[ascii.GetCharCount(asciiBytes, 0, asciiBytes.Length)];
            ascii.GetChars(asciiBytes, 0, asciiBytes.Length, asciiChars, 0);
            string asciiStr = new string(asciiChars);
            Assert.Equal(asciiStr, unicodeStr);
        }
        #endregion

        #region Nagetive Test Cases
        [Fact]
        public void NegTest1()
        {
            string unicodeStr = "test string .";

            Encoding ascii = null;
            Encoding unicode = Encoding.Unicode;

            byte[] unicodeBytes = unicode.GetBytes(unicodeStr);
            Assert.Throws<ArgumentNullException>(() =>
           {
               byte[] asciiBytes = Encoding.Convert(unicode, ascii, unicodeBytes);
           });
        }

        [Fact]
        public void NegTest2()
        {
            Encoding ascii = Encoding.UTF8;
            Encoding unicode = null;

            byte[] unicodeBytes = new byte[] { 1, 2, 3 };
            Assert.Throws<ArgumentNullException>(() =>
            {
                byte[] asciiBytes = Encoding.Convert(unicode, ascii, unicodeBytes);
            });
        }

        [Fact]
        public void NegTest3()
        {
            Encoding ascii = Encoding.UTF8;
            Encoding unicode = Encoding.Unicode;

            byte[] unicodeBytes = null;
            Assert.Throws<ArgumentNullException>(() =>
            {
                byte[] asciiBytes = Encoding.Convert(unicode, ascii, unicodeBytes);
            });
        }
        #endregion
    }
}
