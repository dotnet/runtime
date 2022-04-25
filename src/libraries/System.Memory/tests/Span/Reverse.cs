// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;
using static System.TestHelpers;

namespace System.SpanTests
{
    public static partial class SpanTests
    {
        [Fact]
        public static void ReverseEmpty()
        {
            var span = Span<byte>.Empty;
            span.Reverse();

            byte[] actual = { 1, 2, 3, 4 };
            byte[] expected = { 1, 2, 3, 4 };

            span = actual;
            span.Slice(2, 0).Reverse();

            Assert.Equal<byte>(expected, span.ToArray());
        }

        [Fact]
        public static void ReverseEmptyWithReference()
        {
            var span = Span<string>.Empty;
            span.Reverse();

            string[] actual = { "a", "b", "c", "d" };
            string[] expected = { "a", "b", "c", "d" };

            span = actual;
            span.Slice(2, 0).Reverse();

            Assert.Equal<string>(expected, span.ToArray());
        }

        [Fact]
        public static void ReverseByte()
        {
            for (int length = 0; length < byte.MaxValue; length++)
            {
                byte[] actual = new byte[length];
                byte[] expected = new byte[length];
                for (int i = 0; i < length; i++)
                {
                    actual[i] = expected[length - 1 - i] = (byte)i;
                }

                var span = new Span<byte>(actual);
                span.Reverse();
                Assert.Equal<byte>(expected, actual);
            }
        }

        [Fact]
        public static void ReverseByteTwiceReturnsOriginal()
        {
            byte[] actual = { 1, 2, 3, 4, 5 };
            byte[] expected = { 1, 2, 3, 4, 5 };

            Span<byte> span = actual;
            span.Reverse();
            span.Reverse();
            Assert.Equal<byte>(expected, actual);
        }

        [Fact]
        public static void ReverseByteUnaligned()
        {
            const int length = 32;
            const int offset = 1;
            var actualFull = new byte[length];
            for (int i = 0; i < length; i++)
            {
                actualFull[i] = (byte)i;
            }
            byte[] expectedFull = new byte[length];
            Array.Copy(actualFull, expectedFull, length);
            ArrayReverse(expectedFull, offset, length - offset - 1);

            var expectedSpan = new Span<byte>(expectedFull, offset, length - offset - 1);
            var actualSpan = new Span<byte>(actualFull, offset, length - offset - 1);
            actualSpan.Reverse();

            byte[] actual = actualSpan.ToArray();
            byte[] expected = expectedSpan.ToArray();
            Assert.Equal<byte>(expected, actual);
            Assert.Equal(expectedFull[0], actualFull[0]);
            Assert.Equal(expectedFull[length - 1], actualFull[length - 1]);
        }

        [Fact]
        public static void ReverseIntPtrOffset()
        {
            const int length = 32;
            const int offset = 2;
            var actualFull = new IntPtr[length];
            for (int i = 0; i < length; i++)
            {
                actualFull[i] = IntPtr.Zero + i;
            }
            IntPtr[] expectedFull = new IntPtr[length];
            Array.Copy(actualFull, expectedFull, length);
            ArrayReverse(expectedFull, offset, length - offset);

            var expectedSpan = new Span<IntPtr>(expectedFull, offset, length - offset);
            var actualSpan = new Span<IntPtr>(actualFull, offset, length - offset);
            actualSpan.Reverse();

            IntPtr[] actual = actualSpan.ToArray();
            IntPtr[] expected = expectedSpan.ToArray();
            Assert.Equal<IntPtr>(expected, actual);
            Assert.Equal(expectedFull[0], actualFull[0]);
            Assert.Equal(expectedFull[1], actualFull[1]);
        }

        [Fact]
        public static void ReverseValueTypeWithoutReferences()
        {
            const int length = 2048;
            int[] actual = new int[length];
            int[] expected = new int[length];
            for (int i = 0; i < length; i++)
            {
                actual[i] = expected[length - 1 - i] = i;
            }

            var span = new Span<int>(actual);
            span.Reverse();
            Assert.Equal<int>(expected, actual);
        }

        [Fact]
        public static void ReverseValueTypeWithoutReferencesPointerSize()
        {
            const int length = 15;
            long[] actual = new long[length];
            long[] expected = new long[length];
            for (int i = 0; i < length; i++)
            {
                actual[i] = expected[length -i - 1] = i;
            }

            var span = new Span<long>(actual);
            span.Reverse();
            Assert.Equal<long>(expected, actual);
        }

        [Fact]
        public static void ReverseReferenceType()
        {
            const int length = 2048;
            string[] actual = new string[length];
            string[] expected = new string[length];
            for (int i = 0; i < length; i++)
            {
                actual[i] = expected[length - 1 - i] = i.ToString();
            }

            var span = new Span<string>(actual);
            span.Reverse();
            Assert.Equal<string>(expected, actual);
        }

        [Fact]
        public static void ReverseReferenceTwiceReturnsOriginal()
        {
            string[] actual = { "a1", "b2", "c3" };
            string[] expected = { "a1", "b2", "c3" };

            var span = new Span<string>(actual);
            span.Reverse();
            span.Reverse();
            Assert.Equal<string>(expected, actual);
        }

        [Fact]
        public static void ReverseEnumType()
        {
            TestEnum[] actual = { TestEnum.E0, TestEnum.E1, TestEnum.E2 };
            TestEnum[] expected = { TestEnum.E2, TestEnum.E1, TestEnum.E0 };

            var span = new Span<TestEnum>(actual);
            span.Reverse();
            Assert.Equal<TestEnum>(expected, actual);
        }

        [Fact]
        public static void ReverseValueTypeWithReferences()
        {
            TestValueTypeWithReference[] actual = {
                new TestValueTypeWithReference() { I = 1, S = "a" },
                new TestValueTypeWithReference() { I = 2, S = "b" },
                new TestValueTypeWithReference() { I = 3, S = "c" } };
            TestValueTypeWithReference[] expected = {
                new TestValueTypeWithReference() { I = 3, S = "c" },
                new TestValueTypeWithReference() { I = 2, S = "b" },
                new TestValueTypeWithReference() { I = 1, S = "a" } };

            var span = new Span<TestValueTypeWithReference>(actual);
            span.Reverse();
            Assert.Equal<TestValueTypeWithReference>(expected, actual);
        }

        // this copy of the old Array.Reverse implementation allows us to test new,
        // vectorized logic used by both Array.Reverse and Span.Reverse
        static void ArrayReverse<T>(T[] array, int index, int length)
        {
            if (length <= 1)
            {
                return;
            }

            ref T first = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), index);
            ref T last = ref Unsafe.Add(ref Unsafe.Add(ref first, length), -1);
            do
            {
                T temp = first;
                first = last;
                last = temp;
                first = ref Unsafe.Add(ref first, 1);
                last = ref Unsafe.Add(ref last, -1);
            } while (Unsafe.IsAddressLessThan(ref first, ref last));
        }
    }
}
