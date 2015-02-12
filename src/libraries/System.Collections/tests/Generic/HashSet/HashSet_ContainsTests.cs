// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;
using Tests.HashSet_HashSetTestSupport;
using Tests.HashSet_SetCollectionComparerTests;

namespace Tests
{
    public class HashSet_ContainsTests
    {
        #region Set/Item Relationship Tests
        //Test 1: Set/Item Relationship Test 1: set is Empty
        [Fact]
        public static void Contains_Test1()
        {
            HashSet<Item> hashSet = new HashSet<Item>();
            Item item = new Item(1);
            bool contains = hashSet.Contains(item);
            Assert.False(contains); //"Error: Contains returned wrong value"

            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[0], EqualityComparer<Item>.Default);
        }

        //Test 2: Set/Item Relationship Test 2: set is single-item, item in set
        [Fact]
        public static void Contains_Test2()
        {
            Item x = new Item(2);

            HashSet<Item> hashSet = new HashSet<Item>();
            hashSet.Add(x);

            Item item = x;
            bool contains = hashSet.Contains(item);
            Assert.True(contains); //"Error: Contains returned wrong value"

            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { item }, EqualityComparer<Item>.Default);
        }

        //Test 3: Set/Item Relationship Test 3: set is single-item, item not in set
        [Fact]
        public static void Contains_Test3()
        {
            Item x = new Item(2);

            HashSet<Item> hashSet = new HashSet<Item>(new ItemEqualityComparer());
            hashSet.Add(x);

            Item item = new Item(3);
            bool contains = hashSet.Contains(item);
            Assert.False(contains); //"Error: Contains returned wrong value"

            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(2) }, hashSet.Comparer);
        }

        //Test 4: Set/Item Relationship Test 4: set is multi-item, item in set
        [Fact]
        public static void Contains_Test4()
        {
            Item x = new Item(2);
            Item y = new Item(4);
            Item z = new Item(-23);

            HashSet<Item> hashSet = new HashSet<Item>(new ItemEqualityComparer());
            hashSet.Add(x);
            hashSet.Add(y);
            hashSet.Add(z);

            Item item = z;
            bool contains = hashSet.Contains(item);
            Assert.True(contains); //"Error: Contains returned wrong value"

            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { item, new Item(2), new Item(4) }, hashSet.Comparer);
        }

        //Test 5: Set/Item Relationship Test 5: set is multi-item, item not in set
        [Fact]
        public static void Contains_Test5()
        {
            Item x = new Item(2);
            Item y = new Item(4);
            Item z = new Item(-23);

            HashSet<Item> hashSet = new HashSet<Item>(new ItemEqualityComparer());
            hashSet.Add(x);
            hashSet.Add(y);
            hashSet.Add(z);

            Item item = new Item(0);
            bool contains = hashSet.Contains(item);
            Assert.False(contains); //"Error: Contains returned wrong value"

            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(-23), new Item(2), new Item(4) }, hashSet.Comparer);
        }

        //Test 6: Set/Item Relationship Test 6: Item is the set and item is in the set
        [Fact]
        public static void Contains_Test6()
        {
            List<int> item1 = new List<int>(new int[] { 1, 2 });
            List<int> item2 = new List<int>(new int[] { 2, -1 });

            IEnumerableEqualityComparer comparer = new IEnumerableEqualityComparer();
            HashSet<IEnumerable> hashSet = new HashSet<IEnumerable>(comparer);
            comparer.setSelf(hashSet);
            hashSet.Add(item1);
            hashSet.Add(item2);
            hashSet.Add(hashSet);

            IEnumerable item = hashSet;
            bool contains = hashSet.Contains(item);
            Assert.True(contains); //"Error: Contains returned wrong value"

            HashSetTestSupport<IEnumerable>.VerifyHashSet(hashSet, new IEnumerable[] { item1, item2, item }, hashSet.Comparer);
        }

        //Test 7: Set/Item Relationship Test 7: Item is the set and item is not in the set
        [Fact]
        public static void Contains_Test7()
        {
            List<int> item1 = new List<int>(new int[] { 1, 2 });
            List<int> item2 = new List<int>(new int[] { 2, -1 });

            IEnumerableEqualityComparer comparer = new IEnumerableEqualityComparer();
            HashSet<IEnumerable> hashSet = new HashSet<IEnumerable>(comparer);
            comparer.setSelf(hashSet);
            hashSet.Add(item1);
            hashSet.Add(item2);

            IEnumerable item = hashSet;
            bool contains = hashSet.Contains(item);
            Assert.False(contains); //"Error: Contains returned wrong value"

            HashSetTestSupport<IEnumerable>.VerifyHashSet(hashSet, new IEnumerable[] { item1, item2 }, hashSet.Comparer);
        }

        //Test 8: Set/Item Relationship Test 8: Item is Default<T> and in set. T is a numeric type
        [Fact]
        public static void Contains_Test8()
        {
            int x = 0;

            HashSet<int> hashSet = new HashSet<int>(new int[] { 1, 2, 3, 4, x, 6, 7 });
            int item = x;
            bool contains = hashSet.Contains(item);
            Assert.True(contains); //"Error: Contains returned wrong value"

            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[] { 1, 2, 3, 6, 7, 4, 0 }, hashSet.Comparer);
        }

        //Test 9: Set/Item Relationship Test 9: Item is Default<T> and in set. T is a reference type
        [Fact]
        public static void Contains_Test9()
        {
            Item x = null;
            Item item1 = new Item(3);
            Item item2 = new Item(-3);

            HashSet<Item> hashSet = new HashSet<Item>(new ItemEqualityComparer());
            hashSet.Add(item1);
            hashSet.Add(item2);
            hashSet.Add(x);

            Item item = x;
            bool contains = hashSet.Contains(item);
            Assert.True(contains); //"Error: Contains returned wrong value"

            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { item, new Item(3), new Item(-3) }, hashSet.Comparer);
        }

        //Test 10: Set/Item Relationship Test 10: Item is Default<T> and not in set. T is a numeric type
        [Fact]
        public static void Contains_Test10()
        {
            int x = 0;

            HashSet<int> hashSet = new HashSet<int>(new int[] { 1, 2, 3, 4, 5, 6, 7 });
            int item = x;
            bool contains = hashSet.Contains(item);
            Assert.False(contains); //"Error: Contains returned wrong value"

            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[] { 1, 2, 3, 6, 7, 4, 5 }, hashSet.Comparer);
        }

        //Test 11: Set/Item Relationship Test 11: Item is Default<T> and not in set.  T is a reference type
        [Fact]
        public static void Contains_Test11()
        {
            Item x = null;
            Item item1 = new Item(3);
            Item item2 = new Item(-3);

            HashSet<Item> hashSet = new HashSet<Item>(new ItemEqualityComparer());
            hashSet.Add(item1);
            hashSet.Add(item2);

            Item item = x;
            bool contains = hashSet.Contains(item);
            Assert.False(contains); //"Error: Contains returned wrong value"

            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(3), new Item(-3) }, hashSet.Comparer);
        }

        //Test 12: Set/Item Relationship Test 12: Item is equal to an item in set but different
        [Fact]
        public static void Contains_Test12()
        {
            Item x1 = new Item(1);
            Item x2 = new Item(1);
            Item item1 = new Item(2);
            Item item2 = new Item(3);

            HashSet<Item> hashSet = new HashSet<Item>(new ItemEqualityComparer());
            hashSet.Add(x1);
            hashSet.Add(item1);
            hashSet.Add(item2);

            Item item = x2;
            bool contains = hashSet.Contains(item);
            Assert.True(contains); //"Error: Contains returned wrong value"

            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { item, new Item(3), new Item(2) }, hashSet.Comparer);
        }

        //Test 13: Set/Item Relationship Test 13: Item shares hash value with unequal item in set
        [Fact]
        public static void Contains_Test13()
        {
            Item x1 = new Item(1);
            Item x2 = new Item(-1);
            Item item1 = new Item(2);
            Item item2 = new Item(3);

            HashSet<Item> hashSet = new HashSet<Item>(new ItemAbsoluteEqualityComparer());
            hashSet.Add(x1);
            hashSet.Add(item1);
            hashSet.Add(item2);

            Item item = x2;
            bool contains = hashSet.Contains(item);
            Assert.False(contains); //"Error: Contains returned wrong value"

            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(3), new Item(2), new Item(1) }, hashSet.Comparer);
        }

        //Test 14: Set/Item Relationship Test 14: Item was previously in set but not currently
        [Fact]
        public static void Contains_Test14()
        {
            Item x1 = new Item(1);
            Item item1 = new Item(2);
            Item item2 = new Item(3);

            HashSet<Item> hashSet = new HashSet<Item>(new ItemEqualityComparer());
            hashSet.Add(x1);
            hashSet.Add(item1);
            hashSet.Add(item2);
            hashSet.Remove(x1);

            Item item = x1;
            bool contains = hashSet.Contains(item);
            Assert.False(contains); //"Error: Contains returned wrong value"

            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(3), new Item(2) }, hashSet.Comparer);
        }

        //Test 15: Set/Item Relationship Test 15: Item was previously removed from set but in it currently
        [Fact]
        public static void Contains_Test15()
        {
            Item x1 = new Item(1);
            Item item1 = new Item(2);
            Item item2 = new Item(3);

            HashSet<Item> hashSet = new HashSet<Item>(new ItemEqualityComparer());
            hashSet.Add(x1);
            hashSet.Add(item1);
            hashSet.Add(item2);
            hashSet.Remove(x1);
            hashSet.Add(x1);

            Item item = x1;
            bool contains = hashSet.Contains(item);
            Assert.True(contains); //"Error: Contains returned wrong value"

            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { item, new Item(3), new Item(2) }, hashSet.Comparer);
        }
        #endregion

        #region Set/Item Comparer Tests (Tests 16-20 )
        //Test 16: Set/Item Comparer Test 1: item same as element in set by default comparer, different by sets comparer - set contains item that is equal by sets comparer
        [Fact]
        public static void Contains_Test16()
        {
            HashSet<ValueItem> hashSet;
            ValueItem item;

            SetItemComparerTests.SetupTest1(out hashSet, out item);
            bool contains = hashSet.Contains(item);
            Assert.True(contains); //"Error: Contains returned wrong value"

            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[] { item, new ValueItem(5, 4), new ValueItem(5, -5) }, hashSet.Comparer);
        }

        //Test 17: Set/Item Comparer Test 2: item same as element in set by default comparer, different by sets comparer - set does not contain item that is equal by sets comparer
        [Fact]
        public static void Contains_Test17()
        {
            HashSet<ValueItem> hashSet;
            ValueItem item;

            SetItemComparerTests.SetupTest2(out hashSet, out item);
            bool contains = hashSet.Contains(item);
            Assert.False(contains); //"Error: Contains returned wrong value"

            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[] { new ValueItem(5, 4), new ValueItem(5, -5), new ValueItem(-20, -20) }, hashSet.Comparer);
        }

        //Test 18: Set/Item Comparer Test 3: item same as element in set by sets comparer, different by default comparer - set contains item that is equal by default comparer
        [Fact]
        public static void Contains_Test18()
        {
            HashSet<ValueItem> hashSet;
            ValueItem item;

            SetItemComparerTests.SetupTest3(out hashSet, out item);
            bool contains = hashSet.Contains(item);
            Assert.True(contains); //"Error: Contains returned wrong value"

            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[] { item, new ValueItem(5, 4), new ValueItem(5, -5) }, hashSet.Comparer);
        }

        //Test 19: Set/Item Comparer Test 4: item same as element in set by sets comparer, different by default comparer - set does not contain item that is equal by default comparer
        [Fact]
        public static void Contains_Test19()
        {
            HashSet<ValueItem> hashSet;
            ValueItem item;

            SetItemComparerTests.SetupTest4(out hashSet, out item);
            bool contains = hashSet.Contains(item);
            Assert.True(contains); //"Error: Contains returned wrong value"

            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[] { item, new ValueItem(5, 4), new ValueItem(5, -5) }, hashSet.Comparer);
        }

        //Test 20: Set/Item Comparer Test 5: item contains set and item in set with GetSetComparer<T> as comparer
        [Fact]
        public static void Contains_Test20()
        {
            HashSet<HashSet<IEnumerable>> hashSet;
            HashSet<IEnumerable> item;
            ValueItem itemn4 = new ValueItem(-4, -4);
            ValueItem itemn3 = new ValueItem(-3, -3);
            ValueItem itemn2 = new ValueItem(-2, -2);
            ValueItem itemn1 = new ValueItem(-1, -1);
            ValueItem item1 = new ValueItem(1, 1);
            ValueItem item2 = new ValueItem(2, 2);
            ValueItem item3 = new ValueItem(3, 3);
            ValueItem item4 = new ValueItem(4, 4);
            HashSet<IEnumerable> itemhs1 = new HashSet<IEnumerable>(new ValueItem[] { item1, item2, item3, item4 });
            HashSet<IEnumerable> itemhs2 = new HashSet<IEnumerable>(new ValueItem[] { itemn1, itemn2, itemn3, itemn4 });

            SetItemComparerTests.SetupTest5(out hashSet, out item);
            bool contains = hashSet.Contains(item);
            Assert.True(contains); //"Error: Contains returned wrong value"

            HashSet<IEnumerable>[] expected = new HashSet<IEnumerable>[] { itemhs1, itemhs2, item };
            HashSet<IEnumerable>[] actual = new HashSet<IEnumerable>[3];
            hashSet.CopyTo(actual, 0, 3);

            Assert.Equal(3, hashSet.Count); //"Should be equal."
            HashSetTestSupport.HashSetContains(actual, expected);
        }
        #endregion

        //Test 21: call on each item in a multiset
        [Fact]
        public static void Contains_Test21()
        {
            HashSet<int> hashSet = new HashSet<int>(new int[] { 3, -3, -42, int.MinValue, int.MaxValue, 0 });
            Assert.True(hashSet.Contains(3)); //"Should be true."
            Assert.True(hashSet.Contains(-3)); //"Should be true."
            Assert.True(hashSet.Contains(-42)); //"Should be true."
            Assert.True(hashSet.Contains(int.MinValue)); //"Should be true."
            Assert.True(hashSet.Contains(int.MaxValue)); //"Should be true."
            Assert.True(hashSet.Contains(0)); //"Should be true."

            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[] { int.MinValue, -42, -3, 0, 3, int.MaxValue }, EqualityComparer<int>.Default);
        }
    }
}
