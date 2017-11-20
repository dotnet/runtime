// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

#pragma warning disable 1718 //Comparison made to same variable; did you mean to compare something else?

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
