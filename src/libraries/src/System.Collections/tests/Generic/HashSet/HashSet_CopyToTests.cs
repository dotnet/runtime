// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;
using Tests.HashSet_HashSetTestSupport;

namespace Tests
{
    public class HashSet_CopyToTests
    {
        #region CopyTo(T[]) Tests

        [Fact]
        public static void CopyTo_Array_Exceptions()
        {
            //Test 1: Array is null
            HashSet<int> hashSet = new HashSet<int>(new int[] { 1, 2, 3, 4, 5, 6, 7 });
            int[] array = null;
            Assert.Throws<ArgumentNullException>(() => hashSet.CopyTo(array)); //"Expected ArgumentNullException"

            //Test 2: Array is one smaller than set size
            hashSet = new HashSet<int>(new int[] { 1, 2, 3, 4, 5, 6, 7 });
            array = new int[6];
            Assert.Throws<ArgumentException>(() => hashSet.CopyTo(array)); //"Expected ArgumentException"
        }

        //Test 3: Array is equal to set size
        [Fact]
        public static void CopyTo_Array_Test1()
        {
            HashSet<int> hashSet = new HashSet<int>(new int[] { 1, 2, 3, 4, 5, 6, 7 });
            int[] array = new int[] { -1, -2, -3, -4, -5, -6, -7 };
            hashSet.CopyTo(array);
            HashSet<int> hashSet2 = new HashSet<int>(array);

            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[] { 1, 2, 3, 4, 5, 6, 7 }, EqualityComparer<int>.Default);
            HashSetTestSupport<int>.VerifyHashSet(hashSet2, new int[] { 1, 2, 3, 4, 5, 6, 7 }, EqualityComparer<int>.Default);
        }

        //Test 4: Array is larger than set size
        [Fact]
        public static void CopyTo_Array_Test2()
        {
            HashSet<int> hashSet = new HashSet<int>(new int[] { 1, 2, 3, 4, 5, 6, 7 });
            int[] array = new int[] { -1, -2, -3, -4, -5, -6, -7, -8, -9 };
            hashSet.CopyTo(array);
            HashSet<int> hashSet2 = new HashSet<int>(array);

            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[] { 1, 2, 3, 4, 5, 6, 7 }, EqualityComparer<int>.Default);
            HashSetTestSupport<int>.VerifyHashSet(hashSet2, new int[] { 1, 2, 3, 4, 5, 6, 7, -8, -9 }, EqualityComparer<int>.Default);
        }

        #endregion

        #region CopyTo(T[], int) Tests

        [Fact]
        public static void CopyTo_ArrayInt_Exceptions()
        {
            //Test 1: Array is null
            HashSet<int> hashSet = new HashSet<int>(new int[] { 1, 2, 3, 4, 5, 6, 7 });
            int[] array = null;
            Assert.Throws<ArgumentNullException>(() => hashSet.CopyTo(array, 0)); //"ArgumentNullException expected"

            //Test 2: Array is one smaller than set size
            hashSet = new HashSet<int>(new int[] { 1, 2, 3, 4, 5, 6, 7 });
            array = new int[6];
            Assert.Throws<ArgumentException>(() => hashSet.CopyTo(array, 0)); //"ArgumentException expected"

            //Test 5: ArrayIndex is Int32.MinValue
            hashSet = new HashSet<int>(new int[] { 1, 2, 3, 4, 5, 6, 7 });
            array = new int[7];
            Assert.Throws<ArgumentOutOfRangeException>(() => hashSet.CopyTo(array, Int32.MinValue)); //"ArgumentOutOfRangeException expected"

            //Test 6: ArrayIndex is -4
            hashSet = new HashSet<int>(new int[] { 1, 2, 3, 4, 5, 6, 7 });
            array = new int[7];
            Assert.Throws<ArgumentOutOfRangeException>(() => hashSet.CopyTo(array, -4)); //"ArgumentOutOfRangeException expected"

            //Test 7: ArrayIndex is -1
            hashSet = new HashSet<int>(new int[] { 1, 2, 3, 4, 5, 6, 7 });
            array = new int[7];
            Assert.Throws<ArgumentOutOfRangeException>(() => hashSet.CopyTo(array, -1)); //"ArgumentOutOfRangeException expected"

            //Test 10: ArrayIndex is Int32.MaxValue
            hashSet = new HashSet<int>(new int[] { 1, 2, 3, 4, 5, 6, 7 });
            array = new int[7];
            Assert.Throws<ArgumentException>(() => hashSet.CopyTo(array, Int32.MaxValue)); //"ArgumentException expected"

            //Test 12: ArrayIndex is array.Length - set size + 1 (array larger than set)
            hashSet = new HashSet<int>(new int[] { 1, 2, 3, 4 });
            array = new int[] { -1, -2, -3, -4, -5, -6, -7, -8, -9, -10, -11, -12 };
            Assert.Throws<ArgumentException>(() => hashSet.CopyTo(array, array.Length - hashSet.Count + 1)); //"ArgumentException expected"
        }

        //Test 3: Array is equal to set size
        [Fact]
        public static void CopyTo_ArrayInt_Test3()
        {
            HashSet<int> hashSet = new HashSet<int>(new int[] { 1, 2, 3, 4, 5, 6, 7 });
            int[] array = new int[] { -1, -2, -3, -4, -5, -6, -7 };
            hashSet.CopyTo(array, 0);
            HashSet<int> hashSet2 = new HashSet<int>(array);

            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[] { 1, 2, 3, 4, 5, 6, 7 }, EqualityComparer<int>.Default);
            HashSetTestSupport<int>.VerifyHashSet(hashSet2, new int[] { 1, 2, 3, 4, 5, 6, 7 }, EqualityComparer<int>.Default);
        }

        //Test 4: Array is larger than set size
        [Fact]
        public static void CopyTo_ArrayInt_Test4()
        {
            HashSet<int> hashSet = new HashSet<int>(new int[] { 1, 2, 3, 4, 5, 6, 7 });
            int[] array = new int[] { -1, -2, -3, -4, -5, -6, -7, -8, -9 };
            hashSet.CopyTo(array, 0);
            HashSet<int> hashSet2 = new HashSet<int>(array);

            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[] { 1, 2, 3, 4, 5, 6, 7 }, EqualityComparer<int>.Default);
            HashSetTestSupport<int>.VerifyHashSet(hashSet2, new int[] { 1, 2, 3, 4, 5, 6, 7, -8, -9 }, EqualityComparer<int>.Default);
        }

        //Test 8: ArrayIndex is 0 
        [Fact]
        public static void CopyTo_ArrayInt_Test8()
        {
            HashSet<int> hashSet = new HashSet<int>(new int[] { 1, 2, 3, 4, 5, 6, 7 });
            int[] array = new int[] { -1, -2, -3, -4, -5, -6, -7, -8, -9, -10 };
            hashSet.CopyTo(array, 0);
            HashSet<int> hashSet2 = new HashSet<int>(array);

            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[] { 1, 2, 3, 4, 5, 6, 7 }, EqualityComparer<int>.Default);
            HashSetTestSupport<int>.VerifyHashSet(hashSet2, new int[] { 1, 2, 3, 4, 5, 6, 7, -8, -9, -10 }, EqualityComparer<int>.Default);
        }

        //Test 9: ArrayIndex is 4 (array is 8 larger than set) 
        [Fact]
        public static void CopyTo_ArrayInt_Test9()
        {
            HashSet<int> hashSet = new HashSet<int>(new int[] { 1, 2, 3, 4 });
            int[] array = new int[] { -1, -2, -3, -4, -5, -6, -7, -8, -9, -10, -11, -12 };
            hashSet.CopyTo(array, 4);
            HashSet<int> hashSet2 = new HashSet<int>(array);

            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[] { 1, 2, 3, 4 }, EqualityComparer<int>.Default);
            HashSetTestSupport<int>.VerifyHashSet(hashSet2, new int[] { -1, -2, -3, -4, 1, 2, 3, 4, -9, -10, -11, -12 }, EqualityComparer<int>.Default);
        }

        //Test 11: ArrayIndex is array.Length - set size (array larger than set)
        [Fact]
        public static void CopyTo_ArrayInt_Test11()
        {
            HashSet<int> hashSet = new HashSet<int>(new int[] { 1, 2, 3, 4 });
            int[] array = new int[] { -1, -2, -3, -4, -5, -6, -7, -8, -9, -10, -11, -12 };
            hashSet.CopyTo(array, array.Length - hashSet.Count);
            HashSet<int> hashSet2 = new HashSet<int>(array);

            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[] { 1, 2, 3, 4 }, EqualityComparer<int>.Default);
            HashSetTestSupport<int>.VerifyHashSet(hashSet2, new int[] { -1, -2, -3, -4, -5, -6, -7, -8, 1, 2, 3, 4 }, EqualityComparer<int>.Default);
        }

        //Test 13: ArrayIndex is array.Length - set size - 1 (array larger than set)
        [Fact]
        public static void CopyTo_ArrayInt_Test13()
        {
            HashSet<int> hashSet = new HashSet<int>(new int[] { 1, 2, 3, 4 });
            int[] array = new int[] { -1, -2, -3, -4, -5, -6, -7, -8, -9, -10, -11, -12 };
            hashSet.CopyTo(array, array.Length - hashSet.Count - 1);
            HashSet<int> hashSet2 = new HashSet<int>(array);

            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[] { 1, 2, 3, 4 }, EqualityComparer<int>.Default);
            HashSetTestSupport<int>.VerifyHashSet(hashSet2, new int[] { -1, -2, -3, -4, -5, -6, -7, 1, 2, 3, 4, -12 }, EqualityComparer<int>.Default);
        }

        #endregion

        #region CopyTo(T[], int, int) Tests

        [Fact]
        public static void CopyTo_ArrayIntInt_Exceptions()
        {
            //Test 1: Array is null
            HashSet<int> hashSet = new HashSet<int>(new int[] { 1, 2, 3, 4, 5, 6, 7 });
            int[] array = null;
            Assert.Throws<ArgumentNullException>(() => hashSet.CopyTo(array, 0, hashSet.Count)); //"ArgumentNullException expected."

            //Test 2: Array is one smaller than set size
            hashSet = new HashSet<int>(new int[] { 1, 2, 3, 4, 5, 6, 7 });
            array = new int[6];
            Assert.Throws<ArgumentException>(() => hashSet.CopyTo(array, 0, hashSet.Count)); //"ArgumentException expected."

            // Array Index Tests

            //Test 5: ArrayIndex is Int32.MinValue
            hashSet = new HashSet<int>(new int[] { 1, 2, 3, 4, 5, 6, 7 });
            array = new int[7];
            Assert.Throws<ArgumentOutOfRangeException>(() => hashSet.CopyTo(array, Int32.MinValue, hashSet.Count)); //"ArgumentOutOfRangeException expected."

            //Test 6: ArrayIndex is -4
            hashSet = new HashSet<int>(new int[] { 1, 2, 3, 4, 5, 6, 7 });
            array = new int[7];
            Assert.Throws<ArgumentOutOfRangeException>(() => hashSet.CopyTo(array, -4, hashSet.Count)); //"ArgumentOutOfRangeException expected."

            //Test 7: ArrayIndex is -1
            hashSet = new HashSet<int>(new int[] { 1, 2, 3, 4, 5, 6, 7 });
            array = new int[7];
            Assert.Throws<ArgumentOutOfRangeException>(() => hashSet.CopyTo(array, -1, hashSet.Count)); //"ArgumentOutOfRangeException expected."

            //Test 10: ArrayIndex is Int32.MaxValue
            hashSet = new HashSet<int>(new int[] { 1, 2, 3, 4, 5, 6, 7 });
            array = new int[7];
            Assert.Throws<ArgumentException>(() => hashSet.CopyTo(array, int.MaxValue, hashSet.Count)); //"ArgumentException expected."

            //Test 12: ArrayIndex is array.Length - set size + 1 (array larger than set)
            hashSet = new HashSet<int>(new int[] { 1, 2, 3, 4 });
            array = new int[] { -1, -2, -3, -4, -5, -6, -7, -8, -9, -10, -11, -12 };
            Assert.Throws<ArgumentException>(() => hashSet.CopyTo(array, array.Length - hashSet.Count + 1, hashSet.Count)); //"ArgumentException expected."

            // Count Tests

            //Test 14: Count is Int32.MinValue
            hashSet = new HashSet<int>(new int[] { 1, 2, 3, 4 });
            array = new int[] { -1, -2, -3, -4, -5, -6, -7, -8, -9, -10, -11, -12 };
            Assert.Throws<ArgumentOutOfRangeException>(() => hashSet.CopyTo(array, array.Length - hashSet.Count - 1, Int32.MinValue)); //"ArgumentOutOfRangeException expected."

            //Test 14: Count is -2
            hashSet = new HashSet<int>(new int[] { 1, 2, 3, 4 });
            array = new int[] { -1, -2, -3, -4, -5, -6, -7, -8, -9, -10, -11, -12 };
            Assert.Throws<ArgumentOutOfRangeException>(() => hashSet.CopyTo(array, array.Length - hashSet.Count - 1, -2)); //"ArgumentOutOfRangeException expected."

            //Test 16: Count is -1
            hashSet = new HashSet<int>(new int[] { 1, 2, 3, 4 });
            array = new int[] { -1, -2, -3, -4, -5, -6, -7, -8, -9, -10, -11, -12 };
            Assert.Throws<ArgumentOutOfRangeException>(() => hashSet.CopyTo(array, array.Length - hashSet.Count - 1, -1)); //"ArgumentOutOfRangeException expected."

            //Test 18: Count is Int32.MaxValue
            hashSet = new HashSet<int>(new int[] { 1, 2, 3, 4 });
            array = new int[] { -1, -2, -3, -4, -5, -6, -7, -8, -9, -10, -11, -12 };
            Assert.Throws<ArgumentException>(() => hashSet.CopyTo(array, array.Length - hashSet.Count - 1, Int32.MaxValue)); //"ArgumentException expected."

            //Test 30: Count is larger than the set size: at start of array: wouldn't fit count
            hashSet = new HashSet<int>(new int[] { 1, 2, 3 });
            array = new int[] { -1, -2, -3, -4, -5, -6 };
            Assert.Throws<ArgumentException>(() => hashSet.CopyTo(array, 0, 15)); //"ArgumentException expected."

            //Test 32: Count is larger than the set size: in the middle of the array: wouldn't fit count
            hashSet = new HashSet<int>(new int[] { 1, 2, 3 });
            array = new int[] { -1, -2, -3, -4, -5, -6 };
            Assert.Throws<ArgumentException>(() => hashSet.CopyTo(array, 1, 15)); //"ArgumentException expected."

            //Test 34: Count is larger than the set size: set ends at end of array
            hashSet = new HashSet<int>(new int[] { 1, 2, 3 });
            array = new int[] { -1, -2, -3, -4, -5, -6 };
            Assert.Throws<ArgumentException>(() => hashSet.CopyTo(array, 3, 15)); //"ArgumentException expected."

            //Test 36: Count is larger than the set size: set same as array length
            hashSet = new HashSet<int>(new int[] { 1, 2, 3 });
            array = new int[] { -1, -2, -3 };
            Assert.Throws<ArgumentException>(() => hashSet.CopyTo(array, 0, 15)); //"ArgumentException expected."
        }

        //Test 3: Array is equal to set size
        [Fact]
        public static void CopyTo_ArrayIntInt_Test3()
        {
            HashSet<int> hashSet = new HashSet<int>(new int[] { 1, 2, 3, 4, 5, 6, 7 });
            int[] array = new int[] { -1, -2, -3, -4, -5, -6, -7 };
            hashSet.CopyTo(array, 0, hashSet.Count);
            HashSet<int> hashSet2 = new HashSet<int>(array);

            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[] { 1, 2, 3, 4, 5, 6, 7 }, EqualityComparer<int>.Default);
            HashSetTestSupport<int>.VerifyHashSet(hashSet2, new int[] { 1, 2, 3, 4, 5, 6, 7 }, EqualityComparer<int>.Default);
        }

        //Test 4: Array is larger than set size
        [Fact]
        public static void CopyTo_ArrayIntInt_Test4()
        {
            HashSet<int> hashSet = new HashSet<int>(new int[] { 1, 2, 3, 4, 5, 6, 7 });
            int[] array = new int[] { -1, -2, -3, -4, -5, -6, -7, -8, -9 };
            hashSet.CopyTo(array, 0, hashSet.Count);
            HashSet<int> hashSet2 = new HashSet<int>(array);

            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[] { 1, 2, 3, 4, 5, 6, 7 }, EqualityComparer<int>.Default);
            HashSetTestSupport<int>.VerifyHashSet(hashSet2, new int[] { 1, 2, 3, 4, 5, 6, 7, -8, -9 }, EqualityComparer<int>.Default);
        }

        //Test 8: ArrayIndex is 0 
        [Fact]
        public static void CopyTo_ArrayIntInt_Test8()
        {
            HashSet<int> hashSet = new HashSet<int>(new int[] { 1, 2, 3, 4, 5, 6, 7 });
            int[] array = new int[] { -1, -2, -3, -4, -5, -6, -7, -8, -9, -10 };
            hashSet.CopyTo(array, 0, hashSet.Count);
            HashSet<int> hashSet2 = new HashSet<int>(array);

            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[] { 1, 2, 3, 4, 5, 6, 7 }, EqualityComparer<int>.Default);
            HashSetTestSupport<int>.VerifyHashSet(hashSet2, new int[] { 1, 2, 3, 4, 5, 6, 7, -8, -9, -10 }, EqualityComparer<int>.Default);
        }

        //Test 9: ArrayIndex is 4 (array is 8 larger than set) 
        [Fact]
        public static void CopyTo_ArrayIntInt_Test9()
        {
            HashSet<int> hashSet = new HashSet<int>(new int[] { 1, 2, 3, 4 });
            int[] array = new int[] { -1, -2, -3, -4, -5, -6, -7, -8, -9, -10, -11, -12 };
            hashSet.CopyTo(array, 4, hashSet.Count);
            HashSet<int> hashSet2 = new HashSet<int>(array);

            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[] { 1, 2, 3, 4 }, EqualityComparer<int>.Default);
            HashSetTestSupport<int>.VerifyHashSet(hashSet2, new int[] { -1, -2, -3, -4, 1, 2, 3, 4, -9, -10, -11, -12 }, EqualityComparer<int>.Default);
        }

        //Test 11: ArrayIndex is array.Length - set size (array larger than set)
        [Fact]
        public static void CopyTo_ArrayIntInt_Test11()
        {
            HashSet<int> hashSet = new HashSet<int>(new int[] { 1, 2, 3, 4 });
            int[] array = new int[] { -1, -2, -3, -4, -5, -6, -7, -8, -9, -10, -11, -12 };
            hashSet.CopyTo(array, array.Length - hashSet.Count, hashSet.Count);
            HashSet<int> hashSet2 = new HashSet<int>(array);

            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[] { 1, 2, 3, 4 }, EqualityComparer<int>.Default);
            HashSetTestSupport<int>.VerifyHashSet(hashSet2, new int[] { -1, -2, -3, -4, -5, -6, -7, -8, 1, 2, 3, 4 }, EqualityComparer<int>.Default);
        }

        //Test 13: ArrayIndex is array.Length - set size - 1 (array larger than set)
        [Fact]
        public static void CopyTo_ArrayIntInt_Test13()
        {
            HashSet<int> hashSet = new HashSet<int>(new int[] { 1, 2, 3, 4 });
            int[] array = new int[] { -1, -2, -3, -4, -5, -6, -7, -8, -9, -10, -11, -12 };
            hashSet.CopyTo(array, array.Length - hashSet.Count - 1, hashSet.Count);
            HashSet<int> hashSet2 = new HashSet<int>(array);

            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[] { 1, 2, 3, 4 }, EqualityComparer<int>.Default);
            HashSetTestSupport<int>.VerifyHashSet(hashSet2, new int[] { -1, -2, -3, -4, -5, -6, -7, 1, 2, 3, 4, -12 }, EqualityComparer<int>.Default);
        }

        //Test 17: Count is 0
        [Fact]
        public static void CopyTo_ArrayIntInt_Test17()
        {
            HashSet<int> hashSet = new HashSet<int>(new int[] { 1, 2, 3, 4 });
            int[] array = new int[] { -1, -2, -3, -4, -5, -6, -7, -8, -9, -10, -11, -12 };
            hashSet.CopyTo(array, array.Length - hashSet.Count, 0);
            HashSet<int> hashSet2 = new HashSet<int>(array);

            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[] { 1, 2, 3, 4 }, EqualityComparer<int>.Default);
            HashSetTestSupport<int>.VerifyHashSet(hashSet2, new int[] { -1, -2, -3, -4, -5, -6, -7, -8, -9, -10, -11, -12 }, EqualityComparer<int>.Default);
        }

        //Test 19: Count needed to fit partial set into array space: at start of array
        [Fact]
        public static void CopyTo_ArrayIntInt_Test19()
        {
            HashSet<int> hashSet = new HashSet<int>(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
            int[] array = new int[] { -1, -2, -3, -4, -5 };
            hashSet.CopyTo(array, 0, 3);
            HashSet<int> hashSet2 = new HashSet<int>(array);

            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, EqualityComparer<int>.Default);
            Assert.True(hashSet.Contains(array[0]), "Should contain item: " + array[0]);
            Assert.True(hashSet.Contains(array[1]), "Should contain item: " + array[1]);
            Assert.True(hashSet.Contains(array[2]), "Should contain item: " + array[2]);
            Assert.Equal(-4, array[3]); //"Should be equal"
            Assert.Equal(-5, array[4]); //"Should be equal."
        }

        //Test 20: Count needed to fit partial set into array space: in the middle of the array
        [Fact]
        public static void CopyTo_ArrayIntInt_Test20()
        {
            HashSet<int> hashSet = new HashSet<int>(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
            int[] array = new int[] { -1, -2, -3, -4, -5 };
            hashSet.CopyTo(array, 1, 3);
            HashSet<int> hashSet2 = new HashSet<int>(array);

            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, EqualityComparer<int>.Default);
            Assert.True(hashSet.Contains(array[1]), "Should contain item: " + array[1]);
            Assert.True(hashSet.Contains(array[2]), "Should contain item: " + array[2]);
            Assert.True(hashSet.Contains(array[3]), "Should contain item: " + array[3]);

            Assert.Equal(-1, array[0]); //"Should be equal"
            Assert.Equal(-5, array[4]); //"Should be equal."
        }

        //Test 21: Count needed to fit partial set into array space: at end of the array
        [Fact]
        public static void CopyTo_ArrayIntInt_Test21()
        {
            HashSet<int> hashSet = new HashSet<int>(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
            int[] array = new int[] { -1, -2, -3, -4, -5 };
            hashSet.CopyTo(array, 2, 3);
            HashSet<int> hashSet2 = new HashSet<int>(array);

            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, EqualityComparer<int>.Default);
            Assert.True(hashSet.Contains(array[2]), "Should contain item: " + array[2]);
            Assert.True(hashSet.Contains(array[3]), "Should contain item: " + array[3]);
            Assert.True(hashSet.Contains(array[4]), "Should contain item: " + array[4]);

            Assert.Equal(-1, array[0]); //"Should be equal"
            Assert.Equal(-2, array[1]); //"Should be equal."
        }

        //Test 22: Count needed to fit partial set into array space: fulls the array completely
        [Fact]
        public static void CopyTo_ArrayIntInt_Test22()
        {
            HashSet<int> hashSet = new HashSet<int>(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
            int[] array = new int[] { -1, -2, -3, -4, -5 };
            hashSet.CopyTo(array, 0, 5);
            HashSet<int> hashSet2 = new HashSet<int>(array);

            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, EqualityComparer<int>.Default);
            Assert.True(hashSet.Contains(array[0]), "Should contain item: " + array[0]);
            Assert.True(hashSet.Contains(array[1]), "Should contain item: " + array[1]);
            Assert.True(hashSet.Contains(array[2]), "Should contain item: " + array[2]);
            Assert.True(hashSet.Contains(array[3]), "Should contain item: " + array[3]);
            Assert.True(hashSet.Contains(array[4]), "Should contain item: " + array[4]);
        }

        //Test 23: Count not needed to fit but less than set size: at start of array
        [Fact]
        public static void CopyTo_ArrayIntInt_Test23()
        {
            HashSet<int> hashSet = new HashSet<int>(new int[] { 1, 2, 3 });
            int[] array = new int[] { -1, -2, -3, -4, -5 };
            hashSet.CopyTo(array, 0, 2);
            HashSet<int> hashSet2 = new HashSet<int>(array);

            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[] { 1, 2, 3 }, EqualityComparer<int>.Default);
            Assert.True(hashSet.Contains(array[0]), "Should contain item: " + array[0]);
            Assert.True(hashSet.Contains(array[1]), "Should contain item: " + array[1]);

            Assert.Equal(-3, array[2]); //"Should be equal"
            Assert.Equal(-4, array[3]); //"Should be equal."
            Assert.Equal(-5, array[4]); //"Should be equal."
        }

        //Test 24: Count not needed to fit but less than set size: in middle of array
        [Fact]
        public static void CopyTo_ArrayIntInt_Test24()
        {
            HashSet<int> hashSet = new HashSet<int>(new int[] { 1, 2, 3 });
            int[] array = new int[] { -1, -2, -3, -4, -5 };
            hashSet.CopyTo(array, 1, 2);
            HashSet<int> hashSet2 = new HashSet<int>(array);

            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[] { 1, 2, 3 }, EqualityComparer<int>.Default);
            Assert.True(hashSet.Contains(array[1]), "Should contain item: " + array[1]);
            Assert.True(hashSet.Contains(array[2]), "Should contain item: " + array[2]);

            Assert.Equal(-1, array[0]); //"Should be equal"
            Assert.Equal(-4, array[3]); //"Should be equal."
            Assert.Equal(-5, array[4]); //"Should be equal."
        }

        //Test 25: Count is equal to set size: at start of array
        [Fact]
        public static void CopyTo_ArrayIntInt_Test25()
        {
            HashSet<int> hashSet = new HashSet<int>(new int[] { 1, 2, 3 });
            int[] array = new int[] { -1, -2, -3, -4, -5 };
            hashSet.CopyTo(array, 0, 3);
            HashSet<int> hashSet2 = new HashSet<int>(array);

            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[] { 1, 2, 3 }, EqualityComparer<int>.Default);
            Assert.True(hashSet.Contains(array[0]), "Should contain item: " + array[0]);
            Assert.True(hashSet.Contains(array[1]), "Should contain item: " + array[1]);
            Assert.True(hashSet.Contains(array[2]), "Should contain item: " + array[2]);

            Assert.Equal(-4, array[3]); //"Should be equal."
            Assert.Equal(-5, array[4]); //"Should be equal."
        }

        //Test 26: Count is equal to set size: in middle of array
        [Fact]
        public static void CopyTo_ArrayIntInt_Test26()
        {
            HashSet<int> hashSet = new HashSet<int>(new int[] { 1, 2, 3 });
            int[] array = new int[] { -1, -2, -3, -4, -5 };
            hashSet.CopyTo(array, 1, 3);
            HashSet<int> hashSet2 = new HashSet<int>(array);

            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[] { 1, 2, 3 }, EqualityComparer<int>.Default);

            Assert.True(hashSet.Contains(array[1]), "Should contain item: " + array[1]);
            Assert.True(hashSet.Contains(array[2]), "Should contain item: " + array[2]);
            Assert.True(hashSet.Contains(array[3]), "Should contain item: " + array[3]);

            Assert.Equal(-1, array[0]); //"Should be equal."
            Assert.Equal(-5, array[4]); //"Should be equal."
        }

        //Test 27: Count is equal to set size: at end of array
        [Fact]
        public static void CopyTo_ArrayIntInt_Test27()
        {
            HashSet<int> hashSet = new HashSet<int>(new int[] { 1, 2, 3 });
            int[] array = new int[] { -1, -2, -3, -4, -5 };
            hashSet.CopyTo(array, 2, 3);
            HashSet<int> hashSet2 = new HashSet<int>(array);

            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[] { 1, 2, 3 }, EqualityComparer<int>.Default);

            Assert.True(hashSet.Contains(array[2]), "Should contain item: " + array[2]);
            Assert.True(hashSet.Contains(array[3]), "Should contain item: " + array[3]);
            Assert.True(hashSet.Contains(array[4]), "Should contain item: " + array[4]);

            Assert.Equal(-1, array[0]); //"Should be equal."
            Assert.Equal(-2, array[1]); //"Should be equal."
        }

        //Test 28: Count is equal to set size: fulls the array
        [Fact]
        public static void CopyTo_ArrayIntInt_Test28()
        {
            HashSet<int> hashSet = new HashSet<int>(new int[] { 1, 2, 3 });
            int[] array = new int[] { -1, -2, -3 };
            hashSet.CopyTo(array, 0, 3);
            HashSet<int> hashSet2 = new HashSet<int>(array);

            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[] { 1, 2, 3 }, EqualityComparer<int>.Default);
            Assert.True(hashSet.Contains(array[0]), "Should contain item: " + array[0]);
            Assert.True(hashSet.Contains(array[1]), "Should contain item: " + array[1]);
            Assert.True(hashSet.Contains(array[2]), "Should contain item: " + array[2]);
        }

        //Test 29: Count is larger than the set size: at start of array: would fit count
        [Fact]
        public static void CopyTo_ArrayIntInt_Test29()
        {
            HashSet<int> hashSet = new HashSet<int>(new int[] { 1, 2, 3 });
            int[] array = new int[] { -1, -2, -3, -4, -5, -6 };
            hashSet.CopyTo(array, 0, 5);
            HashSet<int> hashSet2 = new HashSet<int>(array);

            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[] { 1, 2, 3 }, EqualityComparer<int>.Default);
            Assert.True(hashSet.Contains(array[0]), "Should contain item: " + array[0]);
            Assert.True(hashSet.Contains(array[1]), "Should contain item: " + array[1]);
            Assert.True(hashSet.Contains(array[2]), "Should contain item: " + array[2]);

            Assert.Equal(-4, array[3]); //"Should be equal."
            Assert.Equal(-5, array[4]); //"Should be equal."
            Assert.Equal(-6, array[5]); //"Should be equal."
        }

        //Test 31: Count is larger than the set size: in the middle of the array: would fit count
        [Fact]
        public static void CopyTo_ArrayIntInt_Test31()
        {
            HashSet<int> hashSet = new HashSet<int>(new int[] { 1, 2, 3 });
            int[] array = new int[] { -1, -2, -3, -4, -5, -6 };
            hashSet.CopyTo(array, 1, 4);
            HashSet<int> hashSet2 = new HashSet<int>(array);

            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[] { 1, 2, 3 }, EqualityComparer<int>.Default);
            Assert.True(hashSet.Contains(array[1]), "Should contain item: " + array[1]);
            Assert.True(hashSet.Contains(array[2]), "Should contain item: " + array[2]);
            Assert.True(hashSet.Contains(array[3]), "Should contain item: " + array[3]);

            Assert.Equal(-1, array[0]); //"Should be equal."
            Assert.Equal(-5, array[4]); //"Should be equal."
            Assert.Equal(-6, array[5]); //"Should be equal."
        }

        //Test 33: Count is larger than the set size: count ends at end of array
        [Fact]
        public static void CopyTo_ArrayIntInt_Test33()
        {
            HashSet<int> hashSet = new HashSet<int>(new int[] { 1, 2, 3 });
            int[] array = new int[] { -1, -2, -3, -4, -5, -6 };
            hashSet.CopyTo(array, 1, 5);
            HashSet<int> hashSet2 = new HashSet<int>(array);

            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[] { 1, 2, 3 }, EqualityComparer<int>.Default);
            hashSet.Contains(array[1]); hashSet.Contains(array[2]); hashSet.Contains(array[3]);
            Assert.True(hashSet.Contains(array[1]), "Should contain item: " + array[1]);
            Assert.True(hashSet.Contains(array[2]), "Should contain item: " + array[2]);
            Assert.True(hashSet.Contains(array[3]), "Should contain item: " + array[3]);

            Assert.Equal(-1, array[0]); //"Should be equal."
            Assert.Equal(-5, array[4]); //"Should be equal."
            Assert.Equal(-6, array[5]); //"Should be equal."
        }

        //Test 35: Count is larger than the set size: count same as array length
        [Fact]
        public static void CopyTo_ArrayIntInt_Test35()
        {
            HashSet<int> hashSet = new HashSet<int>(new int[] { 1, 2, 3 });
            int[] array = new int[] { -1, -2, -3, -4, -5, -6 };
            hashSet.CopyTo(array, 0, 6);
            HashSet<int> hashSet2 = new HashSet<int>(array);

            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[] { 1, 2, 3 }, EqualityComparer<int>.Default);
            Assert.True(hashSet.Contains(array[0]), "Should contain item: " + array[0]);
            Assert.True(hashSet.Contains(array[1]), "Should contain item: " + array[1]);
            Assert.True(hashSet.Contains(array[2]), "Should contain item: " + array[2]);

            Assert.Equal(-4, array[3]); //"Should be equal."
            Assert.Equal(-5, array[4]); //"Should be equal."
            Assert.Equal(-6, array[5]); //"Should be equal."
        }

        #endregion
    }
}
