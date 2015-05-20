// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    public class EncodingConvert2
    {
        #region Positive Test Cases
        // PosTest1: Verify method Convert when count == bytes.Length 
        [Fact]
        public void PosTest1()
        {
            string unicodeStr = "test string .";
            Encoding ascii = Encoding.UTF8;
            Encoding unicode = Encoding.Unicode;

            byte[] unicodeBytes = unicode.GetBytes(unicodeStr);
            byte[] asciiBytes = Encoding.Convert(unicode, ascii, unicodeBytes, 0, unicodeBytes.Length);

            char[] asciiChars = new char[ascii.GetCharCount(asciiBytes, 0, asciiBytes.Length)];
            ascii.GetChars(asciiBytes, 0, asciiBytes.Length, asciiChars, 0);
            string asciiStr = new string(asciiChars);
            Assert.Equal(asciiStr, unicodeStr);
        }

        // PosTest2: Verify method Convert when count == 0
        [Fact]
        public void PosTest2()
        {
            string unicodeStr = "test string .";
            Encoding ascii = Encoding.UTF8;
            Encoding unicode = Encoding.Unicode;

            byte[] unicodeBytes = unicode.GetBytes(unicodeStr);
            byte[] asciiBytes = Encoding.Convert(unicode, ascii, unicodeBytes, 0, 0);

            char[] asciiChars = new char[ascii.GetCharCount(asciiBytes, 0, asciiBytes.Length)];
            ascii.GetChars(asciiBytes, 0, asciiBytes.Length, asciiChars, 0);
            string asciiStr = new string(asciiChars);
            Assert.Equal("", asciiStr);
        }

        // PosTest3: Verify method Convert when bytes == null.
        [Fact]
        public void PosTest3()
        {
            Encoding ascii = Encoding.UTF8;
            Encoding unicode = Encoding.Unicode;

            byte[] unicodeBytes = new byte[0];

            byte[] asciiBytes = Encoding.Convert(unicode, ascii, unicodeBytes, 0, 0);

            char[] asciiChars = new char[ascii.GetCharCount(asciiBytes, 0, asciiBytes.Length)];
            ascii.GetChars(asciiBytes, 0, asciiBytes.Length, asciiChars, 0);
            string asciiStr = new string(asciiChars);
            Assert.Equal("", asciiStr);
        }
        #endregion

        #region Negative Test Cases
        [Fact]
        public void NegTest1()
        {
            string unicodeStr = "test string .";
            Encoding ascii = null;
            Encoding unicode = Encoding.Unicode;

            byte[] unicodeBytes = unicode.GetBytes(unicodeStr);
            Assert.Throws<ArgumentNullException>(() =>
            {
                byte[] asciiBytes = Encoding.Convert(unicode, ascii, unicodeBytes, 0, unicodeBytes.Length);
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
                byte[] asciiBytes = Encoding.Convert(unicode, ascii, unicodeBytes, 0, unicodeBytes.Length);
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
                byte[] asciiBytes = Encoding.Convert(unicode, ascii, unicodeBytes, 0, 2);
            });
        }

        [Fact]
        public void NegTest4()
        {
            string unicodeStr = "test string .";
            Encoding ascii = Encoding.UTF8;
            Encoding unicode = Encoding.Unicode;
            byte[] unicodeBytes = unicode.GetBytes(unicodeStr);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                byte[] asciiBytes = Encoding.Convert(unicode, ascii, unicodeBytes, -1, unicodeBytes.Length);
            });
        }

        [Fact]
        public void NegTest5()
        {
            string unicodeStr = "test string .";
            Encoding ascii = Encoding.UTF8;
            Encoding unicode = Encoding.Unicode;
            byte[] unicodeBytes = unicode.GetBytes(unicodeStr);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                byte[] asciiBytes = Encoding.Convert(unicode, ascii, unicodeBytes, 0, unicodeBytes.Length + 1);
            });
        }

        [Fact]
        public void NegTest6()
        {
            string unicodeStr = "test string .";
            Encoding ascii = Encoding.UTF8;
            Encoding unicode = Encoding.Unicode;
            byte[] unicodeBytes = unicode.GetBytes(unicodeStr);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                byte[] asciiBytes = Encoding.Convert(unicode, ascii, unicodeBytes, 0, -1);
            });
        }
        #endregion
    }
}
