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

        [Theory]
        [InlineData("", "40")]
        [InlineData("01020304", "4401020304")]
        [InlineData("ffffffffffffffffffffffffffff", "4effffffffffffffffffffffffffff")]
        public static void ByteStringReaderTests(string hexExpectedValue, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            byte[] expectedValue = hexExpectedValue.HexToByteArray();
            var reader = new CborReader(encoding);
            byte[] output = reader.ReadByteString();
            Assert.Equal(expectedValue, output);
        }

        [Theory]
        [InlineData("", "60")]
        [InlineData("a", "6161")]
        [InlineData("IETF", "6449455446")]
        [InlineData("\"\\", "62225c")]
        [InlineData("\u00fc", "62c3bc")]
        [InlineData("\u6c34", "63e6b0b4")]
        [InlineData("\ud800\udd51", "64f0908591")]  
        public static void Utf8StringReaderTests(string expectedValue, string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);
            string actualResult = reader.ReadUtf8String();
            Assert.Equal(expectedValue, actualResult);
        }
    }
}
