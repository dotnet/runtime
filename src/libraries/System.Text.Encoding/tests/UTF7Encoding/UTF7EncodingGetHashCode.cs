// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Tests
{
    public class UTF7EncodingGetHashCode
    {
        // PosTest1: Two return value equals with two ref of a instance
        [Fact]
        public void PosTest1()
        {
            UTF7Encoding utf7a = new UTF7Encoding();
            UTF7Encoding utf7b = utf7a;
            Assert.Equal(utf7a.GetHashCode(), utf7b.GetHashCode());
        }
    }
}
