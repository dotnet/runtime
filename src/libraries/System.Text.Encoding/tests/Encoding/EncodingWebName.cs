// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Tests
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
