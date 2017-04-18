// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text;
using Xunit;

namespace System.Text.Encodings.Tests
{
    public class UnicodeEncodingMiscTests
    {
        [Fact]
        public static void CharSizeTest()
        {
            Assert.Equal(2, UnicodeEncoding.CharSize);
        }

        [Fact]
        public static unsafe void GetCharAndBytesTest()
        {
            Encoding encoding = Encoding.Unicode;
            string s = "some string encoded in Unicode-16 \uD800\uDC00";
            byte [] bytes = encoding.GetBytes(s);
            
            byte [] outputBytes = new byte [100];
            char [] outputChars = new char [100];

            fixed (byte *pBytes = outputBytes)
            fixed (byte *pFilledBytes = bytes)
            fixed (char *pChars = s)
            fixed (char *pOutputChars = outputChars)
            {
                int count = encoding.GetBytes(pChars, s.Length, pBytes, outputBytes.Length);
                Assert.Equal(bytes.Length, count);
                Assert.Equal(bytes.Length, encoding.GetByteCount(pChars, s.Length));
                for (int i=0; i<count; i++) { Assert.Equal(bytes[i], pBytes[i]); }

                count = encoding.GetChars(pFilledBytes, bytes.Length, pOutputChars, outputChars.Length); 
                Assert.Equal(s.Length, count);
                Assert.Equal(s.Length, encoding.GetCharCount(pFilledBytes, bytes.Length));
                for (int i=0; i<count; i++) { Assert.Equal(s[i], pOutputChars[i]); }
            }
        }

        [Fact]
        public static unsafe void GetBytesNegativeTest()
        {
            Encoding enc = Encoding.GetEncoding("utf-16", new EncoderExceptionFallback(), new DecoderExceptionFallback());

            fixed (byte *pBytesPtr = new byte [10] )
            fixed (char *pCharsPtr = "some string")
            fixed (char *pInvalidSurrogatePtr = "\uD800\uB800")
            {
                byte *pBytes = pBytesPtr;
                char *pChars = pCharsPtr;
                char *pInvalidSurrogate = pInvalidSurrogatePtr;

                AssertExtensions.Throws<ArgumentNullException>("chars", () => enc.GetBytes(null, 1, pBytes, 1));
                AssertExtensions.Throws<ArgumentNullException>("chars", () => enc.GetByteCount(null, 1));
                AssertExtensions.Throws<ArgumentNullException>("bytes", () => enc.GetBytes(pChars, 1, null, 1));

                AssertExtensions.Throws<ArgumentOutOfRangeException>("charCount", () => enc.GetBytes(pChars, -1, pBytes, 1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => enc.GetByteCount(pChars, -1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("byteCount", () => enc.GetBytes(pChars, 1, pBytes, -1));

                AssertExtensions.Throws<ArgumentException>("bytes", () => enc.GetBytes(pChars, 4, pBytes, 1));
                
                Assert.Throws<EncoderFallbackException>(() => enc.GetBytes(pInvalidSurrogate, 2, pBytes, 10));
                Assert.Throws<EncoderFallbackException>(() => enc.GetByteCount(pInvalidSurrogate, 2));
            }
        }

        [Fact]
        public static unsafe void GetCharsNegativeTest()
        {
            Encoding enc = Encoding.GetEncoding("utf-16", new EncoderExceptionFallback(), new DecoderExceptionFallback());
            byte [] bytes = enc.GetBytes("Some string");
            char [] chars = new char[20];
            byte[] invalid = new byte[] { 0x00, 0xD8, 0x00, 0xD8 };


            fixed (byte *pBytesPtr = bytes)
            fixed (byte *pInvalidPtr = invalid)
            fixed (char *pCharsPtr = chars)
            {
                byte *pBytes = pBytesPtr;
                byte *pInvalid = pInvalidPtr;
                char *pChars = pCharsPtr;

                AssertExtensions.Throws<ArgumentNullException>("bytes", () => enc.GetChars(null, bytes.Length, pChars, 20));
                AssertExtensions.Throws<ArgumentNullException>("bytes", () => enc.GetCharCount(null, bytes.Length));
                AssertExtensions.Throws<ArgumentNullException>("chars", () => enc.GetChars(pBytes, bytes.Length, null, 20));

                AssertExtensions.Throws<ArgumentOutOfRangeException>("byteCount", () => enc.GetChars(pBytes, -1, pChars, 20));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => enc.GetCharCount(pBytes, -1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("charCount", () => enc.GetChars(pBytes, bytes.Length, pChars, -1));

                AssertExtensions.Throws<ArgumentException>("chars", () => enc.GetChars(pBytes, bytes.Length, pChars, 1));

                Assert.Throws<DecoderFallbackException>(() => enc.GetChars(pInvalid, invalid.Length, pChars, 20));
                Assert.Throws<DecoderFallbackException>(() => enc.GetCharCount(pInvalid, invalid.Length));
            }
        }
    }
}
