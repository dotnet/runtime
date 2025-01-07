// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class DecoderFallbackTests
    {
        private class Utf8DecoderFallBack : DecoderFallback
        {
            public override int MaxCharCount => 0;

            public override DecoderFallbackBuffer CreateFallbackBuffer() => new Utf8DecoderFallbackBuffer();
        }

        private class Utf8DecoderFallbackBuffer : DecoderFallbackBuffer
        {
            public override int Remaining => 0;

            public override bool Fallback(byte[] bytesUnknown, int index)
            {
                if (index < 0)
                    throw new DecoderFallbackException("Encountered a negative index during Utf8 decoding fallback ");

                return false;
            }

            public override char GetNextChar() => (char)0;

            public override bool MovePrevious() => false;
        }

        [Fact]
        public static void TestDecoderFallbackIndex()
        {
            // This test case ensuring when we fallback, we'll never encounter a negative index in
            // Utf8DecoderFallbackBuffer.Fallback
            var e = (Encoding) Encoding.UTF8.Clone();
            e.DecoderFallback = new Utf8DecoderFallBack();

            // Assert.NoThrow
            e.GetString(new byte[] { 255 });
        }
    }
}
