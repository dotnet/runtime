// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace System.MemoryTests
{
    public static partial class MemoryTests
    {
        [Fact]
        public static void ToEnumerable()
        {
            int[] a = { 91, 92, 93 };
            var memory = new Memory<int>(a);
            IEnumerable<int> copy = MemoryMarshal.ToEnumerable<int>(memory);
            Assert.Equal<int>(a, copy);
        }

        [Fact]
        public static void ToEnumerableWithIndex()
        {
            int[] a = { 91, 92, 93, 94, 95 };
            var memory = new Memory<int>(a);
            IEnumerable<int> copy = MemoryMarshal.ToEnumerable<int>(memory.Slice(2));

            Assert.Equal<int>(new int[] { 93, 94, 95 }, copy);
        }

        [Fact]
        public static void ToEnumerableWithIndexAndLength()
        {
            int[] a = { 91, 92, 93 };
            var memory = new Memory<int>(a, 1, 1);
            IEnumerable<int> copy = MemoryMarshal.ToEnumerable<int>(memory);
            Assert.Equal<int>(new int[] { 92 }, copy);
        }

        [Fact]
        public static void ToEnumerableEmpty()
        {
            Memory<int> memory = Memory<int>.Empty;
            IEnumerable<int> copy = MemoryMarshal.ToEnumerable<int>(memory);
            Assert.Equal(0, copy.Count());
        }

        [Fact]
        public static void ToEnumerableDefault()
        {
            Memory<int> memory = default;
            IEnumerable<int> copy = MemoryMarshal.ToEnumerable<int>(memory);
            Assert.Equal(0, copy.Count());
        }

        [Fact]
        public static void ToEnumerableForEach()
        {
            int[] a = { 91, 92, 93 };
            var memory = new Memory<int>(a);
            int index = 0;
            foreach (int curr in MemoryMarshal.ToEnumerable<int>(memory))
            {
                Assert.Equal(a[index++], curr);
            }
        }

        [Fact]
        public static void ToEnumerableGivenToExistingConstructor()
        {
            int[] a = { 91, 92, 93 };
            var memory = new Memory<int>(a);
            IEnumerable<int> enumer = MemoryMarshal.ToEnumerable<int>(memory);
            var li = new List<int>(enumer);
            Assert.Equal(a, li);
        }

        [Fact]
        public static void ToEnumerableSameAsIEnumerator()
        {
            int[] a = { 91, 92, 93 };
            var memory = new Memory<int>(a);
            IEnumerable<int> enumer = MemoryMarshal.ToEnumerable<int>(memory.Slice(1));
            IEnumerator<int> enumerat = enumer.GetEnumerator();
            Assert.Same(enumer, enumerat);
        }

        [Fact]
        public static void ToEnumerableChars()
        {
            char[] charArray = ['a', 'b', 'c']; // array
            ReadOnlyMemory<char> memory = charArray.AsMemory();

            Assert.Equal(charArray, MemoryMarshal.ToEnumerable(memory));
            Assert.Equal(charArray[..2], MemoryMarshal.ToEnumerable(memory[..2]));
            Assert.Equal(charArray[1..], MemoryMarshal.ToEnumerable(memory[1..]));
            Assert.Same(Array.Empty<char>(), MemoryMarshal.ToEnumerable(memory[3..]));

            string str = "abc"; // string
            memory = str.AsMemory();

            Assert.Equal(str, MemoryMarshal.ToEnumerable(memory));
            Assert.Equal(charArray[..2], MemoryMarshal.ToEnumerable(memory[..2]));
            Assert.Equal(charArray[1..], MemoryMarshal.ToEnumerable(memory[1..]));
            Assert.Same(Array.Empty<char>(), MemoryMarshal.ToEnumerable(memory[3..]));

            memory = new WrapperMemoryManager<char>(new char[] { 'a', 'b', 'c' }.AsMemory()).Memory; // memory manager

            Assert.Equal(charArray, MemoryMarshal.ToEnumerable(memory));
            Assert.Equal(charArray[..2], MemoryMarshal.ToEnumerable(memory[..2]));
            Assert.Equal(charArray[1..], MemoryMarshal.ToEnumerable(memory[1..]));
            Assert.Same(Array.Empty<char>(), MemoryMarshal.ToEnumerable(memory[3..]));
        }

        private sealed class WrapperMemoryManager<T>(Memory<T> memory) : MemoryManager<T>
        {
            public override Span<T> GetSpan() => memory.Span;
            public override MemoryHandle Pin(int elementIndex = 0) => throw new NotSupportedException();
            public override void Unpin() => throw new NotSupportedException();
            protected override void Dispose(bool disposing) { }
        }
    }
}
