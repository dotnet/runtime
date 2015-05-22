// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    public class UTF8EncodingGetPreamble
    {
        #region Positive Test Cases
        // PosTest1: Verify method GetPreamble
        [Fact]
        public void PosTest1()
        {
            UTF8Encoding UTF8NoPreamble = new UTF8Encoding();
            Byte[] preamble;

            preamble = UTF8NoPreamble.GetPreamble();
        }
        #endregion
    }
}
