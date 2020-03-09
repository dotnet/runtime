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
        [Fact]
        public static void Peek_EmptyBuffer_ShouldThrowInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                var reader = new CborReader(ReadOnlyMemory<byte>.Empty);
                reader.PeekInitialByte();
            });
        }

        [Fact]
        public static void TryPeek_EmptyBuffer_ShouldReturnFalse()
        {
            var reader = new CborReader(ReadOnlyMemory<byte>.Empty);
            bool result = reader.TryPeek(out CborInitialByte _);
            Assert.False(result);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(255)]
        public static void Peek_SingleByteBuffer_ShouldReturnSameByte(byte initialByte)
        {
            ReadOnlyMemory<byte> buffer = new [] { initialByte };
            var reader = new CborReader(buffer);
            CborInitialByte header = reader.PeekInitialByte();
            Assert.Equal(initialByte, header.InitialByte);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(255)]
        public static void TryPeek_SingleByteBuffer_ShouldReturnSameByte(byte initialByte)
        {
            ReadOnlyMemory<byte> buffer = new[] { initialByte };
            var reader = new CborReader(buffer);
            bool result = reader.TryPeek(out CborInitialByte header);

            Assert.True(result);
            Assert.Equal(initialByte, header.InitialByte);
        }
    }
}
