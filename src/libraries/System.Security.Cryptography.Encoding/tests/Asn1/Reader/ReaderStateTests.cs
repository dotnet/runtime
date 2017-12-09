// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.Asn1;
using Xunit;

namespace System.Security.Cryptography.Tests.Asn1
{
    public static class ReaderStateTests
    {
        [Fact]
        public static void HasDataAndThrowIfNotEmpty()
        {
            AsnReader reader = new AsnReader(new byte[] { 0x01, 0x01, 0x00 }, AsnEncodingRules.BER);
            Assert.True(reader.HasData);
            Assert.Throws<CryptographicException>(() => reader.ThrowIfNotEmpty());

            // Consume the current value and move on.
            reader.GetEncodedValue();

            Assert.False(reader.HasData);
            // Assert.NoThrow
            reader.ThrowIfNotEmpty();
        }

        [Fact]
        public static void HasDataAndThrowIfNotEmpty_StartsEmpty()
        {
            AsnReader reader = new AsnReader(ReadOnlyMemory<byte>.Empty, AsnEncodingRules.BER);
            Assert.False(reader.HasData);
            // Assert.NoThrow
            reader.ThrowIfNotEmpty();
        }
    }
}
