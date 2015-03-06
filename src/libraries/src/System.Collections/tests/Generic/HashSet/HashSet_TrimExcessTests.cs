// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;
using Tests.HashSet_HashSetTestSupport;

namespace Tests
{
    public class HashSet_TrimExcessTests
    {
        //Test 1: Set is empty
        [Fact]
        public static void TrimExcess_Test1()
        {
            HashSet<int> hashSet;

            hashSet = new HashSet<int>();
            hashSet.TrimExcess();

            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[0], EqualityComparer<int>.Default);
        }

        //Test 2: Set is full
        [Fact]
        public static void TrimExcess_Test2()
        {
            HashSet<int> hashSet;
            int[] values;

            hashSet = new HashSet<int>();
            AddItemsInt(hashSet, 197, out values);
            hashSet.TrimExcess();

            HashSetTestSupport<int>.VerifyHashSet(hashSet, values, EqualityComparer<int>.Default);
        }

        //Test 3: Call Repeatedly
        [Fact]
        public static void TrimExcess_Test3()
        {
            HashSet<int> hashSet;
            int[] values;

            hashSet = new HashSet<int>();
            AddItemsInt(hashSet, 197, out values);
            hashSet.Clear();
            hashSet.TrimExcess();
            hashSet.TrimExcess();

            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[0], EqualityComparer<int>.Default);
        }

        //Test 4: Call, Remove one so trimming would change, Call Again
        [Fact]
        public static void TrimExcess_Test4()
        {
            HashSet<int> hashSet;
            HashSet<int> hashSet2;
            int[] values;
            List<int> remainingValues;

            hashSet = new HashSet<int>();
            AddItemsInt(hashSet, 198, out values);
            remainingValues = new List<int>(values);
            hashSet.Remove(0);
            remainingValues.Remove(0);
            hashSet.TrimExcess();

            hashSet2 = new HashSet<int>();
            AddItemsInt(hashSet2, 197);

            HashSetTestSupport<int>.VerifyHashSet(hashSet, remainingValues, EqualityComparer<int>.Default);
        }

        //Test 5: Array is Large and Sparse
        [Fact]
        public static void TrimExcess_Test5()
        {
            HashSet<int> hashSet;
            HashSet<int> hashSet2;
            int[] values;

            hashSet = new HashSet<int>();
            AddItemsInt(hashSet, 17519);
            hashSet.Clear();
            AddItemsInt(hashSet, 197, out values);
            hashSet.TrimExcess();

            hashSet2 = new HashSet<int>();
            AddItemsInt(hashSet2, 197);

            HashSetTestSupport<int>.VerifyHashSet(hashSet, values, EqualityComparer<int>.Default);
        }

        //Test 6: Array is Large and mostly full (some trimming should occur)
        [Fact]
        public static void TrimExcess_Test6()
        {
            HashSet<int> hashSet;
            HashSet<int> hashSet2;
            int[] values;

            hashSet = new HashSet<int>();
            AddItemsInt(hashSet, 17519);
            hashSet.Clear();
            AddItemsInt(hashSet, 8419, out values);
            hashSet.TrimExcess();

            hashSet2 = new HashSet<int>();
            AddItemsInt(hashSet2, 8419);

            HashSetTestSupport<int>.VerifyHashSet(hashSet, values, EqualityComparer<int>.Default);
        }

        //Test 7: Array is Large and mostly full (no trimming should occur)
        [Fact]
        public static void TrimExcess_Test7()
        {
            HashSet<int> hashSet;
            int[] values;

            hashSet = new HashSet<int>();
            AddItemsInt(hashSet, 17519);
            hashSet.Clear();
            AddItemsInt(hashSet, 17000, out values);
            hashSet.TrimExcess();

            HashSetTestSupport<int>.VerifyHashSet(hashSet, values, EqualityComparer<int>.Default);
        }

        public static void AddItemsInt(HashSet<int> set, int number)
        {
            int nextInt = 1;
            Func<int> nextIntFunc = () =>
            {
                int current = nextInt;
                nextInt++;
                return current;
            };

            set.Add(0);
            for (int x = 1; x < number; x++)
            {
                bool notAdded = false;
                while (!notAdded)
                {
                    int value = nextIntFunc();
                    notAdded = set.Add(value);
                }
            }
        }

        public static void AddItemsInt(HashSet<int> set, int number, out int[] values)
        {
            int nextInt = 1;
            Func<int> nextIntFunc = () =>
            {
                int current = nextInt;
                nextInt++;
                return current;
            };

            values = new int[number];
            set.Add(0);
            values[0] = 0;
            for (int x = 1; x < number; x++)
            {
                bool notAdded = false;
                while (!notAdded)
                {
                    int value = nextIntFunc();
                    notAdded = set.Add(value);
                    values[x] = value;
                }
            }
        }
    }
}
