// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Tests
{
    // GetChars(System.Byte[],System.Int32,System.Int32,System.Char[],System.Int32)
    public class UTF8EncodingGetChars
    {
        #region Positive Test Cases
        // PosTest1: Verify method GetChars with non-null chars
        [Fact]
        public void PosTest1()
        {
            Byte[] bytes;
            Char[] chars = new Char[] {
                            '\u0023',
                            '\u0025',
                            '\u03a0',
                            '\u03a3'  };

            UTF8Encoding utf8 = new UTF8Encoding();
            int byteCount = utf8.GetByteCount(chars, 1, 2);
            bytes = new Byte[byteCount];
            int charsEncodedCount = utf8.GetChars(bytes, 1, 2, chars, 0);
        }

        // PosTest2: Verify method GetChars with null chars
        [Fact]
        public void PosTest2()
        {
            Byte[] bytes;
            Char[] chars = new Char[] { };

            UTF8Encoding utf8 = new UTF8Encoding();

            int byteCount = utf8.GetByteCount(chars, 0, 0);
            bytes = new Byte[byteCount];
            int charsEncodedCount = utf8.GetChars(bytes, 0, 0, chars, 0);
            Assert.Equal(0, charsEncodedCount);
        }
        #endregion
    }
}
