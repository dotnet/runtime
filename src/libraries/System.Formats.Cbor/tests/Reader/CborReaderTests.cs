// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Test.Cryptography;
using Xunit;

namespace System.Formats.Cbor.Tests
{
    public partial class CborReaderTests
    {
        [Theory]
        [InlineData((CborConformanceMode)(-1))]
        public static void InvalidConformanceMode_ShouldThrowArgumentOutOfRangeException(CborConformanceMode mode)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CborReader(Array.Empty<byte>(), conformanceMode: mode));
        }

        [Fact]
        public static void Peek_EmptyBuffer_ShouldThrowCborContentException()
        {
            var reader = new CborReader(ReadOnlyMemory<byte>.Empty);
            Assert.Throws<CborContentException>(() => reader.PeekState());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(7)]
        public static void Depth_ShouldReturnExpectedValue(int depth)
        {
            byte[] encoding = Enumerable.Repeat<byte>(0x81, depth).Append<byte>(0x01).ToArray();
            var reader = new CborReader(encoding);

            for (int i = 0; i < depth; i++)
            {
                Assert.Equal(i, reader.CurrentDepth);
                reader.ReadStartArray();
            }

            Assert.Equal(depth, reader.CurrentDepth);
            reader.ReadInt32();
            Assert.Equal(depth, reader.CurrentDepth);

            for (int i = depth - 1; i >= 0; i--)
            {
                reader.ReadEndArray();
                Assert.Equal(i, reader.CurrentDepth);
            }

            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Fact]
        public static void BytesRemaining_NoReads_ShouldReturnTotalLength()
        {
            var reader = new CborReader(new byte[10]);
            Assert.Equal(10, reader.BytesRemaining);
        }

        [Fact]
        public static void BytesRemaining_OnValueRead_ShouldReturnZero()
        {
            var reader = new CborReader(new byte[] { 24, 24 });
            reader.ReadInt64();
            Assert.Equal(0, reader.BytesRemaining);
        }

        [Fact]
        public static void ConformanceMode_DefaultValue_ShouldEqualStrict()
        {
            var reader = new CborReader(Array.Empty<byte>());
            Assert.Equal(CborConformanceMode.Strict, reader.ConformanceMode);
        }

        [Theory]
        [InlineData(0, CborReaderState.UnsignedInteger)]
        [InlineData(1, CborReaderState.NegativeInteger)]
        [InlineData(2, CborReaderState.ByteString)]
        [InlineData(3, CborReaderState.TextString)]
        [InlineData(4, CborReaderState.StartArray)]
        [InlineData(5, CborReaderState.StartMap)]
        [InlineData(6, CborReaderState.Tag)]
        [InlineData(7, CborReaderState.SimpleValue)]
        public static void Peek_SingleByteBuffer_ShouldReturnExpectedState(byte majorType, CborReaderState expectedResult)
        {
            ReadOnlyMemory<byte> buffer = new byte[] { (byte)(majorType << 5) };
            var reader = new CborReader(buffer);
            Assert.Equal(expectedResult, reader.PeekState());
        }

        [Fact]
        public static void Read_EmptyBuffer_ShouldThrowCborContentException()
        {
            var reader = new CborReader(ReadOnlyMemory<byte>.Empty);
            Assert.Throws<CborContentException>(() => reader.ReadInt64());
        }

        [Fact]
        public static void Read_BeyondEndOfFirstValue_ShouldThrowInvalidOperationException()
        {
            var reader = new CborReader("01".HexToByteArray());
            reader.ReadInt64();
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
            Assert.Throws<InvalidOperationException>(() => reader.ReadInt64());
        }

        [Fact]
        public static void CborReader_ReadingTwoRootLevelValues_ShouldThrowInvalidOperationException()
        {
            ReadOnlyMemory<byte> buffer = new byte[] { 0, 0 };
            var reader = new CborReader(buffer);
            reader.ReadInt64();

            int bytesRemaining = reader.BytesRemaining;
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
            Assert.True(reader.BytesRemaining > 0);
            Assert.Throws<InvalidOperationException>(() => reader.ReadInt64());
            Assert.Equal(bytesRemaining, reader.BytesRemaining);
        }

        [Theory]
        [InlineData(1, 2, "0101")]
        [InlineData(10, 10, "0a0a0a0a0a0a0a0a0a0a")]
        [InlineData(new object[] { 1, 2 }, 3, "820102820102820102")]
        public static void CborReader_MultipleRootValuesAllowed_ReadingMultipleValues_HappyPath(object expectedValue, int repetitions, string hexEncoding)
        {
            var reader = new CborReader(hexEncoding.HexToByteArray(), allowMultipleRootLevelValues: true);

            for (int i = 0; i < repetitions; i++)
            {
                Helpers.VerifyValue(reader, expectedValue);
            }

            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Fact]
        public static void CborReader_MultipleRootValuesAllowed_RootLevelBreakByte_ShouldThrowCborContentException()
        {
            string hexEncoding = "018101ff";
            var reader = new CborReader(hexEncoding.HexToByteArray(), allowMultipleRootLevelValues: true);

            reader.ReadInt32();
            reader.ReadStartArray();
            reader.ReadInt32();
            reader.ReadEndArray();

            Assert.Throws<CborContentException>(() => reader.PeekState());
        }

        [Fact]
        public static void CborReader_MultipleRootValuesAllowed_ReadingBeyondEndOfBuffer_ShouldThrowInvalidOperationException()
        {
            string hexEncoding = "810102";
            var reader = new CborReader(hexEncoding.HexToByteArray(), allowMultipleRootLevelValues: true);

            Assert.Equal(CborReaderState.StartArray, reader.PeekState());
            reader.ReadStartArray();
            reader.ReadInt32();
            reader.ReadEndArray();

            Assert.Equal(CborReaderState.UnsignedInteger, reader.PeekState());
            reader.ReadInt32();

            Assert.Equal(CborReaderState.Finished, reader.PeekState());
            Assert.Throws<InvalidOperationException>(() => reader.ReadInt32());
        }

        [Theory]
        [MemberData(nameof(EncodedValueInputs))]
        public static void ReadEncodedValue_RootValue_HappyPath(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            byte[] encodedValue = reader.ReadEncodedValue().ToArray();
            Assert.Equal(hexEncoding, encodedValue.ByteArrayToHex().ToLower());
        }

        [Theory]
        [MemberData(nameof(EncodedValueInputs))]
        public static void ReadEncodedValue_NestedValue_HappyPath(string hexEncoding)
        {
            byte[] encoding = $"8301{hexEncoding}60".HexToByteArray();

            var reader = new CborReader(encoding);

            reader.ReadStartArray();
            reader.ReadInt64();
            byte[] encodedValue = reader.ReadEncodedValue().ToArray();

            Assert.Equal(hexEncoding, encodedValue.ByteArrayToHex().ToLower());
        }

        [Theory]
        [MemberData(nameof(EncodedValueInvalidInputs))]
        public static void ReadEncodedValue_InvalidCbor_ShouldThrowCborContentException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Assert.Throws<CborContentException>(() => reader.ReadEncodedValue());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        public static IEnumerable<object[]> EncodedValueInputs => CborReaderTests.SampleCborValues.Select(x => new[] { x });
        public static IEnumerable<object[]> EncodedValueInvalidInputs => CborReaderTests.InvalidCborValues.Select(x => new[] { x });

        [Theory]
        [MemberData(nameof(NonConformingSkipValueEncodings))]
        public static void ReadEncodedValue_InvalidConformance_ConformanceCheckEnabled_ShouldThrowCborContentException(CborConformanceMode mode, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding, mode);
            Assert.Throws<CborContentException>(() => reader.ReadEncodedValue(disableConformanceModeChecks: false));
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        [MemberData(nameof(NonConformingSkipValueEncodings))]
        public static void ReadEncodedValue_InvalidConformance_ConformanceCheckDisabled_ShouldSucceed(CborConformanceMode mode, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding, mode);
            ReadOnlyMemory<byte> encodedValue = reader.ReadEncodedValue(disableConformanceModeChecks: true);
            Assert.Equal(encoding, encodedValue);
            Assert.Equal(0, reader.BytesRemaining);
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/37669", TestPlatforms.Browser)]
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
        public static void CoseKeyHelpers_ECDsaParseCosePublicKey_HappyPath(string hexEncoding, string hexExpectedQx, string hexExpectedQy, string? expectedHashAlgorithmName, string curveFriendlyName)
        {
            ECPoint q = new ECPoint() { X = hexExpectedQx.HexToByteArray(), Y = hexExpectedQy.HexToByteArray() };
            (ECDsa ecDsa, HashAlgorithmName? name) = CborCoseKeyHelpers.ParseECDsaPublicKey(hexEncoding.HexToByteArray());

            using ECDsa _ = ecDsa;

            ECParameters ecParams = ecDsa.ExportParameters(includePrivateParameters: false);

            string? expectedCurveFriendlyName = NormalizeCurveForPlatform(curveFriendlyName).Oid.FriendlyName;

            Assert.True(ecParams.Curve.IsNamed);
            Assert.Equal(expectedCurveFriendlyName, ecParams.Curve.Oid.FriendlyName);
            Assert.Equal(q.X, ecParams.Q.X);
            Assert.Equal(q.Y, ecParams.Q.Y);
            Assert.Equal(expectedHashAlgorithmName, name?.Name);

            static ECCurve NormalizeCurveForPlatform(string friendlyName)
            {
                ECCurve namedCurve = ECCurve.CreateFromFriendlyName(friendlyName);
                using ECDsa ecDsa = ECDsa.Create(namedCurve);
                ECParameters platformParams = ecDsa.ExportParameters(includePrivateParameters: false);
                return platformParams.Curve;
            }
        }
    }
}
