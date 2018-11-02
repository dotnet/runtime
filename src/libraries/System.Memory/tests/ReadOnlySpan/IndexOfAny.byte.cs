// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Xunit;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
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
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(buffers);
            char[] searchFor = search.ToCharArray();
            byte[] searchForBytes = Encoding.UTF8.GetBytes(searchFor);

            int index = span.IndexOfAny(new ReadOnlySpan<byte>(searchForBytes));
            if (searchFor.Length == 1)
            {
                Assert.Equal(index, span.IndexOf((byte)searchFor[0]));
            }
            else if (searchFor.Length == 2)
            {
                Assert.Equal(index, span.IndexOfAny((byte)searchFor[0], (byte)searchFor[1]));
            }
            else if (searchFor.Length == 3)
            {
                Assert.Equal(index, span.IndexOfAny((byte)searchFor[0], (byte)searchFor[1], (byte)searchFor[2]));
            }

            byte found = span[index];
            Assert.Equal((byte)expectResult, found);
            Assert.Equal(expectIndex, index);
        }

        [Fact]
        public static void ZeroLengthIndexOfTwo_Byte()
        {
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(Array.Empty<byte>());

            Assert.Equal(-1, span.IndexOfAny<byte>(0, 0));
            Assert.Equal(-1, span.IndexOfAny(new byte[2]));
        }

        [Fact]
        public static void DefaultFilledIndexOfTwo_Byte()
        {
            Random rnd = new Random(42);

            for (int length = 0; length < byte.MaxValue; length++)
            {
                byte[] a = new byte[length];
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(a);

                byte[] targets = { default, 99 };

                for (int i = 0; i < length; i++)
                {
                    int index = rnd.Next(0, 2) == 0 ? 0 : 1;
                    byte target0 = targets[index];
                    byte target1 = targets[(index + 1) % 2];

                    Assert.Equal(0, span.IndexOfAny(target0, target1));
                    Assert.Equal(0, span.IndexOfAny(new[] { target0, target1 }));
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
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(a);

                for (int targetIndex = 0; targetIndex < length; targetIndex++)
                {
                    byte target0 = a[targetIndex];
                    byte target1 = 0;

                    Assert.Equal(targetIndex, span.IndexOfAny(target0, target1));
                    Assert.Equal(targetIndex, span.IndexOfAny(new[] { target0, target1 }));
                }

                for (int targetIndex = 0; targetIndex < length - 1; targetIndex++)
                {
                    byte target0 = a[targetIndex];
                    byte target1 = a[targetIndex + 1];

                    Assert.Equal(targetIndex, span.IndexOfAny(target0, target1));
                    Assert.Equal(targetIndex, span.IndexOfAny(new[] { target0, target1 }));
                }

                for (int targetIndex = 0; targetIndex < length - 1; targetIndex++)
                {
                    byte target0 = 0;
                    byte target1 = a[targetIndex + 1];

                    Assert.Equal(targetIndex + 1, span.IndexOfAny(target0, target1));
                    Assert.Equal(targetIndex + 1, span.IndexOfAny(new[] { target0, target1 }));
                }
            }
        }

        [Fact]
        public static void TestNoMatchTwo_Byte()
        {
            Random rnd = new Random(42);
            for (int length = 0; length < byte.MaxValue; length++)
            {
                byte[] a = new byte[length];
                byte target0 = (byte)rnd.Next(1, 256);
                byte target1 = (byte)rnd.Next(1, 256);
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(a);

                Assert.Equal(-1, span.IndexOfAny(target0, target1));
                Assert.Equal(-1, span.IndexOfAny(new[] { target0, target1 }));
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

                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(a);

                Assert.Equal(length - 3, span.IndexOfAny<byte>(200, 200));
                Assert.Equal(length - 3, span.IndexOfAny<byte>(new byte[] { 200, 200 }));
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
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(a, 1, length - 1);

                Assert.Equal(-1, span.IndexOfAny<byte>(99, 98));
                Assert.Equal(-1, span.IndexOfAny<byte>(new byte[] { 99, 98 }));
            }

            for (int length = 1; length < byte.MaxValue; length++)
            {
                byte[] a = new byte[length + 2];
                a[0] = 99;
                a[length + 1] = 99;
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(a, 1, length - 1);

                Assert.Equal(-1, span.IndexOfAny<byte>(99, 99));
                Assert.Equal(-1, span.IndexOfAny<byte>(new byte[] { 99, 99 }));
            }
        }

        [Fact]
        public static void ZeroLengthIndexOfThree_Byte()
        {
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(Array.Empty<byte>());

            Assert.Equal(-1, span.IndexOfAny<byte>(0, 0, 0));
            Assert.Equal(-1, span.IndexOfAny(new byte[3]));
        }

        [Fact]
        public static void DefaultFilledIndexOfThree_Byte()
        {
            Random rnd = new Random(42);

            for (int length = 0; length < byte.MaxValue; length++)
            {
                byte[] a = new byte[length];
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(a);

                byte[] targets = { default, 99, 98 };

                for (int i = 0; i < length; i++)
                {
                    int index = rnd.Next(0, 3);
                    byte target0 = targets[index];
                    byte target1 = targets[(index + 1) % 2];
                    byte target2 = targets[(index + 1) % 3];

                    Assert.Equal(0, span.IndexOfAny(target0, target1, target2));
                    Assert.Equal(0, span.IndexOfAny(new[] { target0, target1, target2 }));
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
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(a);

                for (int targetIndex = 0; targetIndex < length; targetIndex++)
                {
                    byte target0 = a[targetIndex];
                    byte target1 = 0;
                    byte target2 = 0;

                    Assert.Equal(targetIndex, span.IndexOfAny(target0, target1, target2));
                    Assert.Equal(targetIndex, span.IndexOfAny(new[] { target0, target1, target2 }));
                }

                for (int targetIndex = 0; targetIndex < length - 2; targetIndex++)
                {
                    byte target0 = a[targetIndex];
                    byte target1 = a[targetIndex + 1];
                    byte target2 = a[targetIndex + 2];

                    Assert.Equal(targetIndex, span.IndexOfAny(target0, target1, target2));
                    Assert.Equal(targetIndex, span.IndexOfAny(new[] { target0, target1, target2 }));
                }

                for (int targetIndex = 0; targetIndex < length - 2; targetIndex++)
                {
                    byte target0 = 0;
                    byte target1 = 0;
                    byte target2 = a[targetIndex + 2];

                    Assert.Equal(targetIndex + 2, span.IndexOfAny(target0, target1, target2));
                    Assert.Equal(targetIndex + 2, span.IndexOfAny(new[] { target0, target1, target2 }));
                }
            }
        }

        [Fact]
        public static void TestNoMatchThree_Byte()
        {
            Random rnd = new Random(42);
            for (int length = 0; length < byte.MaxValue; length++)
            {
                byte[] a = new byte[length];
                byte target0 = (byte)rnd.Next(1, 256);
                byte target1 = (byte)rnd.Next(1, 256);
                byte target2 = (byte)rnd.Next(1, 256);
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(a);

                Assert.Equal(-1, span.IndexOfAny(target0, target1, target2));
                Assert.Equal(-1, span.IndexOfAny(new[] { target0, target1, target2 }));
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

                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(a);

                Assert.Equal(length - 4, span.IndexOfAny<byte>(200, 200, 200));
                Assert.Equal(length - 4, span.IndexOfAny<byte>(new byte[] { 200, 200, 200 }));
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
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(a, 1, length - 1);

                Assert.Equal(-1, span.IndexOfAny<byte>(99, 98, 99));
                Assert.Equal(-1, span.IndexOfAny<byte>(new byte[] { 99, 98, 99 }));
            }

            for (int length = 1; length < byte.MaxValue; length++)
            {
                byte[] a = new byte[length + 2];
                a[0] = 99;
                a[length + 1] = 99;
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(a, 1, length - 1);

                Assert.Equal(-1, span.IndexOfAny<byte>(99, 99, 99));
                Assert.Equal(-1, span.IndexOfAny<byte>(new byte[] { 99, 99, 99 }));
            }
        }

        [Fact]
        public static void ZeroLengthIndexOfMany_Byte()
        {
            ReadOnlySpan<byte> sp = new ReadOnlySpan<byte>(Array.Empty<byte>());
            ReadOnlySpan<byte> values = new ReadOnlySpan<byte>(new byte[] { 0, 0, 0, 0 });
            int idx = sp.IndexOfAny(values);
            Assert.Equal(-1, idx);

            values = new ReadOnlySpan<byte>(new byte[] { });
            idx = sp.IndexOfAny(values);
            Assert.Equal(0, idx);
        }

        [Fact]
        public static void DefaultFilledIndexOfMany_Byte()
        {
            for (int length = 0; length < byte.MaxValue; length++)
            {
                byte[] a = new byte[length];
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(a);

                ReadOnlySpan<byte> values = new ReadOnlySpan<byte>(new byte[] { default, 99, 98, 0 });

                for (int i = 0; i < length; i++)
                {
                    int idx = span.IndexOfAny(values);
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
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(a);

                for (int targetIndex = 0; targetIndex < length; targetIndex++)
                {
                    ReadOnlySpan<byte> values = new ReadOnlySpan<byte>(new byte[] { a[targetIndex], 0, 0, 0 });
                    int idx = span.IndexOfAny(values);
                    Assert.Equal(targetIndex, idx);
                }

                for (int targetIndex = 0; targetIndex < length - 3; targetIndex++)
                {
                    ReadOnlySpan<byte> values = new ReadOnlySpan<byte>(new byte[] { a[targetIndex], a[targetIndex + 1], a[targetIndex + 2], a[targetIndex + 3] });
                    int idx = span.IndexOfAny(values);
                    Assert.Equal(targetIndex, idx);
                }

                for (int targetIndex = 0; targetIndex < length - 3; targetIndex++)
                {
                    ReadOnlySpan<byte> values = new ReadOnlySpan<byte>(new byte[] { 0, 0, 0, a[targetIndex + 3] });
                    int idx = span.IndexOfAny(values);
                    Assert.Equal(targetIndex + 3, idx);
                }
            }
        }

        [Fact]
        public static void TestMatchValuesLargerMany_Byte()
        {
            Random rnd = new Random(42);
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
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(a);

                byte[] targets = new byte[length * 2];
                for (int i = 0; i < targets.Length; i++)
                {
                    if (i == length + 1)
                    {
                        continue;
                    }
                    targets[i] = (byte)rnd.Next(1, 255);
                }

                ReadOnlySpan<byte> values = new ReadOnlySpan<byte>(targets);
                int idx = span.IndexOfAny(values);
                Assert.Equal(expectedIndex, idx);
            }
        }

        [Fact]
        public static void TestNoMatchMany_Byte()
        {
            Random rnd = new Random(42);
            for (int length = 1; length < byte.MaxValue; length++)
            {
                byte[] a = new byte[length];
                byte[] targets = new byte[length];
                for (int i = 0; i < targets.Length; i++)
                {
                    targets[i] = (byte)rnd.Next(1, 256);
                }
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(a);
                ReadOnlySpan<byte> values = new ReadOnlySpan<byte>(targets);

                int idx = span.IndexOfAny(values);
                Assert.Equal(-1, idx);
            }
        }

        [Fact]
        public static void TestNoMatchValuesLargerMany_Byte()
        {
            Random rnd = new Random(42);
            for (int length = 1; length < byte.MaxValue; length++)
            {
                byte[] a = new byte[length];
                byte[] targets = new byte[length * 2];
                for (int i = 0; i < targets.Length; i++)
                {
                    targets[i] = (byte)rnd.Next(1, 256);
                }
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(a);
                ReadOnlySpan<byte> values = new ReadOnlySpan<byte>(targets);

                int idx = span.IndexOfAny(values);
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

                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(a);
                ReadOnlySpan<byte> values = new ReadOnlySpan<byte>(new byte[] { 200, 200, 200, 200, 200, 200, 200, 200, 200 });
                int idx = span.IndexOfAny(values);
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
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(a, 1, length - 1);
                ReadOnlySpan<byte> values = new ReadOnlySpan<byte>(new byte[] { 99, 98, 99, 98, 99, 98 });
                int index = span.IndexOfAny(values);
                Assert.Equal(-1, index);
            }

            for (int length = 1; length < byte.MaxValue; length++)
            {
                byte[] a = new byte[length + 2];
                a[0] = 99;
                a[length + 1] = 99;
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(a, 1, length - 1);
                ReadOnlySpan<byte> values = new ReadOnlySpan<byte>(new byte[] { 99, 99, 99, 99, 99, 99 });
                int index = span.IndexOfAny(values);
                Assert.Equal(-1, index);
            }
        }
    }
}
