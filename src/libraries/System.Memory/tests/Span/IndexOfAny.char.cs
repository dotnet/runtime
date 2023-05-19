// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Linq;
using System.Numerics;
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

        private static int IndexOf(Span<char> span, char value)
        {
            int index = span.IndexOf(value);
            Assert.Equal(index, span.IndexOfAny(SearchValues.Create(stackalloc char[] { value })));
            return index;
        }

        private static int IndexOfAny(Span<char> span, char value0, char value1)
        {
            int index = span.IndexOfAny(value0, value1);
            Assert.Equal(index, span.IndexOfAny(SearchValues.Create(stackalloc char[] { value0, value1 })));
            return index;
        }

        private static int IndexOfAny(Span<char> span, char value0, char value1, char value2)
        {
            int index = span.IndexOfAny(value0, value1, value2);
            Assert.Equal(index, span.IndexOfAny(SearchValues.Create(stackalloc char[] { value0, value1, value2 })));
            return index;
        }

        private static int IndexOfAny(Span<char> span, ReadOnlySpan<char> values)
        {
            int index = span.IndexOfAny(values);
            Assert.Equal(index, span.IndexOfAny(SearchValues.Create(values)));
            return index;
        }
    }
}
