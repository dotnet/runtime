// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Tests
{
    public class EncodingGetByteCount2
    {
        #region Positive Test Cases
        // PosTest1: Verify method GetByteCount(System.Char[],System.Int32,System.Int32)
        [Fact]
        public void PosTest1()
        {
            char[] testChar = new char[] { 'z', 'a', '\u0306', '\u01FD', '\u03B2', '\uD8FF', '\uDCFF' };
            Encoding u16LE = Encoding.Unicode;
            Encoding u16BE = Encoding.BigEndianUnicode;
            Assert.Equal(6, u16LE.GetByteCount(testChar, 4, 3));
            Assert.Equal(6, u16BE.GetByteCount(testChar, 4, 3));
        }
        #endregion
    }
}
