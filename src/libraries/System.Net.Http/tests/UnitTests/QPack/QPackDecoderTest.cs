// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Net.Http.QPack;
using System.Net.Http.Unit.Tests.HPack;
using System.Text;

using Xunit;

namespace System.Net.Http.Unit.Tests.QPack
{
    public class QPackDecoderTest
    {
        private const int MaxHeaderFieldSize = 8190;

        // 4.5.6 - Literal Field Without Name Reference - (literal-header-field)
        private static readonly byte[] _literalFieldWithoutNameReference = new byte[] { 0x37, 0x0d, 0x6c, 0x69, 0x74, 0x65, 0x72, 0x61, 0x6c, 0x2d, 0x68, 0x65, 0x61, 0x64, 0x65, 0x72, 0x2d, 0x66, 0x69, 0x65, 0x6c, 0x64 };

        private const string _headerNameString = "literal-header-field";
        private const string _headerValueString = "should-not-break";

        private static readonly byte[] _headerValueBytes = Encoding.ASCII.GetBytes(_headerValueString);

        private static readonly byte[] _headerValue = new byte[] { (byte)_headerValueBytes.Length }
            .Concat(_headerValueBytes)
            .ToArray();

        private readonly QPackDecoder _decoder;
        private readonly TestHttpHeadersHandler _handler = new TestHttpHeadersHandler();

        public QPackDecoderTest()
        {
            _decoder = new QPackDecoder(MaxHeaderFieldSize);
        }

        [Fact]
        public void DecodesLiteralField_WithoutNameReferece()
        {
            // The key take away here is that the header name length should be bigger than 16 bytes
            // and the header value length less than or equal to 16 bytes and they cannot all be
            // read at once, they must be broken into separate buffers
            byte[] encoded = _literalFieldWithoutNameReference
                .Concat(_headerValue)
                .ToArray();

            _decoder.Decode(new byte[] { 0, 0 }, endHeaders: false, handler: _handler);
            _decoder.Decode(encoded[..^7], endHeaders: false, handler: _handler);
            _decoder.Decode(encoded[^7..], endHeaders: true, handler: _handler);

            Assert.Equal(1, _handler.DecodedHeaders.Count);
            Assert.Equal(_headerNameString, _handler.DecodedHeaders.Keys.First());
            Assert.Equal(_headerValueString, _handler.DecodedHeaders.Values.First());
        }
    }
}
