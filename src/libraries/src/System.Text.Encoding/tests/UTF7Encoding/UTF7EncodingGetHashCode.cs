// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Tests
{
    public class UTF7EncodingGetHashCode
    {
        [Fact]
        public void GetHashCodeTest()
        {
            UTF7Encoding encoding = new UTF7Encoding();
            Assert.Equal(encoding.GetHashCode(), encoding.GetHashCode());
        }
    }
}
