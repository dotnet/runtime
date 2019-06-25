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
        public static void AsReadOnlyRef()
        {
            ReadOnlySpan<byte> span = new byte[] { 0x11, 0x22, 0x22, 0x11 };
            ref readonly int asInt = ref MemoryMarshal.AsRef<int>(span);

            Assert.Equal(asInt, 0x11222211);
            Assert.True(Unsafe.AreSame<byte>(ref Unsafe.As<int, byte>(ref Unsafe.AsRef(in asInt)), ref MemoryMarshal.GetReference(span)));

            var array = new byte[100];
            Array.Fill<byte>(array, 0x42);
            ref readonly TestHelpers.TestStructExplicit asStruct = ref MemoryMarshal.AsRef<TestHelpers.TestStructExplicit>(new ReadOnlySpan<byte>(array));

            Assert.Equal(asStruct.UI1, (uint)0x42424242);
        }

        [Fact]
        public static void AsReadOnlyRefFail()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => MemoryMarshal.AsRef<uint>(new ReadOnlySpan<byte>(new byte[] { 1 })));
            Assert.Throws<ArgumentOutOfRangeException>(() => MemoryMarshal.AsRef<TestHelpers.TestStructExplicit>(new ReadOnlySpan<byte>(new byte[] { 1 })));

            Assert.Throws<ArgumentException>(() => MemoryMarshal.AsRef<TestHelpers.StructWithReferences>(new ReadOnlySpan<byte>(new byte[100])));
        }
    }
}
