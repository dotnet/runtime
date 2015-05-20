// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
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
