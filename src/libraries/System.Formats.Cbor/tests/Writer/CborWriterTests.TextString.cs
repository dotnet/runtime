// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Test.Cryptography;
using Xunit;

namespace System.Formats.Cbor.Tests
{
    public partial class CborWriterTests
    {
        [Theory]
        [InlineData("", "60")]
        [InlineData("a", "6161")]
        [InlineData("IETF", "6449455446")]
        [InlineData("\"\\", "62225c")]
        [InlineData("\u00fc", "62c3bc")]
        [InlineData("\u6c34", "63e6b0b4")]
        [InlineData("\ud800\udd51", "64f0908591")]
        public static void WriteTextString_SingleValue_HappyPath(string input, string hexExpectedEncoding)
        {
            byte[] expectedEncoding = hexExpectedEncoding.HexToByteArray();
            var writer = new CborWriter();
            writer.WriteTextString(input);
            AssertHelper.HexEqual(expectedEncoding, writer.Encode());
        }

        [Theory]
        [InlineData(new string[] { }, "7fff")]
        [InlineData(new string[] { "" }, "7f60ff")]
        [InlineData(new string[] { "ab", "" }, "7f62616260ff")]
        [InlineData(new string[] { "ab", "bc", "" }, "7f62616262626360ff")]
        public static void WriteTextString_IndefiniteLength_NoPatching_SingleValue_HappyPath(string[] chunkInputs, string hexExpectedEncoding)
        {
            byte[] expectedEncoding = hexExpectedEncoding.HexToByteArray();
            var writer = new CborWriter(convertIndefiniteLengthEncodings: false);
            Helpers.WriteChunkedTextString(writer, chunkInputs);
            AssertHelper.HexEqual(expectedEncoding, writer.Encode());
        }

        [Theory]
        [InlineData(new string[] { }, "60")]
        [InlineData(new string[] { "" }, "60")]
        [InlineData(new string[] { "ab", "" }, "626162")]
        [InlineData(new string[] { "ab", "bc", "" }, "6461626263")]
        public static void WriteTextString_IndefiniteLength_WithPatching_SingleValue_HappyPath(string[] chunkInputs, string hexExpectedEncoding)
        {
            byte[] expectedEncoding = hexExpectedEncoding.HexToByteArray();
            var writer = new CborWriter(convertIndefiniteLengthEncodings: true);
            Helpers.WriteChunkedTextString(writer, chunkInputs);
            AssertHelper.HexEqual(expectedEncoding, writer.Encode());
        }

        [Fact]
        public static void WriteTextString_NullValue_ShouldThrowArgumentNullException()
        {
            var writer = new CborWriter();
            Assert.Throws<ArgumentNullException>(() => writer.WriteTextString(null!));
        }

        [Theory]
        [InlineData(CborConformanceMode.Lax)]
        public static void WriteTextString_InvalidUnicodeString_LaxConformance_ShouldSucceed(CborConformanceMode conformanceMode)
        {
            string invalidUnicodeString = "\ud800";
            byte[] expectedEncoding = { 0x63, 0xef, 0xbf, 0xbd };

            var writer = new CborWriter(conformanceMode);
            writer.WriteTextString(invalidUnicodeString);
            AssertHelper.HexEqual(expectedEncoding, writer.Encode());
        }

        [Theory]
        [InlineData(CborConformanceMode.Strict)]
        [InlineData(CborConformanceMode.Canonical)]
        [InlineData(CborConformanceMode.Ctap2Canonical)]
        public static void WriteTextString_InvalidUnicodeString_StrictConformance_ShouldThrowArgumentException(CborConformanceMode conformanceMode)
        {
            // NB Xunit's InlineDataAttribute will corrupt string literals containing invalid unicode
            string invalidUnicodeString = "\ud800";
            var writer = new CborWriter(conformanceMode);
            ArgumentException exn = Assert.Throws<ArgumentException>(() => writer.WriteTextString(invalidUnicodeString));
            Assert.NotNull(exn.InnerException);
            Assert.IsType<System.Text.EncoderFallbackException>(exn.InnerException);
        }

        [Theory]
        [InlineData(nameof(CborWriter.WriteInt64))]
        [InlineData(nameof(CborWriter.WriteByteString))]
        [InlineData(nameof(CborWriter.WriteStartIndefiniteLengthTextString))]
        [InlineData(nameof(CborWriter.WriteStartIndefiniteLengthByteString))]
        [InlineData(nameof(CborWriter.WriteStartArray))]
        [InlineData(nameof(CborWriter.WriteStartMap))]
        public static void WriteTextString_IndefiniteLength_NestedWrites_ShouldThrowInvalidOperationException(string opName)
        {
            var writer = new CborWriter();
            writer.WriteStartIndefiniteLengthTextString();
            Assert.Throws<InvalidOperationException>(() => Helpers.ExecOperation(writer, opName));
        }

        [Theory]
        [InlineData(nameof(CborWriter.WriteEndIndefiniteLengthByteString))]
        [InlineData(nameof(CborWriter.WriteEndArray))]
        [InlineData(nameof(CborWriter.WriteEndMap))]
        public static void WriteTextString_IndefiniteLength_ImbalancedWrites_ShouldThrowInvalidOperationException(string opName)
        {
            var writer = new CborWriter();
            writer.WriteStartIndefiniteLengthTextString();
            Assert.Throws<InvalidOperationException>(() => Helpers.ExecOperation(writer, opName));
        }

        [Theory]
        [InlineData(CborConformanceMode.Canonical)]
        [InlineData(CborConformanceMode.Ctap2Canonical)]
        public static void WriteStartTextStringIndefiniteLength_NoPatching_UnsupportedConformance_ShouldThrowInvalidOperationException(CborConformanceMode conformanceMode)
        {
            var writer = new CborWriter(conformanceMode, convertIndefiniteLengthEncodings: false);
            Assert.Throws<InvalidOperationException>(() => writer.WriteStartIndefiniteLengthTextString());
        }
    }
}
