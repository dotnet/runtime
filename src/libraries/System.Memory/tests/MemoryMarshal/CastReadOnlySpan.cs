// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.SpanTests
{
    public static partial class MemoryMarshalTests
    {
        [Fact]
        public static void CastReadOnlySpanUIntToUShort()
        {
            uint[] a;
            if (BitConverter.IsLittleEndian)
            {
                a = new uint[] { 0x44332211, 0x88776655 };
            }
            else
            {
                a = new uint[] { 0x22114433, 0x66558877 };
            }
            ReadOnlySpan<uint> span = new ReadOnlySpan<uint>(a);
            ReadOnlySpan<ushort> asUShort = MemoryMarshal.Cast<uint, ushort>(span);

            Assert.True(Unsafe.AreSame<ushort>(ref Unsafe.As<uint, ushort>(ref Unsafe.AsRef(in MemoryMarshal.GetReference(span))), ref Unsafe.AsRef(in MemoryMarshal.GetReference(asUShort))));
            asUShort.Validate<ushort>(0x2211, 0x4433, 0x6655, 0x8877);
        }

        [Fact]
        public static void CastReadOnlySpanShortToLong()
        {
            short[] a;
            if (BitConverter.IsLittleEndian)
            {
                a = new short[] { 0x1234, 0x2345, 0x3456, 0x4567, 0x5678 };
            }
            else
            {
                a = new short[] { 0x4567, 0x3456, 0x2345, 0x1234, 0x5678 };
            }
            ReadOnlySpan<short> span = new ReadOnlySpan<short>(a);
            ReadOnlySpan<long> asLong = MemoryMarshal.Cast<short, long>(span);

            Assert.True(Unsafe.AreSame<long>(ref Unsafe.As<short, long>(ref MemoryMarshal.GetReference(span)), ref MemoryMarshal.GetReference(asLong)));
            asLong.Validate<long>(0x4567345623451234);
        }

        [Fact]
        public static unsafe void CastReadOnlySpanOverflow()
        {
            ReadOnlySpan<TestHelpers.TestStructExplicit> span = new ReadOnlySpan<TestHelpers.TestStructExplicit>(null, int.MaxValue);

            TestHelpers.AssertThrows<OverflowException, TestHelpers.TestStructExplicit>(span, (_span) => MemoryMarshal.Cast<TestHelpers.TestStructExplicit, byte>(_span).DontBox());
            TestHelpers.AssertThrows<OverflowException, TestHelpers.TestStructExplicit>(span, (_span) => MemoryMarshal.Cast<TestHelpers.TestStructExplicit, ulong>(_span).DontBox());
        }

        [Fact]
        public static void CastReadOnlySpanToTypeContainsReferences()
        {
            ReadOnlySpan<uint> span = new ReadOnlySpan<uint>(Array.Empty<uint>());
            TestHelpers.AssertThrows<ArgumentException, uint>(span, (_span) => MemoryMarshal.Cast<uint, TestHelpers.StructWithReferences>(_span).DontBox());
        }

        [Fact]
        public static void CastReadOnlySpanFromTypeContainsReferences()
        {
            ReadOnlySpan<TestHelpers.StructWithReferences> span = new ReadOnlySpan<TestHelpers.StructWithReferences>(Array.Empty<TestHelpers.StructWithReferences>());
            TestHelpers.AssertThrows<ArgumentException, TestHelpers.StructWithReferences>(span, (_span) => MemoryMarshal.Cast<TestHelpers.StructWithReferences, uint>(_span).DontBox());
        }
    }
}
