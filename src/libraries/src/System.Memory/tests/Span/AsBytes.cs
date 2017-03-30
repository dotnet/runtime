// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Runtime.CompilerServices;

namespace System.SpanTests
{
    public static partial class SpanTests
    {
        [Fact]
        public static void AsBytesUIntToByte()
        {
            uint[] a = { 0x44332211, 0x88776655 };
            Span<uint> span = new Span<uint>(a);
            Span<byte> asBytes = span.AsBytes<uint>();

            Assert.True(Unsafe.AreSame<byte>(ref Unsafe.As<uint, byte>(ref span.DangerousGetPinnableReference()), ref asBytes.DangerousGetPinnableReference()));
            asBytes.Validate<byte>(0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88);
        }

        [Fact]
        public static void AsBytesContainsReferences()
        {
            Span<StructWithReferences> span = new Span<StructWithReferences>(Array.Empty<StructWithReferences>());
            AssertThrows<ArgumentException, StructWithReferences>(span, (_span) => _span.AsBytes<StructWithReferences>().DontBox());
        }
    }
}
