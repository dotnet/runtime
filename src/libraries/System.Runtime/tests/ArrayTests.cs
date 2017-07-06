// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Tests
{
    public static class Array17Tests
    {
        [Fact]
        public static void CreateInstance_Type_Int_Int()
        {
            int[,] intArray2 = (int[,])Array.CreateInstance(typeof(int), 1, 2);
            VerifyArray(intArray2, 2, new int[] { 1, 2 }, new int[] { 0, 0 }, new int[] { 0, 1 }, false);
            intArray2[0, 1] = 42;
            Assert.Equal(42, intArray2[0, 1]);
        }

        [Fact]
        public static void CreateInstance_Type_Int_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("elementType", () => Array.CreateInstance(null, 0)); // Element type is null

            Assert.Throws<NotSupportedException>(() => Array.CreateInstance(typeof(void), 0)); // Element type is not supported (void)
            Assert.Throws<NotSupportedException>(() => Array.CreateInstance(typeof(List<>), 0)); // Element type is not supported (generic)
            Assert.Throws<NotSupportedException>(() => Array.CreateInstance(typeof(int).MakeByRefType(), 0)); // Element type is not supported (ref)

            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => Array.CreateInstance(typeof(int), -1)); // Length < 0
        }

        [Fact]
        public static void CreateInstance_Type_Int_Int_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("elementType", () => Array.CreateInstance(null, 0, 1)); // Element type is null

            Assert.Throws<NotSupportedException>(() => Array.CreateInstance(typeof(void), 0, 1)); // Element type is not supported (void)
            Assert.Throws<NotSupportedException>(() => Array.CreateInstance(typeof(List<>), 0, 1)); // Element type is not supported (generic)
            Assert.Throws<NotSupportedException>(() => Array.CreateInstance(typeof(int).MakeByRefType(), 0, 1)); // Element type is not supported (ref)

            AssertExtensions.Throws<ArgumentOutOfRangeException>("length2", () => Array.CreateInstance(typeof(int), 0, -1)); // Length < 0
        }

        [Fact]
        public static void CreateInstance_Type_Int_Int_Int()
        {
            int[,,] intArray3 = (int[,,])Array.CreateInstance(typeof(int), 1, 2, 3);
            VerifyArray(intArray3, 3, new int[] { 1, 2, 3 }, new int[] { 0, 0, 0 }, new int[] { 0, 1, 2 }, false);
            intArray3[0, 1, 2] = 42;
            Assert.Equal(42, intArray3[0, 1, 2]);
        }

        [Fact]
        public static void CreateInstance_Type_Int_Int_Int_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("elementType", () => Array.CreateInstance(null, 0, 1, 2)); // Element type is null

            Assert.Throws<NotSupportedException>(() => Array.CreateInstance(typeof(void), 0, 1, 2)); // Element type is not supported (void)
            Assert.Throws<NotSupportedException>(() => Array.CreateInstance(typeof(List<>), 0, 1, 2)); // Element type is not supported (generic)
            Assert.Throws<NotSupportedException>(() => Array.CreateInstance(typeof(int).MakeByRefType(), 0, 1, 2)); // Element type is not supported (ref)

            AssertExtensions.Throws<ArgumentOutOfRangeException>("length3", () => Array.CreateInstance(typeof(int), 0, 1, -1)); // Length < 0
        }

        [Fact]
        public static void CreateInstance_Type_IntArray()
        {
            string[] stringArray = (string[])Array.CreateInstance(typeof(string), new int[] { 10 });
            Assert.Equal(stringArray, new string[10]);

            stringArray = (string[])Array.CreateInstance(typeof(string), new int[] { 0 });
            Assert.Equal(stringArray, new string[0]);

            int[] intArray1 = (int[])Array.CreateInstance(typeof(int), new int[] { 1 });
            VerifyArray(intArray1, 1, new int[] { 1 }, new int[] { 0 }, new int[] { 0 }, false);
            Assert.Equal(intArray1, new int[1]);

            int[,] intArray2 = (int[,])Array.CreateInstance(typeof(int), new int[] { 1, 2 });
            VerifyArray(intArray2, 2, new int[] { 1, 2 }, new int[] { 0, 0 }, new int[] { 0, 1 }, false);
            intArray2[0, 1] = 42;
            Assert.Equal(42, intArray2[0, 1]);

            int[,,] intArray3 = (int[,,])Array.CreateInstance(typeof(int), new int[] { 1, 2, 3 });
            VerifyArray(intArray3, 3, new int[] { 1, 2, 3 }, new int[] { 0, 0, 0 }, new int[] { 0, 1, 2 }, false);
            intArray3[0, 1, 2] = 42;
            Assert.Equal(42, intArray3[0, 1, 2]);

            int[,,,] intArray4 = (int[,,,])Array.CreateInstance(typeof(int), new int[] { 1, 2, 3, 4 });
            VerifyArray(intArray4, 4, new int[] { 1, 2, 3, 4 }, new int[] { 0, 0, 0, 0 }, new int[] { 0, 1, 2 }, false);
            intArray4[0, 1, 2, 3] = 42;
            Assert.Equal(42, intArray4[0, 1, 2, 3]);
        }

        [Fact]
        public static void CreateInstance_Type_IntArray_IntArray()
        {
            int[] intArray1 = (int[])Array.CreateInstance(typeof(int), new int[] { 5 }, new int[] { 0 });
            Assert.Equal(intArray1, new int[5]);
            VerifyArray(intArray1, 1, new int[] { 5 }, new int[] { 0 }, new int[] { 4 }, false);

            if (PlatformDetection.IsNonZeroLowerBoundArraySupported)
            {
                int[,,] intArray2a = (int[,,])Array.CreateInstance(typeof(int), new int[] { 7, 8, 9 }, new int[] { 1, 2, 3 });
                Assert.Equal(intArray2a, new int[7, 8, 9]);
                VerifyArray(intArray2a, 3, new int[] { 7, 8, 9 }, new int[] { 1, 2, 3 }, new int[] { 7, 9, 11 }, false);
            }
        }
    }
}
