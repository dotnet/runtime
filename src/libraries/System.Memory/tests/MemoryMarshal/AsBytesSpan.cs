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
        public static void Span_AsBytesUIntToByte()
        {
            uint[] a;
            if (BitConverter.IsLittleEndian)
            {
                a = new uint[] { 0x44332211, 0x88776655 };
            }
            else
            {
                a = new uint[] { 0x11223344, 0x55667788 };
            }
            Span<uint> span = new Span<uint>(a);
            Span<byte> asBytes = MemoryMarshal.AsBytes<uint>(span);

            Assert.True(Unsafe.AreSame<byte>(ref Unsafe.As<uint, byte>(ref MemoryMarshal.GetReference(span)), ref MemoryMarshal.GetReference(asBytes)));
            asBytes.Validate<byte>(0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88);
        }

        [Fact]
        public static void Span_AsBytesContainsReferences()
        {
            Span<TestHelpers.StructWithReferences> span = new Span<TestHelpers.StructWithReferences>(Array.Empty<TestHelpers.StructWithReferences>());
            TestHelpers.AssertThrows<ArgumentException, TestHelpers.StructWithReferences>(span, (_span) => MemoryMarshal.AsBytes(_span).DontBox());
        }
    }
}
