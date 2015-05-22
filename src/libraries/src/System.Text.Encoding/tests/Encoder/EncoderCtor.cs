// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    public class TestEncoder : Encoder
    {
        public override int GetByteCount(char[] chars, int index, int count, bool flush)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex, bool flush)
        {
            throw new Exception("The method or operation is not implemented.");
        }
    }

    public class EncoderCtor
    {
        #region Positive Test Cases
        // PosTest1: Call ctor to construct a new instance
        [Fact]
        public void PosTest1()
        {
            Encoder encoder = new TestEncoder();
            Assert.NotNull(encoder);
        }
        #endregion
    }
}
