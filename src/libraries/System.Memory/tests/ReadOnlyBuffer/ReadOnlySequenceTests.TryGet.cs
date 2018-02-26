// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.MemoryTests;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Memory.Tests
{
    public class ReadOnlySequenceTryGetTests
    {
        [Fact]
        public void Ctor_Array_Offset()
        {
            var buffer = new ReadOnlySequence<byte>(new byte[] { 1, 2, 3, 4, 5 }, 2, 3);

            Assert.True(SequenceMarshal.TryGetArray(buffer, out ArraySegment<byte> array));
            Assert.Equal(2, array.Offset);
            Assert.Equal(3, array.Count);

            Assert.False(SequenceMarshal.TryGetMemoryList(buffer, out _, out _, out _, out _));
            Assert.False(SequenceMarshal.TryGetOwnedMemory(buffer, out _, out _, out _));
            Assert.False(SequenceMarshal.TryGetReadOnlyMemory(buffer, out _));
        }

        [Fact]
        public void Ctor_Memory()
        {
            var memory = new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3, 4, 5 });
            var buffer = new ReadOnlySequence<byte>(memory.Slice(2, 3));

            Assert.True(SequenceMarshal.TryGetReadOnlyMemory(buffer, out ReadOnlyMemory<byte> newMemory));
            Assert.Equal(new byte[] { 3, 4, 5 }, newMemory.ToArray());

            // Memory is internally stored in single IMemoryList node so it would be accessible via TryGetMemoryList
            Assert.True(SequenceMarshal.TryGetMemoryList(buffer, out IMemoryList<byte> startSegment, out int startIndex, out IMemoryList<byte> endSegment, out int endIndex));
            Assert.Equal(startSegment, endSegment);
            Assert.Equal(new byte[] { 3, 4, 5 }, startSegment.Memory.ToArray());
            Assert.Equal(0, startIndex);
            Assert.Equal(3, endIndex);

            Assert.False(SequenceMarshal.TryGetArray(buffer, out _));
            Assert.False(SequenceMarshal.TryGetOwnedMemory(buffer, out _, out _, out _));
        }

        [Fact]
        public void Ctor_OwnedMemory_Offset()
        {
            var ownedMemory = new CustomMemoryForTest<byte>(new byte[] { 1, 2, 3, 4, 5 }, 0, 5);
            var buffer = new ReadOnlySequence<byte>(ownedMemory, 2, 3);

            Assert.True(SequenceMarshal.TryGetOwnedMemory(buffer, out OwnedMemory<byte> newOwnedMemory, out int start, out int length));
            Assert.Equal(ownedMemory, newOwnedMemory);
            Assert.Equal(2, start);
            Assert.Equal(3, length);

            Assert.False(SequenceMarshal.TryGetMemoryList(buffer, out _, out _, out _, out _));
            Assert.False(SequenceMarshal.TryGetArray(buffer, out _));
            Assert.False(SequenceMarshal.TryGetReadOnlyMemory(buffer, out _));
        }

        [Fact]
        public void Ctor_IMemoryList()
        {
            var memoryListSegment1 = new BufferSegment(new byte[] { 1, 2, 3 });
            var memoryListSegment2 = memoryListSegment1.Append(new byte[] { 4, 5 });

            var buffer = new ReadOnlySequence<byte>(memoryListSegment1, 2, memoryListSegment2, 1);

            Assert.True(SequenceMarshal.TryGetMemoryList(buffer, out IMemoryList<byte> startSegment, out int startIndex, out IMemoryList<byte> endSegment, out int endIndex));
            Assert.Equal(startSegment, memoryListSegment1);
            Assert.Equal(endSegment, memoryListSegment2);

            Assert.Equal(2, startIndex);
            Assert.Equal(1, endIndex);

            Assert.False(SequenceMarshal.TryGetArray(buffer, out _));
            Assert.False(SequenceMarshal.TryGetOwnedMemory(buffer, out _, out _, out _));
            Assert.False(SequenceMarshal.TryGetReadOnlyMemory(buffer, out _));
        }
    }
}
