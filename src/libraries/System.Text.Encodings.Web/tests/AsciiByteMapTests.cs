// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Xunit;

namespace System.Text.Encodings.Web.Tests
{
    public class AsciiByteMapTests
    {
        [Fact]
        public void Ctor_EmptyByDefault()
        {
            // Act
            var byteMap = new AsciiByteMap();

            // Assert
            for (int i = 0; i < 128; i++)
            {
                Assert.False(byteMap.TryLookup(new Rune(i), out _));
            }
        }

        [Fact]
        public void MapEntries_ZigZag()
        {
            // Arrange - we'll use BoundedMemory in this test to guard against
            // out-of-bounds accesses on the byte map instance.
            using var boundedMem = BoundedMemory.Allocate<AsciiByteMap>(1);
            boundedMem.Span.Clear();
            ref var byteMap = ref boundedMem.Span[0];

            // Act
            // All chars which are multiples of 3 or 7 will be mapped to their one's complement inverse.
            for (int i = 0; i < 128; i += 3)
            {
                byteMap.InsertAsciiChar((char)i, (byte)(~i));
            }
            for (int i = 0; i < 128; i += 7)
            {
                byteMap.InsertAsciiChar((char)i, (byte)(~i));
            }

            // Assert
            for (int i = 0; i < 128; i++)
            {
                if ((i % 3) == 0 || (i % 7) == 0)
                {
                    byte expectedValue = (byte)(~i); // maps to its inverse
                    Assert.True(byteMap.TryLookup(new Rune(i), out byte actualValue));
                    Assert.Equal(expectedValue, actualValue);
                }
                else
                {
                    Assert.False(byteMap.TryLookup(new Rune(i), out _));
                }
            }
        }

        [Fact]
        public void TryLookup_NonAsciiCodePoints_ReturnsFalse()
        {
            // Arrange - we'll use BoundedMemory in this test to guard against
            // out-of-bounds accesses on the bitmap instance.
            using var boundedMem = BoundedMemory.Allocate<AsciiByteMap>(1);
            ref var byteMap = ref boundedMem.Span[0];

            Assert.False(byteMap.TryLookup(new Rune(128), out _)); // start of non-ASCII
            Assert.False(byteMap.TryLookup(new Rune(0xFFFF), out _)); // end of BMP
            Assert.False(byteMap.TryLookup(new Rune(0x10000), out _)); // start of supplementary planes
            Assert.False(byteMap.TryLookup(new Rune(0x10FFFF), out _)); // end of supplementary planes
        }
    }
}
