// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.MemoryTests
{
    public static partial class MemoryTests
    {
        [Fact]
        public static void SlicingUsingIndexAndRangeTest()
        {
            Range range;
            int[] a = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            Memory<int> memory = a;
            ReadOnlyMemory<int> roMemory = a;

            for (int i = 0; i < a.Length; i++)
            {
                range = new Range(Index.FromStart(i), Index.FromEnd(0));
                Assert.Equal(memory.Slice(i, a.Length - i), memory[range]);
                Assert.Equal(roMemory.Slice(i, a.Length - i), roMemory[range]);

                Assert.Equal(memory.Slice(i), memory[i..]);
                Assert.Equal(roMemory.Slice(i), roMemory[i..]);

                Assert.Equal(memory.Slice(i, a.Length - i), memory[range]);
                Assert.Equal(roMemory.Slice(i, a.Length - i), roMemory[range]);
            }

            range = new Range(Index.FromStart(0), Index.FromStart(a.Length + 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => { Memory<int> m = memory[range]; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { ReadOnlyMemory<int> m = roMemory[range]; });
        }

        [Fact]
        public static void MemoryExtensionsTest()
        {
            Range range;
            int[] a = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            Memory<int> memory = a;
            ReadOnlyMemory<int> roMemory = a;

            for (int i = 0; i < a.Length; i++)
            {
                Assert.Equal(memory.Slice(i), a.AsMemory(Index.FromStart(i)));

                range = new Range(Index.FromStart(i), Index.FromEnd(0));
                Assert.Equal(memory.Slice(i, a.Length - i), a.AsMemory(range));
            }

            range = new Range(Index.FromStart(0), Index.FromStart(a.Length + 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => { Memory<int> m = a.AsMemory(Index.FromStart(a.Length + 1)); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { Memory<int> m = a.AsMemory(range); });

            string s = "0123456789ABCDEF";
            ReadOnlyMemory<char> roStringMemory = s.AsMemory();

            for (int i = 0; i < s.Length; i++)
            {
                Assert.Equal(roStringMemory.Slice(i), s.AsMemory(Index.FromStart(i)));

                range = new Range(Index.FromStart(i), Index.FromEnd(0));
                Assert.Equal(roStringMemory.Slice(i, s.Length - i), s.AsMemory(range));
            }

            range = new Range(Index.FromStart(0), Index.FromStart(s.Length + 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => { ReadOnlyMemory<char> m = s.AsMemory(Index.FromStart(s.Length + 1)); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { ReadOnlyMemory<char> m = s.AsMemory(range); });

            Span<int> span = a.AsSpan();

            for (int i = 0; i < a.Length; i++)
            {
                Assert.True(span.Slice(i) == a.AsSpan(Index.FromStart(i)));

                range = new Range(Index.FromStart(i), Index.FromEnd(0));
                Assert.True(span.Slice(i, span.Length - i) == a.AsSpan(range));
            }

            range = new Range(Index.FromStart(0), Index.FromStart(a.Length + 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => { Span<int> sp = a.AsSpan(Index.FromStart(a.Length + 1)); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { Span<int> sp = a.AsSpan(range); });

            ArraySegment<int> segment = new ArraySegment<int>(a);
            for (int i = 0; i < a.Length; i++)
            {
                Assert.True(span.Slice(i) == segment.AsSpan(Index.FromStart(i)));

                range = new Range(Index.FromStart(i), Index.FromEnd(0));
                Assert.True(span.Slice(i, span.Length - i) == segment.AsSpan(range));
            }

            range = new Range(Index.FromStart(0), Index.FromStart(a.Length + 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => { Span<int> sp = segment.AsSpan(Index.FromStart(a.Length + 1)); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { Span<int> sp = segment.AsSpan(range); });
        }
    }
}
