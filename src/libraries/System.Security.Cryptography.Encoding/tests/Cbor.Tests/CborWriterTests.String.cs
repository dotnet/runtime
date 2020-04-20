// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using System.Linq;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encoding.Tests.Cbor
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
            using var writer = new CborWriter();
            writer.WriteByteString(input);
            AssertHelper.HexEqual(expectedEncoding, writer.ToArray());
        }

        [Theory]
        [InlineData(new string[] { }, "5fff")]
        [InlineData(new string[] { "" }, "5f40ff")]
        [InlineData(new string[] { "ab", "" }, "5f41ab40ff")]
        [InlineData(new string[] { "ab", "bc", "" }, "5f41ab41bc40ff")]
        public static void WriteByteString_IndefiteLength_SingleValue_HappyPath(string[] hexChunkInputs, string hexExpectedEncoding)
        {
            byte[][] chunkInputs = hexChunkInputs.Select(ch => ch.HexToByteArray()).ToArray();
            byte[] expectedEncoding = hexExpectedEncoding.HexToByteArray();

            using var writer = new CborWriter();
            Helpers.WriteChunkedByteString(writer, chunkInputs);
            AssertHelper.HexEqual(expectedEncoding, writer.ToArray());
        }

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
            using var writer = new CborWriter();
            writer.WriteTextString(input);
            AssertHelper.HexEqual(expectedEncoding, writer.ToArray());
        }

        [Theory]
        [InlineData(new string[] { }, "7fff")]
        [InlineData(new string[] { "" }, "7f60ff")]
        [InlineData(new string[] { "ab", "" }, "7f62616260ff")]
        [InlineData(new string[] { "ab", "bc", "" }, "7f62616262626360ff")]
        public static void WriteTextString_IndefiniteLength_SingleValue_HappyPath(string[] chunkInputs, string hexExpectedEncoding)
        {
            byte[] expectedEncoding = hexExpectedEncoding.HexToByteArray();
            using var writer = new CborWriter();
            Helpers.WriteChunkedTextString(writer, chunkInputs);
            AssertHelper.HexEqual(expectedEncoding, writer.ToArray());
        }

        [Fact]
        public static void WriteTextString_InvalidUnicodeString_ShouldThrowArgumentException()
        {
            // NB Xunit's InlineDataAttribute will corrupt string literals containing invalid unicode
            string invalidUnicodeString = "\ud800";
            using var writer = new CborWriter();
            ArgumentException exn = Assert.Throws<ArgumentException>(() => writer.WriteTextString(invalidUnicodeString));
            Assert.NotNull(exn.InnerException);
            Assert.IsType<System.Text.EncoderFallbackException>(exn.InnerException);
        }

        [Theory]
        [InlineData(nameof(CborWriter.WriteInt64))]
        [InlineData(nameof(CborWriter.WriteByteString))]
        [InlineData(nameof(CborWriter.WriteStartTextStringIndefiniteLength))]
        [InlineData(nameof(CborWriter.WriteStartByteStringIndefiniteLength))]
        [InlineData(nameof(CborWriter.WriteStartArray))]
        [InlineData(nameof(CborWriter.WriteStartMap))]
        public static void WriteTextString_IndefiniteLength_NestedWrites_ShouldThrowInvalidOperationException(string opName)
        {
            using var writer = new CborWriter();
            writer.WriteStartTextStringIndefiniteLength();
            Assert.Throws<InvalidOperationException>(() => Helpers.ExecOperation(writer, opName));
        }

        [Theory]
        [InlineData(nameof(CborWriter.WriteEndByteStringIndefiniteLength))]
        [InlineData(nameof(CborWriter.WriteEndArray))]
        [InlineData(nameof(CborWriter.WriteEndMap))]
        public static void WriteTextString_IndefiniteLength_ImbalancedWrites_ShouldThrowInvalidOperationException(string opName)
        {
            using var writer = new CborWriter();
            writer.WriteStartTextStringIndefiniteLength();
            Assert.Throws<InvalidOperationException>(() => Helpers.ExecOperation(writer, opName));
        }

        [Theory]
        [InlineData(nameof(CborWriter.WriteInt64))]
        [InlineData(nameof(CborWriter.WriteTextString))]
        [InlineData(nameof(CborWriter.WriteStartTextStringIndefiniteLength))]
        [InlineData(nameof(CborWriter.WriteStartByteStringIndefiniteLength))]
        [InlineData(nameof(CborWriter.WriteStartArray))]
        [InlineData(nameof(CborWriter.WriteStartMap))]
        [InlineData(nameof(CborWriter.WriteEndTextStringIndefiniteLength))]
        [InlineData(nameof(CborWriter.WriteEndArray))]
        [InlineData(nameof(CborWriter.WriteEndMap))]
        public static void WriteByteString_IndefiteLength_NestedWrites_ShouldThrowInvalidOperationException(string opName)
        {
            using var writer = new CborWriter();
            writer.WriteStartByteStringIndefiniteLength();
            Assert.Throws<InvalidOperationException>(() => Helpers.ExecOperation(writer, opName));
        }

        [Theory]
        [InlineData(nameof(CborWriter.WriteEndTextStringIndefiniteLength))]
        [InlineData(nameof(CborWriter.WriteEndArray))]
        [InlineData(nameof(CborWriter.WriteEndMap))]
        public static void WriteByteString_IndefiteLength_ImbalancedWrites_ShouldThrowInvalidOperationException(string opName)
        {
            using var writer = new CborWriter();
            writer.WriteStartByteStringIndefiniteLength();
            Assert.Throws<InvalidOperationException>(() => Helpers.ExecOperation(writer, opName));
        }
    }
}
