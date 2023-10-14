// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Test.Cryptography;
using Xunit;

namespace System.Formats.Cbor.Tests
{
    public partial class CborReaderTests
    {
        // Data points taken from https://tools.ietf.org/html/rfc7049#appendix-A
        // Additional pairs generated using http://cbor.me/

        [Theory]
        [InlineData(1.0, "fa3f800000")]
        [InlineData(100000.0, "fa47c35000")]
        [InlineData(3.4028234663852886e+38, "fa7f7fffff")]
        [InlineData(float.PositiveInfinity, "fa7f800000")]
        [InlineData(float.NegativeInfinity, "faff800000")]
        [InlineData(float.NaN, "fa7fc00000")]
        [InlineData(float.NaN, "faffc00000")]
        public static void ReadSingle_SingleValue_HappyPath(float expectedResult, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Assert.Equal(CborReaderState.SinglePrecisionFloat, reader.PeekState());
            float actualResult = reader.ReadSingle();
            Assert.Equal(expectedResult, actualResult);
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Theory]
        [InlineData(1.1, "fb3ff199999999999a")]
        [InlineData(1.0e+300, "fb7e37e43c8800759c")]
        [InlineData(-4.1, "fbc010666666666666")]
        [InlineData(3.1415926, "fb400921fb4d12d84a")]
        [InlineData(double.PositiveInfinity, "fb7ff0000000000000")]
        [InlineData(double.NegativeInfinity, "fbfff0000000000000")]
        [InlineData(double.NaN, "fb7ff8000000000000")]
        [InlineData(double.NaN, "fbfff8000000000000")]
        public static void ReadDouble_SingleValue_HappyPath(double expectedResult, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Assert.Equal(CborReaderState.DoublePrecisionFloat, reader.PeekState());
            double actualResult = reader.ReadDouble();
            Assert.Equal(expectedResult, actualResult);
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Theory]
        [InlineData(100000.0, "fa47c35000")]
        [InlineData(3.4028234663852886e+38, "fa7f7fffff")]
        [InlineData(double.PositiveInfinity, "fa7f800000")]
        [InlineData(double.NegativeInfinity, "faff800000")]
        [InlineData(double.NaN, "fa7fc00000")]
        [InlineData(double.NaN, "faffc00000")]
        public static void ReadDouble_SinglePrecisionValue_ShouldCoerceToDouble(double expectedResult, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Assert.Equal(CborReaderState.SinglePrecisionFloat, reader.PeekState());
            double actualResult = reader.ReadDouble();
            Assert.Equal(expectedResult, actualResult);
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Theory]
        [InlineData(0.0, "f90000")]
        [InlineData(-0.0, "f98000")]
        [InlineData(1.0, "f93c00")]
        [InlineData(1.5, "f93e00")]
        [InlineData(65504.0, "f97bff")]
        [InlineData(5.960464477539063e-8, "f90001")]
        [InlineData(0.00006103515625, "f90400")]
        [InlineData(-4.0, "f9c400")]
        [InlineData(double.PositiveInfinity, "f97c00")]
        [InlineData(double.NaN, "f97e00")]
        [InlineData(double.NaN, "f9fe00")]
        [InlineData(double.NegativeInfinity, "f9fc00")]
        public static void ReadDouble_HalfPrecisionValue_ShouldCoerceToDouble(double expectedResult, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Assert.Equal(CborReaderState.HalfPrecisionFloat, reader.PeekState());
            double actualResult = reader.ReadDouble();
            Assert.Equal(expectedResult, actualResult);
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Theory]
        [InlineData(0.0, "f90000")]
        [InlineData(-0.0, "f98000")]
        [InlineData(1.0, "f93c00")]
        [InlineData(1.5, "f93e00")]
        [InlineData(65504.0, "f97bff")]
        [InlineData(5.960464477539063e-8, "f90001")]
        [InlineData(0.00006103515625, "f90400")]
        [InlineData(-4.0, "f9c400")]
        [InlineData(float.PositiveInfinity, "f97c00")]
        [InlineData(float.NaN, "f97e00")]
        [InlineData(float.NaN, "f9fe00")]
        [InlineData(float.NegativeInfinity, "f9fc00")]
        public static void ReadSingle_HalfPrecisionValue_ShouldCoerceToSingle(float expectedResult, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Assert.Equal(CborReaderState.HalfPrecisionFloat, reader.PeekState());
            float actualResult = reader.ReadSingle();
            Assert.Equal(expectedResult, actualResult);
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Fact]
        public static void ReadNull_SingleValue_HappyPath()
        {
            byte[] encoding = "f6".HexToByteArray();
            var reader = new CborReader(encoding);
            Assert.Equal(CborReaderState.Null, reader.PeekState());
            reader.ReadNull();
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Theory]
        [InlineData(false, "f4")]
        [InlineData(true, "f5")]
        public static void ReadBoolean_SingleValue_HappyPath(bool expectedResult, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Assert.Equal(CborReaderState.Boolean, reader.PeekState());
            bool actualResult = reader.ReadBoolean();
            Assert.Equal(expectedResult, actualResult);
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Theory]
        [InlineData((CborSimpleValue)0, "e0")]
        [InlineData(CborSimpleValue.False, "f4")]
        [InlineData(CborSimpleValue.True, "f5")]
        [InlineData(CborSimpleValue.Null, "f6")]
        [InlineData(CborSimpleValue.Undefined, "f7")]
        [InlineData((CborSimpleValue)32, "f820")]
        [InlineData((CborSimpleValue)255, "f8ff")]
        public static void ReadSimpleValue_SingleValue_HappyPath(CborSimpleValue expectedResult, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            CborSimpleValue actualResult = reader.ReadSimpleValue();
            Assert.Equal(expectedResult, actualResult);
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Theory]
        [InlineData((CborSimpleValue)0, "f800")]
        [InlineData((CborSimpleValue)23, "f817")]
        [InlineData((CborSimpleValue)24, "f818")]
        [InlineData((CborSimpleValue)31, "f81f")]
        public static void ReadSimpleValue_UnsupportedRanges_LaxConformance_ShouldSucceed(CborSimpleValue expectedResult, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding, CborConformanceMode.Lax);
            CborSimpleValue actualResult = reader.ReadSimpleValue();
            Assert.Equal(expectedResult, actualResult);
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Theory]
        [InlineData(CborConformanceMode.Strict, "f800")]
        [InlineData(CborConformanceMode.Strict, "f801")]
        [InlineData(CborConformanceMode.Strict, "f818")]
        [InlineData(CborConformanceMode.Strict, "f81f")]
        [InlineData(CborConformanceMode.Canonical, "f801")]
        [InlineData(CborConformanceMode.Ctap2Canonical, "f800")]
        public static void ReadSimpleValue_UnsupportedRanges_UnsupportedConformance_ShouldThrowCborContentException(CborConformanceMode conformanceMode, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding, conformanceMode);
            Assert.Throws<CborContentException>(() => reader.ReadSimpleValue());
        }

        [Theory]
        [InlineData("01")] // integer
        [InlineData("40")] // empty text string
        [InlineData("60")] // empty byte string
        [InlineData("80")] // []
        [InlineData("a0")] // {}
        [InlineData("c202")] // tagged value
        public static void ReadSimpleValue_InvalidTypes_ShouldThrowInvalidOperationException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            Assert.Throws<InvalidOperationException>(() => reader.ReadSimpleValue());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        [InlineData("01")] // integer
        [InlineData("40")] // empty text string
        [InlineData("60")] // empty byte string
        [InlineData("80")] // []
        [InlineData("a0")] // {}
        [InlineData("f97e00")] // NaN
        [InlineData("f6")] // null
        [InlineData("fb3ff199999999999a")] // 1.1
        [InlineData("c202")] // tagged value
        public static void ReadBoolean_InvalidTypes_ShouldThrowInvalidOperationException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Assert.Throws<InvalidOperationException>(() => reader.ReadBoolean());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        [InlineData("01")] // integer
        [InlineData("40")] // empty text string
        [InlineData("60")] // empty byte string
        [InlineData("80")] // []
        [InlineData("a0")] // {}
        [InlineData("f4")] // false
        [InlineData("f97e00")] // NaN
        [InlineData("fb3ff199999999999a")] // 1.1
        [InlineData("c202")] // tagged value
        public static void ReadNull_InvalidTypes_ShouldThrowInvalidOperationException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Assert.Throws<InvalidOperationException>(() => reader.ReadNull());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        [InlineData("01")] // integer
        [InlineData("40")] // empty text string
        [InlineData("60")] // empty byte string
        [InlineData("80")] // []
        [InlineData("a0")] // {}
        [InlineData("f6")] // null
        [InlineData("f4")] // false
        [InlineData("c202")] // tagged value
        [InlineData("fb7ff0000000000000")] // double-precision float encoding
        public static void ReadSingle_InvalidTypes_ShouldThrowInvalidOperationException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Assert.Throws<InvalidOperationException>(() => reader.ReadSingle());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        [InlineData("01")] // integer
        [InlineData("40")] // empty text string
        [InlineData("60")] // empty byte string
        [InlineData("80")] // []
        [InlineData("a0")] // {}
        [InlineData("f6")] // null
        [InlineData("f4")] // false
        [InlineData("c202")] // tagged value
        public static void ReadDouble_InvalidTypes_ShouldThrowInvalidOperationException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Assert.Throws<InvalidOperationException>(() => reader.ReadDouble());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }
    }
}
