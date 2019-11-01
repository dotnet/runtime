// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.MemoryTests
{
    public static partial class ReadOnlyMemoryTests
    {
        [Fact]
        public static void EqualityTrue()
        {
            int[] a = { 91, 92, 93, 94, 95 };
            var left = new ReadOnlyMemory<int>(a, 2, 3);
            var right = new ReadOnlyMemory<int>(a, 2, 3);

            Assert.True(left.Equals(right));
            Assert.True(right.Equals(left));
        }

        [Fact]
        public static void EqualityReflexivity()
        {
            int[] a = { 91, 92, 93, 94, 95 };
            var left = new ReadOnlyMemory<int>(a, 2, 3);

            Assert.True(left.Equals(left));
        }

        [Fact]
        public static void EqualityIncludesLength()
        {
            int[] a = { 91, 92, 93, 94, 95 };
            var left = new ReadOnlyMemory<int>(a, 2, 1);
            var right = new ReadOnlyMemory<int>(a, 2, 3);

            Assert.False(left.Equals(right));
            Assert.False(right.Equals(left));
        }

        [Fact]
        public static void EqualityIncludesBase()
        {
            int[] a = { 91, 92, 93, 94, 95 };
            var left = new ReadOnlyMemory<int>(a, 1, 3);
            var right = new ReadOnlyMemory<int>(a, 2, 3);

            Assert.False(left.Equals(right));
            Assert.False(right.Equals(left));
        }

        [Fact]
        public static void EqualityComparesRangeNotContent()
        {
            var left = new ReadOnlyMemory<int>(new int[] { 0, 1, 2 }, 1, 1);
            var right = new ReadOnlyMemory<int>(new int[] { 0, 1, 2 }, 1, 1);

            Assert.False(left.Equals(right));
            Assert.False(right.Equals(left));
        }

        [Fact]
        public static void EmptyMemoryNotUnified()
        {
            var left = new ReadOnlyMemory<int>(new int[0]);
            var right = new ReadOnlyMemory<int>(new int[0]);

            ReadOnlyMemory<int> memoryFromNonEmptyArrayButWithZeroLength = new ReadOnlyMemory<int>(new int[1] { 123 }).Slice(0, 0);

            Assert.False(left.Equals(right));
            Assert.False(right.Equals(left));

            Assert.False(memoryFromNonEmptyArrayButWithZeroLength.Equals(left));
            Assert.False(left.Equals(memoryFromNonEmptyArrayButWithZeroLength));

            // Empty property is equal
            left = ReadOnlyMemory<int>.Empty;
            right = ReadOnlyMemory<int>.Empty;

            Assert.True(left.Equals(right));
            Assert.True(right.Equals(left));
        }

        [Fact]
        public static void EqualityThroughInterface_True()
        {
            int[] array = { 10, 11, 12, 13, 14 };
            ReadOnlyMemory<int> left = new ReadOnlyMemory<int>(array, 2, 3);
            ReadOnlyMemory<int> right = new ReadOnlyMemory<int>(array, 2, 3);
            IEquatable<ReadOnlyMemory<int>> leftAsEquatable = left;
            IEquatable<ReadOnlyMemory<int>> rightAsEquatable = right;

            Assert.True(leftAsEquatable.Equals(right));
            Assert.True(rightAsEquatable.Equals(left));
        }

        [Fact]
        public static void EqualityThroughInterface_Reflexivity()
        {
            int[] array = { 42, 43, 44, 45, 46 };
            ReadOnlyMemory<int> left = new ReadOnlyMemory<int>(array, 2, 3);
            IEquatable<ReadOnlyMemory<int>> leftAsEquatable = left;

            Assert.True(leftAsEquatable.Equals(left));
        }

        [Fact]
        public static void EqualityThroughInterface_IncludesLength()
        {
            int[] array = { 42, 43, 44, 45, 46 };
            ReadOnlyMemory<int> baseline = new ReadOnlyMemory<int>(array, 2, 3);
            ReadOnlyMemory<int> equalRangeButLength = new ReadOnlyMemory<int>(array, 2, 2);
            IEquatable<ReadOnlyMemory<int>> baselineAsEquatable = baseline;
            IEquatable<ReadOnlyMemory<int>> anotherOneAsEquatable = equalRangeButLength;

            Assert.False(baselineAsEquatable.Equals(equalRangeButLength));
            Assert.False(anotherOneAsEquatable.Equals(baseline));
        }

        [Fact]
        public static void EqualityThroughInterface_IncludesBase()
        {
            int[] array = { 42, 43, 44, 45, 46 };
            ReadOnlyMemory<int> baseline = new ReadOnlyMemory<int>(array, 2, 3);
            ReadOnlyMemory<int> equalLengthButRange = new ReadOnlyMemory<int>(array, 1, 3);
            IEquatable<ReadOnlyMemory<int>> baselineAsEquatable = baseline;
            IEquatable<ReadOnlyMemory<int>> anotherOneAsEquatable = equalLengthButRange;

            Assert.False(baselineAsEquatable.Equals(equalLengthButRange));
            Assert.False(anotherOneAsEquatable.Equals(baseline));
        }

        [Fact]
        public static void EqualityThroughInterface_ComparesRangeNotContent()
        {
            ReadOnlyMemory<int> baseline = new ReadOnlyMemory<int>(new[] { 1, 2, 3, 4, 5, 6 }, 2, 3);
            ReadOnlyMemory<int> duplicate = new ReadOnlyMemory<int>(new[] { 1, 2, 3, 4, 5, 6 }, 2, 3);
            IEquatable<ReadOnlyMemory<int>> baselineAsEquatable = baseline;
            IEquatable<ReadOnlyMemory<int>> duplicateAsEquatable = duplicate;

            Assert.False(baselineAsEquatable.Equals(duplicate));
            Assert.False(duplicateAsEquatable.Equals(baseline));
        }

        [Fact]
        public static void EqualityThroughInterface_Strings()
        {
            string[] array = { "A", "B", "C", "D", "E", "F" };
            string[] anotherArray = { "A", "B", "C", "D", "E", "F" };

            ReadOnlyMemory<string> baseline = new ReadOnlyMemory<string>(array, 2, 3);
            IEquatable<ReadOnlyMemory<string>> baselineAsEquatable = baseline;
            ReadOnlyMemory<string> equalRangeAndLength = new ReadOnlyMemory<string>(array, 2, 3);
            ReadOnlyMemory<string> equalRangeButLength = new ReadOnlyMemory<string>(array, 2, 2);
            ReadOnlyMemory<string> equalLengthButReference = new ReadOnlyMemory<string>(array, 3, 3);
            ReadOnlyMemory<string> differentArraySegmentAsMemory = new ReadOnlyMemory<string>(anotherArray, 2, 3);

            Assert.True(baselineAsEquatable.Equals(baseline)); // Reflexivity
            Assert.True(baselineAsEquatable.Equals(equalRangeAndLength)); // Range check & length check
            Assert.False(baselineAsEquatable.Equals(equalRangeButLength)); // Length check
            Assert.False(baselineAsEquatable.Equals(equalLengthButReference)); // Range check

            Assert.True(baseline.Span.SequenceEqual(differentArraySegmentAsMemory.Span));
            Assert.False(baselineAsEquatable.Equals(differentArraySegmentAsMemory)); // Doesn't check for content equality
        }

        [Fact]
        public static void EqualityThroughInterface_Chars()
        {
            char[] array = { 'H', 'e', 'l', 'l', 'o', ',', ' ', 'w', 'o', 'r', 'l', 'd', '!' };
            string str = new string(array); // To prevent both string literals being interned therefore having same reference
            string anotherStr = new string(array);

            ReadOnlyMemory<char> baseline = str.AsMemory(2, 3);
            IEquatable<ReadOnlyMemory<char>> baselineAsEquatable = baseline;
            ReadOnlyMemory<char> equalRangeAndLength = str.AsMemory(2, 3);
            ReadOnlyMemory<char> equalRangeButLength = str.AsMemory(2, 2);
            ReadOnlyMemory<char> equalLengthButReference = str.AsMemory(3, 3);
            ReadOnlyMemory<char> differentArraySegmentAsMemory = anotherStr.AsMemory(2, 3);

            Assert.True(baselineAsEquatable.Equals(baseline)); // Reflexivity
            Assert.True(baselineAsEquatable.Equals(equalRangeAndLength)); // Range check & length check
            Assert.False(baselineAsEquatable.Equals(equalRangeButLength)); // Length check
            Assert.False(baselineAsEquatable.Equals(equalLengthButReference)); // Range check

            Assert.True(baseline.Span.SequenceEqual(differentArraySegmentAsMemory.Span));
            Assert.False(baselineAsEquatable.Equals(differentArraySegmentAsMemory)); // Doesn't check for content equality
        }

        [Theory]
        [MemberData(nameof(ValidArraySegments))]
        public static void MemoryReferencingSameMemoryAreEqualInEveryAspect(byte[] bytes, int start, int length)
        {
            var memory = new ReadOnlyMemory<byte>(bytes, start, length);
            var pointingToSameMemory = new ReadOnlyMemory<byte>(bytes, start, length);
            ReadOnlyMemory<byte> structCopy = memory;

            Assert.True(memory.Equals(pointingToSameMemory));
            Assert.True(pointingToSameMemory.Equals(memory));

            Assert.True(memory.Equals(structCopy));
            Assert.True(structCopy.Equals(memory));
        }

        [Theory]
        [MemberData(nameof(ValidArraySegments))]
        public static void MemoryOfEqualValuesAreNotEqual(byte[] bytes, int start, int length)
        {
            byte[] bytesCopy = bytes.ToArray();

            var memory = new ReadOnlyMemory<byte>(bytes, start, length);
            var ofSameValues = new ReadOnlyMemory<byte>(bytesCopy, start, length);

            Assert.False(memory.Equals(ofSameValues));
            Assert.False(ofSameValues.Equals(memory));
        }

        [Theory]
        [MemberData(nameof(ValidArraySegments))]
        public static void MemoryOfDifferentValuesAreNotEqual(byte[] bytes, int start, int length)
        {
            byte[] differentBytes = bytes.Select(value => ++value).ToArray();

            var memory = new ReadOnlyMemory<byte>(bytes, start, length);
            var ofDifferentValues = new ReadOnlyMemory<byte>(differentBytes, start, length);

            Assert.False(memory.Equals(ofDifferentValues));
            Assert.False(ofDifferentValues.Equals(memory));
        }

        public static IEnumerable<object[]> ValidArraySegments
        {
            get
            {
                return new List<object[]>
                {
                    new object[] { new byte[1] { 0 }, 0, 1},
                    new object[] { new byte[2] { 0, 0 }, 0, 2},
                    new object[] { new byte[2] { 0, 0 }, 0, 1},
                    new object[] { new byte[2] { 0, 0 }, 1, 1},
                    new object[] { new byte[3] { 0, 0, 0 }, 0, 3},
                    new object[] { new byte[3] { 0, 0, 0 }, 0, 2},
                    new object[] { new byte[3] { 0, 0, 0 }, 1, 2},
                    new object[] { new byte[3] { 0, 0, 0 }, 1, 1},
                    new object[] { Enumerable.Range(0, 100000).Select(i => (byte)i).ToArray(), 0, 100000 }
                };
            }
        }

        public static IEnumerable<object[]> FullArraySegments
        {
            get
            {
                return new List<object[]>
                {
                    new object[] { new byte[1] { 0 } },
                    new object[] { new byte[2] { 0, 0 } },
                    new object[] { new byte[3] { 0, 0, 0 } },
                    new object[] { Enumerable.Range(0, 100000).Select(i => (byte)i).ToArray() }
                };
            }
        }
    }
}
