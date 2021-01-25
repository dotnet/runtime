// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Formats.Asn1.Tests.Reader
{
    public static class ReaderStateTests
    {
        [Fact]
        public static void HasDataAndThrowIfNotEmpty()
        {
            AsnReader reader = new AsnReader(new byte[] { 0x01, 0x01, 0x00 }, AsnEncodingRules.BER);
            Assert.True(reader.HasData);
            Assert.Throws<AsnContentException>(() => reader.ThrowIfNotEmpty());

            // Consume the current value and move on.
            reader.ReadEncodedValue();

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
