// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encoding.Tests.Cbor
{
    public partial class CborWriterTests
    {
        [Fact]
        public static void IsWriteCompleted_OnWrittenPrimitive_ShouldBeTrue()
        {
            using var writer = new CborWriter();
            Assert.False(writer.IsWriteCompleted);
            writer.WriteInt64(42);
            Assert.True(writer.IsWriteCompleted);
        }

        [Fact]
        public static void ToArray_OnInCompleteValue_ShouldThrowInvalidOperationExceptoin()
        {
            using var writer = new CborWriter();
            Assert.Throws<InvalidOperationException>(() => writer.ToArray());
        }

        [Fact]
        public static void CborWriter_WritingTwoPrimitiveValues_ShouldThrowInvalidOperationException()
        {
            using var writer = new CborWriter();
            writer.WriteInt64(42);
            Assert.Throws<InvalidOperationException>(() => writer.WriteTextString("lorem ipsum"));
        }

        [Fact]
        public static void BytesWritten_SingleValue_ShouldReturnBytesWritten()
        {
            using var writer = new CborWriter();
            Assert.Equal(0, writer.BytesWritten);
            writer.WriteTextString("test");
            Assert.Equal(5, writer.BytesWritten);
        }

        [Theory]
        [MemberData(nameof(EncodedValueInputs))]
        public static void WriteEncodedValue_RootValue_HappyPath(string hexEncodedValue)
        {
            byte[] encodedValue = hexEncodedValue.HexToByteArray();

            using var writer = new CborWriter();
            writer.WriteEncodedValue(encodedValue);

            string hexResult = writer.ToArray().ByteArrayToHex();
            Assert.Equal(hexEncodedValue, hexResult.ToLower());
        }

        [Theory]
        [MemberData(nameof(EncodedValueInputs))]
        public static void WriteEncodedValue_NestedValue_HappyPath(string hexEncodedValue)
        {
            byte[] encodedValue = hexEncodedValue.HexToByteArray();

            using var writer = new CborWriter();
            writer.WriteStartArray(3);
            writer.WriteInt64(1);
            writer.WriteEncodedValue(encodedValue);
            writer.WriteTextString("");
            writer.WriteEndArray();

            string hexResult = writer.ToArray().ByteArrayToHex();
            Assert.Equal("8301" + hexEncodedValue + "60", hexResult.ToLower());
        }

        public const string Enc = Helpers.EncodedPrefixIdentifier;

        [Theory]
        [InlineData(new object[] { new object[] { Enc, "8101" } }, true, "818101")]
        [InlineData(new object[] { new object[] { Enc, "8101" } }, false, "9f8101ff")]
        [InlineData(new object[] { Map, new object[] { Enc, "8101" }, 42 }, true, "a18101182a")]
        [InlineData(new object[] { Map, new object[] { Enc, "8101" }, 42 }, false, "bf8101182aff")]
        [InlineData(new object[] { Map, 42, new object[] { Enc, "8101" } }, true, "a1182a8101")]
        [InlineData(new object[] { Map, 42, new object[] { Enc, "8101" } }, false, "bf182a8101ff")]

        public static void WriteEncodedValue_ContextScenaria_HappyPath(object value, bool useDefiniteLength, string hexExpectedEncoding)
        {
            using var writer = new CborWriter();

            Helpers.WriteValue(writer, value, useDefiniteLengthCollections: useDefiniteLength);

            string hexEncoding = writer.ToArray().ByteArrayToHex().ToLower();
            Assert.Equal(hexExpectedEncoding, hexEncoding);
        }

        [Fact]
        public static void WriteEncodedValue_IndefiniteLengthTextString_HappyPath()
        {
            using var writer = new CborWriter();

            writer.WriteStartTextStringIndefiniteLength();
            writer.WriteTextString("foo");
            writer.WriteEncodedValue("63626172".HexToByteArray());
            writer.WriteEndTextStringIndefiniteLength();

            byte[] encoding = writer.ToArray();
            Assert.Equal("7f63666f6f63626172ff", encoding.ByteArrayToHex().ToLower());
        }

        [Fact]
        public static void WriteEncodedValue_IndefiniteLengthByteString_HappyPath()
        {
            using var writer = new CborWriter();

            writer.WriteStartByteStringIndefiniteLength();
            writer.WriteByteString(new byte[] { 1, 1, 1 });
            writer.WriteEncodedValue("43020202".HexToByteArray());
            writer.WriteEndByteStringIndefiniteLength();

            byte[] encoding = writer.ToArray();
            Assert.Equal("5f4301010143020202ff", encoding.ByteArrayToHex().ToLower());
        }

        [Fact]
        public static void WriteEncodedValue_BadIndefiniteLengthStringValue_ShouldThrowInvalidOperationException()
        {
            using var writer = new CborWriter();
            writer.WriteStartTextStringIndefiniteLength();
            Assert.Throws<InvalidOperationException>(() => writer.WriteEncodedValue(new byte[] { 0x01 }));
        }

        [Fact]
        public static void WriteEncodedValue_AtEndOfDefiniteLengthCollection_ShouldThrowInvalidOperationException()
        {
            using var writer = new CborWriter();
            writer.WriteInt64(0);
            Assert.Throws<InvalidOperationException>(() => writer.WriteEncodedValue(new byte[] { 0x01 }));
        }

        [Theory]
        [MemberData(nameof(EncodedValueBadInputs))]
        public static void WriteEncodedValue_InvalidCbor_ShouldThrowArgumentException(string hexEncodedInput)
        {
            byte[] encodedInput = hexEncodedInput.HexToByteArray();
            using var writer = new CborWriter();
            Assert.Throws<ArgumentException>(() => writer.WriteEncodedValue(encodedInput));
        }

        [Fact]
        public static void WriteEncodedValue_ValidPayloadWithTrailingBytes_ShouldThrowArgumentException()
        {
            using var writer = new CborWriter();
            Assert.Throws<ArgumentException>(() => writer.WriteEncodedValue(new byte[] { 0x01, 0x01 }));
        }

        public static IEnumerable<object[]> EncodedValueInputs => CborReaderTests.SampleCborValues.Select(x => new [] { x });
        public static IEnumerable<object[]> EncodedValueBadInputs => CborReaderTests.InvalidCborValues.Select(x => new[] { x });
    }
}
