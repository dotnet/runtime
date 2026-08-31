// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Text.Tests
{
    public class ASCIIEncodingGetDecoder
    {
        [Fact]
        public void GetDecoder()
        {
            Decoder decoder = new ASCIIEncoding().GetDecoder();
            Assert.NotNull(decoder);
        }
    }
}
