// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Test.Cryptography;
using Xunit;

namespace System.Formats.Cbor.Tests
{
    public partial class CborWriterTests
    {
        private const string MaxDepthMessageSentinel = "maximum allowed depth";

        [Fact]
        public static void IsWriteCompleted_OnWrittenPrimitive_ShouldBeTrue()
        {
            var writer = new CborWriter();
            Assert.False(writer.IsWriteCompleted);
            writer.WriteInt64(42);
            Assert.True(writer.IsWriteCompleted);
        }

        [Fact]
        public static void GetEncoding_OnInCompleteValue_ShouldThrowInvalidOperationExceptoin()
        {
            var writer = new CborWriter();
            Assert.Throws<InvalidOperationException>(() => writer.Encode());
        }

        [Fact]
        public static void CborWriter_WritingTwoPrimitiveValues_ShouldThrowInvalidOperationException()
        {
            var writer = new CborWriter();
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
            var writer = new CborWriter(allowMultipleRootLevelValues: true);

            for (int i = 0; i < repetitions; i++)
            {
                Helpers.WriteValue(writer, value);
            }

            AssertHelper.HexEqual(expectedEncoding, writer.Encode());
        }

        [Fact]
        public static void GetEncoding_MultipleRootLevelValuesAllowed_PartialRootValue_ShouldThrowInvalidOperationException()
        {
            var writer = new CborWriter(allowMultipleRootLevelValues: true);

            writer.WriteStartArray(1);
            writer.WriteDouble(3.14);
            writer.WriteEndArray();
            writer.WriteStartArray(1);
            writer.WriteDouble(3.14);
            // misses writer.WriteEndArray();

            Assert.Throws<InvalidOperationException>(() => writer.Encode());
        }

        [Fact]
        public static void BytesWritten_SingleValue_ShouldReturnBytesWritten()
        {
            var writer = new CborWriter();
            Assert.Equal(0, writer.BytesWritten);
            writer.WriteTextString("test");
            Assert.Equal(5, writer.BytesWritten);
        }

        [Fact]
        public static void Reset_NonTrivialWriter_HappyPath()
        {
            // Set up: build a nontrivial writer state.
            // Favor maps and Ctap2 canonicalization since
            // since that utilizes most of the moving parts.
            var writer = new CborWriter(conformanceMode: CborConformanceMode.Ctap2Canonical);

            for (int i = 0; i < 10; i++)
            {
                if (i % 2 == 0)
                {
                    writer.WriteStartMap(100);
                }
                else
                {
                    writer.WriteStartArray(100);
                }
            }

            writer.WriteStartMap(3);

            writer.WriteInt32(1); // key
            writer.WriteInt32(2); // value

            writer.WriteInt32(-1); // key
            writer.WriteInt32(1); // value

            // End set up

            Assert.Equal(11, writer.CurrentDepth);
            Assert.True(writer.BytesWritten > 11, "must have written a nontrivial number of bytes to the buffer");

            writer.Reset();

            Assert.Equal(0, writer.CurrentDepth);
            Assert.Equal(0, writer.BytesWritten);

            // Write an object from scratch and validate that it is correct

            writer.WriteInt32(42);
            Assert.Equal(new byte[] { 0x18, 0x2a }, writer.Encode());
        }

        [Fact]
        public static void Reset_PreservesMultipleRootLevels()
        {
            var writer = new CborWriter(allowMultipleRootLevelValues: false);
            writer.WriteBoolean(true);
            writer.Reset();
            writer.WriteInt32(6);
            Assert.Throws<InvalidOperationException>(() => writer.WriteInt32(7));
        }

        [Fact]
        public static void Reset_ClearsIndefiniteLengthRanges()
        {
            var writer = new CborWriter(convertIndefiniteLengthEncodings: true);

            writer.WriteStartIndefiniteLengthByteString();
            writer.WriteByteString([1, 2, 3]);
            writer.Reset();

            writer.WriteStartIndefiniteLengthByteString();
            writer.WriteByteString([1, 2, 3]);
            writer.WriteEndIndefiniteLengthByteString();
            ReadOnlySpan<byte> encoded = writer.Encode();
            ReadOnlySpan<byte> expected = [0x43, 0x01, 0x02, 0x03];
            AssertExtensions.SequenceEqual(expected, encoded);
        }

        [Fact]
        public static void ConformanceMode_DefaultValue_ShouldEqualStrict()
        {
            var writer = new CborWriter();
            Assert.Equal(CborConformanceMode.Strict, writer.ConformanceMode);
        }

        [Fact]
        public static void ConvertIndefiniteLengthEncodings_DefaultValue_ShouldEqualFalse()
        {
            var writer = new CborWriter();
            Assert.False(writer.ConvertIndefiniteLengthEncodings);
        }

        [Theory]
        [MemberData(nameof(EncodedValueInputs))]
        public static void WriteEncodedValue_RootValue_HappyPath(string hexEncodedValue)
        {
            byte[] encodedValue = hexEncodedValue.HexToByteArray();

            var writer = new CborWriter();
            writer.WriteEncodedValue(encodedValue);

            string hexResult = writer.Encode().ByteArrayToHex();
            Assert.Equal(hexEncodedValue, hexResult.ToLower());
        }

        [Theory]
        [InlineData(42)]
        [InlineData("value1")]
        [InlineData(new object[] { new object[] { 1, 2, 3 } })]
        public static void EncodeSpan_HappyPath(object value)
        {
            var writer = new CborWriter();
            Helpers.WriteValue(writer, value);

            byte[] target = new byte[writer.BytesWritten];
            int bytesWritten = writer.Encode(target);
            byte[] encoding = writer.Encode();

            Assert.Equal(encoding.Length, bytesWritten);
            Assert.Equal(encoding, target);
        }

        [Theory]
        [InlineData(42)]
        [InlineData("value1")]
        [InlineData(new object[] { new object[] { 1, 2, 3 } })]
        public static void EncodeSpan_DestinationTooSmall_ShouldThrowArgumentException(object value)
        {
            var writer = new CborWriter();
            Helpers.WriteValue(writer, value);

            byte[] encoding = writer.Encode();
            byte[] target = new byte[encoding.Length - 1];

            Assert.Throws<ArgumentException>(() => writer.Encode(target));
            Assert.All(target, b => Assert.Equal(0, b));
        }

        [Theory]
        [InlineData(42)]
        [InlineData("value1")]
        [InlineData(new object[] { new object[] { 1, 2, 3 } })]
        public static void TryEncode_HappyPath(object value)
        {
            var writer = new CborWriter();
            Helpers.WriteValue(writer, value);

            byte[] encoding = writer.Encode();
            byte[] target = new byte[encoding.Length];

            bool result = writer.TryEncode(target, out int bytesWritten);

            Assert.True(result);
            Assert.Equal(encoding.Length, bytesWritten);
            Assert.Equal(encoding, target);
        }

        [Theory]
        [InlineData(42)]
        [InlineData("value1")]
        [InlineData(new object[] { new object[] { 1, 2, 3 } })]
        public static void TryEncode_DestinationTooSmall_ShouldReturnFalse(object value)
        {
            var writer = new CborWriter();
            Helpers.WriteValue(writer, value);

            byte[] encoding = writer.Encode();
            byte[] target = new byte[encoding.Length - 1];

            bool result = writer.TryEncode(target, out int bytesWritten);

            Assert.False(result);
            Assert.Equal(0, bytesWritten);
            Assert.All(target, b => Assert.Equal(0, b));
        }

        [Theory]
        [MemberData(nameof(EncodedValueInputs))]
        public static void WriteEncodedValue_NestedValue_HappyPath(string hexEncodedValue)
        {
            byte[] encodedValue = hexEncodedValue.HexToByteArray();

            var writer = new CborWriter();
            writer.WriteStartArray(3);
            writer.WriteInt64(1);
            writer.WriteEncodedValue(encodedValue);
            writer.WriteTextString("");
            writer.WriteEndArray();

            string hexResult = writer.Encode().ByteArrayToHex();
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

        public static void WriteEncodedValue_ContextScenaria_HappyPath(object value, bool useDefiniteLengthEncoding, string hexExpectedEncoding)
        {
            var writer = new CborWriter(convertIndefiniteLengthEncodings: useDefiniteLengthEncoding);

            Helpers.WriteValue(writer, value, useDefiniteLengthCollections: useDefiniteLengthEncoding);

            string hexEncoding = writer.Encode().ByteArrayToHex().ToLower();
            Assert.Equal(hexExpectedEncoding, hexEncoding);
        }

        [Fact]
        public static void WriteEncodedValue_IndefiniteLengthTextString_HappyPath()
        {
            var writer = new CborWriter(convertIndefiniteLengthEncodings: false);

            writer.WriteStartIndefiniteLengthTextString();
            writer.WriteTextString("foo");
            writer.WriteEncodedValue("63626172".HexToByteArray());
            writer.WriteEndIndefiniteLengthTextString();

            byte[] encoding = writer.Encode();
            Assert.Equal("7f63666f6f63626172ff", encoding.ByteArrayToHex().ToLower());
        }

        [Fact]
        public static void WriteEncodedValue_IndefiniteLengthByteString_HappyPath()
        {
            var writer = new CborWriter(convertIndefiniteLengthEncodings: false);

            writer.WriteStartIndefiniteLengthByteString();
            writer.WriteByteString(new byte[] { 1, 1, 1 });
            writer.WriteEncodedValue("43020202".HexToByteArray());
            writer.WriteEndIndefiniteLengthByteString();

            byte[] encoding = writer.Encode();
            Assert.Equal("5f4301010143020202ff", encoding.ByteArrayToHex().ToLower());
        }

        [Fact]
        public static void WriteEncodedValue_BadIndefiniteLengthStringValue_ShouldThrowInvalidOperationException()
        {
            var writer = new CborWriter();
            writer.WriteStartIndefiniteLengthTextString();
            Assert.Throws<InvalidOperationException>(() => writer.WriteEncodedValue(new byte[] { 0x01 }));
        }

        [Fact]
        public static void WriteEncodedValue_AtEndOfDefiniteLengthCollection_ShouldThrowInvalidOperationException()
        {
            var writer = new CborWriter();
            writer.WriteInt64(0);
            Assert.Throws<InvalidOperationException>(() => writer.WriteEncodedValue(new byte[] { 0x01 }));
        }

        [Theory]
        [MemberData(nameof(EncodedValueBadInputs))]
        public static void WriteEncodedValue_InvalidCbor_ShouldThrowArgumentException(string hexEncodedInput)
        {
            byte[] encodedInput = hexEncodedInput.HexToByteArray();
            var writer = new CborWriter();
            Assert.Throws<ArgumentException>(() => writer.WriteEncodedValue(encodedInput));
        }

        [Theory]
        [InlineData(CborConformanceMode.Strict, "a201010101")] // duplicate key encodings
        [InlineData(CborConformanceMode.Canonical, "9f01ff")]  // indefinite-length array
        [InlineData(CborConformanceMode.Ctap2Canonical, "a280800101")]  // unsorted key encodings
        public static void WriteEncodedValue_InvalidConformance_ShouldThrowArgumentException(CborConformanceMode conformanceMode, string hexEncodedInput)
        {
            byte[] encodedInput = hexEncodedInput.HexToByteArray();
            var writer = new CborWriter(conformanceMode);
            Assert.Throws<ArgumentException>(() => writer.WriteEncodedValue(encodedInput));
        }

        [Fact]
        public static void WriteEncodedValue_ValidPayloadWithTrailingBytes_ShouldThrowArgumentException()
        {
            var writer = new CborWriter();
            Assert.Throws<ArgumentException>(() => writer.WriteEncodedValue(new byte[] { 0x01, 0x01 }));
        }

        [Theory]
        [InlineData((CborConformanceMode)(-1))]
        public static void InvalidConformanceMode_ShouldThrowArgumentOutOfRangeException(CborConformanceMode mode)
        {
            Assert.Throws<ArgumentOutOfRangeException>("conformanceMode", () => new CborWriter(conformanceMode: mode));
        }

        [Theory]
        [InlineData(-2)]
        [InlineData(int.MinValue)]
        public static void InvalidInitialCapacity_ShouldThrowArgumentOutOfRangeException(int capacity)
        {
            Assert.Throws<ArgumentOutOfRangeException>("initialCapacity", () => new CborWriter(initialCapacity: capacity));
        }

        [Theory]
        [InlineData(-1, 0)]
        [InlineData(0, 0)]
        [InlineData(1, 1)]
        [InlineData(1023, 1023)]
        public static void InitialCapacity_ShouldSetInitialBuffer(int capacity, int expectedBufferLength)
        {
            CborWriter writer = new CborWriter(initialCapacity: capacity);
            byte[]? buffer = (byte[]?)typeof(CborWriter).GetField("_buffer", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(writer);

            Assert.NotNull(buffer);
            Assert.Equal(expectedBufferLength, buffer.Length);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(1)]
        public static void Encode_InitialCapacity_Grows(int capacity)
        {
            CborWriter writer = new CborWriter(initialCapacity: capacity);
            writer.WriteByteString((ReadOnlySpan<byte>)new byte[] { 1, 2, 3, 4, 5, 6 });
            byte[] encoded = writer.Encode();

            ReadOnlySpan<byte> expected = new byte[] { (2 << 5) | 6, 1, 2, 3, 4, 5, 6 };
            AssertExtensions.SequenceEqual(expected, encoded);
        }

        public static IEnumerable<object[]> EncodedValueInputs => CborReaderTests.SampleCborValues.Select(x => new [] { x });
        public static IEnumerable<object[]> EncodedValueBadInputs => CborReaderTests.InvalidCborValues.Select(x => new[] { x });

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/37669", TestPlatforms.Browser | TestPlatforms.Wasi)]
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

        [Fact]
        public static void CborWriterOptions_DefaultValues()
        {
            var options = new CborWriterOptions();
            Assert.Equal(CborConformanceMode.Strict, options.ConformanceMode);
            Assert.False(options.ConvertIndefiniteLengthEncodings);
            Assert.False(options.AllowMultipleRootLevelValues);
            Assert.Equal(-1, options.MaxDepth);
            Assert.Equal(-1, options.InitialCapacity);
        }

        [Fact]
        public static void CborWriter_NullOptions_DefaultValues()
        {
            var writer = new CborWriter((CborWriterOptions)null!);
            Assert.Equal(CborConformanceMode.Strict, writer.ConformanceMode);
            Assert.False(writer.ConvertIndefiniteLengthEncodings);
            Assert.False(writer.AllowMultipleRootLevelValues);
            Assert.Equal(1000, writer.MaxDepth);

            // Capacity (default 0)
            byte[]? buffer = (byte[]?)typeof(CborWriter).GetField("_buffer", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(writer);

            Assert.NotNull(buffer);
            Assert.Equal(0, buffer.Length);
        }

        [Theory]
        [InlineData(CborConformanceMode.Ctap2Canonical)]
        [InlineData(CborConformanceMode.Canonical)]
        [InlineData(CborConformanceMode.Lax)]
        [InlineData(CborConformanceMode.Strict)]
        public static void CborWriterOptions_SetConformanceMode(CborConformanceMode mode)
        {
            var options = new CborWriterOptions { ConformanceMode = mode };
            Assert.Equal(mode, options.ConformanceMode);
        }

        [Fact]
        public static void CborWriterOptions_InvalidConformanceMode_Throws()
        {
            var options = new CborWriterOptions();
            Assert.Throws<ArgumentOutOfRangeException>(() => options.ConformanceMode = (CborConformanceMode)(-1));
        }

        [Fact]
        public static void CborWriterOptions_SetConvertIndefiniteLengthEncodings()
        {
            var options = new CborWriterOptions { ConvertIndefiniteLengthEncodings = true };
            Assert.True(options.ConvertIndefiniteLengthEncodings);
        }

        [Fact]
        public static void CborWriterOptions_SetAllowMultipleRootLevelValues()
        {
            var options = new CborWriterOptions { AllowMultipleRootLevelValues = true };
            Assert.True(options.AllowMultipleRootLevelValues);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(64)]
        [InlineData(1000)]
        public static void CborWriterOptions_SetMaxDepth(int maxDepth)
        {
            var options = new CborWriterOptions { MaxDepth = maxDepth };
            Assert.Equal(maxDepth, options.MaxDepth);
        }

        [Theory]
        [InlineData(-2)]
        [InlineData(-100)]
        public static void CborWriterOptions_SetMaxDepth_TooNegative_Throws(int maxDepth)
        {
            var options = new CborWriterOptions();
            Assert.Throws<ArgumentOutOfRangeException>(() => options.MaxDepth = maxDepth);
        }

        [Fact]
        public static void Constructor_DefaultOptions_HasCorrectDefaults()
        {
            var writer = new CborWriter(new CborWriterOptions());
            Assert.Equal(CborConformanceMode.Strict, writer.ConformanceMode);
            Assert.False(writer.ConvertIndefiniteLengthEncodings);
            Assert.False(writer.AllowMultipleRootLevelValues);
            Assert.Equal(1000, writer.MaxDepth);
        }

        [Fact]
        public static void Constructor_CustomOptions_SetsProperties()
        {
            var options = new CborWriterOptions
            {
                ConformanceMode = CborConformanceMode.Canonical,
                ConvertIndefiniteLengthEncodings = true,
                AllowMultipleRootLevelValues = true,
                MaxDepth = 42,
            };

            var writer = new CborWriter(options);
            Assert.Equal(CborConformanceMode.Canonical, writer.ConformanceMode);
            Assert.True(writer.ConvertIndefiniteLengthEncodings);
            Assert.True(writer.AllowMultipleRootLevelValues);
            Assert.Equal(42, writer.MaxDepth);
        }

        [Fact]
        public static void Constructor_NoArgs_HasCorrectDefaults()
        {
            var writer = new CborWriter();
            Assert.Equal(CborConformanceMode.Strict, writer.ConformanceMode);
            Assert.False(writer.ConvertIndefiniteLengthEncodings);
            Assert.False(writer.AllowMultipleRootLevelValues);
            Assert.Equal(1000, writer.MaxDepth);
        }

        [Fact]
        public static void Constructor_MaxDepthZero_ResolvesToZero()
        {
            var writer = new CborWriter(new CborWriterOptions { MaxDepth = 0 });
            Assert.Equal(0, writer.MaxDepth);
        }

        [Fact]
        public static void Constructor_MaxDepthNegativeOneSentinel_ResolvesToDefault()
        {
            var writer = new CborWriter(new CborWriterOptions { MaxDepth = -1 });
            Assert.Equal(1000, writer.MaxDepth);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(64)]
        [InlineData(1000)]
        public static void Constructor_MaxDepthExplicit_SetsProperty(int maxDepth)
        {
            var writer = new CborWriter(new CborWriterOptions { MaxDepth = maxDepth });
            Assert.Equal(maxDepth, writer.MaxDepth);
        }

        [Theory]
        [InlineData(1, false)]
        [InlineData(2, false)]
        [InlineData(10, false)]
        [InlineData(1, true)]
        [InlineData(2, true)]
        [InlineData(10, true)]
        public static void MaxDepth_GuardsNestedArrays(int maxDepth, bool useIndefiniteLength)
        {
            var writer = new CborWriter(new CborWriterOptions { MaxDepth = maxDepth });

            for (int i = 0; i < maxDepth; i++)
            {
                writer.WriteStartArray(useIndefiniteLength ? null : 1);
            }

            int bytesWritten = writer.BytesWritten;

            AssertExtensions.ThrowsContains<InvalidOperationException>(
                () => writer.WriteStartArray(useIndefiniteLength ? null : 1),
                MaxDepthMessageSentinel);

            Assert.Equal(bytesWritten, writer.BytesWritten);
        }

        [Theory]
        [InlineData(1, false)]
        [InlineData(2, false)]
        [InlineData(10, false)]
        [InlineData(1, true)]
        [InlineData(2, true)]
        [InlineData(10, true)]
        public static void MaxDepth_GuardsNestedMaps(int maxDepth, bool useIndefiniteLength)
        {
            var writer = new CborWriter(new CborWriterOptions { MaxDepth = maxDepth });

            for (int i = 0; i < maxDepth; i++)
            {
                writer.WriteStartMap(useIndefiniteLength ? null : 1);
                writer.WriteInt32(i); // key
            }

            int bytesWritten = writer.BytesWritten;

            AssertExtensions.ThrowsContains<InvalidOperationException>(
                () => writer.WriteStartMap(useIndefiniteLength ? null : 1),
                MaxDepthMessageSentinel);

            Assert.Equal(bytesWritten, writer.BytesWritten);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void MaxDepth_GuardsMixedArrayAndMapNesting(bool useIndefiniteLength)
        {
            var writer = new CborWriter(new CborWriterOptions { MaxDepth = 2 });

            writer.WriteStartArray(useIndefiniteLength ? null : 1);
            writer.WriteStartMap(useIndefiniteLength ? null : 1);
            writer.WriteInt32(0); // key

            int bytesWritten = writer.BytesWritten;

            AssertExtensions.ThrowsContains<InvalidOperationException>(
                () => writer.WriteStartArray(useIndefiniteLength ? null : 1),
                MaxDepthMessageSentinel);

            Assert.Equal(bytesWritten, writer.BytesWritten);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void MaxDepth_GuardsIndefiniteByteString_FromArray(bool useIndefiniteLength)
        {
            var writer = new CborWriter(new CborWriterOptions
            {
                ConformanceMode = CborConformanceMode.Lax,
                MaxDepth = 1,
            });

            writer.WriteStartArray(useIndefiniteLength ? null : 1);

            int bytesWritten = writer.BytesWritten;

            AssertExtensions.ThrowsContains<InvalidOperationException>(
                () => writer.WriteStartIndefiniteLengthByteString(),
                MaxDepthMessageSentinel);

            Assert.Equal(bytesWritten, writer.BytesWritten);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void MaxDepth_GuardsIndefiniteByteString_FromMap(bool useIndefiniteLength)
        {
            var writer = new CborWriter(new CborWriterOptions
            {
                ConformanceMode = CborConformanceMode.Lax,
                MaxDepth = 1,
            });

            writer.WriteStartMap(useIndefiniteLength ? null : 1);
            writer.WriteInt32(0); // key

            int bytesWritten = writer.BytesWritten;

            AssertExtensions.ThrowsContains<InvalidOperationException>(
                () => writer.WriteStartIndefiniteLengthByteString(),
                MaxDepthMessageSentinel);

            Assert.Equal(bytesWritten, writer.BytesWritten);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void MaxDepth_GuardsIndefiniteTextString_FromArray(bool useIndefiniteLength)
        {
            var writer = new CborWriter(new CborWriterOptions
            {
                ConformanceMode = CborConformanceMode.Lax,
                MaxDepth = 1,
            });

            writer.WriteStartArray(useIndefiniteLength ? null : 1);

            int bytesWritten = writer.BytesWritten;

            AssertExtensions.ThrowsContains<InvalidOperationException>(
                () => writer.WriteStartIndefiniteLengthTextString(),
                MaxDepthMessageSentinel);

            Assert.Equal(bytesWritten, writer.BytesWritten);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void MaxDepth_GuardsIndefiniteTextString_FromMap(bool useIndefiniteLength)
        {
            var writer = new CborWriter(new CborWriterOptions
            {
                ConformanceMode = CborConformanceMode.Lax,
                MaxDepth = 1,
            });

            writer.WriteStartMap(useIndefiniteLength ? null : 1);
            writer.WriteInt32(0); // key

            int bytesWritten = writer.BytesWritten;

            AssertExtensions.ThrowsContains<InvalidOperationException>(
                () => writer.WriteStartIndefiniteLengthTextString(),
                MaxDepthMessageSentinel);

            Assert.Equal(bytesWritten, writer.BytesWritten);
        }

        [Fact]
        public static void MaxDepth_SucceedsAtExactLimit()
        {
            var writer = new CborWriter(new CborWriterOptions { MaxDepth = 3 });

            writer.WriteStartArray(1);
            writer.WriteStartArray(1);
            writer.WriteStartArray(1);
            writer.WriteInt32(42);
            writer.WriteEndArray();
            writer.WriteEndArray();
            writer.WriteEndArray();

            byte[] encoded = writer.Encode();
            Assert.NotEmpty(encoded);
        }

        [Fact]
        public static void MaxDepth_Zero_AllowsPrimitiveValues()
        {
            var writer = new CborWriter(new CborWriterOptions { MaxDepth = 0 });

            Assert.Equal(0, writer.MaxDepth);
            writer.WriteInt32(42);

            byte[] encoded = writer.Encode();
            Assert.Equal(new byte[] { 0x18, 0x2a }, encoded);
        }

        [Fact]
        public static void MaxDepth_Zero_BlocksArray()
        {
            var writer = new CborWriter(new CborWriterOptions { MaxDepth = 0 });

            int bytesWritten = writer.BytesWritten;
            AssertExtensions.ThrowsContains<InvalidOperationException>(
                () => writer.WriteStartArray(1), MaxDepthMessageSentinel);
            Assert.Equal(bytesWritten, writer.BytesWritten);
        }

        [Fact]
        public static void MaxDepth_Zero_BlocksMap()
        {
            var writer = new CborWriter(new CborWriterOptions { MaxDepth = 0 });

            int bytesWritten = writer.BytesWritten;
            AssertExtensions.ThrowsContains<InvalidOperationException>(
                () => writer.WriteStartMap(1), MaxDepthMessageSentinel);
            Assert.Equal(bytesWritten, writer.BytesWritten);
        }

        [Theory]
        [InlineData("8181182A", 2)] // [[42]]
        [InlineData("7F6161FF", 1)] // (_ "a")
        [InlineData("7FFF", 1)] // (_ )
        [InlineData("817FFF", 2)] // [(_ )]
        [InlineData("5F4101FF", 1)] // (_ h'01')
        [InlineData("5FFF", 1)] // (_ )
        [InlineData("9F01FF", 1)] // [_ 1]
        [InlineData("819F01FF", 2)] // [[_ 1]]
        [InlineData("BFFF", 1)] // {_ }
        [InlineData("BF0101FF", 1)] // {_ 1: 1}
        public static void MaxDepth_WriteEncodedValue_ThrowsWhenDepthExceeded(string encodedValueHex, int appliedDepth)
        {
            const int MaxDepth = 2;
            byte[] encodedValue = encodedValueHex.HexToByteArray();

            var limitedWriter = new CborWriter(new CborWriterOptions { MaxDepth = MaxDepth });
            var writer = new CborWriter(new CborWriterOptions { MaxDepth = MaxDepth + 1 });

            int testLimit = MaxDepth - appliedDepth + 1;

            for (int i = testLimit; i > 0; i--)
            {
                limitedWriter.WriteStartArray(1);
                writer.WriteStartArray(1);
            }

            int bytesWritten = limitedWriter.BytesWritten;
            Assert.Equal(bytesWritten, writer.BytesWritten);

            // limitedWriter can't accept the value
            Assert.Throws<ArgumentException>(() => limitedWriter.WriteEncodedValue(encodedValue));
            Assert.Equal(bytesWritten, limitedWriter.BytesWritten);

            // writer can
            writer.WriteEncodedValue(encodedValue);
            Assert.Equal(bytesWritten + encodedValue.Length, writer.BytesWritten);
        }
    }
}
