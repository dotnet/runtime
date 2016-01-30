// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Tests
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
