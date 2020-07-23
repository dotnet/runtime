// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        public void AsBytes_FromSpan_Default_netcoreapp()
        {
            // a span wrapping data should become a span wrapping that same data.

            Utf8String theString = u8("Hello");

            Assert.True(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in theString.GetPinnableReference()), 5) == (theString.AsMemory().Span).AsBytes());
        }

        [Fact]
        public void AsBytes_FromUtf8String_netcoreapp()
        {
            Utf8String theString = u8("Hello");
            Assert.True(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in theString.GetPinnableReference()), 5) == theString.AsBytes());
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
    }
}
