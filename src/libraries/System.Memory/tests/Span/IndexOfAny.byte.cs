// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
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
        public static void IndexOfAnyStrings_Byte(string raw, string search, char expectResult, int expectIndex)
        {
            byte[] buffers = Encoding.UTF8.GetBytes(raw);
            var span = new Span<byte>(buffers);
            char[] searchFor = search.ToCharArray();
            byte[] searchForBytes = Encoding.UTF8.GetBytes(searchFor);

            var index = IndexOfAny(span, new ReadOnlySpan<byte>(searchForBytes));
            if (searchFor.Length == 1)
            {
                Assert.Equal(index, IndexOf(span, (byte)searchFor[0]));
            }
            else if (searchFor.Length == 2)
            {
                Assert.Equal(index, IndexOfAny(span, (byte)searchFor[0], (byte)searchFor[1]));
            }
            else if (searchFor.Length == 3)
            {
                Assert.Equal(index, IndexOfAny(span, (byte)searchFor[0], (byte)searchFor[1], (byte)searchFor[2]));
            }

            var found = span[index];
            Assert.Equal((byte)expectResult, found);
            Assert.Equal(expectIndex, index);
        }

        [Fact]
        public static void ZeroLengthIndexOfTwo_Byte()
        {
            Span<byte> span = new Span<byte>(Array.Empty<byte>());

            Assert.Equal(-1, IndexOfAny(span, 0, 0));
            Assert.Equal(-1, IndexOfAny(span, new byte[2]));
        }

        [Fact]
        public static void DefaultFilledIndexOfTwo_Byte()
        {
            Random rnd = new Random(42);

            for (int length = 0; length < byte.MaxValue; length++)
            {
                byte[] a = new byte[length];
                Span<byte> span = new Span<byte>(a);

                byte[] targets = { default, 99 };

                for (int i = 0; i < length; i++)
                {
                    int index = rnd.Next(0, 2) == 0 ? 0 : 1;
                    byte target0 = targets[index];
                    byte target1 = targets[(index + 1) % 2];

                    Assert.Equal(0, IndexOfAny(span, target0, target1));
                    Assert.Equal(0, IndexOfAny(span, new[] { target0, target1 }));
                }
            }
        }

        [Fact]
        public static void TestMatchTwo_Byte()
        {
            for (int length = 0; length < byte.MaxValue; length++)
            {
                byte[] a = new byte[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = (byte)(i + 1);
                }
                Span<byte> span = new Span<byte>(a);

                for (int targetIndex = 0; targetIndex < length; targetIndex++)
                {
                    byte target0 = a[targetIndex];
                    byte target1 = 0;

                    Assert.Equal(targetIndex, IndexOfAny(span, target0, target1));
                    Assert.Equal(targetIndex, IndexOfAny(span, new[] { target0, target1 }));
                }

                for (int targetIndex = 0; targetIndex < length - 1; targetIndex++)
                {
                    byte target0 = a[targetIndex];
                    byte target1 = a[targetIndex + 1];

                    Assert.Equal(targetIndex, IndexOfAny(span, target0, target1));
                    Assert.Equal(targetIndex, IndexOfAny(span, new[] { target0, target1 }));
                }

                for (int targetIndex = 0; targetIndex < length - 1; targetIndex++)
                {
                    byte target0 = 0;
                    byte target1 = a[targetIndex + 1];

                    Assert.Equal(targetIndex + 1, IndexOfAny(span, target0, target1));
                    Assert.Equal(targetIndex + 1, IndexOfAny(span, new[] { target0, target1 }));
                }
            }
        }

        [Fact]
        public static void TestNoMatchTwo_Byte()
        {
            var rnd = new Random(42);
            for (int length = 0; length < byte.MaxValue; length++)
            {
                byte[] a = new byte[length];
                byte target0 = (byte)rnd.Next(1, 256);
                byte target1 = (byte)rnd.Next(1, 256);
                Span<byte> span = new Span<byte>(a);

                Assert.Equal(-1, IndexOfAny(span, target0, target1));
                Assert.Equal(-1, IndexOfAny(span, new[] { target0, target1 }));
            }
        }

        [Fact]
        public static void TestMultipleMatchTwo_Byte()
        {
            for (int length = 3; length < byte.MaxValue; length++)
            {
                byte[] a = new byte[length];
                for (int i = 0; i < length; i++)
                {
                    byte val = (byte)(i + 1);
                    a[i] = val == 200 ? (byte)201 : val;
                }

                a[length - 1] = 200;
                a[length - 2] = 200;
                a[length - 3] = 200;

                Span<byte> span = new Span<byte>(a);

                Assert.Equal(length - 3, IndexOfAny(span, 200, 200));
                Assert.Equal(length - 3, IndexOfAny(span, new byte[] { 200, 200 }));
            }
        }

        [Fact]
        public static void MakeSureNoChecksGoOutOfRangeTwo_Byte()
        {
            for (int length = 1; length < byte.MaxValue; length++)
            {
                byte[] a = new byte[length + 2];
                a[0] = 99;
                a[length + 1] = 98;
                Span<byte> span = new Span<byte>(a, 1, length - 1);

                Assert.Equal(-1, IndexOfAny(span, 99, 98));
                Assert.Equal(-1, IndexOfAny(span, new byte[] { 99, 98 }));
            }

            for (int length = 1; length < byte.MaxValue; length++)
            {
                byte[] a = new byte[length + 2];
                a[0] = 99;
                a[length + 1] = 99;
                Span<byte> span = new Span<byte>(a, 1, length - 1);

                Assert.Equal(-1, IndexOfAny(span, 99, 99));
                Assert.Equal(-1, IndexOfAny(span, new byte[] { 99, 99 }));
            }
        }

        [Fact]
        public static void ZeroLengthIndexOfThree_Byte()
        {
            Span<byte> span = new Span<byte>(Array.Empty<byte>());

            Assert.Equal(-1, IndexOfAny(span, 0, 0, 0));
            Assert.Equal(-1, IndexOfAny(span, new byte[3]));
        }

        [Fact]
        public static void DefaultFilledIndexOfThree_Byte()
        {
            Random rnd = new Random(42);

            for (int length = 0; length < byte.MaxValue; length++)
            {
                byte[] a = new byte[length];
                Span<byte> span = new Span<byte>(a);

                byte[] targets = { default, 99, 98 };

                for (int i = 0; i < length; i++)
                {
                    int index = rnd.Next(0, 3);
                    byte target0 = targets[index];
                    byte target1 = targets[(index + 1) % 2];
                    byte target2 = targets[(index + 1) % 3];

                    Assert.Equal(0, IndexOfAny(span, target0, target1, target2));
                    Assert.Equal(0, IndexOfAny(span, new[] { target0, target1, target2 }));
                }
            }
        }

        [Fact]
        public static void TestMatchThree_Byte()
        {
            for (int length = 0; length < byte.MaxValue; length++)
            {
                byte[] a = new byte[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = (byte)(i + 1);
                }
                Span<byte> span = new Span<byte>(a);

                for (int targetIndex = 0; targetIndex < length; targetIndex++)
                {
                    byte target0 = a[targetIndex];
                    byte target1 = 0;
                    byte target2 = 0;

                    Assert.Equal(targetIndex, IndexOfAny(span, target0, target1, target2));
                    Assert.Equal(targetIndex, IndexOfAny(span, new[] { target0, target1, target2 }));
                }

                for (int targetIndex = 0; targetIndex < length - 2; targetIndex++)
                {
                    byte target0 = a[targetIndex];
                    byte target1 = a[targetIndex + 1];
                    byte target2 = a[targetIndex + 2];

                    Assert.Equal(targetIndex, IndexOfAny(span, target0, target1, target2));
                    Assert.Equal(targetIndex, IndexOfAny(span, new[] { target0, target1, target2 }));
                }

                for (int targetIndex = 0; targetIndex < length - 2; targetIndex++)
                {
                    byte target0 = 0;
                    byte target1 = 0;
                    byte target2 = a[targetIndex + 2];

                    Assert.Equal(targetIndex + 2, IndexOfAny(span, target0, target1, target2));
                    Assert.Equal(targetIndex + 2, IndexOfAny(span, new[] { target0, target1, target2 }));
                }
            }
        }

        [Fact]
        public static void TestNoMatchThree_Byte()
        {
            var rnd = new Random(42);
            for (int length = 0; length < byte.MaxValue; length++)
            {
                byte[] a = new byte[length];
                byte target0 = (byte)rnd.Next(1, 256);
                byte target1 = (byte)rnd.Next(1, 256);
                byte target2 = (byte)rnd.Next(1, 256);
                Span<byte> span = new Span<byte>(a);

                Assert.Equal(-1, IndexOfAny(span, target0, target1, target2));
                Assert.Equal(-1, IndexOfAny(span, new[] { target0, target1, target2 }));
            }
        }

        [Fact]
        public static void TestMultipleMatchThree_Byte()
        {
            for (int length = 4; length < byte.MaxValue; length++)
            {
                byte[] a = new byte[length];
                for (int i = 0; i < length; i++)
                {
                    byte val = (byte)(i + 1);
                    a[i] = val == 200 ? (byte)201 : val;
                }

                a[length - 1] = 200;
                a[length - 2] = 200;
                a[length - 3] = 200;
                a[length - 4] = 200;

                Span<byte> span = new Span<byte>(a);

                Assert.Equal(length - 4, IndexOfAny(span, 200, 200, 200));
                Assert.Equal(length - 4, IndexOfAny(span, new byte[] { 200, 200, 200 }));
            }
        }

        [Fact]
        public static void MakeSureNoChecksGoOutOfRangeThree_Byte()
        {
            for (int length = 1; length < byte.MaxValue; length++)
            {
                byte[] a = new byte[length + 2];
                a[0] = 99;
                a[length + 1] = 98;
                Span<byte> span = new Span<byte>(a, 1, length - 1);

                Assert.Equal(-1, IndexOfAny(span, 99, 98, 99));
                Assert.Equal(-1, IndexOfAny(span, new byte[] { 99, 98, 99 }));
            }

            for (int length = 1; length < byte.MaxValue; length++)
            {
                byte[] a = new byte[length + 2];
                a[0] = 99;
                a[length + 1] = 99;
                Span<byte> span = new Span<byte>(a, 1, length - 1);

                Assert.Equal(-1, IndexOfAny(span, 99, 99, 99));
                Assert.Equal(-1, IndexOfAny(span, new byte[] { 99, 99, 99 }));
            }
        }

        [Fact]
        public static void ZeroLengthIndexOfMany_Byte()
        {
            Span<byte> span = new Span<byte>(Array.Empty<byte>());
            var values = new ReadOnlySpan<byte>(new byte[] { 0, 0, 0, 0 });
            int idx = IndexOfAny(span, values);
            Assert.Equal(-1, idx);

            values = new ReadOnlySpan<byte>(new byte[] { });
            idx = IndexOfAny(span, values);
            Assert.Equal(-1, idx);
        }

        [Fact]
        public static void DefaultFilledIndexOfMany_Byte()
        {
            for (int length = 0; length < byte.MaxValue; length++)
            {
                byte[] a = new byte[length];
                Span<byte> span = new Span<byte>(a);

                var values = new ReadOnlySpan<byte>(new byte[] { default, 99, 98, 0 });

                for (int i = 0; i < length; i++)
                {
                    int idx = IndexOfAny(span, values);
                    Assert.Equal(0, idx);
                }
            }
        }

        [Fact]
        public static void TestMatchMany_Byte()
        {
            for (int length = 0; length < byte.MaxValue; length++)
            {
                byte[] a = new byte[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = (byte)(i + 1);
                }
                Span<byte> span = new Span<byte>(a);

                for (int targetIndex = 0; targetIndex < length; targetIndex++)
                {
                    var values = new ReadOnlySpan<byte>(new byte[] { a[targetIndex], 0, 0, 0 });
                    int idx = IndexOfAny(span, values);
                    Assert.Equal(targetIndex, idx);
                }

                for (int targetIndex = 0; targetIndex < length - 3; targetIndex++)
                {
                    var values = new ReadOnlySpan<byte>(new byte[] { a[targetIndex], a[targetIndex + 1], a[targetIndex + 2], a[targetIndex + 3] });
                    int idx = IndexOfAny(span, values);
                    Assert.Equal(targetIndex, idx);
                }

                for (int targetIndex = 0; targetIndex < length - 3; targetIndex++)
                {
                    var values = new ReadOnlySpan<byte>(new byte[] { 0, 0, 0, a[targetIndex + 3] });
                    int idx = IndexOfAny(span, values);
                    Assert.Equal(targetIndex + 3, idx);
                }
            }
        }

        [Fact]
        public static void TestMatchValuesLargerMany_Byte()
        {
            var rnd = new Random(42);
            for (int length = 2; length < byte.MaxValue; length++)
            {
                byte[] a = new byte[length];
                int expectedIndex = length / 2;
                for (int i = 0; i < length; i++)
                {
                    if (i == expectedIndex)
                    {
                        continue;
                    }
                    a[i] = 255;
                }
                Span<byte> span = new Span<byte>(a);

                byte[] targets = new byte[length * 2];
                for (int i = 0; i < targets.Length; i++)
                {
                    if (i == length + 1)
                    {
                        continue;
                    }
                    targets[i] = (byte)rnd.Next(1, 255);
                }

                var values = new ReadOnlySpan<byte>(targets);
                int idx = IndexOfAny(span, values);
                Assert.Equal(expectedIndex, idx);
            }
        }

        [Fact]
        public static void TestNoMatchMany_Byte()
        {
            var rnd = new Random(42);
            for (int length = 1; length < byte.MaxValue; length++)
            {
                byte[] a = new byte[length];
                byte[] targets = new byte[length];
                for (int i = 0; i < targets.Length; i++)
                {
                    targets[i] = (byte)rnd.Next(1, 256);
                }
                Span<byte> span = new Span<byte>(a);
                var values = new ReadOnlySpan<byte>(targets);

                int idx = IndexOfAny(span, values);
                Assert.Equal(-1, idx);
            }
        }

        [Fact]
        public static void TestNoMatchValuesLargerMany_Byte()
        {
            var rnd = new Random(42);
            for (int length = 1; length < byte.MaxValue; length++)
            {
                byte[] a = new byte[length];
                byte[] targets = new byte[length * 2];
                for (int i = 0; i < targets.Length; i++)
                {
                    targets[i] = (byte)rnd.Next(1, 256);
                }
                Span<byte> span = new Span<byte>(a);
                var values = new ReadOnlySpan<byte>(targets);

                int idx = IndexOfAny(span, values);
                Assert.Equal(-1, idx);
            }
        }

        [Fact]
        public static void TestMultipleMatchMany_Byte()
        {
            for (int length = 5; length < byte.MaxValue; length++)
            {
                byte[] a = new byte[length];
                for (int i = 0; i < length; i++)
                {
                    byte val = (byte)(i + 1);
                    a[i] = val == 200 ? (byte)201 : val;
                }

                a[length - 1] = 200;
                a[length - 2] = 200;
                a[length - 3] = 200;
                a[length - 4] = 200;
                a[length - 5] = 200;

                Span<byte> span = new Span<byte>(a);
                var values = new ReadOnlySpan<byte>(new byte[] { 200, 200, 200, 200, 200, 200, 200, 200, 200 });
                int idx = IndexOfAny(span, values);
                Assert.Equal(length - 5, idx);
            }
        }

        [Fact]
        public static void MakeSureNoChecksGoOutOfRangeMany_Byte()
        {
            for (int length = 1; length < byte.MaxValue; length++)
            {
                byte[] a = new byte[length + 2];
                a[0] = 99;
                a[length + 1] = 98;
                Span<byte> span = new Span<byte>(a, 1, length - 1);
                var values = new ReadOnlySpan<byte>(new byte[] { 99, 98, 99, 98, 99, 98 });
                int index = IndexOfAny(span, values);
                Assert.Equal(-1, index);
            }

            for (int length = 1; length < byte.MaxValue; length++)
            {
                byte[] a = new byte[length + 2];
                a[0] = 99;
                a[length + 1] = 99;
                Span<byte> span = new Span<byte>(a, 1, length - 1);
                var values = new ReadOnlySpan<byte>(new byte[] { 99, 99, 99, 99, 99, 99 });
                int index = IndexOfAny(span, values);
                Assert.Equal(-1, index);
            }
        }

        [Fact]
        [OuterLoop("Takes about a second to execute")]
        public static void TestIndexOfAny_RandomInputs_Byte()
        {
            IndexOfAnyCharTestHelper.TestRandomInputs(
                expected: IndexOfAnyReferenceImpl,
                indexOfAny: (searchSpace, values) => searchSpace.IndexOfAny(values),
                indexOfAnyValues: (searchSpace, values) => searchSpace.IndexOfAny(values));

            static int IndexOfAnyReferenceImpl(ReadOnlySpan<byte> searchSpace, ReadOnlySpan<byte> values)
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
        public static void AsciiNeedle_ProperlyHandlesEdgeCases_Byte(bool needleContainsZero)
        {
            // There is some special handling we have to do for ASCII needles to properly filter out non-ASCII results
            ReadOnlySpan<byte> needleValues = needleContainsZero ? "AEIOU\0"u8 : "AEIOU!"u8;
            IndexOfAnyValues<byte> needle = IndexOfAnyValues.Create(needleValues);
            Assert.Contains("Ascii", needle.GetType().Name);

            ReadOnlySpan<byte> repeatingHaystack = "AaAaAaAaAaAa"u8;
            Assert.Equal(0, repeatingHaystack.IndexOfAny(needle));
            Assert.Equal(1, repeatingHaystack.IndexOfAnyExcept(needle));
            Assert.Equal(10, repeatingHaystack.LastIndexOfAny(needle));
            Assert.Equal(11, repeatingHaystack.LastIndexOfAnyExcept(needle));

            ReadOnlySpan<byte> haystackWithZeroes = "Aa\0Aa\0Aa\0"u8;
            Assert.Equal(0, haystackWithZeroes.IndexOfAny(needle));
            Assert.Equal(1, haystackWithZeroes.IndexOfAnyExcept(needle));
            Assert.Equal(needleContainsZero ? 8 : 6, haystackWithZeroes.LastIndexOfAny(needle));
            Assert.Equal(needleContainsZero ? 7 : 8, haystackWithZeroes.LastIndexOfAnyExcept(needle));

            Span<byte> haystackWithOffsetNeedle = new byte[100];
            for (int i = 0; i < haystackWithOffsetNeedle.Length; i++)
            {
                haystackWithOffsetNeedle[i] = (byte)(128 + needleValues[i % needleValues.Length]);
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

        private static int IndexOf(Span<byte> span, byte value)
        {
            int index = span.IndexOf(value);
            Assert.Equal(index, span.IndexOfAny(IndexOfAnyValues.Create(stackalloc byte[] { value })));
            return index;
        }

        private static int IndexOfAny(Span<byte> span, byte value0, byte value1)
        {
            int index = span.IndexOfAny(value0, value1);
            Assert.Equal(index, span.IndexOfAny(IndexOfAnyValues.Create(stackalloc byte[] { value0, value1 })));
            return index;
        }

        private static int IndexOfAny(Span<byte> span, byte value0, byte value1, byte value2)
        {
            int index = span.IndexOfAny(value0, value1, value2);
            Assert.Equal(index, span.IndexOfAny(IndexOfAnyValues.Create(stackalloc byte[] { value0, value1, value2 })));
            return index;
        }

        private static int IndexOfAny(Span<byte> span, ReadOnlySpan<byte> values)
        {
            int index = span.IndexOfAny(values);
            Assert.Equal(index, span.IndexOfAny(IndexOfAnyValues.Create(values)));
            return index;
        }
    }
}
