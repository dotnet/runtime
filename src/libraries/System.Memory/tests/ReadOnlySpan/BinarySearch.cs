// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        public static TheoryData<(uint[] Array, uint Value, int ExpectedIndex)> UIntCases =>
            new TheoryData<(uint[] Array, uint Value, int ExpectedIndex)> {
                (new uint[] { }, 0u, -1),
                (new uint[] { 1u, 2u, 4u, 5u }, 0u, -1),
                (new uint[] { 1u, 2u, 4u, 5u }, 1u, 0),
                (new uint[] { 1u, 2u, 4u, 5u }, 2u, 1),
                (new uint[] { 1u, 2u, 4u, 5u }, 3u, -3),
                (new uint[] { 1u, 2u, 4u, 5u }, 4u, 2),
                (new uint[] { 1u, 2u, 4u, 5u }, 5u, 3),
                (new uint[] { 1u, 2u, 4u, 5u }, 6u, -5),
            };
        public static TheoryData<(double[] Array, double Value, int ExpectedIndex)> DoubleCases =>
            new TheoryData<(double[] Array, double Value, int ExpectedIndex)> {
                (new double[] { }, 0u, -1),
                (new double[] { 1.0, 2.0, 4.0, 5u }, 0.0, -1),
                (new double[] { 1.0, 2.0, 4.0, 5u }, 1.0, 0),
                (new double[] { 1.0, 2.0, 4.0, 5u }, 2.0, 1),
                (new double[] { 1.0, 2.0, 4.0, 5u }, 3.0, -3),
                (new double[] { 1.0, 2.0, 4.0, 5u }, 4.0, 2),
                (new double[] { 1.0, 2.0, 4.0, 5u }, 5.0, 3),
                (new double[] { 1.0, 2.0, 4.0, 5u }, 6.0, -5),
            };
        public static TheoryData<(string[] Array, string Value, int ExpectedIndex)> StringCases =>
            new TheoryData<(string[] Array, string Value, int ExpectedIndex)> {
                (new string[] { "b", "c", "e", "f" }, "a", -1),
                (new string[] { "b", "c", "e", "f" }, "b", 0),
                (new string[] { "b", "c", "e", "f" }, "c", 1),
                (new string[] { "b", "c", "e", "f" }, "d", -3),
                (new string[] { "b", "c", "e", "f" }, "e", 2),
                (new string[] { "b", "c", "e", "f" }, "f", 3),
                (new string[] { "b", "c", "e", "f" }, "g", -5),
            };

        [Theory, MemberData(nameof(UIntCases))]
        public static void BinarySearch_UInt(
            (uint[] Array, uint Value, int ExpectedIndex) c)
        {
            TestOverloads(c.Array, c.Value, c.ExpectedIndex);
        }

        [Theory, MemberData(nameof(DoubleCases))]
        public static void BinarySearch_Double(
            (double[] Array, double Value, int ExpectedIndex) c)
        {
            TestOverloads(c.Array, c.Value, c.ExpectedIndex);
        }

        [Theory, MemberData(nameof(StringCases))]
        public static void BinarySearch_String(
            (string[] Array, string Value, int ExpectedIndex) c)
        {
            TestOverloads(c.Array, c.Value, c.ExpectedIndex);
        }
        
        [Fact]
        // TODO: Does it need to be OuterLoop, it's pretty fast, compared to total time
        //[OuterLoop]
        // TODO: Do we need to be platform specific?
        //[PlatformSpecific(TestPlatforms.Windows | TestPlatforms.OSX)]
        public unsafe static void BinarySearch_MaxLength_NoOverflow()
        {
            if (sizeof(IntPtr) == sizeof(long))
            {
                // Allocate max size span memory
                var length = int.MaxValue;
                if (!AllocationHelper.TryAllocNative(new IntPtr(length), out IntPtr memory))
                {
                    Console.WriteLine($"Span.BinarySearch test {nameof(BinarySearch_MaxLength_NoOverflow)} skipped (could not alloc memory).");
                    return;
                }
                try
                {
                    var span = new Span<byte>(memory.ToPointer(), length);
                    span.Fill(0);
                    // Fill end of span, so we can search for a value there, just at the end.
                    // But only to 254 so we can search for 255 just after end.
                    for (int i = 0; i < byte.MaxValue; i++)
                    {
                        var index = span.Length - (byte.MaxValue - i);
                        span[index] = (byte)i;
                    }

                    Assert.Equal(int.MaxValue - 2, span.BinarySearch((byte)(byte.MaxValue - 2)));
                    Assert.Equal(int.MaxValue - 1, span.BinarySearch((byte)(byte.MaxValue - 1)));
                    Assert.Equal(int.MinValue, span.BinarySearch(byte.MaxValue));
                }
                finally
                {
                    AllocationHelper.ReleaseNative(ref memory);
                }
            }
        }

        private static void TestOverloads<T, TComparable>(
            T[] array, TComparable value, int expectedIndex)
            where TComparable : IComparable<T>, T
        {
            TestSpan(array, value, expectedIndex);
            TestReadOnlySpan(array, value, expectedIndex);
            TestIComparableSpan(array, value, expectedIndex);
            TestIComparableReadOnlySpan(array, value, expectedIndex);
            TestComparerSpan(array, value, expectedIndex);
            TestComparerReadOnlySpan(array, value, expectedIndex);
        }

        private static void TestSpan<T, TComparable>(
            T[] array, TComparable value, int expectedIndex)
            where TComparable : IComparable<T>
        {
            var span = new Span<T>(array);
            var index = span.BinarySearch(value);
            Assert.Equal(expectedIndex, index);
        }
        private static void TestReadOnlySpan<T, TComparable>(
            T[] array, TComparable value, int expectedIndex)
            where TComparable : IComparable<T>
        {
            var span = new ReadOnlySpan<T>(array);
            var index = span.BinarySearch(value);
            Assert.Equal(expectedIndex, index);
        }
        private static void TestIComparableSpan<T, TComparable>(
            T[] array, TComparable value, int expectedIndex)
            where TComparable : IComparable<T>, T
        {
            var span = new Span<T>(array);
            var index = span.BinarySearch((IComparable<T>)value);
            Assert.Equal(expectedIndex, index);
        }
        private static void TestIComparableReadOnlySpan<T, TComparable>(
            T[] array, TComparable value, int expectedIndex)
            where TComparable : IComparable<T>, T
        {
            var span = new ReadOnlySpan<T>(array);
            var index = span.BinarySearch((IComparable<T>)value);
            Assert.Equal(expectedIndex, index);
        }
        private static void TestComparerSpan<T, TComparable>(
            T[] array, TComparable value, int expectedIndex)
            where TComparable : IComparable<T>, T
        {
            var span = new Span<T>(array);
            var index = span.BinarySearch(value, Comparer<T>.Default);
            Assert.Equal(expectedIndex, index);
        }
        private static void TestComparerReadOnlySpan<T, TComparable>(
            T[] array, TComparable value, int expectedIndex)
            where TComparable : IComparable<T>, T
        {
            var span = new ReadOnlySpan<T>(array);
            var index = span.BinarySearch(value, Comparer<T>.Default);
            Assert.Equal(expectedIndex, index);
        }
    }
}
