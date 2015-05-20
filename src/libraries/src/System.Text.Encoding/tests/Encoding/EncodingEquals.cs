// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    public class EncodingEquals
    {
        #region Positive Test Cases
        // Verify method Equals
        [Fact]
        public void PosTest1()
        {
            Encoding e1 = Encoding.GetEncoding("utf-8");
            Encoding e2 = Encoding.UTF8;
            Assert.True(e1.Equals(e2));
        }
        #endregion
    }
}
