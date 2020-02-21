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
        [InlineData(0, "00")]
        [InlineData(1, "01")]
        [InlineData(10, "0a")]
        [InlineData(23, "17")]
        [InlineData(24, "1818")]
        [InlineData(25, "1819")]
        [InlineData(100, "1864")]
        [InlineData(1000, "1903e8")]
        [InlineData(1000000, "1a000f4240")]
        [InlineData(1000000000000, "1b000000e8d4a51000")]
        [InlineData(-1, "20")]
        [InlineData(-10, "29")]
        [InlineData(-100, "3863")]
        [InlineData(-1000, "3903e7")]
        public static void Int64WriterTests(long input, string hexExpectedEncoding)
        {
            byte[] expectedEncoding = hexExpectedEncoding.HexToByteArray();
            using var writer = new CborWriter();
            writer.Write(input);
            Assert.Equal(expectedEncoding, writer.ToArray());
        }

        [Theory]
        [InlineData(0, "00")]
        [InlineData(1, "01")]
        [InlineData(10, "0a")]
        [InlineData(23, "17")]
        [InlineData(24, "1818")]
        [InlineData(25, "1819")]
        [InlineData(100, "1864")]
        [InlineData(1000, "1903e8")]
        [InlineData(1000000, "1a000f4240")]
        [InlineData(1000000000000, "1b000000e8d4a51000")]
        [InlineData(18446744073709551615, "1bffffffffffffffff")]
        public static void UInt64WriterTests(ulong input, string hexExpectedEncoding)
        {
            byte[] expectedEncoding = hexExpectedEncoding.HexToByteArray();
            using var writer = new CborWriter();
            writer.Write(input);
            Assert.Equal(expectedEncoding, writer.ToArray());
        }
    }
}
