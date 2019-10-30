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

            Assert.False(SequenceMarshal.TryGetReadOnlySequenceSegment(buffer, out _, out _, out _, out _));

            // Array can be retrieved with TryGetReadOnlyMemory
            Assert.True(SequenceMarshal.TryGetReadOnlyMemory(buffer, out ReadOnlyMemory<byte> newMemory));
            Assert.Equal(new byte[] { 3, 4, 5 }, newMemory.ToArray());
        }

        [Fact]
        public void Ctor_Memory()
        {
            var memory = new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3, 4, 5 });
            var buffer = new ReadOnlySequence<byte>(memory.Slice(2, 3));

            Assert.True(SequenceMarshal.TryGetReadOnlyMemory(buffer, out ReadOnlyMemory<byte> newMemory));
            Assert.Equal(new byte[] { 3, 4, 5 }, newMemory.ToArray());

            Assert.False(SequenceMarshal.TryGetReadOnlySequenceSegment(buffer, out ReadOnlySequenceSegment<byte> startSegment, out int startIndex, out ReadOnlySequenceSegment<byte> endSegment, out int endIndex));

            // Memory is internally decomposed to its container so it would be accessible via TryGetArray
            Assert.True(SequenceMarshal.TryGetArray(buffer, out ArraySegment<byte> array));
            Assert.Equal(2, array.Offset);
            Assert.Equal(3, array.Count);
        }


        [Fact]
        public void Ctor_Memory_String()
        {
            var text = "Hello";
            var memory = text.AsMemory();
            var buffer = new ReadOnlySequence<char>(memory.Slice(2, 3));

            Assert.True(SequenceMarshal.TryGetReadOnlyMemory(buffer, out ReadOnlyMemory<char> newMemory));
            Assert.Equal(text.Substring(2, 3).ToCharArray(), newMemory.ToArray());

            Assert.False(SequenceMarshal.TryGetReadOnlySequenceSegment(buffer, out ReadOnlySequenceSegment<char> startSegment, out int startIndex, out ReadOnlySequenceSegment<char> endSegment, out int endIndex));
            Assert.False(SequenceMarshal.TryGetArray(buffer, out _));
        }

        [Fact]
        public void Ctor_IMemoryList_SingleBlock()
        {
            var memoryListSegment = new BufferSegment<byte>(new byte[] { 1, 2, 3, 4, 5 });

            var buffer = new ReadOnlySequence<byte>(memoryListSegment, 2, memoryListSegment, 5);

            Assert.True(SequenceMarshal.TryGetReadOnlySequenceSegment(buffer, out ReadOnlySequenceSegment<byte> startSegment, out int startIndex, out ReadOnlySequenceSegment<byte> endSegment, out int endIndex));
            Assert.Equal(startSegment, memoryListSegment);
            Assert.Equal(endSegment, memoryListSegment);

            Assert.Equal(2, startIndex);
            Assert.Equal(5, endIndex);

            Assert.False(SequenceMarshal.TryGetArray(buffer, out _));

            // Single block can be retrieved with TryGetReadOnlyMemory
            Assert.True(SequenceMarshal.TryGetReadOnlyMemory(buffer, out ReadOnlyMemory<byte> newMemory));
            Assert.Equal(new byte[] { 3, 4, 5 }, newMemory.ToArray());
        }

        [Fact]
        public void Ctor_IMemoryList_MultiBlock()
        {
            var memoryListSegment1 = new BufferSegment<byte>(new byte[] { 1, 2, 3 });
            var memoryListSegment2 = memoryListSegment1.Append(new byte[] { 4, 5 });

            var buffer = new ReadOnlySequence<byte>(memoryListSegment1, 2, memoryListSegment2, 1);

            Assert.True(SequenceMarshal.TryGetReadOnlySequenceSegment(buffer, out ReadOnlySequenceSegment<byte> startSegment, out int startIndex, out ReadOnlySequenceSegment<byte> endSegment, out int endIndex));
            Assert.Equal(startSegment, memoryListSegment1);
            Assert.Equal(endSegment, memoryListSegment2);

            Assert.Equal(2, startIndex);
            Assert.Equal(1, endIndex);

            Assert.False(SequenceMarshal.TryGetArray(buffer, out _));

            // Multi-block can't be retrieved with TryGetReadOnlyMemory
            Assert.False(SequenceMarshal.TryGetReadOnlyMemory(buffer, out _));
        }
    }
}
