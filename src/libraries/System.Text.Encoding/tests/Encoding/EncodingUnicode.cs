// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    public class EncodingUnicode
    {
        #region Positive Test Cases
        // PosTest1: Verify property Unicode
        [Fact]
        public void PosTest1()
        {
            Encoding encoding = Encoding.Unicode;
            Assert.NotNull(encoding);
        }
        #endregion
    }
}
