// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace System.SpanTests
{
    public static partial class SpanTests
    {
        [Theory]
        [InlineData("a", "a", 'a', 0)]
        [InlineData("ab", "a", 'a', 0)]
        [InlineData("aab", "a", 'a', 0)]
        [InlineData("acab", "a", 'a', 0)]
        [InlineData("acab", "c", 'c', 1)]
        [InlineData("abcdefghijklmnopqrstuvwxyz", "lo", 'l', 11)]
        [InlineData("abcdefghijklmnopqrstuvwxyz", "ol", 'l', 11)]
        [InlineData("abcdefghijklmnopqrstuvwxyz", "ll", 'l', 11)]
        [InlineData("abcdefghijklmnopqrstuvwxyz", "lmr", 'l', 11)]
        [InlineData("abcdefghijklmnopqrstuvwxyz", "rml", 'l', 11)]
        [InlineData("abcdefghijklmnopqrstuvwxyz", "mlr", 'l', 11)]
        [InlineData("abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyz", "lmr", 'l', 11)]
        [InlineData("aaaaaaaaaaalmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyz", "lmr", 'l', 11)]
        [InlineData("aaaaaaaaaaacmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyz", "lmr", 'm', 12)]
        [InlineData("aaaaaaaaaaarmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyz", "lmr", 'r', 11)]
        [InlineData("/localhost:5000/PATH/%2FPATH2/ HTTP/1.1", " %?", '%', 21)]
        [InlineData("/localhost:5000/PATH/%2FPATH2/?key=value HTTP/1.1", " %?", '%', 21)]
        [InlineData("/localhost:5000/PATH/PATH2/?key=value HTTP/1.1", " %?", '?', 27)]
        [InlineData("/localhost:5000/PATH/PATH2/ HTTP/1.1", " %?", ' ', 27)]
        public static void IndexOfAnyStrings_Char(string raw, string search, char expectResult, int expectIndex)
        {
            char[] buffers = raw.ToCharArray();
            Span<char> span = new Span<char>(buffers);
            char[] searchFor = search.ToCharArray();

            int index = IndexOfAny(span, searchFor);
            if (searchFor.Length == 1)
            {
                Assert.Equal(index, IndexOf(span, searchFor[0]));
            }
            else if (searchFor.Length == 2)
            {
                Assert.Equal(index, IndexOfAny(span, searchFor[0], searchFor[1]));
            }
            else if (searchFor.Length == 3)
            {
                Assert.Equal(index, IndexOfAny(span, searchFor[0], searchFor[1], searchFor[2]));
            }

            char found = span[index];
            Assert.Equal(expectResult, found);
            Assert.Equal(expectIndex, index);
        }

        [Fact]
        public static void ZeroLengthIndexOfTwo_Char()
        {
            Span<char> span = new Span<char>(Array.Empty<char>());
            int idx = IndexOfAny(span, (char)0, (char)0);
            Assert.Equal(-1, idx);
        }

        [Fact]
        public static void DefaultFilledIndexOfTwo_Char()
        {
            Random rnd = new Random(42);

            for (int length = 1; length <= byte.MaxValue + 1; length++)
            {
                char[] a = new char[length];
                Span<char> span = new Span<char>(a);

                char[] targets = { default, (char)99 };

                for (int i = 0; i < length; i++)
                {
                    int index = rnd.Next(0, targets.Length) == 0 ? 0 : 1;
                    char target0 = targets[index];
                    char target1 = targets[(index + 1) % 2];
                    int idx = IndexOfAny(span, target0, target1);
                    Assert.Equal(0, idx);
                }
            }
        }

        [Fact]
        public static void TestMatchTwo_Char()
        {
            for (int length = Vector<short>.Count; length <= byte.MaxValue + 1; length++)
            {
                char[] a = Enumerable.Range(0, length).Select(i => (char)(i + 1)).ToArray();

                for (int i = 0; i < Vector<short>.Count; i++)
                {
                    Span<char> span = new Span<char>(a).Slice(i);

                    for (int targetIndex = 0; targetIndex < length - Vector<short>.Count; targetIndex++)
                    {
                        char target0 = a[targetIndex + i];
                        char target1 = (char)0;
                        int idx = IndexOfAny(span, target0, target1);
                        Assert.Equal(targetIndex, idx);
                    }

                    for (int targetIndex = 0; targetIndex < length - 1 - Vector<short>.Count; targetIndex++)
                    {
                        char target0 = a[targetIndex + i];
                        char target1 = a[targetIndex + i + 1];
                        int idx = IndexOfAny(span, target0, target1);
                        Assert.Equal(targetIndex, idx);
                    }

                    for (int targetIndex = 0; targetIndex < length - 1 - Vector<short>.Count; targetIndex++)
                    {
                        char target0 = (char)0;
                        char target1 = a[targetIndex + i + 1];
                        int idx = IndexOfAny(span, target0, target1);
                        Assert.Equal(targetIndex + 1, idx);
                    }
                }
            }
        }

        [Fact]
        public static void TestNoMatchTwo_Char()
        {
            Random rnd = new Random(42);
            for (int length = 0; length <= byte.MaxValue + 1; length++)
            {
                char[] a = new char[length];
                char target0 = (char)rnd.Next(1, 256);
                char target1 = (char)rnd.Next(1, 256);
                Span<char> span = new Span<char>(a);

                int idx = IndexOfAny(span, target0, target1);
                Assert.Equal(-1, idx);
            }
        }

        [Fact]
        public static void TestMultipleMatchTwo_Char()
        {
            for (int length = 3; length <= byte.MaxValue + 1; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    char val = (char)(i + 1);
                    a[i] = val == (char)200 ? (char)201 : val;
                }

                a[length - 1] = (char)200;
                a[length - 2] = (char)200;
                a[length - 3] = (char)200;

                Span<char> span = new Span<char>(a);
                int idx = IndexOfAny(span, (char)200, (char)200);
                Assert.Equal(length - 3, idx);
            }
        }

        [Fact]
        public static void MakeSureNoChecksGoOutOfRangeTwo_Char()
        {
            for (int length = 1; length <= byte.MaxValue + 1; length++)
            {
                char[] a = new char[length + 2];
                a[0] = (char)99;
                a[length + 1] = (char)98;
                Span<char> span = new Span<char>(a, 1, length - 1);
                int index = IndexOfAny(span, (char)99, (char)98);
                Assert.Equal(-1, index);
            }

            for (int length = 1; length <= byte.MaxValue + 1; length++)
            {
                char[] a = new char[length + 2];
                a[0] = (char)99;
                a[length + 1] = (char)99;
                Span<char> span = new Span<char>(a, 1, length - 1);
                int index = IndexOfAny(span, (char)99, (char)99);
                Assert.Equal(-1, index);
            }
        }

        [Fact]
        public static void ZeroLengthIndexOfThree_Char()
        {
            Span<char> span = new Span<char>(Array.Empty<char>());
            int idx = IndexOfAny(span, (char)0, (char)0, (char)0);
            Assert.Equal(-1, idx);
        }

        [Fact]
        public static void DefaultFilledIndexOfThree_Char()
        {
            Random rnd = new Random(42);

            for (int length = 1; length <= byte.MaxValue + 1; length++)
            {
                char[] a = new char[length];
                Span<char> span = new Span<char>(a);

                char[] targets = { default, (char)99, (char)98 };

                for (int i = 0; i < length; i++)
                {
                    int index = rnd.Next(0, targets.Length);
                    char target0 = targets[index];
                    char target1 = targets[(index + 1) % 2];
                    char target2 = targets[(index + 1) % 3];
                    int idx = IndexOfAny(span, target0, target1, target2);
                    Assert.Equal(0, idx);
                }
            }
        }

        [Fact]
        public static void TestMatchThree_Char()
        {
            for (int length = Vector<short>.Count; length <= byte.MaxValue + 1; length++)
            {
                char[] a = Enumerable.Range(0, length).Select(i => (char)(i + 1)).ToArray();
                for (int i = 0; i < Vector<short>.Count; i++)
                {
                    Span<char> span = new Span<char>(a).Slice(i);

                    for (int targetIndex = 0; targetIndex < length - Vector<short>.Count; targetIndex++)
                    {
                        char target0 = a[targetIndex + i];
                        char target1 = (char)0;
                        char target2 = (char)0;
                        int idx = IndexOfAny(span, target0, target1, target2);
                        Assert.Equal(targetIndex, idx);
                    }

                    for (int targetIndex = 0; targetIndex < length - 2 - Vector<short>.Count; targetIndex++)
                    {
                        char target0 = a[targetIndex + i];
                        char target1 = a[targetIndex + i + 1];
                        char target2 = a[targetIndex + i + 2];
                        int idx = IndexOfAny(span, target0, target1, target2);
                        Assert.Equal(targetIndex, idx);
                    }

                    for (int targetIndex = 0; targetIndex < length - 2 - Vector<short>.Count; targetIndex++)
                    {
                        char target0 = (char)0;
                        char target1 = (char)0;
                        char target2 = a[targetIndex + i + 2];
                        int idx = IndexOfAny(span, target0, target1, target2);
                        Assert.Equal(targetIndex + 2, idx);
                    }
                }
            }
        }

        [Fact]
        public static void TestNoMatchThree_Char()
        {
            Random rnd = new Random(42);
            for (int length = 0; length <= byte.MaxValue + 1; length++)
            {
                char[] a = new char[length];
                char target0 = (char)rnd.Next(1, 256);
                char target1 = (char)rnd.Next(1, 256);
                char target2 = (char)rnd.Next(1, 256);
                Span<char> span = new Span<char>(a);

                int idx = IndexOfAny(span, target0, target1, target2);
                Assert.Equal(-1, idx);
            }
        }

        [Fact]
        public static void TestMultipleMatchThree_Char()
        {
            for (int length = 4; length <= byte.MaxValue + 1; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    char val = (char)(i + 1);
                    a[i] = val == (char)200 ? (char)201 : val;
                }

                a[length - 1] = (char)200;
                a[length - 2] = (char)200;
                a[length - 3] = (char)200;
                a[length - 4] = (char)200;

                Span<char> span = new Span<char>(a);
                int idx = IndexOfAny(span, (char)200, (char)200, (char)200);
                Assert.Equal(length - 4, idx);
            }
        }

        [Fact]
        public static void MakeSureNoChecksGoOutOfRangeThree_Char()
        {
            for (int length = 1; length <= byte.MaxValue + 1; length++)
            {
                char[] a = new char[length + 2];
                a[0] = (char)99;
                a[length + 1] = (char)98;
                Span<char> span = new Span<char>(a, 1, length - 1);
                int index = IndexOfAny(span, (char)99, (char)98, (char)99);
                Assert.Equal(-1, index);
            }

            for (int length = 1; length <= byte.MaxValue + 1; length++)
            {
                char[] a = new char[length + 2];
                a[0] = (char)99;
                a[length + 1] = (char)99;
                Span<char> span = new Span<char>(a, 1, length - 1);
                int index = IndexOfAny(span, (char)99, (char)99, (char)99);
                Assert.Equal(-1, index);
            }
        }

        [Fact]
        public static void ZeroLengthIndexOfFour_Char()
        {
            Span<char> span = new Span<char>(Array.Empty<char>());
            ReadOnlySpan<char> values = new char[] { (char)0, (char)0, (char)0, (char)0 };
            int idx = IndexOfAny(span, values);
            Assert.Equal(-1, idx);
        }

        [Fact]
        public static void DefaultFilledIndexOfFour_Char()
        {
            Random rnd = new Random(42);

            for (int length = 1; length <= byte.MaxValue + 1; length++)
            {
                char[] a = new char[length];
                Span<char> span = new Span<char>(a);

                char[] targets = { default, (char)99, (char)98, (char)97 };

                for (int i = 0; i < length; i++)
                {
                    int index = rnd.Next(0, targets.Length);
                    ReadOnlySpan<char> values = new char[] { targets[index], targets[(index + 1) % 2], targets[(index + 1) % 3], targets[(index + 1) % 4] };
                    int idx = IndexOfAny(span, values);
                    Assert.Equal(0, idx);
                }
            }
        }

        [Fact]
        public static void TestMatchFour_Char()
        {
            for (int length = Vector<short>.Count; length <= byte.MaxValue + 1; length++)
            {
                char[] a = Enumerable.Range(0, length).Select(i => (char)(i + 1)).ToArray();

                for (int i = 0; i < Vector<short>.Count; i++)
                {
                    Span<char> span = new Span<char>(a).Slice(i);

                    for (int targetIndex = 0; targetIndex < length - Vector<short>.Count; targetIndex++)
                    {
                        ReadOnlySpan<char> values = new char[] { a[targetIndex + i], (char)0, (char)0, (char)0 };
                        int idx = IndexOfAny(span, values);
                        Assert.Equal(targetIndex, idx);
                    }

                    for (int targetIndex = 0; targetIndex < length - 3 - Vector<short>.Count; targetIndex++)
                    {
                        ReadOnlySpan<char> values = new char[] { a[targetIndex + i], a[targetIndex + i + 1], a[targetIndex + i + 2], a[targetIndex + i + 3] };
                        int idx = IndexOfAny(span, values);
                        Assert.Equal(targetIndex, idx);
                    }

                    for (int targetIndex = 0; targetIndex < length - 3 - Vector<short>.Count; targetIndex++)
                    {
                        ReadOnlySpan<char> values = new char[] { (char)0, (char)0, (char)0, a[targetIndex + i + 3] };
                        int idx = IndexOfAny(span, values);
                        Assert.Equal(targetIndex + 3, idx);
                    }
                }
            }
        }

        [Fact]
        public static void TestNoMatchFour_Char()
        {
            Random rnd = new Random(42);
            for (int length = 0; length <= byte.MaxValue + 1; length++)
            {
                char[] a = new char[length];
                ReadOnlySpan<char> values = new char[] { (char)rnd.Next(1, 256), (char)rnd.Next(1, 256), (char)rnd.Next(1, 256), (char)rnd.Next(1, 256) };
                Span<char> span = new Span<char>(a);

                int idx = IndexOfAny(span, values);
                Assert.Equal(-1, idx);
            }
        }

        [Fact]
        public static void TestMultipleMatchFour_Char()
        {
            for (int length = 5; length <= byte.MaxValue + 1; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    char val = (char)(i + 1);
                    a[i] = val == (char)200 ? (char)201 : val;
                }

                a[length - 1] = (char)200;
                a[length - 2] = (char)200;
                a[length - 3] = (char)200;
                a[length - 4] = (char)200;
                a[length - 5] = (char)200;

                Span<char> span = new Span<char>(a);
                ReadOnlySpan<char> values = new char[] { (char)200, (char)200, (char)200, (char)200 };
                int idx = IndexOfAny(span, values);
                Assert.Equal(length - 5, idx);
            }
        }

        [Fact]
        public static void MakeSureNoChecksGoOutOfRangeFour_Char()
        {
            for (int length = 1; length <= byte.MaxValue + 1; length++)
            {
                char[] a = new char[length + 2];
                a[0] = (char)99;
                a[length + 1] = (char)98;
                Span<char> span = new Span<char>(a, 1, length - 1);
                ReadOnlySpan<char> values = new char[] { (char)99, (char)98, (char)99, (char)99 };
                int index = IndexOfAny(span, values);
                Assert.Equal(-1, index);
            }

            for (int length = 1; length <= byte.MaxValue + 1; length++)
            {
                char[] a = new char[length + 2];
                a[0] = (char)99;
                a[length + 1] = (char)99;
                Span<char> span = new Span<char>(a, 1, length - 1);
                ReadOnlySpan<char> values = new char[] { (char)99, (char)99, (char)99, (char)99 };
                int index = IndexOfAny(span, values);
                Assert.Equal(-1, index);
            }
        }

        [Fact]
        public static void ZeroLengthIndexOfFive_Char()
        {
            Span<char> span = new Span<char>(Array.Empty<char>());
            ReadOnlySpan<char> values = new char[] { (char)0, (char)0, (char)0, (char)0, (char)0 };
            int idx = IndexOfAny(span, values);
            Assert.Equal(-1, idx);
        }

        [Fact]
        public static void DefaultFilledIndexOfFive_Char()
        {
            Random rnd = new Random(42);

            for (int length = 1; length <= byte.MaxValue + 1; length++)
            {
                char[] a = new char[length];
                Span<char> span = new Span<char>(a);

                char[] targets = { default, (char)99, (char)98, (char)97, (char)96 };

                for (int i = 0; i < length; i++)
                {
                    int index = rnd.Next(0, targets.Length);
                    ReadOnlySpan<char> values = new char[] { targets[index], targets[(index + 1) % 2], targets[(index + 1) % 3], targets[(index + 1) % 4], targets[(index + 1) % 5] };
                    int idx = IndexOfAny(span, values);
                    Assert.Equal(0, idx);
                }
            }
        }

        [Fact]
        public static void TestMatchFive_Char()
        {
            for (int length = Vector<short>.Count; length <= byte.MaxValue + 1; length++)
            {
                char[] a = Enumerable.Range(0, length).Select(i => (char)(i + 1)).ToArray();
                for (int i = 0; i < Vector<short>.Count; i++)
                {
                    Span<char> span = new Span<char>(a).Slice(i);

                    for (int targetIndex = 0; targetIndex < length - Vector<short>.Count; targetIndex++)
                    {
                        ReadOnlySpan<char> values = new char[] { a[targetIndex + i], (char)0, (char)0, (char)0, (char)0 };
                        int idx = IndexOfAny(span, values);
                        Assert.Equal(targetIndex, idx);
                    }

                    for (int targetIndex = 0; targetIndex < length - 4 - Vector<short>.Count; targetIndex++)
                    {
                        ReadOnlySpan<char> values = new char[] { a[targetIndex + i], a[targetIndex + i + 1], a[targetIndex + i + 2], a[targetIndex + i + 3], a[targetIndex + i + 4] };
                        int idx = IndexOfAny(span, values);
                        Assert.Equal(targetIndex, idx);
                    }

                    for (int targetIndex = 0; targetIndex < length - 4 - Vector<short>.Count; targetIndex++)
                    {
                        ReadOnlySpan<char> values = new char[] { (char)0, (char)0, (char)0, (char)0, a[targetIndex + i + 4] };
                        int idx = IndexOfAny(span, values);
                        Assert.Equal(targetIndex + 4, idx);
                    }
                }
            }
        }

        [Fact]
        public static void TestNoMatchFive_Char()
        {
            Random rnd = new Random(42);
            for (int length = 0; length <= byte.MaxValue + 1; length++)
            {
                char[] a = new char[length];
                ReadOnlySpan<char> values = new char[] { (char)rnd.Next(1, 256), (char)rnd.Next(1, 256), (char)rnd.Next(1, 256), (char)rnd.Next(1, 256), (char)rnd.Next(1, 256) };
                Span<char> span = new Span<char>(a);

                int idx = IndexOfAny(span, values);
                Assert.Equal(-1, idx);
            }
        }

        [Fact]
        public static void TestMultipleMatchFive_Char()
        {
            for (int length = 6; length <= byte.MaxValue + 1; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    char val = (char)(i + 1);
                    a[i] = val == (char)200 ? (char)201 : val;
                }

                a[length - 1] = (char)200;
                a[length - 2] = (char)200;
                a[length - 3] = (char)200;
                a[length - 4] = (char)200;
                a[length - 5] = (char)200;
                a[length - 6] = (char)200;

                Span<char> span = new Span<char>(a);
                ReadOnlySpan<char> values = new char[] { (char)200, (char)200, (char)200, (char)200, (char)200 };
                int idx = IndexOfAny(span, values);
                Assert.Equal(length - 6, idx);
            }
        }

        [Fact]
        public static void MakeSureNoChecksGoOutOfRangeFive_Char()
        {
            for (int length = 1; length <= byte.MaxValue + 1; length++)
            {
                char[] a = new char[length + 2];
                a[0] = (char)99;
                a[length + 1] = (char)98;
                Span<char> span = new Span<char>(a, 1, length - 1);
                ReadOnlySpan<char> values = new char[] { (char)99, (char)98, (char)99, (char)99, (char)99 };
                int index = IndexOfAny(span, values);
                Assert.Equal(-1, index);
            }

            for (int length = 1; length <= byte.MaxValue + 1; length++)
            {
                char[] a = new char[length + 2];
                a[0] = (char)99;
                a[length + 1] = (char)99;
                Span<char> span = new Span<char>(a, 1, length - 1);
                ReadOnlySpan<char> values = new char[] { (char)99, (char)99, (char)99, (char)99, (char)99 };
                int index = IndexOfAny(span, values);
                Assert.Equal(-1, index);
            }
        }

        [Fact]
        public static void ZeroLengthIndexOfMany_Char()
        {
            Span<char> span = new Span<char>(Array.Empty<char>());
            ReadOnlySpan<char> values = new ReadOnlySpan<char>(new char[] { (char)0, (char)0, (char)0, (char)0, (char)0, (char)0 });
            int idx = IndexOfAny(span, values);
            Assert.Equal(-1, idx);

            values = new ReadOnlySpan<char>(new char[] { });
            idx = IndexOfAny(span, values);
            Assert.Equal(-1, idx);
        }

        [Fact]
        public static void DefaultFilledIndexOfMany_Char()
        {
            for (int length = 1; length <= byte.MaxValue + 1; length++)
            {
                char[] a = new char[length];
                Span<char> span = new Span<char>(a);

                ReadOnlySpan<char> values = new ReadOnlySpan<char>(new char[] { default, (char)99, (char)98, (char)97, (char)96, (char)0 });

                for (int i = 0; i < length; i++)
                {
                    int idx = IndexOfAny(span, values);
                    Assert.Equal(0, idx);
                }
            }
        }

        [Fact]
        public static void TestMatchMany_Char()
        {
            for (int length = 1; length <= byte.MaxValue + 1; length++)
            {
                char[] a = Enumerable.Range(0, length).Select(i => (char)(i + 1)).ToArray();
                Span<char> span = new Span<char>(a);

                for (int targetIndex = 0; targetIndex < length; targetIndex++)
                {
                    ReadOnlySpan<char> values = new ReadOnlySpan<char>(new char[] { a[targetIndex], (char)0, (char)0, (char)0, (char)0, (char)0 });
                    int idx = IndexOfAny(span, values);
                    Assert.Equal(targetIndex, idx);
                }

                for (int targetIndex = 0; targetIndex < length - 5; targetIndex++)
                {
                    ReadOnlySpan<char> values = new ReadOnlySpan<char>(new char[] { a[targetIndex], a[targetIndex + 1], a[targetIndex + 2], a[targetIndex + 3], a[targetIndex + 4], a[targetIndex + 5] });
                    int idx = IndexOfAny(span, values);
                    Assert.Equal(targetIndex, idx);
                }

                for (int targetIndex = 0; targetIndex < length - 5; targetIndex++)
                {
                    ReadOnlySpan<char> values = new ReadOnlySpan<char>(new char[] { (char)0, (char)0, (char)0, (char)0, (char)0, a[targetIndex + 5] });
                    int idx = IndexOfAny(span, values);
                    Assert.Equal(targetIndex + 5, idx);
                }
            }
        }

        [Fact]
        public static void TestMatchValuesLargerMany_Char()
        {
            Random rnd = new Random(42);
            for (int length = 2; length <= byte.MaxValue + 1; length++)
            {
                char[] a = new char[length];
                int expectedIndex = length / 2;
                for (int i = 0; i < length; i++)
                {
                    if (i == expectedIndex)
                    {
                        continue;
                    }
                    a[i] = (char)255;
                }
                Span<char> span = new Span<char>(a);

                char[] targets = new char[length * 2];
                for (int i = 0; i < targets.Length; i++)
                {
                    if (i == length + 1)
                    {
                        continue;
                    }
                    targets[i] = (char)rnd.Next(1, 255);
                }

                ReadOnlySpan<char> values = new ReadOnlySpan<char>(targets);
                int idx = IndexOfAny(span, values);
                Assert.Equal(expectedIndex, idx);
            }
        }

        [Fact]
        public static void TestNoMatchMany_Char()
        {
            Random rnd = new Random(42);
            for (int length = 1; length <= byte.MaxValue + 1; length++)
            {
                char[] a = new char[length];
                char[] targets = new char[length];
                for (int i = 0; i < targets.Length; i++)
                {
                    targets[i] = (char)rnd.Next(1, 256);
                }
                Span<char> span = new Span<char>(a);
                ReadOnlySpan<char> values = new ReadOnlySpan<char>(targets);

                int idx = IndexOfAny(span, values);
                Assert.Equal(-1, idx);
            }
        }

        [Fact]
        public static void TestNoMatchValuesLargerMany_Char()
        {
            Random rnd = new Random(42);
            for (int length = 1; length <= byte.MaxValue + 1; length++)
            {
                char[] a = new char[length];
                char[] targets = new char[length * 2];
                for (int i = 0; i < targets.Length; i++)
                {
                    targets[i] = (char)rnd.Next(1, 256);
                }
                Span<char> span = new Span<char>(a);
                ReadOnlySpan<char> values = new ReadOnlySpan<char>(targets);

                int idx = IndexOfAny(span, values);
                Assert.Equal(-1, idx);
            }
        }

        [Fact]
        public static void TestMultipleMatchMany_Char()
        {
            for (int length = 5; length <= byte.MaxValue + 1; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    char val = (char)(i + 1);
                    a[i] = val == 200 ? (char)201 : val;
                }

                a[length - 1] = (char)200;
                a[length - 2] = (char)200;
                a[length - 3] = (char)200;
                a[length - 4] = (char)200;
                a[length - 5] = (char)200;

                Span<char> span = new Span<char>(a);
                ReadOnlySpan<char> values = new ReadOnlySpan<char>(new char[] { (char)200, (char)200, (char)200, (char)200, (char)200, (char)200, (char)200, (char)200, (char)200 });
                int idx = IndexOfAny(span, values);
                Assert.Equal(length - 5, idx);
            }
        }

        [Fact]
        public static void MakeSureNoChecksGoOutOfRangeMany_Char()
        {
            for (int length = 1; length <= byte.MaxValue + 1; length++)
            {
                char[] a = new char[length + 2];
                a[0] = (char)99;
                a[length + 1] = (char)98;
                Span<char> span = new Span<char>(a, 1, length - 1);
                ReadOnlySpan<char> values = new ReadOnlySpan<char>(new char[] { (char)99, (char)98, (char)99, (char)98, (char)99, (char)98 });
                int index = IndexOfAny(span, values);
                Assert.Equal(-1, index);
            }

            for (int length = 1; length <= byte.MaxValue + 1; length++)
            {
                char[] a = new char[length + 2];
                a[0] = (char)99;
                a[length + 1] = (char)99;
                Span<char> span = new Span<char>(a, 1, length - 1);
                ReadOnlySpan<char> values = new ReadOnlySpan<char>(new char[] { (char)99, (char)99, (char)99, (char)99, (char)99, (char)99 });
                int index = IndexOfAny(span, values);
                Assert.Equal(-1, index);
            }
        }

        [Fact]
        [OuterLoop("Takes about a second to execute")]
        public static void TestIndexOfAny_RandomInputs_Char()
        {
            IndexOfAnyCharTestHelper.TestRandomInputs(
                expected: IndexOfAnyReferenceImpl,
                indexOfAny: (searchSpace, values) => searchSpace.IndexOfAny(values),
                indexOfAnyValues: (searchSpace, values) => searchSpace.IndexOfAny(values));

            static int IndexOfAnyReferenceImpl(ReadOnlySpan<char> searchSpace, ReadOnlySpan<char> values)
            {
                for (int i = 0; i < searchSpace.Length; i++)
                {
                    if (values.Contains(searchSpace[i]))
                    {
                        return i;
                    }
                }

                return -1;
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void AsciiNeedle_ProperlyHandlesEdgeCases_Char(bool needleContainsZero)
        {
            // There is some special handling we have to do for ASCII needles to properly filter out non-ASCII results
            ReadOnlySpan<char> needleValues = needleContainsZero ? "AEIOU\0" : "AEIOU!";
            IndexOfAnyValues<char> needle = IndexOfAnyValues.Create(needleValues);
            Assert.Contains("Ascii", needle.GetType().Name);

            ReadOnlySpan<char> repeatingHaystack = "AaAaAaAaAaAa";
            Assert.Equal(0, repeatingHaystack.IndexOfAny(needle));
            Assert.Equal(1, repeatingHaystack.IndexOfAnyExcept(needle));
            Assert.Equal(10, repeatingHaystack.LastIndexOfAny(needle));
            Assert.Equal(11, repeatingHaystack.LastIndexOfAnyExcept(needle));

            ReadOnlySpan<char> haystackWithZeroes = "Aa\0Aa\0Aa\0";
            Assert.Equal(0, haystackWithZeroes.IndexOfAny(needle));
            Assert.Equal(1, haystackWithZeroes.IndexOfAnyExcept(needle));
            Assert.Equal(needleContainsZero ? 8 : 6, haystackWithZeroes.LastIndexOfAny(needle));
            Assert.Equal(needleContainsZero ? 7 : 8, haystackWithZeroes.LastIndexOfAnyExcept(needle));

            Span<char> haystackWithOffsetNeedle = new char[100];
            for (int i = 0; i < haystackWithOffsetNeedle.Length; i++)
            {
                haystackWithOffsetNeedle[i] = (char)(128 + needleValues[i % needleValues.Length]);
            }

            Assert.Equal(-1, haystackWithOffsetNeedle.IndexOfAny(needle));
            Assert.Equal(0, haystackWithOffsetNeedle.IndexOfAnyExcept(needle));
            Assert.Equal(-1, haystackWithOffsetNeedle.LastIndexOfAny(needle));
            Assert.Equal(haystackWithOffsetNeedle.Length - 1, haystackWithOffsetNeedle.LastIndexOfAnyExcept(needle));

            // Mix matching characters back in
            for (int i = 0; i < haystackWithOffsetNeedle.Length; i += 3)
            {
                haystackWithOffsetNeedle[i] = needleValues[i % needleValues.Length];
            }

            Assert.Equal(0, haystackWithOffsetNeedle.IndexOfAny(needle));
            Assert.Equal(1, haystackWithOffsetNeedle.IndexOfAnyExcept(needle));
            Assert.Equal(haystackWithOffsetNeedle.Length - 1, haystackWithOffsetNeedle.LastIndexOfAny(needle));
            Assert.Equal(haystackWithOffsetNeedle.Length - 2, haystackWithOffsetNeedle.LastIndexOfAnyExcept(needle));

            // With chars, the lower byte could be matching, but we have to check that the higher byte is also 0
            for (int i = 0; i < haystackWithOffsetNeedle.Length; i++)
            {
                haystackWithOffsetNeedle[i] = (char)(((i + 1) * 256) + needleValues[i % needleValues.Length]);
            }

            Assert.Equal(-1, haystackWithOffsetNeedle.IndexOfAny(needle));
            Assert.Equal(0, haystackWithOffsetNeedle.IndexOfAnyExcept(needle));
            Assert.Equal(-1, haystackWithOffsetNeedle.LastIndexOfAny(needle));
            Assert.Equal(haystackWithOffsetNeedle.Length - 1, haystackWithOffsetNeedle.LastIndexOfAnyExcept(needle));

            // Mix matching characters back in
            for (int i = 0; i < haystackWithOffsetNeedle.Length; i += 3)
            {
                haystackWithOffsetNeedle[i] = needleValues[i % needleValues.Length];
            }

            Assert.Equal(0, haystackWithOffsetNeedle.IndexOfAny(needle));
            Assert.Equal(1, haystackWithOffsetNeedle.IndexOfAnyExcept(needle));
            Assert.Equal(haystackWithOffsetNeedle.Length - 1, haystackWithOffsetNeedle.LastIndexOfAny(needle));
            Assert.Equal(haystackWithOffsetNeedle.Length - 2, haystackWithOffsetNeedle.LastIndexOfAnyExcept(needle));
        }

        private static int IndexOf(Span<char> span, char value)
        {
            int index = span.IndexOf(value);
            Assert.Equal(index, span.IndexOfAny(IndexOfAnyValues.Create(stackalloc char[] { value })));
            return index;
        }

        private static int IndexOfAny(Span<char> span, char value0, char value1)
        {
            int index = span.IndexOfAny(value0, value1);
            Assert.Equal(index, span.IndexOfAny(IndexOfAnyValues.Create(stackalloc char[] { value0, value1 })));
            return index;
        }

        private static int IndexOfAny(Span<char> span, char value0, char value1, char value2)
        {
            int index = span.IndexOfAny(value0, value1, value2);
            Assert.Equal(index, span.IndexOfAny(IndexOfAnyValues.Create(stackalloc char[] { value0, value1, value2 })));
            return index;
        }

        private static int IndexOfAny(Span<char> span, ReadOnlySpan<char> values)
        {
            int index = span.IndexOfAny(values);
            Assert.Equal(index, span.IndexOfAny(IndexOfAnyValues.Create(values)));
            return index;
        }
    }

    public static class IndexOfAnyCharTestHelper
    {
        private const int MaxNeedleLength = 10;
        private const int MaxHaystackLength = 40;

        private static readonly char[] s_randomAsciiChars;
        private static readonly char[] s_randomLatin1Chars;
        private static readonly char[] s_randomChars;
        private static readonly byte[] s_randomAsciiBytes;
        private static readonly byte[] s_randomBytes;

        static IndexOfAnyCharTestHelper()
        {
            s_randomAsciiChars = new char[100 * 1024];
            s_randomLatin1Chars = new char[100 * 1024];
            s_randomChars = new char[1024 * 1024];
            s_randomBytes = new byte[100 * 1024];

            var rng = new Random(42);

            for (int i = 0; i < s_randomAsciiChars.Length; i++)
            {
                s_randomAsciiChars[i] = (char)rng.Next(0, 128);
            }

            for (int i = 0; i < s_randomLatin1Chars.Length; i++)
            {
                s_randomLatin1Chars[i] = (char)rng.Next(0, 256);
            }

            rng.NextBytes(MemoryMarshal.Cast<char, byte>(s_randomChars));

            s_randomAsciiBytes = Encoding.ASCII.GetBytes(s_randomAsciiChars);

            rng.NextBytes(s_randomBytes);
        }

        public delegate int IndexOfAnySearchDelegate<T>(ReadOnlySpan<T> searchSpace, ReadOnlySpan<T> values) where T : IEquatable<T>?;

        public delegate int IndexOfAnyValuesSearchDelegate<T>(ReadOnlySpan<T> searchSpace, IndexOfAnyValues<T> values) where T : IEquatable<T>?;

        public static void TestRandomInputs(IndexOfAnySearchDelegate<byte> expected, IndexOfAnySearchDelegate<byte> indexOfAny, IndexOfAnyValuesSearchDelegate<byte> indexOfAnyValues)
        {
            var rng = new Random(42);

            for (int iterations = 0; iterations < 1_000_000; iterations++)
            {
                // There are more interesting corner cases with ASCII needles, test those more.
                Test(rng, s_randomBytes, s_randomAsciiBytes, expected, indexOfAny, indexOfAnyValues);

                Test(rng, s_randomBytes, s_randomBytes, expected, indexOfAny, indexOfAnyValues);
            }
        }

        public static void TestRandomInputs(IndexOfAnySearchDelegate<char> expected, IndexOfAnySearchDelegate<char> indexOfAny, IndexOfAnyValuesSearchDelegate<char> indexOfAnyValues)
        {
            var rng = new Random(42);

            for (int iterations = 0; iterations < 1_000_000; iterations++)
            {
                // There are more interesting corner cases with ASCII needles, test those more.
                Test(rng, s_randomChars, s_randomAsciiChars, expected, indexOfAny, indexOfAnyValues);

                Test(rng, s_randomChars, s_randomLatin1Chars, expected, indexOfAny, indexOfAnyValues);

                Test(rng, s_randomChars, s_randomChars, expected, indexOfAny, indexOfAnyValues);
            }
        }

        private static void Test<T>(Random rng, ReadOnlySpan<T> haystackRandom, ReadOnlySpan<T> needleRandom,
            IndexOfAnySearchDelegate<T> expected, IndexOfAnySearchDelegate<T> indexOfAny, IndexOfAnyValuesSearchDelegate<T> indexOfAnyValues)
            where T : INumber<T>
        {
            ReadOnlySpan<T> haystack = GetRandomSlice(rng, haystackRandom, MaxHaystackLength);
            ReadOnlySpan<T> needle = GetRandomSlice(rng, needleRandom, MaxNeedleLength);

            IndexOfAnyValues<T> indexOfAnyValuesInstance = (IndexOfAnyValues<T>)(object)(typeof(T) == typeof(byte)
                ? IndexOfAnyValues.Create(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(needle)), needle.Length))
                : IndexOfAnyValues.Create(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(needle)), needle.Length)));

            int expectedIndex = expected(haystack, needle);
            int indexOfAnyIndex = indexOfAny(haystack, needle);
            int indexOfAnyValuesIndex = indexOfAnyValues(haystack, indexOfAnyValuesInstance);

            if (expectedIndex != indexOfAnyIndex)
            {
                AssertionFailed(haystack, needle, expectedIndex, indexOfAnyIndex, nameof(indexOfAny));
            }

            if (expectedIndex != indexOfAnyValuesIndex)
            {
                AssertionFailed(haystack, needle, expectedIndex, indexOfAnyValuesIndex, nameof(indexOfAnyValues));
            }
        }

        private static ReadOnlySpan<T> GetRandomSlice<T>(Random rng, ReadOnlySpan<T> span, int maxLength)
        {
            ReadOnlySpan<T> slice = span.Slice(rng.Next(span.Length + 1));
            return slice.Slice(0, Math.Min(slice.Length, rng.Next(maxLength + 1)));
        }

        private static void AssertionFailed<T>(ReadOnlySpan<T> haystack, ReadOnlySpan<T> needle, int expected, int actual, string approach)
            where T : INumber<T>
        {
            string readableHaystack = string.Join(", ", haystack.ToString().Select(c => int.CreateChecked(c)));
            string readableNeedle = string.Join(", ", needle.ToString().Select(c => int.CreateChecked(c)));

            Assert.True(false, $"Expected {expected}, got {approach}={actual} for needle='{readableNeedle}', haystack='{readableHaystack}'");
        }
    }
}
