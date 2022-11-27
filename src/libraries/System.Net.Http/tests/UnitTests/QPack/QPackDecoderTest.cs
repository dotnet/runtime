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

        private const string _headerNameString = "literal-header-field"; // 20 bytes
        private const string _headerValueString = "should-not-break"; // 16 bytes

        private static readonly byte[] _headerValueBytes = Encoding.ASCII.GetBytes(_headerValueString);

        private static readonly byte[] _headerValue = new byte[] { (byte)_headerValueBytes.Length }
            .Concat(_headerValueBytes)
            .ToArray();

        // The key take away here is that the header name length should be
        // at least 2^n + 1 and the header value length less than or equal to 2^n.
        // This is due to how System.Buffer buckets the arrays to be used by the decoder.
        private static readonly byte[] _encodedLiteralField = _literalFieldWithoutNameReference
            .Concat(_headerValue)
            .ToArray();

        private readonly QPackDecoder _decoder;
        private readonly TestHttpHeadersHandler _handler = new TestHttpHeadersHandler();

        public QPackDecoderTest()
        {
            _decoder = new QPackDecoder(MaxHeaderFieldSize);
        }

        [Fact]
        public void LiteralFieldWithoutNameReferece_SingleBuffer()
        {
            _decoder.Decode(new byte[] { 0, 0 }, endHeaders: false, handler: _handler);
            _decoder.Decode(_encodedLiteralField, endHeaders: true, handler: _handler);

            Assert.Equal(1, _handler.DecodedHeaders.Count);
            Assert.Equal(_headerNameString, _handler.DecodedHeaders.Keys.First());
            Assert.Equal(_headerValueString, _handler.DecodedHeaders.Values.First());
        }

        [Fact]
        public void LiteralFieldWithoutNameReferece_NameLengthBrokenIntoSeparateBuffers()
        {
            _decoder.Decode(new byte[] { 0, 0 }, endHeaders: false, handler: _handler);
            _decoder.Decode(_encodedLiteralField[..1], endHeaders: false, handler: _handler);
            _decoder.Decode(_encodedLiteralField[1..], endHeaders: true, handler: _handler);

            Assert.Equal(1, _handler.DecodedHeaders.Count);
            Assert.Equal(_headerNameString, _handler.DecodedHeaders.Keys.First());
            Assert.Equal(_headerValueString, _handler.DecodedHeaders.Values.First());
        }

        [Fact]
        public void LiteralFieldWithoutNameReferece_NameBrokenIntoSeparateBuffers()
        {
            _decoder.Decode(new byte[] { 0, 0 }, endHeaders: false, handler: _handler);
            _decoder.Decode(_encodedLiteralField[..(_headerNameString.Length / 2)], endHeaders: false, handler: _handler);
            _decoder.Decode(_encodedLiteralField[(_headerNameString.Length / 2)..], endHeaders: true, handler: _handler);

            Assert.Equal(1, _handler.DecodedHeaders.Count);
            Assert.Equal(_headerNameString, _handler.DecodedHeaders.Keys.First());
            Assert.Equal(_headerValueString, _handler.DecodedHeaders.Values.First());
        }

        [Fact]
        public void LiteralFieldWithoutNameReferece_NameAndValueBrokenIntoSeparateBuffers()
        {
            _decoder.Decode(new byte[] { 0, 0 }, endHeaders: false, handler: _handler);
            _decoder.Decode(_encodedLiteralField[..^_headerValue.Length], endHeaders: false, handler: _handler);
            _decoder.Decode(_encodedLiteralField[^_headerValue.Length..], endHeaders: true, handler: _handler);

            Assert.Equal(1, _handler.DecodedHeaders.Count);
            Assert.Equal(_headerNameString, _handler.DecodedHeaders.Keys.First());
            Assert.Equal(_headerValueString, _handler.DecodedHeaders.Values.First());
        }

        [Fact]
        // Ideally should be ran with a value length of or bigger than 2 bytes
        public void LiteralFieldWithoutNameReferece_ValueLengthBrokenIntoSeparateBuffers()
        {
            _decoder.Decode(new byte[] { 0, 0 }, endHeaders: false, handler: _handler);
            _decoder.Decode(_encodedLiteralField[..^(_headerValue.Length - 1)], endHeaders: false, handler: _handler);
            _decoder.Decode(_encodedLiteralField[^(_headerValue.Length - 1)..], endHeaders: true, handler: _handler);

            Assert.Equal(1, _handler.DecodedHeaders.Count);
            Assert.Equal(_headerNameString, _handler.DecodedHeaders.Keys.First());
            Assert.Equal(_headerValueString, _handler.DecodedHeaders.Values.First());
        }

        [Fact]
        public void LiteralFieldWithoutNameReferece_ValueBrokenIntoSeparateBuffers()
        {
            _decoder.Decode(new byte[] { 0, 0 }, endHeaders: false, handler: _handler);
            _decoder.Decode(_encodedLiteralField[..^(_headerValueString.Length / 2)], endHeaders: false, handler: _handler);
            _decoder.Decode(_encodedLiteralField[^(_headerValueString.Length / 2)..], endHeaders: true, handler: _handler);

            Assert.Equal(1, _handler.DecodedHeaders.Count);
            Assert.Equal(_headerNameString, _handler.DecodedHeaders.Keys.First());
            Assert.Equal(_headerValueString, _handler.DecodedHeaders.Values.First());
        }
    }
}
