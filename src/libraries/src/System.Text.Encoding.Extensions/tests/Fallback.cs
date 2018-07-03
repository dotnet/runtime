// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Text;

namespace EncodingTests
{
    public static class Fallback
    {
        private static readonly string s_asciiInputStringWithFallback = "\u00abX\u00bb"; // DOUBLE ANGLE QUOTATION MARK (U+00AB), 'X' (U+0058), and RIGHT POINTING DOUBLE ANGLE QUOTATION MARK (U+00BB).
        private static readonly string s_asciiInputStringWinNoFallback = "abc";

        [Fact]
        public static void TestEncoderReplacementFallback()
        {
            Encoding asciiEncoding = Encoding.GetEncoding("us-ascii", new EncoderReplacementFallback("(unknown)"), new DecoderReplacementFallback(""));
            byte[] encodedBytes = new byte[asciiEncoding.GetByteCount(s_asciiInputStringWithFallback)];

            int numberOfEncodedBytes = asciiEncoding.GetBytes(s_asciiInputStringWithFallback, 0, s_asciiInputStringWithFallback.Length, encodedBytes, 0);

            Assert.Equal(
                    encodedBytes,
                    new byte[] {0x28, 0x75, 0x6E, 0x6B, 0x6E, 0x6F, 0x77, 0x6E, 0x29, 0x58, 0x28, 0x75, 0x6E, 0x6B, 0x6E, 0x6F, 0x77, 0x6E, 0x29 });

            string decodedString = asciiEncoding.GetString(encodedBytes);
            Assert.Equal(decodedString, "(unknown)X(unknown)");

            // Test when the fallback will not occur
            encodedBytes = new byte[asciiEncoding.GetByteCount(s_asciiInputStringWinNoFallback)];
            numberOfEncodedBytes = asciiEncoding.GetBytes(s_asciiInputStringWinNoFallback, 0, s_asciiInputStringWinNoFallback.Length, encodedBytes, 0);
            Assert.Equal(encodedBytes, new byte[] { (byte)'a', (byte)'b', (byte)'c' });
            decodedString = asciiEncoding.GetString(encodedBytes);
            Assert.Equal(decodedString, s_asciiInputStringWinNoFallback);
        }

        [Fact]
        public static void TestDecoderReplacementFallback()
        {
            Encoding asciiEncoding = Encoding.GetEncoding("us-ascii", new EncoderReplacementFallback("(unknown)"), new DecoderReplacementFallback("Error"));
            Assert.Equal(asciiEncoding.GetString(new byte [] { 0xAA, (byte)'x', 0xAB }), "ErrorxError");
        }

        [Fact]
        public static void TestEncoderExceptionFallback()
        {
            Encoding asciiEncoding = Encoding.GetEncoding("us-ascii", new EncoderExceptionFallback(), new DecoderExceptionFallback());
            Assert.Throws<EncoderFallbackException>(() => asciiEncoding.GetByteCount(s_asciiInputStringWithFallback));
        }

        [Fact]
        public static void TestDecoderExceptionFallback()
        {
            Encoding asciiEncoding = Encoding.GetEncoding("us-ascii", new EncoderExceptionFallback(), new DecoderExceptionFallback());
            Assert.Throws<DecoderFallbackException>(() => asciiEncoding.GetString(new byte [] { 0xAA, (byte)'x', 0xAB }));
        }

        [Fact]
        public static void TestCustomFallback()
        {
            Encoding asciiEncoding = Encoding.GetEncoding("us-ascii", new EncoderCustomFallback(), new DecoderReplacementFallback(""));
            byte[] encodedBytes = new byte[asciiEncoding.GetByteCount(s_asciiInputStringWithFallback)];

            int numberOfEncodedBytes = asciiEncoding.GetBytes(s_asciiInputStringWithFallback, 0, s_asciiInputStringWithFallback.Length, encodedBytes, 0);

            Assert.Equal(
                    encodedBytes,
                    new byte[] {(byte) 'a', 0x58, (byte) 'b'});

            string decodedString = asciiEncoding.GetString(encodedBytes);
            Assert.Equal(decodedString, "aXb");
        }
    }


    // a simple fallback class which fallback using the character sequence 'a', 'b', 'c',...etc.
    internal sealed class EncoderCustomFallback : EncoderFallback
    {
        public EncoderCustomFallback()
        {
        }

        public string DefaultString
        {
             get { return " "; }
        }

        public override EncoderFallbackBuffer CreateFallbackBuffer()
        {
            return new EncoderCustomFallbackBuffer();
        }

        public override int MaxCharCount
        {
            get { return 1; }
        }

        public override bool Equals(object value)
        {
            return value is EncoderCustomFallback;
        }

        public override int GetHashCode()
        {
            return "EncoderCustomFallback".GetHashCode();
        }
    }

    internal sealed class EncoderCustomFallbackBuffer : EncoderFallbackBuffer
    {
        private int _nextChar;
        private bool _fallback;

        public EncoderCustomFallbackBuffer()
        {
            _fallback = false;
            _nextChar = (int)'a' - 1;
        }

        public override bool Fallback(char charUnknown, int index)
        {
            _fallback = true;
            return true;
        }

        public override bool Fallback(char charUnknownHigh, char charUnknownLow, int index)
        {
            _fallback = true;
            return true;
        }

        public override char GetNextChar()
        {
            if (!_fallback)
                return (char)0;

            _fallback = false;

            return (char)++_nextChar;
        }

        public override bool MovePrevious()
        {
            return --_nextChar >= (int)'a' - 1;
        }

        public override int Remaining
        {
            get { return 0; }
        }

        public override void Reset()
        {
            _fallback = false;
            _nextChar = (int)'a' - 1;
        }
    }
}
