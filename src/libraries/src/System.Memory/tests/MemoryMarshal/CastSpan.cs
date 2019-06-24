// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.SpanTests
{
    public static partial class MemoryMarshalTests
    {

        [Fact]
        public static void CastSpanUIntToUShort()
        {
            uint[] a = { 0x44332211, 0x88776655 };
            Span<uint> span = new Span<uint>(a);
            Span<ushort> asUShort = MemoryMarshal.Cast<uint, ushort>(span);

            Assert.True(Unsafe.AreSame<ushort>(ref Unsafe.As<uint, ushort>(ref MemoryMarshal.GetReference(span)), ref MemoryMarshal.GetReference(asUShort)));
            asUShort.Validate<ushort>(0x2211, 0x4433, 0x6655, 0x8877);
        }

        struct EmptyStruct { }

        [Fact]
        public static void CastSpanToEmptyStruct()
        {
            Span<uint> span = new Span<uint>(new uint[] { 1 });
            Span<EmptyStruct> emptyspan = MemoryMarshal.Cast<uint, EmptyStruct>(span);
            Assert.Equal(1, Unsafe.SizeOf<EmptyStruct>());
            Assert.Equal(4, emptyspan.Length);
        }

        [Fact]
        public static void CastSpanShortToLong()
        {
            short[] a = { 0x1234, 0x2345, 0x3456, 0x4567, 0x5678 };
            Span<short> span = new Span<short>(a);
            Span<long> asLong = MemoryMarshal.Cast<short, long>(span);

            Assert.True(Unsafe.AreSame<long>(ref Unsafe.As<short, long>(ref MemoryMarshal.GetReference(span)), ref MemoryMarshal.GetReference(asLong)));
            asLong.Validate<long>(0x4567345623451234);
        }

        [Fact]
        public static unsafe void CastSpanOverflow()
        {
            Span<MemoryTestHelpers.TestStructExplicit> span = new Span<MemoryTestHelpers.TestStructExplicit>(null, int.MaxValue);

            MemoryTestHelpers.AssertThrows<OverflowException, MemoryTestHelpers.TestStructExplicit>(span, (_span) => MemoryMarshal.Cast<MemoryTestHelpers.TestStructExplicit, byte>(_span).DontBox());
            MemoryTestHelpers.AssertThrows<OverflowException, MemoryTestHelpers.TestStructExplicit>(span, (_span) => MemoryMarshal.Cast<MemoryTestHelpers.TestStructExplicit, ulong>(_span).DontBox());
        }

        [Fact]
        public static void CastSpanToTypeContainsReferences()
        {
            Span<uint> span = new Span<uint>(Array.Empty<uint>());
            MemoryTestHelpers.AssertThrows<ArgumentException, uint>(span, (_span) => MemoryMarshal.Cast<uint, MemoryTestHelpers.StructWithReferences>(_span).DontBox());
        }

        [Fact]
        public static void CastSpanFromTypeContainsReferences()
        {
            Span<MemoryTestHelpers.StructWithReferences> span = new Span<MemoryTestHelpers.StructWithReferences>(Array.Empty<MemoryTestHelpers.StructWithReferences>());
            MemoryTestHelpers.AssertThrows<ArgumentException, MemoryTestHelpers.StructWithReferences>(span, (_span) => MemoryMarshal.Cast<MemoryTestHelpers.StructWithReferences, uint>(_span).DontBox());
        }
    }
}
