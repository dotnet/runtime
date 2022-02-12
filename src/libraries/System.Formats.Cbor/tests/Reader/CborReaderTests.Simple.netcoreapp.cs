// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Test.Cryptography;
using Xunit;

namespace System.Formats.Cbor.Tests
{
    public partial class CborReaderTests
    {
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
        public static void ReadHalf_SingleValue_HappyPath(float expectedResult, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Assert.Equal(CborReaderState.HalfPrecisionFloat, reader.PeekState());
            Half actualResult = reader.ReadHalf();
            AssertHelpers.Equal((Half)expectedResult, actualResult);
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
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
        [InlineData("fa47c35000")] // single-precision float encoding
        [InlineData("fb7ff0000000000000")] // double-precision float encoding
        public static void ReadHalf_InvalidTypes_ShouldThrowInvalidOperationException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Assert.Throws<InvalidOperationException>(() => reader.ReadHalf());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        public static class AssertHelpers
        {

            // temporary workaround for xunit's lack of support for Half equality assertions
            public static void Equal(Half expected, Half actual)
            {
                if (Half.IsNaN(expected))
                {
                    Assert.True(Half.IsNaN(actual), $"Expected: {expected}\nActual:  {actual}");
                }
                else
                {
                    Assert.Equal(expected, actual);
                }
            }
        }
    }
}
