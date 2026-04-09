// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Test.Cryptography;
using Xunit;

namespace System.Formats.Cbor.Tests
{
    public partial class CborWriterTests
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
        [InlineData(float.PositiveInfinity, "f97c00")]
        [InlineData(float.NaN, "f97e00")]
        [InlineData(float.NegativeInfinity, "f9fc00")]
        public static void WriteHalf_SingleValue_HappyPath(float input, string hexExpectedEncoding)
        {
            byte[] expectedEncoding = hexExpectedEncoding.HexToByteArray();
            var writer = new CborWriter();
            writer.WriteHalf((Half)input);
            AssertHelper.HexEqual(expectedEncoding, writer.Encode());
        }
    }
}
