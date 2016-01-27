// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Tests
{
    public class UTF8EncodingGetHashCode
    {
        #region Positive Test Cases
        // PosTest1: Two return value equals with two ref of a instance
        [Fact]
        public void PosTest1()
        {
            UTF8Encoding utf8a = new UTF8Encoding();
            UTF8Encoding utf8b = utf8a;
            Assert.Equal(utf8a.GetHashCode(), utf8b.GetHashCode());
        }

        // PosTest2: Two return value is not equal with two instance
        [Fact]
        public void PosTest2()
        {
            UTF8Encoding utf8a = new UTF8Encoding(true);
            UTF8Encoding utf8b = new UTF8Encoding(true, true);
            Assert.NotEqual(utf8a.GetHashCode(), utf8b.GetHashCode());
        }
        #endregion
    }
}
