// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        public static void AsReadOnlyMemory_ToArray_Roundtrips(string input)
        {
            ReadOnlyMemory<char> m = input.AsReadOnlyMemory();
            Assert.Equal(input, new string(m.ToArray()));
        }

        [Theory]
        [MemberData(nameof(StringInputs))]
        public static void AsReadOnlyMemory_Span_Roundtrips(string input)
        {
            ReadOnlyMemory<char> m = input.AsReadOnlyMemory();
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
        public static void AsReadOnlyMemory_Slice_MatchesSubstring(string input, int offset, int count)
        {
            ReadOnlyMemory<char> m = input.AsReadOnlyMemory();
            Assert.Equal(input.Substring(offset, count), new string(m.Slice(offset, count).ToArray()));
            Assert.Equal(input.Substring(offset, count), new string(m.Slice(offset, count).Span.ToArray()));
            Assert.Equal(input.Substring(offset), new string(m.Slice(offset).ToArray()));
        }

        [Fact]
        public static void AsReadOnlyMemory_NullString_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("text", () => ((string)null).AsReadOnlyMemory());
            AssertExtensions.Throws<ArgumentNullException>("text", () => ((string)null).AsReadOnlyMemory(0));
            AssertExtensions.Throws<ArgumentNullException>("text", () => ((string)null).AsReadOnlyMemory(0, 0));
        }

        [Fact]
        public static void AsReadOnlyMemory_TryGetString_Roundtrips()
        {
            string input = "0123456789";
            ReadOnlyMemory<char> m = input.AsReadOnlyMemory();
            Assert.False(m.IsEmpty);

            Assert.True(m.TryGetString(out string text, out int start, out int length));
            Assert.Same(input, text);
            Assert.Equal(0, start);
            Assert.Equal(input.Length, length);

            m = m.Slice(1);
            Assert.True(m.TryGetString(out text, out start, out length));
            Assert.Same(input, text);
            Assert.Equal(1, start);
            Assert.Equal(input.Length - 1, length);

            m = m.Slice(1);
            Assert.True(m.TryGetString(out text, out start, out length));
            Assert.Same(input, text);
            Assert.Equal(2, start);
            Assert.Equal(input.Length - 2, length);

            m = m.Slice(3, 2);
            Assert.True(m.TryGetString(out text, out start, out length));
            Assert.Same(input, text);
            Assert.Equal(5, start);
            Assert.Equal(2, length);

            m = m.Slice(m.Length);
            Assert.True(m.TryGetString(out text, out start, out length));
            Assert.Same(input, text);
            Assert.Equal(7, start);
            Assert.Equal(0, length);

            m = m.Slice(0);
            Assert.True(m.TryGetString(out text, out start, out length));
            Assert.Same(input, text);
            Assert.Equal(7, start);
            Assert.Equal(0, length);

            m = m.Slice(0, 0);
            Assert.True(m.TryGetString(out text, out start, out length));
            Assert.Same(input, text);
            Assert.Equal(7, start);
            Assert.Equal(0, length);

            Assert.True(m.IsEmpty);
        }

        [Fact]
        public static void Array_TryGetString_ReturnsFalse()
        {
            ReadOnlyMemory<char> m = new char[10];
            Assert.False(m.TryGetString(out string text, out int start, out int length));
            Assert.Null(text);
            Assert.Equal(0, start);
            Assert.Equal(0, length);
        }

        [Fact]
        public static void AsReadOnlyMemory_TryGetArray_ReturnsFalse()
        {
            ReadOnlyMemory<char> m = "0123456789".AsReadOnlyMemory();
            Assert.False(MemoryMarshal.TryGetArray(m, out ArraySegment<char> array));
            Assert.Null(array.Array);
            Assert.Equal(0, array.Offset);
            Assert.Equal(0, array.Count);
        }

        [Fact]
        public static unsafe void AsReadOnlyMemory_Retain_ExpectedPointerValue()
        {
            string input = "0123456789";
            ReadOnlyMemory<char> m = input.AsReadOnlyMemory();

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

        [Theory]
        [MemberData(nameof(TestHelpers.StringSliceTestData), MemberType = typeof(TestHelpers))]
        public static unsafe void AsReadOnlyMemory_PointerAndLength(string text, int start, int length)
        {
            ReadOnlyMemory<char> m;
            if (start == -1)
            {
                start = 0;
                length = text.Length;
                m = text.AsReadOnlyMemory();
            }
            else if (length == -1)
            {
                length = text.Length - start;
                m = text.AsReadOnlyMemory(start);
            }
            else
            {
                m = text.AsReadOnlyMemory(start, length);
            }

            Assert.Equal(length, m.Length);

            using (MemoryHandle h = m.Retain(pin: true))
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
        public static unsafe void AsReadOnlyMemory_2Arg_OutOfRange(string text, int start)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("start", () => text.AsReadOnlyMemory(start));
        }

        [Theory]
        [MemberData(nameof(TestHelpers.StringSlice3ArgTestOutOfRangeData), MemberType = typeof(TestHelpers))]
        public static unsafe void AsReadOnlyMemory_3Arg_OutOfRange(string text, int start, int length)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("start", () => text.AsReadOnlyMemory(start, length));
        }

        [Fact]
        public static void AsReadOnlyMemory_EqualsAndGetHashCode_ExpectedResults()
        {
            ReadOnlyMemory<char> m1 = new string('a', 4).AsReadOnlyMemory();
            ReadOnlyMemory<char> m2 = new string('a', 4).AsReadOnlyMemory();

            Assert.True(m1.Span.SequenceEqual(m2.Span));
            Assert.True(m1.Equals(m1));
            Assert.True(m1.Equals((object)m1));
            Assert.False(m1.Equals(m2));
            Assert.False(m1.Equals((object)m2));

            Assert.Equal(m1.GetHashCode(), m1.GetHashCode());
        }
    }
}
