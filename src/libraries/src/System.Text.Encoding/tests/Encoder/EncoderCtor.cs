// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Tests
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
