// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Test.Cryptography;
using Xunit;

namespace System.Formats.Cbor.Tests
{
    public partial class CborWriterTests
    {
        // Data points taken from https://tools.ietf.org/html/rfc7049#appendix-A
        // Additional pairs generated using http://cbor.me/

        [Theory]
        [InlineData(100000.0, "fa47c35000")]
        [InlineData(3.4028234663852886e+38, "fa7f7fffff")]
        [InlineData(float.PositiveInfinity, "f97c00")]
        [InlineData(float.NegativeInfinity, "f9fc00")]
        [InlineData(float.NaN, "f97e00")]
        public static void WriteSingle_SingleValue_HappyPath(float input, string hexExpectedEncoding)
        {
            byte[] expectedEncoding = hexExpectedEncoding.HexToByteArray();
            var writer = new CborWriter();
            writer.WriteSingle(input);
            AssertHelper.HexEqual(expectedEncoding, writer.Encode());
        }

        [Theory]
        [InlineData(float.NaN, "f97e00", CborConformanceMode.Lax)]
        [InlineData(float.NaN, "f97e00", CborConformanceMode.Strict)]
        [InlineData(float.NaN, "f97e00", CborConformanceMode.Canonical)]
        public static void WriteSingle_NonCtapConformance_ShouldMinimizePrecision(float input, string hexExpectedEncoding, CborConformanceMode mode)
        {
            byte[] expectedEncoding = hexExpectedEncoding.HexToByteArray();
            var writer = new CborWriter(mode);
            writer.WriteSingle(input);
            AssertHelper.HexEqual(expectedEncoding, writer.Encode());
        }

        [Theory]
        [InlineData(100000.0, "fa47c35000")]
        [InlineData(3.4028234663852886e+38, "fa7f7fffff")]
        [InlineData(float.PositiveInfinity, "fa7f800000")]
        [InlineData(float.NegativeInfinity, "faff800000")]
        public static void WriteSingle_Ctap2Conformance_ShouldPreservePrecision(float input, string hexExpectedEncoding)
        {
            byte[] expectedEncoding = hexExpectedEncoding.HexToByteArray();
            var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
            writer.WriteSingle(input);
            AssertHelper.HexEqual(expectedEncoding, writer.Encode());
        }

        [Fact]
        public static void WriteSingle_Ctap2Conformance_ShouldPreservePrecision_NaN()
        {
            // float.NaN may differ across architectures, in particular it's negative on x86 and positive elsewhere
            byte[] expectedEncoding = ("fa" + CborTestHelpers.SingleToInt32Bits(float.NaN).ToString("x4")).HexToByteArray();
            var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
            writer.WriteSingle(float.NaN);
            AssertHelper.HexEqual(expectedEncoding, writer.Encode());
        }

        [Theory]
        [InlineData(1.1, "fb3ff199999999999a")]
        [InlineData(1.0e+300, "fb7e37e43c8800759c")]
        [InlineData(-4.1, "fbc010666666666666")]
        [InlineData(3.1415926, "fb400921fb4d12d84a")]
        [InlineData(double.PositiveInfinity, "f97c00")]
        [InlineData(double.NegativeInfinity, "f9fc00")]
        [InlineData(double.NaN, "f97e00")]
        public static void WriteDouble_SingleValue_HappyPath(double input, string hexExpectedEncoding)
        {
            byte[] expectedEncoding = hexExpectedEncoding.HexToByteArray();
            var writer = new CborWriter();
            writer.WriteDouble(input);
            AssertHelper.HexEqual(expectedEncoding, writer.Encode());
        }

        [Theory]
        [InlineData(double.NaN, "f97e00", CborConformanceMode.Lax)]
        [InlineData(double.NaN, "f97e00", CborConformanceMode.Strict)]
        [InlineData(double.NaN, "f97e00", CborConformanceMode.Canonical)]
        [InlineData(65505, "fa477fe100", CborConformanceMode.Lax)]
        [InlineData(65505, "fa477fe100", CborConformanceMode.Strict)]
        [InlineData(65505, "fa477fe100", CborConformanceMode.Canonical)]
        public static void WriteDouble_NonCtapConformance_ShouldMinimizePrecision(double input, string hexExpectedEncoding, CborConformanceMode mode)
        {
            byte[] expectedEncoding = hexExpectedEncoding.HexToByteArray();
            var writer = new CborWriter(mode);
            writer.WriteDouble(input);
            AssertHelper.HexEqual(expectedEncoding, writer.Encode());
        }

        [Theory]
        [InlineData(1.1, "fb3ff199999999999a")]
        [InlineData(1.0e+300, "fb7e37e43c8800759c")]
        [InlineData(-4.1, "fbc010666666666666")]
        [InlineData(3.1415926, "fb400921fb4d12d84a")]
        [InlineData(double.PositiveInfinity, "fb7ff0000000000000")]
        [InlineData(double.NegativeInfinity, "fbfff0000000000000")]
        public static void WriteDouble_Ctap2Conformance_ShouldPreservePrecision(double input, string hexExpectedEncoding)
        {
            byte[] expectedEncoding = hexExpectedEncoding.HexToByteArray();
            var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
            writer.WriteDouble(input);
            AssertHelper.HexEqual(expectedEncoding, writer.Encode());
        }

        [Fact]
        public static void WriteDouble_Ctap2Conformance_ShouldPreservePrecision_NaN()
        {
            // double.NaN may differ across architectures, in particular it's negative on x86 and positive elsewhere
            byte[] expectedEncoding = ("fb" + BitConverter.DoubleToInt64Bits(double.NaN).ToString("x8")).HexToByteArray();
            var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
            writer.WriteDouble(double.NaN);
            AssertHelper.HexEqual(expectedEncoding, writer.Encode());
        }

        [Fact]
        public static void WriteNull_SingleValue_HappyPath()
        {
            byte[] expectedEncoding = "f6".HexToByteArray();
            var writer = new CborWriter();
            writer.WriteNull();
            AssertHelper.HexEqual(expectedEncoding, writer.Encode());
        }

        [Theory]
        [InlineData(false, "f4")]
        [InlineData(true, "f5")]
        public static void WriteBoolean_SingleValue_HappyPath(bool input, string hexExpectedEncoding)
        {
            byte[] expectedEncoding = hexExpectedEncoding.HexToByteArray();
            var writer = new CborWriter();
            writer.WriteBoolean(input);
            AssertHelper.HexEqual(expectedEncoding, writer.Encode());
        }

        [Theory]
        [InlineData((CborSimpleValue)0, "e0")]
        [InlineData(CborSimpleValue.False, "f4")]
        [InlineData(CborSimpleValue.True, "f5")]
        [InlineData(CborSimpleValue.Null, "f6")]
        [InlineData(CborSimpleValue.Undefined, "f7")]
        [InlineData((CborSimpleValue)32, "f820")]
        [InlineData((CborSimpleValue)255, "f8ff")]
        public static void WriteSimpleValue_SingleValue_HappyPath(CborSimpleValue input, string hexExpectedEncoding)
        {
            byte[] expectedEncoding = hexExpectedEncoding.HexToByteArray();
            var writer = new CborWriter();
            writer.WriteSimpleValue(input);
            AssertHelper.HexEqual(expectedEncoding, writer.Encode());
        }

        [Theory]
        [InlineData((CborSimpleValue)24, "f818")]
        [InlineData((CborSimpleValue)31, "f81f")]
        public static void WriteSimpleValue_InvalidValue_LaxConformance_ShouldSucceed(CborSimpleValue input, string hexExpectedEncoding)
        {
            byte[] expectedEncoding = hexExpectedEncoding.HexToByteArray();
            var writer = new CborWriter(CborConformanceMode.Lax);
            writer.WriteSimpleValue(input);
            AssertHelper.HexEqual(expectedEncoding, writer.Encode());
        }

        [Theory]
        [InlineData(CborConformanceMode.Strict, (CborSimpleValue)24)]
        [InlineData(CborConformanceMode.Canonical, (CborSimpleValue)24)]
        [InlineData(CborConformanceMode.Ctap2Canonical, (CborSimpleValue)24)]
        [InlineData(CborConformanceMode.Strict, (CborSimpleValue)31)]
        [InlineData(CborConformanceMode.Canonical, (CborSimpleValue)31)]
        [InlineData(CborConformanceMode.Ctap2Canonical, (CborSimpleValue)31)]

        public static void WriteSimpleValue_InvalidValue_UnsupportedConformance_ShouldThrowArgumentOutOfRangeException(CborConformanceMode conformanceMode, CborSimpleValue input)
        {
            var writer = new CborWriter(conformanceMode);
            Assert.Throws<ArgumentOutOfRangeException>(() => writer.WriteSimpleValue(input));
        }
    }
}
