// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Text.Tests
{
    public class TestEncoderFallbackBuffer : EncoderFallbackBuffer
    {
        private int _fallbackCount = 0;
        private int _expectedCharUnknown;

        public TestEncoderFallbackBuffer(int expectedCharUnknown)
        {
            _expectedCharUnknown = expectedCharUnknown;
        }

        public override bool Fallback(char charUnknownHigh, char charUnknownLow, int index)
        {
            _fallbackCount++;
            return true;
        }

        public override bool Fallback(char charUnknown, int index)
        {
            // Assert Fallback method is called with the expected inputs from the EncoderFallbackBufferHelper instance.
            Assert.Equal(_expectedCharUnknown, charUnknown - '0');
            Assert.Equal(0, index);

            _fallbackCount++;
            return true;
        }

        public override bool MovePrevious()
        {
            if (_fallbackCount > 0)
            {
                return true;
            }

            return false;
        }

        public override int Remaining { get { return _fallbackCount; } }

        public override char GetNextChar()
        {
            if (_fallbackCount > 0)
            {
                _fallbackCount--;
                return '?';
            }
            return '\0';
        }
    }

    public class TestEncoderFallback : EncoderFallback
    {
        private int _expectedCharUnknown;

        public TestEncoderFallback(int expectedCharUnknown)
        {
            _expectedCharUnknown = expectedCharUnknown;
        }

        public override EncoderFallbackBuffer CreateFallbackBuffer() => new TestEncoderFallbackBuffer(_expectedCharUnknown);

        public override int MaxCharCount => 2;
    }

    public class EncoderFallbackBufferHelperTest
    {
        [Fact]
        public void Test_EncoderFallbackBufferHelper_ValidateFallbackForDataRoundTrips()
        {
            RemoteExecutor.Invoke(() =>
            {
                // Add the code page provider.
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                // Roundtrip unsupported character.
                string chInStr = "\u0661";
                int chAsInt = 1585;

                // Use windows-1252 with a custom fallback that utilizes the EncoderFallbackBufferHelper.
                Encoding encoding = Encoding.GetEncoding(1252, new TestEncoderFallback(chAsInt), DecoderFallback.ReplacementFallback);

                byte[] bytes = encoding.GetBytes(chInStr);
                char[] chars = encoding.GetChars(bytes);
                Assert.Equal("?", new string(chars));

                // Roundtrip a high surrogate character.
                chInStr = "\uD800";
                chAsInt = 55248;

                encoding = Encoding.GetEncoding(1252, new TestEncoderFallback(chAsInt), DecoderFallback.ReplacementFallback);

                bytes = encoding.GetBytes(chInStr);
                chars = encoding.GetChars(bytes);
                Assert.Equal("?", new string(chars));
            }).Dispose();
        }
    }
}
