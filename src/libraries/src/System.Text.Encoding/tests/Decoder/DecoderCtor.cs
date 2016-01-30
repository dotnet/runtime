// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Tests
{
    public class DecoderCtorDecoder : Decoder
    {
        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            throw new Exception("The method or operation is not implemented.");
        }
    }


    public class DecoderCtor
    {
        // PosTest1: Call ctor to create a decoder instance
        [Fact]
        public void PosTest1()
        {
            DecoderCtorDecoder decoder = new DecoderCtorDecoder();
            Assert.NotNull(decoder);
        }
    }
}
