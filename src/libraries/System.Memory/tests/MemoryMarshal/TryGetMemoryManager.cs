// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Runtime.InteropServices;
using Xunit;

namespace System.MemoryTests
{
    public static partial class MemoryMarshalTests
    {
        [Fact]
        public static void TryGetMemoryManagerFromDefaultMemory()
        {
            ReadOnlyMemory<int> memory = default;
            Assert.False(MemoryMarshal.TryGetMemoryManager(memory, out MemoryManager<int> manager));
            Assert.Null(manager);
        }

        [Fact]
        public static void TryGetMemoryManager()
        {
            int[] array = new int[10];
            MemoryManager<int> originalManager = new CustomMemoryForTest<int>(array);
            ReadOnlyMemory<int> memory = originalManager.Memory;

            Assert.True(MemoryMarshal.TryGetMemoryManager(memory, out CustomMemoryForTest<int> customManager));
            Assert.Same(originalManager, customManager);

            Assert.True(MemoryMarshal.TryGetMemoryManager(memory, out MemoryManager<int> manager));
            Assert.Same(originalManager, manager);

            Assert.False(MemoryMarshal.TryGetMemoryManager(memory, out OtherMemoryForTest<int> notManager));
            Assert.Null(notManager);
        }

        [Fact]
        public static void TryGetMemoryManagerFromDefaultMemory_IndexLength()
        {
            ReadOnlyMemory<int> memory = default;
            Assert.False(MemoryMarshal.TryGetMemoryManager(memory, out MemoryManager<int> manager, out int index, out int length));
            Assert.Equal(0, index);
            Assert.Equal(0, length);
            Assert.Null(manager);
        }

        [Fact]
        public static void TryGetMemoryManager_IndexLength()
        {
            int[] array = new int[10];
            MemoryManager<int> originalManager = new CustomMemoryForTest<int>(array);
            ReadOnlyMemory<int> memory = originalManager.Memory;

            for (int i = 0; i < array.Length; i++)
            {
                Assert.True(MemoryMarshal.TryGetMemoryManager(memory.Slice(i), out CustomMemoryForTest<int> customOwner, out int index, out int length));
                Assert.Same(originalManager, customOwner);
                Assert.Equal(i, index);
                Assert.Equal(array.Length - i, length);
            }

            for (int i = 0; i < array.Length; i++)
            {
                Assert.True(MemoryMarshal.TryGetMemoryManager(memory.Slice(i), out MemoryManager<int> manager, out int index, out int length));
                Assert.Same(originalManager, manager);
                Assert.Equal(i, index);
                Assert.Equal(array.Length - i, length);
            }

            for (int i = 0; i < array.Length; i++)
            {
                Assert.False(MemoryMarshal.TryGetMemoryManager(memory.Slice(i), out OtherMemoryForTest<int> notManager, out int index, out int length));
                Assert.Null(notManager);
            }
        }

        internal class OtherMemoryForTest<T> : MemoryManager<T>
        {
            public OtherMemoryForTest() { }

            public override Span<T> GetSpan() => throw new NotImplementedException();
            public override MemoryHandle Pin(int elementIndex = 0) => throw new NotImplementedException();
            protected override void Dispose(bool disposing) => throw new NotImplementedException();
            public override void Unpin() => throw new NotImplementedException();
        }
    }
}
