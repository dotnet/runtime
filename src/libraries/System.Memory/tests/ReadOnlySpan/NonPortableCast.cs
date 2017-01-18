// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Runtime.CompilerServices;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        [Fact]
        public static void PortableCastUIntToUShort()
        {
            uint[] a = { 0x44332211, 0x88776655 };
            ReadOnlySpan<uint> span = new ReadOnlySpan<uint>(a);
            ReadOnlySpan<ushort> asUShort = span.NonPortableCast<uint, ushort>();

            Assert.True(Unsafe.AreSame<ushort>(ref Unsafe.As<uint, ushort>(ref span.DangerousGetPinnableReference()), ref asUShort.DangerousGetPinnableReference()));
            asUShort.Validate<ushort>(0x2211, 0x4433, 0x6655, 0x8877);
        }

        [Fact]
        public static void PortableCastToTypeContainsReferences()
        {
            ReadOnlySpan<uint> span = new ReadOnlySpan<uint>(Array.Empty<uint>());
            AssertThrows<ArgumentException, uint>(span, (_span) => _span.NonPortableCast<uint, StructWithReferences>().DontBox());
        }

        [Fact]
        public static void PortableCastFromTypeContainsReferences()
        {
            ReadOnlySpan<StructWithReferences> span = new ReadOnlySpan<StructWithReferences>(Array.Empty<StructWithReferences>());
            AssertThrows<ArgumentException, StructWithReferences>(span, (_span) => _span.NonPortableCast<StructWithReferences, uint>().DontBox());
        }
    }
}
