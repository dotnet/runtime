// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Tests
{
    public static partial class ArrayTests
    {
        public static IEnumerable<object[]> Reverse_Generic_Int_TestData()
        {
            // TODO: use (or merge this data into) Reverse_TestData if/when xunit/xunit#965 is merged
            yield return new object[] { new int[] { 1, 2, 3 }, 0, 3, new int[] { 3, 2, 1 } };
            yield return new object[] { new int[] { 1, 2, 3 }, 0, 2, new int[] { 2, 1, 3 } };
            yield return new object[] { new int[] { 1, 2, 3 }, 1, 2, new int[] { 1, 3, 2 } };

            // Nothing to reverse
            yield return new object[] { new int[] { 1, 2, 3 }, 2, 1, new int[] { 1, 2, 3 } };
            yield return new object[] { new int[] { 1, 2, 3 }, 0, 1, new int[] { 1, 2, 3 } };
            yield return new object[] { new int[] { 1, 2, 3 }, 0, 0, new int[] { 1, 2, 3 } };
            yield return new object[] { new int[] { 1, 2, 3 }, 3, 0, new int[] { 1, 2, 3 } };
            yield return new object[] { new int[0], 0, 0, new int[0] };
        }

        [Theory]
        [MemberData(nameof(Reverse_Generic_Int_TestData))]
        public static void Reverse_Generic(int[] array, int index, int length, int[] expected)
        {
            if (index == 0 && length == array.Length)
            {
                int[] arrayClone1 = (int[])array.Clone();
                Array.Reverse(arrayClone1);
                Assert.Equal(expected, arrayClone1);
            }
            int[] arrayClone2 = (int[])array.Clone();
            Array.Reverse(arrayClone2, index, length);
            Assert.Equal(expected, arrayClone2);
        }

        [Fact]
        public static void Reverse_Generic_NullArray_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("array", () => Array.Reverse((string[])null));
            Assert.Throws<ArgumentNullException>("array", () => Array.Reverse((string[])null, 0, 0));
        }

        [Fact]
        public static void Reverse_Generic_NegativeIndex_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>("index", () => Array.Reverse(new string[0], -1, 0));
        }

        [Fact]
        public static void Reverse_Generic_NegativeLength_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>("length", () => Array.Reverse(new string[0], 0, -1));
        }

        [Theory]
        [InlineData(0, 0, 1)]
        [InlineData(3, 4, 0)]
        [InlineData(3, 3, 1)]
        [InlineData(3, 2, 2)]
        [InlineData(3, 1, 3)]
        [InlineData(3, 0, 4)]
        public static void Reverse_Generic_InvalidOffsetPlusLength_ThrowsArgumentException(int arrayLength, int index, int length)
        {
            Assert.Throws<ArgumentException>(null, () => Array.Reverse(new string[arrayLength], index, length));
        }
    }
}
