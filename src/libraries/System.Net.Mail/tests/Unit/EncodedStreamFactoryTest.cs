// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;
using Xunit;

namespace System.Net.Mime.Tests
{
    public class EncodedStreamFactoryTests
    {
        [Fact]
        public void EncodedStreamFactory_WhenAskedForEncodedStream_WithQuotedPrintable_ShouldReturnQuotedPrintableStream()
        {
            var esf = new EncodedStreamFactory();
            var stream = new MemoryStream();
            IEncodableStream test = esf.GetEncoder(TransferEncoding.QuotedPrintable, stream);

            Assert.True(test is QuotedPrintableStream, "Factory returned wrong stream");
            Assert.True(test.GetStream() != null, "GetStream was null");
            Assert.True(test.GetStream() is Stream, "GetStream was not a Stream");
        }


        [Fact]
        public void EncodedStreamFactory_WhenAskedForEncodedStream_WithBase64_ShouldReturnBase64Stream()
        {
            var esf = new EncodedStreamFactory();
            Stream stream = new MemoryStream();
            IEncodableStream test = esf.GetEncoder(TransferEncoding.Base64, stream);

            Assert.True(test is Base64Stream);
            Assert.True(test.GetStream() != null);
            Assert.True(test.GetStream() is Stream);
        }

        [Fact]
        public void EncodedStreamFactory_WhenAskedForEncodedStream_WithUnknown_ShouldThrow()
        {
            var esf = new EncodedStreamFactory();
            var stream = new MemoryStream();
            Assert.Throws<NotSupportedException>(() => esf.GetEncoder(TransferEncoding.Unknown, stream));
        }

        [Fact]
        public void EncodedStreamFactory_WhenAskedForEncodedStreamForHeader_WithBase64_ShouldReturnBase64Stream()
        {
            var esf = new EncodedStreamFactory();
            IEncodableStream test = esf.GetEncoderForHeader(Encoding.UTF8, true, 5);
            Assert.True(test is Base64Stream);
        }
    }
}
