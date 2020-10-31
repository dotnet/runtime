// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Runtime.CompilerServices;
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

            Assert.False(MemoryMarshal.TryGetArray(rom, out ArraySegment<byte> segment));
            Assert.True(default(ArraySegment<byte>).Equals(segment));
        }

        [Fact]
        public static void MemoryMarshal_TryGetArrayOfChar8_Utf8String()
        {
            ReadOnlyMemory<Char8> rom = u8("Hello").AsMemory();

            Assert.False(MemoryMarshal.TryGetArray(rom, out ArraySegment<Char8> segment));
            Assert.True(default(ArraySegment<Char8>).Equals(segment));
        }

        [Fact]
        public static unsafe void MemoryOfChar8_WithUtf8String_Pin()
        {
            Utf8String theString = u8("Hello");
            ReadOnlyMemory<Char8> rom = theString.AsMemory();
            MemoryHandle memHandle = default;
            try
            {
                memHandle = Unsafe.As<ReadOnlyMemory<Char8>, Memory<Char8>>(ref rom).Pin();
                Assert.True(memHandle.Pointer == Unsafe.AsPointer(ref Unsafe.AsRef(in theString.GetPinnableReference())));
            }
            finally
            {
                memHandle.Dispose();
            }
        }

        [Fact]
        public static void MemoryOfChar8_WithUtf8String_ToString()
        {
            ReadOnlyMemory<Char8> rom = u8("Hello").AsMemory();
            Assert.Equal("Hello", Unsafe.As<ReadOnlyMemory<Char8>, Memory<Char8>>(ref rom).ToString());
        }

        [Fact]
        public static unsafe void ReadOnlyMemoryOfChar8_WithUtf8String_Pin()
        {
            Utf8String theString = u8("Hello");
            ReadOnlyMemory<Char8> rom = theString.AsMemory();
            MemoryHandle memHandle = default;
            try
            {
                memHandle = rom.Pin();
                Assert.True(memHandle.Pointer == Unsafe.AsPointer(ref Unsafe.AsRef(in theString.GetPinnableReference())));
            }
            finally
            {
                memHandle.Dispose();
            }
        }

        [Fact]
        public static void ReadOnlyMemoryOfChar8_WithUtf8String_ToString()
        {
            Assert.Equal("Hello", u8("Hello").AsMemory().ToString());
        }

        [Fact]
        public static void ReadOnlySpanOfChar8_ToString()
        {
            ReadOnlySpan<Char8> span = stackalloc Char8[] { (Char8)'H', (Char8)'i' };
            Assert.Equal("Hi", span.ToString());
        }

        [Fact]
        public static void SpanOfChar8_ToString()
        {
            Span<Char8> span = stackalloc Char8[] { (Char8)'H', (Char8)'i' };
            Assert.Equal("Hi", span.ToString());
        }
    }
}
