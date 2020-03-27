// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encoding.Tests.Cbor
{
    public partial class CborReaderTests
    {
        // Data points taken from https://tools.ietf.org/html/rfc7049#appendix-A
        // Additional pairs generated using http://cbor.me/

        [Theory]
        [InlineData(100000.0, "fa47c35000")]
        [InlineData(3.4028234663852886e+38, "fa7f7fffff")]
        [InlineData(float.PositiveInfinity, "fa7f800000")]
        [InlineData(float.NegativeInfinity, "faff800000")]
        [InlineData(float.NaN, "fa7fc00000")]
        internal static void ReadSingle_SingleValue_HappyPath(float expectedResult, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Assert.Equal(CborReaderState.SinglePrecisionFloat, reader.Peek());
            float actualResult = reader.ReadSingle();
            Assert.Equal(expectedResult, actualResult);
            Assert.Equal(CborReaderState.Finished, reader.Peek());
        }

        [Theory]
        [InlineData(1.1, "fb3ff199999999999a")]
        [InlineData(1.0e+300, "fb7e37e43c8800759c")]
        [InlineData(-4.1, "fbc010666666666666")]
        [InlineData(3.1415926, "fb400921fb4d12d84a")]
        [InlineData(double.PositiveInfinity, "fb7ff0000000000000")]
        [InlineData(double.NegativeInfinity, "fbfff0000000000000")]
        [InlineData(double.NaN, "fb7ff8000000000000")]
        internal static void ReadDouble_SingleValue_HappyPath(double expectedResult, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Assert.Equal(CborReaderState.DoublePrecisionFloat, reader.Peek());
            double actualResult = reader.ReadDouble();
            Assert.Equal(expectedResult, actualResult);
            Assert.Equal(CborReaderState.Finished, reader.Peek());
        }

        [Theory]
        [InlineData(100000.0, "fa47c35000")]
        [InlineData(3.4028234663852886e+38, "fa7f7fffff")]
        [InlineData(double.PositiveInfinity, "fa7f800000")]
        [InlineData(double.NegativeInfinity, "faff800000")]
        [InlineData(double.NaN, "fa7fc00000")]
        internal static void ReadDouble_SinglePrecisionValue_ShouldCoerceToDouble(double expectedResult, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Assert.Equal(CborReaderState.SinglePrecisionFloat, reader.Peek());
            double actualResult = reader.ReadDouble();
            Assert.Equal(expectedResult, actualResult);
            Assert.Equal(CborReaderState.Finished, reader.Peek());
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
        [InlineData(double.NegativeInfinity, "f9fc00")]
        internal static void ReadDouble_HalfPrecisionValue_ShouldCoerceToDouble(double expectedResult, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Assert.Equal(CborReaderState.HalfPrecisionFloat, reader.Peek());
            double actualResult = reader.ReadDouble();
            Assert.Equal(expectedResult, actualResult);
            Assert.Equal(CborReaderState.Finished, reader.Peek());
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
        [InlineData(float.NegativeInfinity, "f9fc00")]
        internal static void ReadSingle_HalfPrecisionValue_ShouldCoerceToSingle(float expectedResult, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Assert.Equal(CborReaderState.HalfPrecisionFloat, reader.Peek());
            float actualResult = reader.ReadSingle();
            Assert.Equal(expectedResult, actualResult);
            Assert.Equal(CborReaderState.Finished, reader.Peek());
        }

        [Fact]
        internal static void ReadNull_SingleValue_HappyPath()
        {
            byte[] encoding = "f6".HexToByteArray();
            var reader = new CborReader(encoding);
            Assert.Equal(CborReaderState.Null, reader.Peek());
            reader.ReadNull();
            Assert.Equal(CborReaderState.Finished, reader.Peek());
        }

        [Theory]
        [InlineData(false, "f4")]
        [InlineData(true, "f5")]
        internal static void ReadBoolean_SingleValue_HappyPath(bool expectedResult, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Assert.Equal(CborReaderState.Boolean, reader.Peek());
            bool actualResult = reader.ReadBoolean();
            Assert.Equal(expectedResult, actualResult);
            Assert.Equal(CborReaderState.Finished, reader.Peek());
        }

        [Theory]
        [InlineData((CborSpecialValue)0, "e0")]
        [InlineData(CborSpecialValue.False, "f4")]
        [InlineData(CborSpecialValue.True, "f5")]
        [InlineData(CborSpecialValue.Null, "f6")]
        [InlineData(CborSpecialValue.Undefined, "f7")]
        [InlineData((CborSpecialValue)32, "f820")]
        [InlineData((CborSpecialValue)255, "f8ff")]
        internal static void ReadSpecialValue_SingleValue_HappyPath(CborSpecialValue expectedResult, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            CborSpecialValue actualResult = reader.ReadSpecialValue();
            Assert.Equal(expectedResult, actualResult);
            Assert.Equal(CborReaderState.Finished, reader.Peek());
        }

        [Theory]
        [InlineData("01")] // integer
        [InlineData("40")] // empty text string
        [InlineData("60")] // empty byte string
        [InlineData("80")] // []
        [InlineData("a0")] // {}
        [InlineData("c202")] // tagged value
        public static void ReadSpecialValue_InvalidTypes_ShouldThrowInvalidOperationException(string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);
            InvalidOperationException exn = Assert.Throws<InvalidOperationException>(() => reader.ReadSpecialValue());

            Assert.Equal("Data item major type mismatch.", exn.Message);
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
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);
            Assert.Throws<InvalidOperationException>(() => reader.ReadBoolean());
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
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);
            Assert.Throws<InvalidOperationException>(() => reader.ReadNull());
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
        public static void ReadSingle_InvalidTypes_ShouldThrowInvalidOperationException(string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);
            Assert.Throws<InvalidOperationException>(() => reader.ReadSingle());
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
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);
            Assert.Throws<InvalidOperationException>(() => reader.ReadDouble());
        }
    }
}
