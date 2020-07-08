// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Xunit;

using static System.Tests.Utf8TestUtilities;

namespace System.Tests
{
    public partial class MemoryTests
    {
        [Fact]
        public static void MemoryMarshal_TryGetArrayOfByte_Utf8String()
        {
            ReadOnlyMemory<byte> rom = u8("Hello").AsMemoryBytes();

            Assert.True(MemoryMarshal.TryGetArray(rom, out ArraySegment<byte> segment));
            Assert.NotNull(segment.Array);
            Assert.Equal(0, segment.Offset);
            Assert.Equal(5, segment.Count);
        }

        [Fact]
        public static void ReadOnlySpanOfChar8_ToString()
        {
            // unable to override ReadOnlySpan.ToString on netfx

            ReadOnlySpan<Char8> span = stackalloc Char8[] { (Char8)'H', (Char8)'i' };
            Assert.Equal("System.ReadOnlySpan<Char8>[2]", span.ToString());
        }

        [Fact]
        public static void SpanOfChar8_ToString()
        {
            // unable to override Span.ToString on netfx

            Span<Char8> span = stackalloc Char8[] { (Char8)'H', (Char8)'i' };
            Assert.Equal("System.Span<Char8>[2]", span.ToString());
        }
    }
}
