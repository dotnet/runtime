// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Tests
{
    public class EncodingGetPreamble
    {
        #region Positive Test Cases
        // PosTest1: Verify method GetPreamble
        [Fact]
        public void PosTest1()
        {
            Encoding unicode = Encoding.Unicode;
            byte[] preamble = unicode.GetPreamble();
            Assert.Equal(0xFF, preamble[0]);
            Assert.Equal(0xFE, preamble[1]);
        }
        #endregion
    }
}
