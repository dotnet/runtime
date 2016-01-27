// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Tests
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
