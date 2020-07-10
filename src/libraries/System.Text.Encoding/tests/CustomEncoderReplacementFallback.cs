// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Text.Encodings.Tests
{
    // A custom encoder fallback which substitutes unknown chars with "[xxxx]" (the code point as hex)
    internal sealed class CustomEncoderReplacementFallback : EncoderFallback
    {
        public override int MaxCharCount => 8; // = "[10FFFF]".Length

        public override EncoderFallbackBuffer CreateFallbackBuffer()
        {
            return new CustomEncoderFallbackBuffer();
        }

        private sealed class CustomEncoderFallbackBuffer : EncoderFallbackBuffer
        {
            private string _remaining = string.Empty;
            private int _remainingIdx = 0;

            public override int Remaining => _remaining.Length - _remainingIdx;

            public override bool Fallback(char charUnknownHigh, char charUnknownLow, int index)
                => FallbackCommon((uint)char.ConvertToUtf32(charUnknownHigh, charUnknownLow));

            public override bool Fallback(char charUnknown, int index)
                => FallbackCommon(charUnknown);

            private bool FallbackCommon(uint codePoint)
            {
                Assert.True(codePoint <= 0x10FFFF);
                _remaining = FormattableString.Invariant($"[{codePoint:X4}]");
                _remainingIdx = 0;
                return true;
            }

            public override char GetNextChar()
            {
                return (_remainingIdx < _remaining.Length)
                    ? _remaining[_remainingIdx++]
                    : '\0' /* end of string reached */;
            }

            public override bool MovePrevious()
            {
                if (_remainingIdx == 0)
                {
                    return false;
                }

                _remainingIdx--;
                return true;
            }
        }
    }
}
