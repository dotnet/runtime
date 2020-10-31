// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

using static System.Tests.Utf8TestUtilities;

namespace System.Tests
{
    [SkipOnMono("The features from System.Utf8String.Experimental namespace are experimental.")]
    public partial class MemoryTests
    {
        [Fact]
        public static unsafe void MemoryOfByte_WithUtf8String_Pin()
        {
            Utf8String theString = u8("Hello");
            ReadOnlyMemory<byte> rom = theString.AsMemoryBytes();
            MemoryHandle memHandle = default;
            try
            {
                memHandle = Unsafe.As<ReadOnlyMemory<byte>, Memory<byte>>(ref rom).Pin();
                Assert.True(memHandle.Pointer == Unsafe.AsPointer(ref Unsafe.AsRef(in theString.GetPinnableReference())));
            }
            finally
            {
                memHandle.Dispose();
            }
        }

        [Fact]
        public static void MemoryOfByte_WithUtf8String_ToString()
        {
            ReadOnlyMemory<byte> rom = u8("Hello").AsMemoryBytes();
            Assert.Equal("System.Memory<Byte>[5]", Unsafe.As<ReadOnlyMemory<byte>, Memory<byte>>(ref rom).ToString());
        }

        [Fact]
        public static unsafe void ReadOnlyMemoryOfByte_WithUtf8String_Pin()
        {
            Utf8String theString = u8("Hello");
            ReadOnlyMemory<byte> rom = theString.AsMemoryBytes();
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
        public static void ReadOnlyMemoryOfByte_WithUtf8String_ToString()
        {
            Assert.Equal("System.ReadOnlyMemory<Byte>[5]", u8("Hello").AsMemoryBytes().ToString());
        }

        [Fact]
        public static void ReadOnlySpanOfByte_ToString()
        {
            ReadOnlySpan<byte> span = stackalloc byte[] { (byte)'H', (byte)'i' };
            Assert.Equal("System.ReadOnlySpan<Byte>[2]", span.ToString());
        }

        [Fact]
        public static void SpanOfByte_ToString()
        {
            Span<byte> span = stackalloc byte[] { (byte)'H', (byte)'i' };
            Assert.Equal("System.Span<Byte>[2]", span.ToString());
        }
    }
}
