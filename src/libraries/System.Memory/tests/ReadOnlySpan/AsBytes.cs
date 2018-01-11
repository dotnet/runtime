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
        public static void AsBytesUIntToByte()
        {
            uint[] a = { 0x44332211, 0x88776655 };
            ReadOnlySpan<uint> span = new ReadOnlySpan<uint>(a);
            ReadOnlySpan<byte> asBytes = span.AsBytes<uint>();

            Assert.True(Unsafe.AreSame(ref Unsafe.As<uint, byte>(ref Unsafe.AsRef(in MemoryMarshal.GetReference(span))), ref Unsafe.AsRef(in MemoryMarshal.GetReference(asBytes))));
            asBytes.Validate<byte>(0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88);
        }

        [Fact]
        public static void AsBytesContainsReferences()
        {
            ReadOnlySpan<StructWithReferences> span = new ReadOnlySpan<StructWithReferences>(Array.Empty<StructWithReferences>());
            TestHelpers.AssertThrows<ArgumentException, StructWithReferences>(span, (_span) => _span.AsBytes().DontBox());
        }
    }
}
