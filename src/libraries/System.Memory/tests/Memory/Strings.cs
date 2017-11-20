// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace System.MemoryTests
{
    public static partial class MemoryTests
    {
        public static IEnumerable<object[]> StringInputs()
        {
            yield return new object[] { "" };
            yield return new object[] { "a" };
            yield return new object[] { "a\0bcdefghijklmnopqrstuvwxyz" };
        }

        [Theory]
        [MemberData(nameof(StringInputs))]
        public static void Memory_ToArray_Roundtrips(string input)
        {
            ReadOnlyMemory<char> readonlyMemory = input.AsReadOnlyMemory();
            Memory<char> m = MemoryMarshal.AsMemory(readonlyMemory);
            Assert.Equal(input, new string(m.ToArray()));
        }

        [Theory]
        [MemberData(nameof(StringInputs))]
        public static void Memory_Span_Roundtrips(string input)
        {
            ReadOnlyMemory<char> readonlyMemory = input.AsReadOnlyMemory();
            Memory<char> m = MemoryMarshal.AsMemory(readonlyMemory);
            ReadOnlySpan<char> s = m.Span;
            Assert.Equal(input, new string(s.ToArray()));
        }

        [Theory]
        [InlineData("", 0, 0)]
        [InlineData("0123456789", 0, 0)]
        [InlineData("0123456789", 10, 0)]
        [InlineData("0123456789", 0, 10)]
        [InlineData("0123456789", 1, 9)]
        [InlineData("0123456789", 2, 8)]
        [InlineData("0123456789", 9, 1)]
        [InlineData("0123456789", 1, 8)]
        [InlineData("0123456789", 5, 3)]
        public static void Memory_Slice_MatchesSubstring(string input, int offset, int count)
        {
            ReadOnlyMemory<char> readonlyMemory = input.AsReadOnlyMemory();
            Memory<char> m = MemoryMarshal.AsMemory(readonlyMemory);
            Assert.Equal(input.Substring(offset, count), new string(m.Slice(offset, count).ToArray()));
            Assert.Equal(input.Substring(offset, count), new string(m.Slice(offset, count).Span.ToArray()));
            Assert.Equal(input.Substring(offset), new string(m.Slice(offset).ToArray()));
        }

        [Fact]
        public static unsafe void Memory_Retain_ExpectedPointerValue()
        {
            string input = "0123456789";
            ReadOnlyMemory<char> readonlyMemory = input.AsReadOnlyMemory();
            Memory<char> m = MemoryMarshal.AsMemory(readonlyMemory);

            using (MemoryHandle h = m.Retain(pin: false))
            {
                Assert.Equal(IntPtr.Zero, (IntPtr)h.Pointer);
            }

            using (MemoryHandle h = m.Retain(pin: true))
            {
                GC.Collect();
                fixed (char* ptr = input)
                {
                    Assert.Equal((IntPtr)ptr, (IntPtr)h.Pointer);
                }
            }
        }

        [Fact]
        public static void Memory_EqualsAndGetHashCode_ExpectedResults()
        {
            ReadOnlyMemory<char> readonlyMemory1 = new string('a', 4).AsReadOnlyMemory();
            ReadOnlyMemory<char> readonlyMemory2 = new string('a', 4).AsReadOnlyMemory();

            Memory<char> m1 = MemoryMarshal.AsMemory(readonlyMemory1);
            Memory<char> m2 = MemoryMarshal.AsMemory(readonlyMemory2);

            Assert.True(m1.Span.SequenceEqual(m2.Span));
            Assert.True(m1.Equals(m1));
            Assert.True(m1.Equals((object)m1));
            Assert.False(m1.Equals(m2));
            Assert.False(m1.Equals((object)m2));

            Assert.Equal(m1.GetHashCode(), m1.GetHashCode());
        }
    }
}
