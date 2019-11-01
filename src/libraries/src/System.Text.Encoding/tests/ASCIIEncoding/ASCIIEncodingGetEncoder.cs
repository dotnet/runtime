// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Tests
{
    public class ASCIIEncodingGetEncoder
    {
        [Fact]
        public void GetEncoder()
        {
            Encoder decoder = new ASCIIEncoding().GetEncoder();
            Assert.NotNull(decoder);
        }
    }
}
