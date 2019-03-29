// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

using static System.Tests.Utf8TestUtilities;

namespace System.Tests
{
    public partial class Utf8ExtensionsTests
    {
        [Fact]
        public unsafe void AsBytes_FromSpan_Default()
        {
            // First, a default span should become a default span.

            Assert.True(default(ReadOnlySpan<byte>) == new ReadOnlySpan<Char8>().AsBytes());

            // Next, an empty but non-default span should become an empty but non-default span.

            Assert.True(new ReadOnlySpan<byte>((void*)0x12345, 0) == new ReadOnlySpan<Char8>((void*)0x12345, 0).AsBytes());

            // Finally, a span wrapping data should become a span wrapping that same data.

            Utf8String theString = u8("Hello");
            Assert.True(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in theString.GetPinnableReference()), 5) == ((ReadOnlySpan<Char8>)theString).AsBytes());
        }

        [Fact]
        public void AsBytes_FromUtf8String()
        {
            Assert.True(default(ReadOnlySpan<byte>) == ((Utf8String)null).AsBytes());

            Utf8String theString = u8("Hello");
            Assert.True(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in theString.GetPinnableReference()), 5) == theString.AsBytes());
        }

        [Fact]
        public void AsBytes_FromUtf8String_WithStart()
        {
            Assert.True(default(ReadOnlySpan<byte>) == ((Utf8String)null).AsBytes(0));
            Assert.True(u8("Hello").AsBytes(5).IsEmpty);

            SpanAssert.Equal(new byte[] { (byte)'e', (byte)'l', (byte)'l', (byte)'o' }, u8("Hello").AsBytes(1));
        }

        [Fact]
        public void AsBytes_FromUtf8String_WithStart_ArgOutOfRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>("start", () => ((Utf8String)null).AsBytes(1));
            Assert.Throws<ArgumentOutOfRangeException>("start", () => u8("Hello").AsBytes(-1));
            Assert.Throws<ArgumentOutOfRangeException>("start", () => u8("Hello").AsBytes(6));
        }

        [Fact]
        public void AsBytes_FromUtf8String_WithStartAndLength()
        {
            Assert.True(default(ReadOnlySpan<byte>) == ((Utf8String)null).AsBytes(0, 0));
            Assert.True(u8("Hello").AsBytes(5, 0).IsEmpty);

            SpanAssert.Equal(new byte[] { (byte)'e', (byte)'l', (byte)'l' }, u8("Hello").AsBytes(1, 3));
        }

        [Fact]
        public void AsBytes_FromUtf8String_WithStartAndLength_ArgOutOfRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>("start", () => ((Utf8String)null).AsBytes(0, 1));
            Assert.Throws<ArgumentOutOfRangeException>("start", () => ((Utf8String)null).AsBytes(1, 0));
            Assert.Throws<ArgumentOutOfRangeException>("start", () => u8("Hello").AsBytes(5, 1));
            Assert.Throws<ArgumentOutOfRangeException>("start", () => u8("Hello").AsBytes(4, -2));
        }

        [Fact]
        public void AsMemory_FromUtf8String()
        {
            Assert.True(default(ReadOnlyMemory<Char8>).Equals(((Utf8String)null).AsMemory()));

            Utf8String theString = u8("Hello");
            Assert.True(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<byte, Char8>(ref Unsafe.AsRef(in theString.GetPinnableReference())), 5) == theString.AsMemory().Span);
        }

        [Fact]
        public void AsMemory_FromUtf8String_WithStart()
        {
            Assert.True(default(ReadOnlyMemory<Char8>).Equals(((Utf8String)null).AsMemory(0)));
            Assert.True(u8("Hello").AsMemory(5).IsEmpty);

            SpanAssert.Equal(new Char8[] { (Char8)'e', (Char8)'l', (Char8)'l', (Char8)'o' }, u8("Hello").AsMemory(1).Span);
        }

        [Fact]
        public void AsMemory_FromUtf8String_WithStart_ArgOutOfRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>("start", () => ((Utf8String)null).AsMemory(1));
            Assert.Throws<ArgumentOutOfRangeException>("start", () => u8("Hello").AsMemory(-1));
            Assert.Throws<ArgumentOutOfRangeException>("start", () => u8("Hello").AsMemory(6));
        }

        [Fact]
        public void AsMemory_FromUtf8String_WithStartAndLength()
        {
            Assert.True(default(ReadOnlyMemory<Char8>).Equals(((Utf8String)null).AsMemory(0, 0)));
            Assert.True(u8("Hello").AsMemory(5, 0).IsEmpty);

            SpanAssert.Equal(new Char8[] { (Char8)'e', (Char8)'l', (Char8)'l' }, u8("Hello").AsMemory(1, 3).Span);
        }

        [Fact]
        public void AsMemory_FromUtf8String_WithStartAndLength_ArgOutOfRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>("start", () => ((Utf8String)null).AsMemory(0, 1));
            Assert.Throws<ArgumentOutOfRangeException>("start", () => ((Utf8String)null).AsMemory(1, 0));
            Assert.Throws<ArgumentOutOfRangeException>("start", () => u8("Hello").AsMemory(5, 1));
            Assert.Throws<ArgumentOutOfRangeException>("start", () => u8("Hello").AsMemory(4, -2));
        }

        [Fact]
        public void AsMemoryBytes_FromUtf8String()
        {
            Assert.True(default(ReadOnlyMemory<byte>).Equals(((Utf8String)null).AsMemoryBytes()));

            Utf8String theString = u8("Hello");
            Assert.True(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in theString.GetPinnableReference()), 5) == theString.AsMemoryBytes().Span);
        }

        [Fact]
        public void AsMemoryBytes_FromUtf8String_WithStart()
        {
            Assert.True(default(ReadOnlyMemory<byte>).Equals(((Utf8String)null).AsMemoryBytes(0)));
            Assert.True(u8("Hello").AsMemoryBytes(5).IsEmpty);

            SpanAssert.Equal(new byte[] { (byte)'e', (byte)'l', (byte)'l', (byte)'o' }, u8("Hello").AsMemoryBytes(1).Span);
        }

        [Fact]
        public void AsMemoryBytes_FromUtf8String_WithStart_ArgOutOfRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>("start", () => ((Utf8String)null).AsMemoryBytes(1));
            Assert.Throws<ArgumentOutOfRangeException>("start", () => u8("Hello").AsMemoryBytes(-1));
            Assert.Throws<ArgumentOutOfRangeException>("start", () => u8("Hello").AsMemoryBytes(6));
        }

        [Fact]
        public void AsMemoryBytes_FromUtf8String_WithStartAndLength()
        {
            Assert.True(default(ReadOnlyMemory<byte>).Equals(((Utf8String)null).AsMemoryBytes(0, 0)));
            Assert.True(u8("Hello").AsMemoryBytes(5, 0).IsEmpty);

            SpanAssert.Equal(new byte[] { (byte)'e', (byte)'l', (byte)'l' }, u8("Hello").AsMemoryBytes(1, 3).Span);
        }

        [Fact]
        public void AsMemoryBytes_FromUtf8String_WithStartAndLength_ArgOutOfRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>("start", () => ((Utf8String)null).AsMemoryBytes(0, 1));
            Assert.Throws<ArgumentOutOfRangeException>("start", () => ((Utf8String)null).AsMemoryBytes(1, 0));
            Assert.Throws<ArgumentOutOfRangeException>("start", () => u8("Hello").AsMemoryBytes(5, 1));
            Assert.Throws<ArgumentOutOfRangeException>("start", () => u8("Hello").AsMemoryBytes(4, -2));
        }

        [Fact]
        public void AsSpan_FromUtf8String()
        {
            Assert.True(default(ReadOnlySpan<Char8>) == ((Utf8String)null).AsSpan());

            Utf8String theString = u8("Hello");
            Assert.True(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<byte, Char8>(ref Unsafe.AsRef(in theString.GetPinnableReference())), 5) == theString.AsSpan());
        }

        [Fact]
        public void AsSpan_FromUtf8String_WithStart()
        {
            Assert.True(default(ReadOnlySpan<Char8>) == ((Utf8String)null).AsSpan(0));
            Assert.True(u8("Hello").AsSpan(5).IsEmpty);

            SpanAssert.Equal(new Char8[] { (Char8)'e', (Char8)'l', (Char8)'l', (Char8)'o' }, u8("Hello").AsSpan(1));
        }

        [Fact]
        public void AsSpan_FromUtf8String_WithStart_ArgOutOfRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>("start", () => ((Utf8String)null).AsSpan(1));
            Assert.Throws<ArgumentOutOfRangeException>("start", () => u8("Hello").AsSpan(-1));
            Assert.Throws<ArgumentOutOfRangeException>("start", () => u8("Hello").AsSpan(6));
        }

        [Fact]
        public void AsSpan_FromUtf8String_WithStartAndLength()
        {
            Assert.True(default(ReadOnlySpan<Char8>) == ((Utf8String)null).AsSpan(0, 0));
            Assert.True(u8("Hello").AsSpan(5, 0).IsEmpty);

            SpanAssert.Equal(new Char8[] { (Char8)'e', (Char8)'l', (Char8)'l' }, u8("Hello").AsSpan(1, 3));
        }

        [Fact]
        public void AsSpan_FromUtf8String_WithStartAndLength_ArgOutOfRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>("start", () => ((Utf8String)null).AsSpan(0, 1));
            Assert.Throws<ArgumentOutOfRangeException>("start", () => ((Utf8String)null).AsSpan(1, 0));
            Assert.Throws<ArgumentOutOfRangeException>("start", () => u8("Hello").AsSpan(5, 1));
            Assert.Throws<ArgumentOutOfRangeException>("start", () => u8("Hello").AsSpan(4, -2));
        }
    }
}
