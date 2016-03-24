// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Tests
{
    // GetString(System.Byte[],System.Int32,System.Int32)
    public class UTF8EncodingGetString
    {
        #region Positive Test Cases
        [Fact]
        public void PosTest1()
        {
            Byte[] bytes = new Byte[] {
                             85,  84,  70,  56,  32,  69, 110,
                             99, 111, 100, 105, 110, 103,  32,
                             69, 120,  97, 109, 112, 108, 101};

            UTF8Encoding utf8 = new UTF8Encoding();
            string str = utf8.GetString(bytes, 0, bytes.Length);
        }
        #endregion
    }
}
