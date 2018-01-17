// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        [Fact]
        public static void NonPortableCastUIntToUShort()
        {
            uint[] a = { 0x44332211, 0x88776655 };
            ReadOnlySpan<uint> span = new ReadOnlySpan<uint>(a);
            ReadOnlySpan<ushort> asUShort = span.NonPortableCast<uint, ushort>();

            Assert.True(Unsafe.AreSame<ushort>(ref Unsafe.As<uint, ushort>(ref Unsafe.AsRef(in MemoryMarshal.GetReference(span))), ref Unsafe.AsRef(in MemoryMarshal.GetReference(asUShort))));
            asUShort.Validate<ushort>(0x2211, 0x4433, 0x6655, 0x8877);
        }

        [Fact]
        public static void NonPortableCastShortToLong()
        {
            short[] a = { 0x1234, 0x2345, 0x3456, 0x4567, 0x5678 };
            ReadOnlySpan<short> span = new ReadOnlySpan<short>(a);
            ReadOnlySpan<long> asLong = span.NonPortableCast<short, long>();

            Assert.True(Unsafe.AreSame<long>(ref Unsafe.As<short, long>(ref MemoryMarshal.GetReference(span)), ref MemoryMarshal.GetReference(asLong)));
            asLong.Validate<long>(0x4567345623451234);
        }

        [Fact]
        public static unsafe void NonPortableCastOverflow()
        {
            ReadOnlySpan<TestHelpers.TestStructExplicit> span = new ReadOnlySpan<TestHelpers.TestStructExplicit>(null, Int32.MaxValue);

            TestHelpers.AssertThrows<OverflowException, TestHelpers.TestStructExplicit>(span, (_span) => _span.NonPortableCast<TestHelpers.TestStructExplicit, byte>().DontBox());
            TestHelpers.AssertThrows<OverflowException, TestHelpers.TestStructExplicit>(span, (_span) => _span.NonPortableCast<TestHelpers.TestStructExplicit, ulong>().DontBox());
        }

        [Fact]
        public static void NonPortableCastToTypeContainsReferences()
        {
            ReadOnlySpan<uint> span = new ReadOnlySpan<uint>(Array.Empty<uint>());
            TestHelpers.AssertThrows<ArgumentException, uint>(span, (_span) => _span.NonPortableCast<uint, StructWithReferences>().DontBox());
        }

        [Fact]
        public static void NonPortableCastFromTypeContainsReferences()
        {
            ReadOnlySpan<StructWithReferences> span = new ReadOnlySpan<StructWithReferences>(Array.Empty<StructWithReferences>());
            TestHelpers.AssertThrows<ArgumentException, StructWithReferences>(span, (_span) => _span.NonPortableCast<StructWithReferences, uint>().DontBox());
        }
    }
}
