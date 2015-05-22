// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    // BigEndianUnicode
    public class EncodingBigEndianUnicode
    {
        #region Positive Test Cases
        // PosTest1: Verify property BigEndianUnicode
        [Fact]
        public void PosTest1()
        {
            Encoding ascii = Encoding.BigEndianUnicode;
            Assert.NotNull(ascii);
        }
        #endregion
    }
}
