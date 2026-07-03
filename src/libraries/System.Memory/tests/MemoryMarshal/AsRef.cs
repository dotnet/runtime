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
        public static void AsRef()
        {
            Span<byte> span = new byte[] { 0x11, 0x22, 0x22, 0x11 };
            ref int asInt = ref MemoryMarshal.AsRef<int>(span);

            Assert.Equal(0x11222211, asInt);
            Assert.True(Unsafe.AreSame<byte>(ref Unsafe.As<int, byte>(ref asInt), ref MemoryMarshal.GetReference(span)));

            var array = new byte[100];
            Array.Fill<byte>(array, 0x42);
            ref TestHelpers.TestStructExplicit asStruct = ref MemoryMarshal.AsRef<TestHelpers.TestStructExplicit>(new Span<byte>(array));

            Assert.Equal((uint)0x42424242, asStruct.UI1);
        }

        [Fact]
        public static void AsRefFail()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => MemoryMarshal.AsRef<uint>(new Span<byte>(new byte[] { 1 })));
            Assert.Throws<ArgumentOutOfRangeException>(() => MemoryMarshal.AsRef<TestHelpers.TestStructExplicit>(new Span<byte>(new byte[] { 1 })));

            Assert.Throws<ArgumentException>(() => MemoryMarshal.AsRef<TestHelpers.StructWithReferences>(new Span<byte>(new byte[100])));
        }

        [Fact]
        public static void AsRef_ImplicitSpanConversion_ReturnsMutableRef()
        {
            // Validates that when a type has an implicit conversion to Span<byte>,
            // AsRef returns a mutable ref (not ref readonly). This is enabled by
            // [OverloadResolutionPriority(1)] on the Span<byte> overload.
            SpanConvertibleStruct s = new();
            ref int value = ref MemoryMarshal.AsRef<int>(s);
            value = 42;
            Assert.Equal(42, s.Value);
        }

        private struct SpanConvertibleStruct
        {
#pragma warning disable CS0649 // Field is assigned via MemoryMarshal.AsRef
            private int _value;
#pragma warning restore CS0649
            public int Value => _value;

            public static implicit operator Span<byte>(in SpanConvertibleStruct s) =>
                MemoryMarshal.CreateSpan(ref Unsafe.As<int, byte>(ref Unsafe.AsRef(in s._value)), sizeof(int));
        }
    }
}
