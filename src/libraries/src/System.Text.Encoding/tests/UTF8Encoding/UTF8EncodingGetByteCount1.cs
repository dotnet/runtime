// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Tests
{
    // GetByteCount(System.Char[],System.Int32,System.Int32)
    public class UTF8EncodingGetByteCount1
    {
        #region Positive Test Cases
        // PosTest1: Verify method GetByteCount(Char[],Int32,Int32) with non-null char[]
        [Fact]
        public void PosTest1()
        {
            Char[] chars = new Char[] {
                            '\u0023',
                            '\u0025',
                            '\u03a0',
                            '\u03a3'  };
            UTF8Encoding utf8 = new UTF8Encoding();
            int byteCount = utf8.GetByteCount(chars, 1, 2);
        }

        // PosTest2: Verify method GetByteCount(Char[],Int32,Int32) with null char[]
        [Fact]
        public void PosTest2()
        {
            Char[] chars = new Char[] { };
            UTF8Encoding utf8 = new UTF8Encoding();
            int byteCount = utf8.GetByteCount(chars, 0, 0);
            Assert.Equal(0, byteCount);
        }
        #endregion
    }
}
