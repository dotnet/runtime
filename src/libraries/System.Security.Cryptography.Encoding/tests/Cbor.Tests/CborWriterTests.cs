// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Test.Cryptography;
using Xunit;

namespace System.Formats.Cbor.Tests
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
        public static void GetEncoding_OnInCompleteValue_ShouldThrowInvalidOperationExceptoin()
        {
            using var writer = new CborWriter();
            Assert.Throws<InvalidOperationException>(() => writer.GetEncoding());
        }

        [Fact]
        public static void CborWriter_WritingTwoPrimitiveValues_ShouldThrowInvalidOperationException()
        {
            using var writer = new CborWriter();
            writer.WriteInt64(42);
            int bytesWritten = writer.BytesWritten;
            Assert.Throws<InvalidOperationException>(() => writer.WriteTextString("lorem ipsum"));
            Assert.Equal(bytesWritten, writer.BytesWritten);
        }

        [Theory]
        [InlineData(1, 2, "0101")]
        [InlineData(10, 10, "0a0a0a0a0a0a0a0a0a0a")]
        [InlineData(new object[] { 1, 2 }, 3, "820102820102820102")]
        public static void CborWriter_MultipleRootLevelValuesAllowed_WritingMultipleRootValues_HappyPath(object value, int repetitions, string expectedHexEncoding)
        {
            byte[] expectedEncoding = expectedHexEncoding.HexToByteArray();
            using var writer = new CborWriter(allowMultipleRootLevelValues: true);

            for (int i = 0; i < repetitions; i++)
            {
                Helpers.WriteValue(writer, value);
            }

            AssertHelper.HexEqual(expectedEncoding, writer.GetEncoding());
        }

        [Fact]
        public static void GetEncoding_MultipleRootLevelValuesAllowed_PartialRootValue_ShouldThrowInvalidOperationException()
        {
            using var writer = new CborWriter(allowMultipleRootLevelValues: true);

            writer.WriteStartArray(1);
            writer.WriteDouble(3.14);
            writer.WriteEndArray();
            writer.WriteStartArray(1);
            writer.WriteDouble(3.14);
            // misses writer.WriteEndArray();

            Assert.Throws<InvalidOperationException>(() => writer.GetEncoding());
        }

        [Fact]
        public static void BytesWritten_SingleValue_ShouldReturnBytesWritten()
        {
            using var writer = new CborWriter();
            Assert.Equal(0, writer.BytesWritten);
            writer.WriteTextString("test");
            Assert.Equal(5, writer.BytesWritten);
        }

        [Fact]
        public static void ConformanceLevel_DefaultValue_ShouldEqualLax()
        {
            using var writer = new CborWriter();
            Assert.Equal(CborConformanceLevel.Lax, writer.ConformanceLevel);
        }

        [Theory]
        [MemberData(nameof(EncodedValueInputs))]
        public static void WriteEncodedValue_RootValue_HappyPath(string hexEncodedValue)
        {
            byte[] encodedValue = hexEncodedValue.HexToByteArray();

            using var writer = new CborWriter();
            writer.WriteEncodedValue(encodedValue);

            string hexResult = writer.GetEncoding().ByteArrayToHex();
            Assert.Equal(hexEncodedValue, hexResult.ToLower());
        }


        [Theory]
        [InlineData(42)]
        [InlineData("value1")]
        [InlineData(new object[] { new object[] { 1, 2, 3 } })]
        public static void TryWriteEncoding_HappyPath(object value)
        {
            using var writer = new CborWriter();
            Helpers.WriteValue(writer, value);

            byte[] encoding = writer.GetEncoding();
            byte[] target = new byte[encoding.Length];

            bool result = writer.TryWriteEncoding(target, out int bytesWritten);

            Assert.True(result);
            Assert.Equal(encoding.Length, bytesWritten);
            Assert.Equal(encoding, target);
        }

        [Theory]
        [InlineData(42)]
        [InlineData("value1")]
        [InlineData(new object[] { new object[] { 1, 2, 3 } })]
        public static void TryWriteEncoding_DestinationTooSmall_ShouldReturnFalse(object value)
        {
            using var writer = new CborWriter();
            Helpers.WriteValue(writer, value);

            byte[] encoding = writer.GetEncoding();
            byte[] target = new byte[encoding.Length - 1];

            bool result = writer.TryWriteEncoding(target, out int bytesWritten);

            Assert.False(result);
            Assert.Equal(0, bytesWritten);
            Assert.All(target, b => Assert.Equal(0, b));
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

            string hexResult = writer.GetEncoding().ByteArrayToHex();
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
            using var writer = new CborWriter(encodeIndefiniteLengths: !useDefiniteLength);

            Helpers.WriteValue(writer, value, useDefiniteLengthCollections: useDefiniteLength);

            string hexEncoding = writer.GetEncoding().ByteArrayToHex().ToLower();
            Assert.Equal(hexExpectedEncoding, hexEncoding);
        }

        [Fact]
        public static void WriteEncodedValue_IndefiniteLengthTextString_HappyPath()
        {
            using var writer = new CborWriter(encodeIndefiniteLengths: true);

            writer.WriteStartTextString();
            writer.WriteTextString("foo");
            writer.WriteEncodedValue("63626172".HexToByteArray());
            writer.WriteEndTextString();

            byte[] encoding = writer.GetEncoding();
            Assert.Equal("7f63666f6f63626172ff", encoding.ByteArrayToHex().ToLower());
        }

        [Fact]
        public static void WriteEncodedValue_IndefiniteLengthByteString_HappyPath()
        {
            using var writer = new CborWriter(encodeIndefiniteLengths: true);

            writer.WriteStartByteString();
            writer.WriteByteString(new byte[] { 1, 1, 1 });
            writer.WriteEncodedValue("43020202".HexToByteArray());
            writer.WriteEndByteString();

            byte[] encoding = writer.GetEncoding();
            Assert.Equal("5f4301010143020202ff", encoding.ByteArrayToHex().ToLower());
        }

        [Fact]
        public static void WriteEncodedValue_BadIndefiniteLengthStringValue_ShouldThrowInvalidOperationException()
        {
            using var writer = new CborWriter();
            writer.WriteStartTextString();
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

        [Theory]
        [InlineData((CborConformanceLevel)(-1))]
        public static void InvalidConformanceLevel_ShouldThrowArgumentOutOfRangeException(CborConformanceLevel level)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CborWriter(conformanceLevel: level));
        }

        [Theory]
        [InlineData(CborConformanceLevel.Rfc7049Canonical)]
        [InlineData(CborConformanceLevel.Ctap2Canonical)]
        public static void EncodeIndefiniteLengths_UnsupportedConformanceLevel_ShouldThrowArgumentException(CborConformanceLevel level)
        {
            Assert.Throws<ArgumentException>(() => new CborWriter(level, encodeIndefiniteLengths: true));
        }

        public static IEnumerable<object[]> EncodedValueInputs => CborReaderTests.SampleCborValues.Select(x => new [] { x });
        public static IEnumerable<object[]> EncodedValueBadInputs => CborReaderTests.InvalidCborValues.Select(x => new[] { x });

        [Theory]
        [InlineData("a501020326200121582065eda5a12577c2bae829437fe338701a10aaa375e1bb5b5de108de439c08551d2258201e52ed75701163f7f9e40ddf9f341b3dc9ba860af7e0ca7ca7e9eecd0084d19c",
                    "65eda5a12577c2bae829437fe338701a10aaa375e1bb5b5de108de439c08551d",
                    "1e52ed75701163f7f9e40ddf9f341b3dc9ba860af7e0ca7ca7e9eecd0084d19c",
                    "SHA256", "ECDSA_P256")]
        [InlineData("a501020338222002215830ed57d8608c5734a5ed5d22026bad8700636823e45297306479beb61a5bd6b04688c34a2f0de51d91064355eef7548bdd22583024376b4fee60ba65db61de54234575eec5d37e1184fbafa1f49d71e1795bba6bda9cbe2ebb815f9b49b371486b38fa1b",
                    "ed57d8608c5734a5ed5d22026bad8700636823e45297306479beb61a5bd6b04688c34a2f0de51d91064355eef7548bdd",
                    "24376b4fee60ba65db61de54234575eec5d37e1184fbafa1f49d71e1795bba6bda9cbe2ebb815f9b49b371486b38fa1b",
                    "SHA384", "ECDSA_P384")]
        [InlineData("a50102033823200321584200b03811bef65e330bb974224ec3ab0a5469f038c92177b4171f6f66f91244d4476e016ee77cf7e155a4f73567627b5d72eaf0cb4a6036c6509a6432d7cd6a3b325c2258420114b597b6c271d8435cfa02e890608c93f5bc118ca7f47bf191e9f9e49a22f8a15962315f0729781e1d78b302970c832db2fa8f7f782a33f8e1514950dc7499035f",
                    "00b03811bef65e330bb974224ec3ab0a5469f038c92177b4171f6f66f91244d4476e016ee77cf7e155a4f73567627b5d72eaf0cb4a6036c6509a6432d7cd6a3b325c",
                    "0114b597b6c271d8435cfa02e890608c93f5bc118ca7f47bf191e9f9e49a22f8a15962315f0729781e1d78b302970c832db2fa8f7f782a33f8e1514950dc7499035f",
                    "SHA512", "ECDSA_P521")]
        [InlineData("a40102200121582065eda5a12577c2bae829437fe338701a10aaa375e1bb5b5de108de439c08551d2258201e52ed75701163f7f9e40ddf9f341b3dc9ba860af7e0ca7ca7e9eecd0084d19c",
                    "65eda5a12577c2bae829437fe338701a10aaa375e1bb5b5de108de439c08551d",
                    "1e52ed75701163f7f9e40ddf9f341b3dc9ba860af7e0ca7ca7e9eecd0084d19c",
                    null, "ECDSA_P256")]
        public static void CoseKeyHelpers_ECDsaExportCosePublicKey_HappyPath(string expectedHexEncoding, string hexQx, string hexQy, string? hashAlgorithmName, string curveFriendlyName)
        {
            byte[] expectedEncoding = expectedHexEncoding.HexToByteArray();
            var hashAlgName = hashAlgorithmName != null ? new HashAlgorithmName(hashAlgorithmName) : (HashAlgorithmName?)null;
            var ecParameters = new ECParameters()
            {
                Curve = ECCurve.CreateFromFriendlyName(curveFriendlyName),
                Q = new ECPoint() { X = hexQx.HexToByteArray(), Y = hexQy.HexToByteArray() },
            };

            using ECDsa ecDsa = ECDsa.Create(ecParameters);

            byte[] coseKeyEncoding = CborCoseKeyHelpers.ExportECDsaPublicKey(ecDsa, hashAlgName);
            AssertHelper.HexEqual(expectedEncoding, coseKeyEncoding);
        }
    }
}
