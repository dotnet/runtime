// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Xunit;

namespace System.Net.Mime.Tests
{
    public class EncodedStreamFactoryTests
    {
        [Fact]
        public void EncodedStreamFactory_WhenAskedForEncodedStreamForHeader_WithBase64_ShouldReturnBase64Stream()
        {
            IEncodableStream test = EncodedStreamFactory.GetEncoderForHeader(Encoding.UTF8, true, 5);
            Assert.True(test is Base64Stream);
        }
    }
}
