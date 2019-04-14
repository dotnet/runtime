// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.MemoryTests
{
    public static partial class MemoryTests
    {
        [Fact]
        public static void EqualityTrue()
        {
            int[] a = { 91, 92, 93, 94, 95 };
            var left = new Memory<int>(a, 2, 3);
            var right = new Memory<int>(a, 2, 3);

            Assert.True(left.Equals(right));
            Assert.True(right.Equals(left));
        }

        [Fact]
        public static void EqualityReflexivity()
        {
            int[] a = { 91, 92, 93, 94, 95 };
            var left = new Memory<int>(a, 2, 3);

            Assert.True(left.Equals(left));
        }

        [Fact]
        public static void EqualityIncludesLength()
        {
            int[] a = { 91, 92, 93, 94, 95 };
            var left = new Memory<int>(a, 2, 1);
            var right = new Memory<int>(a, 2, 3);

            Assert.False(left.Equals(right));
            Assert.False(right.Equals(left));
        }

        [Fact]
        public static void EqualityIncludesBase()
        {
            int[] a = { 91, 92, 93, 94, 95 };
            var left = new Memory<int>(a, 1, 3);
            var right = new Memory<int>(a, 2, 3);

            Assert.False(left.Equals(right));
            Assert.False(right.Equals(left));
        }

        [Fact]
        public static void EqualityComparesRangeNotContent()
        {
            var left = new Memory<int>(new int[] { 0, 1, 2 }, 1, 1);
            var right = new Memory<int>(new int[] { 0, 1, 2 }, 1, 1);

            Assert.False(left.Equals(right));
            Assert.False(right.Equals(left));
        }

        [Fact]
        public static void EmptyMemoryNotUnified()
        {
            var left = new Memory<int>(new int[0]);
            var right = new Memory<int>(new int[0]);

            Memory<int> memoryFromNonEmptyArrayButWithZeroLength = new Memory<int>(new int[1] { 123 }).Slice(0, 0);

            Assert.False(left.Equals(right));
            Assert.False(right.Equals(left));

            Assert.False(memoryFromNonEmptyArrayButWithZeroLength.Equals(left));
            Assert.False(left.Equals(memoryFromNonEmptyArrayButWithZeroLength));

            // Empty property is equal
            left = Memory<int>.Empty;
            right = Memory<int>.Empty;

            Assert.True(left.Equals(right));
            Assert.True(right.Equals(left));
        }

        [Fact]
        public static void MemoryCanBeBoxed()
        {
            Memory<byte> memory = Memory<byte>.Empty;
            object memoryAsObject = memory;
            Assert.True(memory.Equals(memoryAsObject));

            ReadOnlyMemory<byte> readOnlyMemory = ReadOnlyMemory<byte>.Empty;
            object readOnlyMemoryAsObject = readOnlyMemory;
            Assert.True(readOnlyMemory.Equals(readOnlyMemoryAsObject));

            Assert.False(memory.Equals(new object()));
            Assert.False(readOnlyMemory.Equals(new object()));

            Assert.False(memory.Equals((object)(new Memory<byte>(new byte[] { 1, 2 }))));
            Assert.False(readOnlyMemory.Equals((object)(new ReadOnlyMemory<byte>(new byte[] { 1, 2 }))));

            Assert.True(memory.Equals(readOnlyMemoryAsObject));
            Assert.True(readOnlyMemory.Equals(memoryAsObject));
        }

        [Fact]
        public static void DefaultMemoryCanBeBoxed()
        {
            Memory<byte> memory = default;
            object memoryAsObject = memory;
            Assert.True(memory.Equals(memoryAsObject));

            ReadOnlyMemory<byte> readOnlyMemory = default;
            object readOnlyMemoryAsObject = readOnlyMemory;
            Assert.True(readOnlyMemory.Equals(readOnlyMemoryAsObject));

            Assert.True(memory.Equals(readOnlyMemoryAsObject));
            Assert.True(readOnlyMemory.Equals(memoryAsObject));
        }

        [Fact]
        public static void EqualityThroughInterface_True()
        {
            int[] a = { 10, 11, 12, 13, 14 };
            Memory<int> left = new Memory<int>(a, 2, 3);
            Memory<int> right = new Memory<int>(a, 2, 3);
            IEquatable<Memory<int>> leftAsEquatable = left;
            IEquatable<Memory<int>> rightAsEquatable = right;

            Assert.True(leftAsEquatable.Equals(right));
            Assert.True(rightAsEquatable.Equals(left));
        }

        [Fact]
        public static void EqualityThroughInterface_Reflexivity()
        {
            int[] array = { 42, 43, 44, 45, 46 };
            Memory<int> left = new Memory<int>(array, 2, 3);
            IEquatable<Memory<int>> leftAsEquatable = left;

            Assert.True(leftAsEquatable.Equals(left));
        }

        [Fact]
        public static void EqualityThroughInterface_IncludesLength()
        {
            int[] array = { 42, 43, 44, 45, 46 };
            Memory<int> baseline = new Memory<int>(array, 2, 3);
            Memory<int> equalRangeButLength = new Memory<int>(array, 2, 2);
            IEquatable<Memory<int>> baselineAsEquatable = baseline;
            IEquatable<Memory<int>> anotherOneAsEquatable = equalRangeButLength;

            Assert.False(baselineAsEquatable.Equals(equalRangeButLength));
            Assert.False(anotherOneAsEquatable.Equals(baseline));
        }

        [Fact]
        public static void EqualityThroughInterface_IncludesBase()
        {
            int[] array = { 42, 43, 44, 45, 46 };
            Memory<int> baseline = new Memory<int>(array, 2, 3);
            Memory<int> equalLengthButRange = new Memory<int>(array, 1, 3);
            IEquatable<Memory<int>> baselineAsEquatable = baseline;
            IEquatable<Memory<int>> anotherOneAsEquatable = equalLengthButRange;

            Assert.False(baselineAsEquatable.Equals(equalLengthButRange));
            Assert.False(anotherOneAsEquatable.Equals(baseline));
        }

        [Fact]
        public static void EqualityThroughInterface_ComparesRangeNotContent()
        {
            Memory<int> baseline = new Memory<int>(new [] { 1, 2, 3, 4, 5, 6 }, 2, 3);
            Memory<int> duplicate = new Memory<int>(new [] { 1, 2, 3, 4, 5, 6 }, 2, 3);
            IEquatable<Memory<int>> baselineAsEquatable = baseline;
            IEquatable<Memory<int>> duplicateAsEquatable = duplicate;

            Assert.False(baselineAsEquatable.Equals(duplicate));
            Assert.False(duplicateAsEquatable.Equals(baseline));
        }

        [Fact]
        public static void EqualityThroughInterface_Strings()
        {
            string[] array = { "A", "B", "C", "D", "E", "F" };
            string[] anotherArray = { "A", "B", "C", "D", "E", "F" };

            Memory<string> baseline = new Memory<string>(array, 2, 3);
            IEquatable<Memory<string>> baselineAsEquatable = baseline;
            Memory<string> equalRangeAndLength = new Memory<string>(array, 2, 3);
            Memory<string> equalRangeButLength = new Memory<string>(array, 2, 2);
            Memory<string> equalLengthButReference = new Memory<string>(array, 3, 3);
            Memory<string> differentArraySegmentAsMemory = new Memory<string>(anotherArray, 2, 3);

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
            char[] anotherArray = { 'H', 'e', 'l', 'l', 'o', ',', ' ', 'w', 'o', 'r', 'l', 'd', '!' };

            Memory<char> baseline = new Memory<char>(array, 2, 3);
            IEquatable<Memory<char>> baselineAsEquatable = baseline;
            Memory<char> equalRangeAndLength = new Memory<char>(array, 2, 3);
            Memory<char> equalRangeButLength = new Memory<char>(array, 2, 2);
            Memory<char> equalLengthButReference = new Memory<char>(array, 3, 3);
            Memory<char> differentArraySegmentAsMemory = new Memory<char>(anotherArray, 2, 3);

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
            var memory = new Memory<byte>(bytes, start, length);
            var pointingToSameMemory = new Memory<byte>(bytes, start, length);
            Memory<byte> structCopy = memory;

            Assert.True(memory.Equals(pointingToSameMemory));
            Assert.True(pointingToSameMemory.Equals(memory));

            Assert.True(memory.Equals(structCopy));
            Assert.True(structCopy.Equals(memory));
        }

        [Theory]
        [MemberData(nameof(FullArraySegments))]
        public static void MemoryArrayEquivalenceAndImplicitCastsAreEqual(byte[] bytes)
        {
            var memory = new Memory<byte>(bytes);
            var readOnlyMemory = new ReadOnlyMemory<byte>(bytes);
            ReadOnlyMemory<byte> implicitReadOnlyMemory = memory;
            Memory<byte> implicitMemoryArray = bytes;
            ReadOnlyMemory<byte> implicitReadOnlyMemoryArray = bytes;

            Assert.True(memory.Equals(bytes));
            Assert.True(readOnlyMemory.Equals(bytes));
            Assert.True(implicitReadOnlyMemory.Equals(bytes));
            Assert.True(implicitMemoryArray.Equals(bytes));
            Assert.True(implicitReadOnlyMemoryArray.Equals(bytes));

            Assert.True(readOnlyMemory.Equals(memory));
            Assert.True(implicitReadOnlyMemory.Equals(memory));
            Assert.True(implicitMemoryArray.Equals(memory));
            Assert.True(implicitReadOnlyMemoryArray.Equals(memory));

            Assert.True(memory.Equals(readOnlyMemory));
            Assert.True(implicitReadOnlyMemory.Equals(readOnlyMemory));
            Assert.True(implicitMemoryArray.Equals(readOnlyMemory));
            Assert.True(implicitReadOnlyMemoryArray.Equals(readOnlyMemory));

            Assert.True(memory.Equals(implicitMemoryArray));
            Assert.True(readOnlyMemory.Equals(implicitMemoryArray));
            Assert.True(implicitReadOnlyMemory.Equals(implicitMemoryArray));
            Assert.True(implicitReadOnlyMemoryArray.Equals(implicitMemoryArray));

            Assert.True(memory.Equals(implicitReadOnlyMemory));
            Assert.True(readOnlyMemory.Equals(implicitReadOnlyMemory));
            Assert.True(implicitMemoryArray.Equals(implicitReadOnlyMemory));
            Assert.True(implicitReadOnlyMemoryArray.Equals(implicitReadOnlyMemory));

            Assert.True(memory.Equals(implicitReadOnlyMemoryArray));
            Assert.True(readOnlyMemory.Equals(implicitReadOnlyMemoryArray));
            Assert.True(implicitReadOnlyMemory.Equals(implicitReadOnlyMemoryArray));
            Assert.True(implicitMemoryArray.Equals(implicitReadOnlyMemoryArray));
        }

        [Theory]
        [MemberData(nameof(ValidArraySegments))]
        public static void RangedMemoryEquivalenceAndImplicitCastsAreEqual(byte[] bytes, int start, int length)
        {
            var memory = new Memory<byte>(bytes, start, length);
            var readOnlyMemory = new ReadOnlyMemory<byte>(bytes, start, length);
            ReadOnlyMemory<byte> implicitReadOnlyMemory = memory;

            Assert.True(readOnlyMemory.Equals(memory));
            Assert.True(implicitReadOnlyMemory.Equals(memory));

            Assert.True(memory.Equals(readOnlyMemory));
            Assert.True(implicitReadOnlyMemory.Equals(readOnlyMemory));

            Assert.True(memory.Equals(implicitReadOnlyMemory));
            Assert.True(readOnlyMemory.Equals(implicitReadOnlyMemory));
        }

        [Theory]
        [MemberData(nameof(ValidArraySegments))]
        public static void MemoryOfEqualValuesAreNotEqual(byte[] bytes, int start, int length)
        {
            byte[] bytesCopy = bytes.ToArray();

            var memory = new Memory<byte>(bytes, start, length);
            var ofSameValues = new Memory<byte>(bytesCopy, start, length);

            Assert.False(memory.Equals(ofSameValues));
            Assert.False(ofSameValues.Equals(memory));
        }

        [Theory]
        [MemberData(nameof(ValidArraySegments))]
        public static void MemoryOfDifferentValuesAreNotEqual(byte[] bytes, int start, int length)
        {
            byte[] differentBytes = bytes.Select(value => ++value).ToArray();

            var memory = new Memory<byte>(bytes, start, length);
            var ofDifferentValues = new Memory<byte>(differentBytes, start, length);

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
