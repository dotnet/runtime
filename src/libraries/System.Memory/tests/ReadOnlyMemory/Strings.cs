// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace System.MemoryTests
{
    public static partial class ReadOnlyMemoryTests
    {
        public static IEnumerable<object[]> StringInputs()
        {
            yield return new object[] { "" };
            yield return new object[] { "a" };
            yield return new object[] { "a\0bcdefghijklmnopqrstuvwxyz" };
        }

        [Theory]
        [MemberData(nameof(StringInputs))]
        public static void AsMemory_ToArray_Roundtrips(string input)
        {
            ReadOnlyMemory<char> m = input.AsMemory();
            Assert.Equal(input, new string(m.ToArray()));
        }

        [Theory]
        [MemberData(nameof(StringInputs))]
        public static void AsMemory_Span_Roundtrips(string input)
        {
            ReadOnlyMemory<char> m = input.AsMemory();
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
        public static void AsMemory_Slice_MatchesSubstring(string input, int offset, int count)
        {
            ReadOnlyMemory<char> m = input.AsMemory();
            Assert.Equal(input.Substring(offset, count), new string(m.Slice(offset, count).ToArray()));
            Assert.Equal(input.Substring(offset, count), new string(m.Slice(offset, count).Span.ToArray()));
            Assert.Equal(input.Substring(offset), new string(m.Slice(offset).ToArray()));
        }

        [Fact]
        public static void AsMemory_NullString_Default()
        {
            ReadOnlyMemory<char> m = ((string)null).AsMemory();
            m.Validate();
            Assert.Equal(default, m);

            m = ((string)null).AsMemory(0);
            m.Validate();
            Assert.Equal(default, m);

            m = ((string)null).AsMemory(0, 0);
            m.Validate();
            Assert.Equal(default, m);
        }

        [Fact]
        public static void NullAsMemoryNonZeroStartAndLength()
        {
            string str = null;

            Assert.Throws<ArgumentOutOfRangeException>(() => str.AsMemory(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => str.AsMemory(-1));

            Assert.Throws<ArgumentOutOfRangeException>(() => str.AsMemory(0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => str.AsMemory(1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => str.AsMemory(1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => str.AsMemory(-1, -1));
        }

        [Fact]
        public static void AsMemory_TryGetArray_ReturnsFalse()
        {
            ReadOnlyMemory<char> m = "0123456789".AsMemory();
            Assert.False(MemoryMarshal.TryGetArray(m, out ArraySegment<char> array));
            Assert.Null(array.Array);
            Assert.Equal(0, array.Offset);
            Assert.Equal(0, array.Count);
        }

        [Fact]
        public static unsafe void AsMemory_Pin_ExpectedPointerValue()
        {
            string input = "0123456789";
            ReadOnlyMemory<char> m = input.AsMemory();

            using (MemoryHandle h = m.Pin())
            {
                GC.Collect();
                fixed (char* ptr = input)
                {
                    Assert.Equal((IntPtr)ptr, (IntPtr)h.Pointer);
                }
            }
        }

        [Theory]
        [MemberData(nameof(TestHelpers.StringSliceTestData), MemberType = typeof(TestHelpers))]
        public static unsafe void AsMemory_StartAndLength(string text, int start, int length)
        {
            ReadOnlyMemory<char> m;
            if (start == -1)
            {
                start = 0;
                length = text.Length;
                m = text.AsMemory();
            }
            else if (length == -1)
            {
                length = text.Length - start;
                m = text.AsMemory(start);
            }
            else
            {
                m = text.AsMemory(start, length);
            }

            Assert.Equal(length, m.Length);

            using (MemoryHandle h = m.Pin())
            {
                fixed (char* pText = text)
                {
                    char* expected = pText + start;
                    void* actual = h.Pointer;
                    Assert.Equal((IntPtr)expected, (IntPtr)actual);
                }
            }
        }

        [Theory]
        [MemberData(nameof(TestHelpers.StringSlice2ArgTestOutOfRangeData), MemberType = typeof(TestHelpers))]
        public static unsafe void AsMemory_2Arg_OutOfRange(string text, int start)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("start", () => text.AsMemory(start));
        }

        [Theory]
        [MemberData(nameof(TestHelpers.StringSlice3ArgTestOutOfRangeData), MemberType = typeof(TestHelpers))]
        public static unsafe void AsMemory_3Arg_OutOfRange(string text, int start, int length)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("start", () => text.AsMemory(start, length));
        }

        [Fact]
        public static void AsMemory_EqualsAndGetHashCode_ExpectedResults()
        {
            ReadOnlyMemory<char> m1 = new string('a', 4).AsMemory();
            ReadOnlyMemory<char> m2 = new string('a', 4).AsMemory();

            Assert.True(m1.Span.SequenceEqual(m2.Span));
            Assert.True(m1.Equals(m1));
            Assert.True(m1.Equals((object)m1));
            Assert.False(m1.Equals(m2));
            Assert.False(m1.Equals((object)m2));

            Assert.Equal(m1.GetHashCode(), m1.GetHashCode());
        }
    }
}
