// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.VisualBasic.CompilerServices;
using Xunit;

namespace Microsoft.VisualBasic.CompilerServices.Tests
{
    public class UtilsTests
    {
        public static IEnumerable<object[]> CopyArray_TestData()
        {
            yield return new object[] { new int[] { 1, 2 }, new int[3], new int[] { 1, 2, 0 } };
            yield return new object[] { new int[] { 1, 2 }, new int[2], new int[] { 1, 2 } };
            yield return new object[] { new int[] { 1, 2 }, new int[1], new int[] { 1 } };

            yield return new object[] { new int[,] { { 1, 2 }, { 3, 4 } }, new int[2, 2], new int[,] { { 1, 2 }, { 3, 4 } } };

            if (PlatformDetection.IsNonZeroLowerBoundArraySupported)
            {
                Array array1 = Array.CreateInstance(typeof(int), 1);
                Array array2 = Array.CreateInstance(typeof(int), new int[] { 1 }, new int[] { 2 });
                yield return new object[] { array1, array2, array2 };

                Array array3 = Array.CreateInstance(typeof(int), new int[] { 1, 1 }, new int[] { 0, -1 });
                Array array4 = Array.CreateInstance(typeof(int), new int[] { 1, 0 }, new int[] { 0, 2 });
                yield return new object[] { array3, array4, array4 };

                Array array5 = Array.CreateInstance(typeof(int), new int[] { 1, 2 }, new int[] { 0, 0 });
                Array array6 = Array.CreateInstance(typeof(int), new int[] { 1, 1 }, new int[] { 0, 1 });
                yield return new object[] { array5, array6, array6};
            }
        }

        [Theory]
        [MemberData(nameof(CopyArray_TestData))]
        public void CopyArray_Valid_ReturnsExpected(Array source, Array destination, Array expected)
        {
            Assert.Same(destination, Utils.CopyArray(source, destination));
            Assert.Equal(expected, destination);
        }


        [Fact]
        public void CopyArray_NullSourceArray_ReturnsDestination()
        {
            Array destination = new object[0];
            Assert.Same(destination, Utils.CopyArray(null, destination));
        }

        [Fact]
        public void CopyArray_EmptySourceArray_ReturnsDestination()
        {
            Array destination = new object[0];
            Assert.Same(destination, Utils.CopyArray(new int[0], destination));
            Assert.Null(Utils.CopyArray(new int[0], null));
        }

        [Fact]
        public void CopyArray_NullDestinationArray_ThrowsNullReferenceException()
        {
            Assert.Throws<NullReferenceException>(() => Utils.CopyArray(new int[1], null));
        }

        [Fact]
        public void CopyArray_NonMatchingRanks_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => Utils.CopyArray(new int[1], new int[1, 1]));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNonZeroLowerBoundArraySupported))]
        public void CopyArray_RankGreaterThanTwoAndNonMatchingBounds_ThrowsArrayTypeMismatchException()
        {
            Array array1 = Array.CreateInstance(typeof(int), new int[] { 1, 2, 3 }, new int[] { 2, 3, 4 });
            Array array2 = Array.CreateInstance(typeof(int), new int[] { 1, 2, 3}, new int[] { 2, 4, 4 });
            Assert.Throws<ArrayTypeMismatchException>(() => Utils.CopyArray(array1, array2));
            Assert.Throws<ArrayTypeMismatchException>(() => Utils.CopyArray(array2, array1));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNonZeroLowerBoundArraySupported))]
        public void CopyArray_NonMatchingBounds_ThrowsArgumentOutOfRangeException()
        {
            Array array1 = Array.CreateInstance(typeof(int), new int[] { 1, 2 }, new int[] { 2, 3 });
            Array array2 = Array.CreateInstance(typeof(int), new int[] { 1, 2 }, new int[] { 2, 4 });
            AssertExtensions.Throws<ArgumentOutOfRangeException>("sourceIndex", "srcIndex", () => Utils.CopyArray(array1, array2));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("sourceIndex", "srcIndex", () => Utils.CopyArray(array2, array1));
        }
    }
}
