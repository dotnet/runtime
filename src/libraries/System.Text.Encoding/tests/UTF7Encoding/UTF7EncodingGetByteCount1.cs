// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    public class UTF7EncodingGetByteCount
    {
        private const string c_STRING_DIRECTCHARS = "\t\n\rXYZabc123";
        private const int c_INT_DIRECTCHARSLENGTH = 12;

        private const string c_STRING_OPTIONALCHARS = "!\"#$%&*;<=>@[]^_`{|}";
        private const int c_INT_OPTIONALCHARSLENTTH = 20;

        private const string c_STRING_SPECIALCHARS = "\u03a0\u03a3";
        private const int c_INT_SPECIALCHARSLENGTH = 8;

        private const int c_INT_STRINGEMPTYlENGTH = 0;

        // PosTest1: to test direct chars with new UTF7Encoding().
        [Fact]
        public void PosTest1()
        {
            UTF7Encoding utf7 = new UTF7Encoding();
            Assert.Equal(c_INT_DIRECTCHARSLENGTH, utf7.GetByteCount(c_STRING_DIRECTCHARS));
        }


        // PosTest2: to test direct chars with new UTF7Encoding(true).
        [Fact]
        public void PosTest2()
        {
            UTF7Encoding utf7 = new UTF7Encoding(true);
            Assert.Equal(c_INT_DIRECTCHARSLENGTH, utf7.GetByteCount(c_STRING_DIRECTCHARS));
        }

        // PosTest3: to test OPTIONALCHARS with new UTF7Encoding().
        [Fact]
        public void PosTest3()
        {
            UTF7Encoding utf7 = new UTF7Encoding();
            Assert.NotEqual(c_INT_OPTIONALCHARSLENTTH, utf7.GetByteCount(c_STRING_OPTIONALCHARS));
        }

        // PosTest4: to test OPTIONALCHARS with new UTF7Encoding(true).
        [Fact]
        public void PosTest4()
        {
            UTF7Encoding utf7 = new UTF7Encoding(true);
            Assert.Equal(c_INT_OPTIONALCHARSLENTTH, utf7.GetByteCount(c_STRING_OPTIONALCHARS));
        }

        // PosTest5: to test SPECIALCHARS with new UTF7Encoding().
        [Fact]
        public void PosTest5()
        {
            UTF7Encoding utf7 = new UTF7Encoding();
            Assert.Equal(c_INT_SPECIALCHARSLENGTH, utf7.GetByteCount(c_STRING_SPECIALCHARS));
        }

        // PosTest6: to test SPECIALCHARS with new UTF7Encoding(true).
        [Fact]
        public void PosTest6()
        {
            UTF7Encoding utf7 = new UTF7Encoding(true);
            Assert.Equal(c_INT_SPECIALCHARSLENGTH, utf7.GetByteCount(c_STRING_SPECIALCHARS));
        }

        // PosTest7: to test String.Empty with new UTF7Encoding().
        [Fact]
        public void PosTest7()
        {
            UTF7Encoding utf7 = new UTF7Encoding();
            Assert.Equal(c_INT_STRINGEMPTYlENGTH, utf7.GetByteCount(String.Empty));
        }

        //NegTest1: The argument is null reference
        [Fact]
        public void NegTest1()
        {
            string source = null;
            Assert.Throws<ArgumentNullException>(() =>
            {
                UTF7Encoding UTF7 = new UTF7Encoding();
                UTF7.GetByteCount(source);
            });
        }
    }
}
