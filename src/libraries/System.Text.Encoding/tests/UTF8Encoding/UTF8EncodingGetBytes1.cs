// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Tests
{
    // GetBytes(System.Char[],System.Int32,System.Int32,System.Byte[],System.Int32)
    public class UTF8EncodingGetBytes1
    {
        #region Positive Test Cases
        // PosTest1: Verify method GetBytes(Char[],Int32,Int32,Byte[],Int32) with non-null chars
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
            int bytesEncodedCount = utf8.GetBytes(chars, 1, 2, bytes, 0);
        }

        // PosTest2: Verify method GetBytes(Char[],Int32,Int32,Byte[],Int32) with null chars
        [Fact]
        public void PosTest2()
        {
            Byte[] bytes;
            Char[] chars = new Char[] { };

            UTF8Encoding utf8 = new UTF8Encoding();

            int byteCount = utf8.GetByteCount(chars, 0, 0);
            bytes = new Byte[byteCount];
            int bytesEncodedCount = utf8.GetBytes(chars, 0, 0, bytes, 0);
            Assert.Equal(0, bytesEncodedCount);
        }
        #endregion
    }
}
