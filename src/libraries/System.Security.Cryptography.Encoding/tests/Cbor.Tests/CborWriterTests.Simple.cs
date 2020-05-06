﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
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
        [InlineData(float.PositiveInfinity, "fa7f800000")]
        [InlineData(float.NegativeInfinity, "faff800000")]
        [InlineData(float.NaN, "faffc00000")]
        public static void WriteSingle_SingleValue_HappyPath(float input, string hexExpectedEncoding)
        {
            byte[] expectedEncoding = hexExpectedEncoding.HexToByteArray();
            using var writer = new CborWriter();
            writer.WriteSingle(input);
            AssertHelper.HexEqual(expectedEncoding, writer.GetEncoding());
        }

        [Theory]
        [InlineData(1.1, "fb3ff199999999999a")]
        [InlineData(1.0e+300, "fb7e37e43c8800759c")]
        [InlineData(-4.1, "fbc010666666666666")]
        [InlineData(3.1415926, "fb400921fb4d12d84a")]
        [InlineData(double.PositiveInfinity, "fb7ff0000000000000")]
        [InlineData(double.NegativeInfinity, "fbfff0000000000000")]
        [InlineData(double.NaN, "fbfff8000000000000")]
        public static void WriteDouble_SingleValue_HappyPath(double input, string hexExpectedEncoding)
        {
            byte[] expectedEncoding = hexExpectedEncoding.HexToByteArray();
            using var writer = new CborWriter();
            writer.WriteDouble(input);
            AssertHelper.HexEqual(expectedEncoding, writer.GetEncoding());
        }

        [Fact]
        public static void WriteNull_SingleValue_HappyPath()
        {
            byte[] expectedEncoding = "f6".HexToByteArray();
            using var writer = new CborWriter();
            writer.WriteNull();
            AssertHelper.HexEqual(expectedEncoding, writer.GetEncoding());
        }

        [Theory]
        [InlineData(false, "f4")]
        [InlineData(true, "f5")]
        public static void WriteBoolean_SingleValue_HappyPath(bool input, string hexExpectedEncoding)
        {
            byte[] expectedEncoding = hexExpectedEncoding.HexToByteArray();
            using var writer = new CborWriter();
            writer.WriteBoolean(input);
            AssertHelper.HexEqual(expectedEncoding, writer.GetEncoding());
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
            using var writer = new CborWriter();
            writer.WriteSimpleValue(input);
            AssertHelper.HexEqual(expectedEncoding, writer.GetEncoding());
        }
    }
}
