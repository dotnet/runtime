// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Xunit;

namespace System.Net.Mime.Tests
{
    public class EncodedStreamFactoryTests
    {
        [Fact]
        public void EncodedStreamFactory_WhenAskedForEncodedStreamForHeader_WithBase64_ShouldReturnBase64Stream()
        {
            var esf = new EncodedStreamFactory();
            IEncodableStream test = esf.GetEncoderForHeader(Encoding.UTF8, true, 5);
            Assert.True(test is Base64Stream);
        }
    }
}
