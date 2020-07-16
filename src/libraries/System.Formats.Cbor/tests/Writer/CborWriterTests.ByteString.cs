// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Test.Cryptography;
using Xunit;

namespace System.Formats.Cbor.Tests
{
    public partial class CborWriterTests
    {
        // Data points taken from https://tools.ietf.org/html/rfc7049#appendix-A

        [Theory]
        [InlineData("", "40")]
        [InlineData("01020304", "4401020304")]
        [InlineData("ffffffffffffffffffffffffffff", "4effffffffffffffffffffffffffff")]
        public static void WriteByteString_SingleValue_HappyPath(string hexInput, string hexExpectedEncoding)
        {
            byte[] expectedEncoding = hexExpectedEncoding.HexToByteArray();
            byte[] input = hexInput.HexToByteArray();
            var writer = new CborWriter();
            writer.WriteByteString(input);
            AssertHelper.HexEqual(expectedEncoding, writer.Encode());
        }

        [Fact]
        public static void WriteByteString_NullValue_ShouldThrowArgumentNullException()
        {
            var writer = new CborWriter();
            Assert.Throws<ArgumentNullException>(() => writer.WriteByteString(null!));
        }

        [Theory]
        [InlineData(new string[] { }, "5fff")]
        [InlineData(new string[] { "" }, "5f40ff")]
        [InlineData(new string[] { "ab", "" }, "5f41ab40ff")]
        [InlineData(new string[] { "ab", "bc", "" }, "5f41ab41bc40ff")]
        public static void WriteByteString_IndefiniteLength_NoPatching_SingleValue_HappyPath(string[] hexChunkInputs, string hexExpectedEncoding)
        {
            byte[][] chunkInputs = hexChunkInputs.Select(ch => ch.HexToByteArray()).ToArray();
            byte[] expectedEncoding = hexExpectedEncoding.HexToByteArray();

            var writer = new CborWriter(convertIndefiniteLengthEncodings: false);
            Helpers.WriteChunkedByteString(writer, chunkInputs);
            AssertHelper.HexEqual(expectedEncoding, writer.Encode());
        }

        [Theory]
        [InlineData(new string[] { }, "40")]
        [InlineData(new string[] { "" }, "40")]
        [InlineData(new string[] { "ab", "" }, "41ab")]
        [InlineData(new string[] { "ab", "bc", "" }, "42abbc")]
        public static void WriteByteString_IndefiniteLength_WithPatching_SingleValue_HappyPath(string[] hexChunkInputs, string hexExpectedEncoding)
        {
            byte[][] chunkInputs = hexChunkInputs.Select(ch => ch.HexToByteArray()).ToArray();
            byte[] expectedEncoding = hexExpectedEncoding.HexToByteArray();

            var writer = new CborWriter(convertIndefiniteLengthEncodings: true);
            Helpers.WriteChunkedByteString(writer, chunkInputs);
            AssertHelper.HexEqual(expectedEncoding, writer.Encode());
        }

        [Theory]
        [InlineData(nameof(CborWriter.WriteInt64))]
        [InlineData(nameof(CborWriter.WriteTextString))]
        [InlineData(nameof(CborWriter.WriteStartIndefiniteLengthTextString))]
        [InlineData(nameof(CborWriter.WriteStartIndefiniteLengthByteString))]
        [InlineData(nameof(CborWriter.WriteStartArray))]
        [InlineData(nameof(CborWriter.WriteStartMap))]
        [InlineData(nameof(CborWriter.WriteEndIndefiniteLengthTextString))]
        [InlineData(nameof(CborWriter.WriteEndArray))]
        [InlineData(nameof(CborWriter.WriteEndMap))]
        public static void WriteByteString_IndefiniteLength_NestedWrites_ShouldThrowInvalidOperationException(string opName)
        {
            var writer = new CborWriter();
            writer.WriteStartIndefiniteLengthByteString();
            Assert.Throws<InvalidOperationException>(() => Helpers.ExecOperation(writer, opName));
        }

        [Theory]
        [InlineData(nameof(CborWriter.WriteEndIndefiniteLengthTextString))]
        [InlineData(nameof(CborWriter.WriteEndArray))]
        [InlineData(nameof(CborWriter.WriteEndMap))]
        public static void WriteByteString_IndefiniteLength_ImbalancedWrites_ShouldThrowInvalidOperationException(string opName)
        {
            var writer = new CborWriter();
            writer.WriteStartIndefiniteLengthByteString();
            Assert.Throws<InvalidOperationException>(() => Helpers.ExecOperation(writer, opName));
        }

        [Theory]
        [InlineData(CborConformanceMode.Canonical)]
        [InlineData(CborConformanceMode.Ctap2Canonical)]
        public static void WriteStartByteStringIndefiniteLength_NoPatching_UnsupportedConformance_ShouldThrowInvalidOperationException(CborConformanceMode conformanceMode)
        {
            var writer = new CborWriter(conformanceMode, convertIndefiniteLengthEncodings: false);
            Assert.Throws<InvalidOperationException>(() => writer.WriteStartIndefiniteLengthByteString());
        }
    }
}
