// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

#pragma warning disable 1718 //Comparison made to same variable; did you mean to compare something else?

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
