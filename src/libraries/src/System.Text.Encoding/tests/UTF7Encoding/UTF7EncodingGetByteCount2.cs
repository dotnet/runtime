// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    public class UTF7EncodingGetByteCount2
    {
        private readonly Char[] _ARRAY_DIRECTCHARS = { '\t', '\n', '\r', 'X', 'Y', 'Z', 'a', 'b', 'c', '1', '2', '3' };
        private const int c_INT_DIRECTCHARSLENGTH = 12;

        private readonly Char[] _ARRAY_OPTIONALCHARS = { '!', '\"', '#', '$', '%', '&', '*', ';', '<', '=' };     // "!\"#$%&*;<=>@[]^_`{|}";
        private const int c_INT_OPTIONALCHARSLENTTH = 10;

        private readonly Char[] _ARRAY_SPECIALCHARS = { '\u03a0', '\u03a3' };                        // "\u03a0\u03a3";
        private const int c_INT_SPECIALCHARSLENGTH = 8;

        private readonly Char[] _ARRAY_EMPTY = new Char[0];
        private const int c_INT_EMPTYlENGTH = 0;

        // PosTest1: to test direct chars with new UTF7Encoding().
        [Fact]
        public void PosTest1()
        {
            UTF7Encoding utf7 = new UTF7Encoding();
            Assert.Equal(c_INT_DIRECTCHARSLENGTH, utf7.GetByteCount(_ARRAY_DIRECTCHARS, 0, c_INT_DIRECTCHARSLENGTH));
        }

        // PosTest2: to test direct chars with new UTF7Encoding(true).
        [Fact]
        public void PosTest2()
        {
            UTF7Encoding utf7 = new UTF7Encoding(true);
            Assert.Equal(c_INT_DIRECTCHARSLENGTH, utf7.GetByteCount(_ARRAY_DIRECTCHARS, 0, c_INT_DIRECTCHARSLENGTH));
        }

        // PosTest3: to test OPTIONALCHARS with new UTF7Encoding().
        [Fact]
        public void PosTest3()
        {
            UTF7Encoding utf7 = new UTF7Encoding();
            Assert.NotEqual(c_INT_OPTIONALCHARSLENTTH, utf7.GetByteCount(_ARRAY_OPTIONALCHARS, 0, c_INT_OPTIONALCHARSLENTTH));
        }

        // PosTest4: to test OPTIONALCHARS with new UTF7Encoding(true).
        [Fact]
        public void PosTest4()
        {
            Char[] CHARS = { '!', '\"', '#', '$', '%', '&', '*', ';', '<', '=' };
            UTF7Encoding utf7 = new UTF7Encoding(true);
            Assert.Equal(c_INT_OPTIONALCHARSLENTTH, utf7.GetByteCount(_ARRAY_OPTIONALCHARS, 0, c_INT_OPTIONALCHARSLENTTH));
        }

        // PosTest5: to test SPECIALCHARS with new UTF7Encoding().
        [Fact]
        public void PosTest5()
        {
            UTF7Encoding utf7 = new UTF7Encoding();
            Assert.Equal(c_INT_SPECIALCHARSLENGTH, utf7.GetByteCount(_ARRAY_SPECIALCHARS, 0, _ARRAY_SPECIALCHARS.Length));
        }

        // PosTest6: to test SPECIALCHARS with new UTF7Encoding(true).
        [Fact]
        public void PosTest6()
        {
            UTF7Encoding utf7 = new UTF7Encoding(true);
            Assert.Equal(c_INT_SPECIALCHARSLENGTH, utf7.GetByteCount(_ARRAY_SPECIALCHARS, 0, _ARRAY_SPECIALCHARS.Length));
        }

        // PosTest7: to test Empty Char[] with new UTF7Encoding().
        [Fact]
        public void PosTest7()
        {
            UTF7Encoding utf7 = new UTF7Encoding();
            Assert.Equal(c_INT_EMPTYlENGTH, utf7.GetByteCount(_ARRAY_EMPTY, 0, 0));
        }

        //NegTest1: to test UTF7Encoding.GetByteCount(null,Int32 index ,Int32 count)
        [Fact]
        public void NegTest1()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                UTF7Encoding UTF7 = new UTF7Encoding();
                UTF7.GetByteCount(null, 0, 1);
            });
        }

        // NegTest2: to test index is less than zero
        [Fact]
        public void NegTest2()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                UTF7Encoding UTF7 = new UTF7Encoding();
                UTF7.GetByteCount(_ARRAY_DIRECTCHARS, -1, 1);
            });
        }

        // NegTest3: to test count is less than zero
        [Fact]
        public void NegTest3()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                UTF7Encoding UTF7 = new UTF7Encoding();
                UTF7.GetByteCount(_ARRAY_DIRECTCHARS, 0, -1);
            });
        }

        // NegTest4: to test index do not denote a valid range in chars
        [Fact]
        public void NegTest4()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                UTF7Encoding UTF7 = new UTF7Encoding();
                UTF7.GetByteCount(_ARRAY_DIRECTCHARS, 13, 1);
            });
        }

        // NegTest5: to test count do not denote a valid range in chars
        [Fact]
        public void NegTest5()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                UTF7Encoding UTF7 = new UTF7Encoding();
                UTF7.GetByteCount(_ARRAY_DIRECTCHARS, 0, 13);
            });
        }
    }
}
