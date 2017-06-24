// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class ArrayWithOffsetTests
    {
        public static IEnumerable<object[]> Ctor_Array_Offset_TestData()
        {
            yield return new object[] { null, 0, 0 };
            yield return new object[] { new int[2], -1, 8 };
            yield return new object[] { new int[2], 2, 8 };
            yield return new object[] { new int[2], 3, 8 };
            yield return new object[] { new byte[4], 1, 4 };
        }

        [Theory]
        [MemberData(nameof(Ctor_Array_Offset_TestData))]
        public void Ctor_Array_Offset(object array, int offset, int expectedHashCode)
        {
            var arrayWithOffset = new ArrayWithOffset(array, offset);
            Assert.Equal(array, arrayWithOffset.GetArray());
            Assert.Equal(offset, arrayWithOffset.GetOffset());
            Assert.Equal(expectedHashCode, arrayWithOffset.GetHashCode());
        }

        [Fact]
        public void Ctor_NotAnArray_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new ArrayWithOffset("array", 2));
        }

        [Fact]
        public void Ctor_MultidimensionalArray_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new ArrayWithOffset(new int[1, 2], 2));
        }

        [Theory]
        [InlineData(null, -1)]
        [InlineData(null, 1)]
        [InlineData(new int[] { 1 }, 5)]
        public void Ctor_InvalidOffset_ThrowsIndexOutOfRangeException(object array, int offset)
        {
            Assert.Throws<IndexOutOfRangeException>(() => new ArrayWithOffset(array, offset));
        }

        public static IEnumerable<object[]> NonPrimitiveArray_TestData()
        {
            yield return new object[] { new object[0] };
            yield return new object[] { new string[0] };
        }

        [Theory]
        [MemberData(nameof(NonPrimitiveArray_TestData))]
        public void Ctor_NonPrimitiveArray_ThrowsArgumentException(object array)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new ArrayWithOffset(array, 0));
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            int[] array = new int[2];
            yield return new object[] { new ArrayWithOffset(array, 0), new ArrayWithOffset(array, 0), true };
            yield return new object[] { new ArrayWithOffset(array, 0), new ArrayWithOffset(new int[2], 0), false };
            yield return new object[] { new ArrayWithOffset(array, 0), new ArrayWithOffset(null, 0), false };
            yield return new object[] { new ArrayWithOffset(array, 0), new ArrayWithOffset(array, 1), false };

            yield return new object[] { new ArrayWithOffset(null, 0), new ArrayWithOffset(null, 0), true };
            yield return new object[] { new ArrayWithOffset(null, 0), new ArrayWithOffset(new int[2], 0), false };

            yield return new object[] { new ArrayWithOffset(array, 0), new object(), false };
            yield return new object[] { new ArrayWithOffset(array, 0), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Object_ReturnsExpected(ArrayWithOffset arrayWithOffset, object other, bool expected)
        {
            Assert.Equal(expected, arrayWithOffset.Equals(other));
            if (other is ArrayWithOffset otherArrayWithOffset)
            {
                Assert.Equal(expected, arrayWithOffset == otherArrayWithOffset);
                Assert.Equal(!expected, arrayWithOffset != otherArrayWithOffset);
            }
        }
    }
}
