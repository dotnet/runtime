// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    public class EncodingWebName
    {
        #region Positive Test Cases
        // PosTest1: Verify property WebName.
        [Fact]
        public void PosTest1()
        {
            Assert.Equal("utf-8", Encoding.UTF8.WebName);
            Assert.Equal("utf-16", Encoding.Unicode.WebName);
        }

        // PosTest2: Round-trip WebNames.
        [Fact]
        public void PosTest2()
        {
            Assert.Equal(Encoding.UTF8, Encoding.GetEncoding(Encoding.UTF8.WebName));
            Assert.Equal(Encoding.Unicode, Encoding.GetEncoding(Encoding.Unicode.WebName));
            Assert.Equal(Encoding.BigEndianUnicode, Encoding.GetEncoding(Encoding.BigEndianUnicode.WebName));
        }
        #endregion
    }
}
