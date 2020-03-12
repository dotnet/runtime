// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encoding.Tests.Cbor
{
    public partial class CborWriterTests
    {
        // Data points taken from https://tools.ietf.org/html/rfc7049#appendix-A

        [Theory]
        [InlineData("", "40")]
        [InlineData("01020304", "4401020304")]
        [InlineData("ffffffffffffffffffffffffffff", "4effffffffffffffffffffffffffff")]
        public static void WriteByteString_SingleValue_HappyPath(string hexInput, string hexExpectedEncoding)
        {
            byte[] expectedEncoding = hexExpectedEncoding.HexToByteArray();
            byte[] input = hexInput.HexToByteArray();
            using var writer = new CborWriter();
            writer.WriteByteString(input);
            AssertHelper.HexEqual(expectedEncoding, writer.ToArray());
        }

        [Theory]
        [InlineData("", "60")]
        [InlineData("a", "6161")]
        [InlineData("IETF", "6449455446")]
        [InlineData("\"\\", "62225c")]
        [InlineData("\u00fc", "62c3bc")]
        [InlineData("\u6c34", "63e6b0b4")]
        [InlineData("\ud800\udd51", "64f0908591")]
        public static void WriteTextString_SingleValue_HappyPath(string input, string hexExpectedEncoding)
        {
            byte[] expectedEncoding = hexExpectedEncoding.HexToByteArray();
            using var writer = new CborWriter();
            writer.WriteTextString(input);
            AssertHelper.HexEqual(expectedEncoding, writer.ToArray());
        }

        [Fact]
        public static void WriteTextString_InvalidUnicodeString_ShouldThrowEncoderFallbackException()
        {
            // NB Xunit's InlineDataAttribute will corrupt string literals containing invalid unicode
            string invalidUnicodeString = "\ud800";
            using var writer = new CborWriter();
            Assert.Throws<System.Text.EncoderFallbackException>(() => writer.WriteTextString(invalidUnicodeString));
        }
    }
}
