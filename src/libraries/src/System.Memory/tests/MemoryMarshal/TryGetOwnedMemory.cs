// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Runtime.InteropServices;
using Xunit;

namespace System.MemoryTests
{
    public static partial class MemoryMarshalTests
    {
        [Fact]
        public static void TryGetOwnedMemoryFromDefaultMemory()
        {
            ReadOnlyMemory<int> memory = default;
            Assert.False(MemoryMarshal.TryGetOwnedMemory(memory, out OwnedMemory<int> owner));
            Assert.Null(owner);
        }

        [Fact]
        public static void TryGetOwnedMemory()
        {
            int[] array = new int[10];
            OwnedMemory<int> orignalOwner = new CustomMemoryForTest<int>(array);
            ReadOnlyMemory<int> memory = orignalOwner.Memory;

            Assert.True(MemoryMarshal.TryGetOwnedMemory(memory, out CustomMemoryForTest<int> customOwner));
            Assert.Same(orignalOwner, customOwner);

            Assert.True(MemoryMarshal.TryGetOwnedMemory(memory, out OwnedMemory<int> owner));
            Assert.Same(orignalOwner, owner);

            Assert.False(MemoryMarshal.TryGetOwnedMemory(memory, out OtherMemoryForTest<int> notOwner));
            Assert.Null(notOwner);
        }

        [Fact]
        public static void TryGetOwnedMemoryFromDefaultMemory_IndexLength()
        {
            ReadOnlyMemory<int> memory = default;
            Assert.False(MemoryMarshal.TryGetOwnedMemory(memory, out OwnedMemory<int> owner, out int index, out int length));
            Assert.Equal(0, index);
            Assert.Equal(0, length);
            Assert.Null(owner);
        }

        [Fact]
        public static void TryGetOwnedMemory_IndexLength()
        {
            int[] array = new int[10];
            OwnedMemory<int> orignalOwner = new CustomMemoryForTest<int>(array);
            ReadOnlyMemory<int> memory = orignalOwner.Memory;

            for (int i = 0; i < array.Length; i++)
            {
                Assert.True(MemoryMarshal.TryGetOwnedMemory(memory.Slice(i), out CustomMemoryForTest<int> customOwner, out int index, out int length));
                Assert.Same(orignalOwner, customOwner);
                Assert.Equal(i, index);
                Assert.Equal(array.Length - i, length);
            }

            for (int i = 0; i < array.Length; i++)
            {
                Assert.True(MemoryMarshal.TryGetOwnedMemory(memory.Slice(i), out OwnedMemory<int> owner, out int index, out int length));
                Assert.Same(orignalOwner, owner);
                Assert.Equal(i, index);
                Assert.Equal(array.Length - i, length);
            }

            for (int i = 0; i < array.Length; i++)
            {
                Assert.False(MemoryMarshal.TryGetOwnedMemory(memory.Slice(i), out OtherMemoryForTest<int> notOwner, out int index, out int length));
                Assert.Null(notOwner);
            }
        }

        internal class OtherMemoryForTest<T> : OwnedMemory<T>
        {
            public OtherMemoryForTest() { }

            public override int Length => 0;
            public override bool IsDisposed => false;
            protected override bool IsRetained => false;
            public override Span<T> Span => throw new NotImplementedException();
            public override MemoryHandle Pin(int byteOffset = 0) => throw new NotImplementedException();
            protected override bool TryGetArray(out ArraySegment<T> segment) => throw new NotImplementedException();
            protected override void Dispose(bool disposing) => throw new NotImplementedException();
            public override void Retain() => throw new NotImplementedException();
            public override bool Release() => throw new NotImplementedException();
        }
    }
}
