// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;
using Tests.HashSet_HashSetTestSupport;
using Tests.HashSet_HashSet_Add_RemoveTests;

namespace Tests
{
    public class HashSet_AddRemoveTests
    {
        #region Set/Item Relationship Tests (Tests 1-16)
        //Test 1: Set/Item Relationship Test 1: set is empty
        [Fact]
        public static void HashSet_Add_Empty()
        {
            HashSet<Item> hashSet = new HashSet<Item>();
            Item item = new Item(1);
            bool added = hashSet.Add(item);
            Assert.True(added); //"Error: Add returned wrong value"
            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { item }, EqualityComparer<Item>.Default);
        }

        [Fact]
        public static void HashSet_Remove_Empty()
        {
            HashSet<Item> hashSet = new HashSet<Item>();
            Item item = new Item(1);
            bool removed = hashSet.Remove(item);
            Assert.False(removed); //"Error: Remove returned wrong value"

            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[0], EqualityComparer<Item>.Default);
        }

        //Test 2: Set/Item Relationship Test 2: set is single-item, item in set
        [Fact]
        public static void HashSet_Single_Add_ItemInSet()
        {
            Item item = new Item(2);
            HashSet<Item> hashSet = new HashSet<Item>();
            hashSet.Add(item);

            bool added = hashSet.Add(item);
            Assert.False(added); //"Error: Add returned wrong value"
            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { item }, EqualityComparer<Item>.Default);
        }

        [Fact]
        public static void HashSet_Single_Remove_ItemInSet()
        {
            Item item = new Item(2);
            HashSet<Item> hashSet = new HashSet<Item>();
            hashSet.Add(item);

            bool removed = hashSet.Remove(item);
            Assert.True(removed); //"Error: Remove returned wrong value"
            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[0], EqualityComparer<Item>.Default);
        }

        //Test 3: Set/Item Relationship Test 3: set is single-item, item not in set
        [Fact]
        public static void HashSet_Single_Add_ItemNotInSet()
        {
            HashSet<Item> hashSet = new HashSet<Item>(new ItemEqualityComparer());
            hashSet.Add(new Item(2));

            Item item = new Item(3);
            bool added = hashSet.Add(item);
            Assert.True(added); //"Error: Add returned wrong value"
            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { item, new Item(2) }, hashSet.Comparer);
        }

        [Fact]
        public static void HashSet_Single_Remove_ItemNotInSet()
        {
            HashSet<Item> hashSet = new HashSet<Item>(new ItemEqualityComparer());
            hashSet.Add(new Item(2));

            Item item = new Item(3);
            bool removed = hashSet.Remove(item);
            Assert.False(removed); //"Error: Remove returned wrong value"

            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(2) }, hashSet.Comparer);
        }

        //Test 4: Set/Item Relationship Test 4: set is multi-item, item in set
        [Fact]
        public static void HashSet_Multi_Add_ItemInSet()
        {
            Item x = new Item(2);
            Item y = new Item(4);
            Item z = new Item(-23);

            HashSet<Item> hashSet = new HashSet<Item>(new ItemEqualityComparer());
            hashSet.Add(x);
            hashSet.Add(y);
            hashSet.Add(z);

            Item item = z;
            bool added = hashSet.Add(item);
            Assert.False(added); //"Error: Add returned wrong value"
            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { item, new Item(2), new Item(4) }, hashSet.Comparer);
        }

        [Fact]
        public static void HashSet_Multi_Remove_ItemInSet()
        {
            Item x = new Item(2);
            Item y = new Item(4);
            Item z = new Item(-23);

            HashSet<Item> hashSet = new HashSet<Item>(new ItemEqualityComparer());
            hashSet.Add(x);
            hashSet.Add(y);
            hashSet.Add(z);

            Item item = z;
            bool removed = hashSet.Remove(item);
            Assert.True(removed); //"Error: Remove returned wrong value"
            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(2), new Item(4) }, hashSet.Comparer);
        }

        //Test 5: Set/Item Relationship Test 5: set is multi-item, item not in set
        [Fact]
        public static void HashSet_Multi_Add_ItemNotInSet()
        {
            Item x = new Item(2);
            Item y = new Item(4);
            Item z = new Item(-23);

            HashSet<Item> hashSet = new HashSet<Item>(new ItemEqualityComparer());
            hashSet.Add(x);
            hashSet.Add(y);
            hashSet.Add(z);

            Item item = new Item(0);
            bool added = hashSet.Add(item);
            Assert.True(added); //"Error: Add returned wrong value"
            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(-23), item, new Item(2), new Item(4) }, hashSet.Comparer);
        }

        [Fact]
        public static void HashSet_Multi_Remove_ItemNotInSet()
        {
            Item x = new Item(2);
            Item y = new Item(4);
            Item z = new Item(-23);

            HashSet<Item> hashSet = new HashSet<Item>(new ItemEqualityComparer());
            hashSet.Add(x);
            hashSet.Add(y);
            hashSet.Add(z);

            Item item = new Item(0);
            bool removed = hashSet.Remove(item);
            Assert.False(removed); //"Error: Remove returned wrong value"

            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(-23), new Item(2), new Item(4) }, hashSet.Comparer);
        }

        //Test 6: Set/Item Relationship Test 6: Item is the set and item is in the set
        [Fact]
        public static void HashSet_Set_Add_ItemInSet()
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

            bool added = hashSet.Add(item);
            Assert.False(added); //"Error: Add returned wrong value"
            HashSetTestSupport<IEnumerable>.VerifyHashSet(hashSet, new IEnumerable[] { item1, item2, item }, hashSet.Comparer);
        }

        [Fact]
        public static void HashSet_Set_Remove_ItemInSet()
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
            bool removed = hashSet.Remove(item);
            Assert.True(removed); //"Error: Remove returned wrong value"
            HashSetTestSupport<IEnumerable>.VerifyHashSet(hashSet, new IEnumerable[] { item1, item2 }, hashSet.Comparer);
        }

        //Test 7: Set/Item Relationship Test 7: Item is the set and item is not in the set
        [Fact]
        public static void HashSet_Set_Add_ItemNotInSet()
        {
            List<int> item1 = new List<int>(new int[] { 1, 2 });
            List<int> item2 = new List<int>(new int[] { 2, -1 });

            IEnumerableEqualityComparer comparer = new IEnumerableEqualityComparer();
            HashSet<IEnumerable> hashSet = new HashSet<IEnumerable>(comparer);
            comparer.setSelf(hashSet);
            hashSet.Add(item1);
            hashSet.Add(item2);

            IEnumerable item = hashSet;
            bool added = hashSet.Add(item);
            Assert.True(added); //"Error: Add returned wrong value"

            HashSetTestSupport<IEnumerable>.VerifyHashSet(hashSet, new IEnumerable[] { item1, item2, item }, hashSet.Comparer);
        }

        [Fact]
        public static void HashSet_Set_Remove_ItemNotInSet()
        {
            List<int> item1 = new List<int>(new int[] { 1, 2 });
            List<int> item2 = new List<int>(new int[] { 2, -1 });

            IEnumerableEqualityComparer comparer = new IEnumerableEqualityComparer();
            HashSet<IEnumerable> hashSet = new HashSet<IEnumerable>(comparer);
            comparer.setSelf(hashSet);
            hashSet.Add(item1);
            hashSet.Add(item2);

            IEnumerable item = hashSet;
            bool removed = hashSet.Remove(item);
            Assert.False(removed); //"Error: Remove returned wrong value"

            HashSetTestSupport<IEnumerable>.VerifyHashSet(hashSet, new IEnumerable[] { item1, item2 }, hashSet.Comparer);
        }

        //Test 8: Set/Item Relationship Test 8: Item is Default<T> and in set. T is a numeric type
        [Fact]
        public static void HashSet_Add_T_Default()
        {
            int x = 0;
            HashSet<int> hashSet = new HashSet<int>(new int[] { 1, 2, 3, 4, x, 6, 7 });
            int item = x;
            bool added = hashSet.Add(item);
            Assert.False(added); //"Error: Add returned wrong value"
            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[] { 1, 2, 3, 6, 7, 4, 0 }, hashSet.Comparer);
        }

        [Fact]
        public static void HashSet_Remove_T_Default()
        {
            int x = 0;
            HashSet<int> hashSet = new HashSet<int>(new int[] { 1, 2, 3, 4, x, 6, 7 });
            int item = x;
            bool removed = hashSet.Remove(item);
            Assert.True(removed); //"Error: Remove returned wrong value"

            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[] { 1, 2, 3, 6, 7, 4 }, hashSet.Comparer);
        }

        //Test 9: Set/Item Relationship Test 9: Item is Default<T> and in set. T is a reference type
        [Fact]
        public static void HashSet_Add_T_Reference()
        {
            Item x = null;
            Item item1 = new Item(3);
            Item item2 = new Item(-3);

            HashSet<Item> hashSet = new HashSet<Item>(new ItemEqualityComparer());
            hashSet.Add(item1);
            hashSet.Add(item2);
            hashSet.Add(x);

            Item item = x;
            bool added = hashSet.Add(item);
            Assert.False(added); //"Error: Add returned wrong value"
            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { item, new Item(3), new Item(-3) }, hashSet.Comparer);
        }

        [Fact]
        public static void HashSet_Remove_T_Reference()
        {
            Item x = null;
            Item item1 = new Item(3);
            Item item2 = new Item(-3);

            HashSet<Item> hashSet = new HashSet<Item>(new ItemEqualityComparer());
            hashSet.Add(item1);
            hashSet.Add(item2);
            hashSet.Add(x);

            Item item = x;
            bool removed = hashSet.Remove(item);
            Assert.True(removed); //"Error: Remove returned wrong value"
            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(3), new Item(-3) }, hashSet.Comparer);
        }

        //Test 10: Set/Item Relationship Test 10: Item is Default<T> and not in set. T is a numeric type
        [Fact]
        public static void HashSet_Add_T_Default_NotInSet()
        {
            HashSet<int> hashSet = new HashSet<int>(new int[] { 1, 2, 3, 4, 5, 6, 7 });
            int item = 0;
            bool added = hashSet.Add(item);
            Assert.True(added); //"Error: Add returned wrong value"
            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[] { 1, 2, 3, 6, 7, 4, 0, 5 }, hashSet.Comparer);
        }

        [Fact]
        public static void HashSet_Remove_T_Default_NotInSet()
        {
            HashSet<int> hashSet = new HashSet<int>(new int[] { 1, 2, 3, 4, 5, 6, 7 });
            int item = 0;
            bool removed = hashSet.Remove(item);
            Assert.False(removed); //"Error: Remove returned wrong value"

            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[] { 1, 2, 3, 6, 7, 4, 5 }, hashSet.Comparer);
        }

        //Test 11: Set/Item Relationship Test 11: Item is Default<T> and not in set.  T is a reference type
        [Fact]
        public static void HashSet_Add_T_Reference_NotInSet()
        {
            Item item1 = new Item(3);
            Item item2 = new Item(-3);

            HashSet<Item> hashSet = new HashSet<Item>(new ItemEqualityComparer());
            hashSet.Add(item1);
            hashSet.Add(item2);

            Item item = null;
            bool added = hashSet.Add(item);
            Assert.True(added); //"Error: Add returned wrong value"
            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { item, new Item(3), new Item(-3) }, hashSet.Comparer);
        }

        [Fact]
        public static void HashSet_Remove_T_Reference_NotInSet()
        {
            Item item1 = new Item(3);
            Item item2 = new Item(-3);

            HashSet<Item> hashSet = new HashSet<Item>(new ItemEqualityComparer());
            hashSet.Add(item1);
            hashSet.Add(item2);

            Item item = null;
            bool removed = hashSet.Remove(item);
            Assert.False(removed); //"Error: Remove returned wrong value"
            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(3), new Item(-3) }, hashSet.Comparer);
        }

        //Test 12: Set/Item Relationship Test 12: Item is equal to an item in set but different
        [Fact]
        public static void HashSet_Add_EqualAndDifferent()
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
            bool added = hashSet.Add(item);
            Assert.False(added); //"Error: Add returned wrong value"

            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { item, new Item(3), new Item(2) }, hashSet.Comparer);
        }

        [Fact]
        public static void HashSet_Remove_EqualAndDifferent()
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
            bool removed = hashSet.Remove(item);
            Assert.True(removed); //"Error: Remove returned wrong value"
            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(3), new Item(2) }, hashSet.Comparer);
        }

        //Test 13: Set/Item Relationship Test 13: Item shares hash value with unequal item in set
        [Fact]
        public static void HashSet_Add_NotEqualSameHash()
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
            bool added = hashSet.Add(item);
            Assert.True(added); //"Error: Add returned wrong value"
            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { item, new Item(3), new Item(2), new Item(1) }, hashSet.Comparer);
        }

        [Fact]
        public static void HashSet_Remove_NotEqualSameHash()
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
            bool removed = hashSet.Remove(item);
            Assert.False(removed); //"Error: Remove returned wrong value"
            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(3), new Item(2), new Item(1) }, hashSet.Comparer);
        }

        //Test 14: Set/Item Relationship Test 14: Item was previously in set but not currently
        [Fact]
        public static void HashSet_Add_ItemRemovedBefore()
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
            bool added = hashSet.Add(item);
            Assert.True(added); //"Error: Add returned wrong value"
            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { item, new Item(3), new Item(2) }, hashSet.Comparer);
        }

        [Fact]
        public static void HashSet_Remove_ItemRemovedBefore()
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
            bool removed = hashSet.Remove(item);
            Assert.False(removed); //"Error: Remove returned wrong value"

            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(3), new Item(2) }, hashSet.Comparer);
        }

        #endregion

        #region Set/Item Comparer Tests (Tests 16-20)

        //Test 16: Set/Item Comparer Test 1: item same as element in set by default comparer, different by sets comparer - set contains item that is equal by sets comparer
        [Fact]
        public static void HashSet_Add_SetComparer_SetHasItem()
        {
            ValueItem item1 = new ValueItem(34, -5);
            ValueItem item2 = new ValueItem(4, 4);
            ValueItem item3 = new ValueItem(9999, -2);

            HashSet<ValueItem> hashSet = new HashSet<ValueItem>(new ValueItemYEqualityComparer());
            hashSet.Add(item1);
            hashSet.Add(item2);
            hashSet.Add(item3);

            ValueItem item = new ValueItem(34, -2);
            bool added = hashSet.Add(item);
            Assert.False(added); //"Error: Add returned wrong value"
            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[] { item, new ValueItem(5, 4), new ValueItem(5, -5) }, hashSet.Comparer);
        }

        [Fact]
        public static void HashSet_Remove_SetComparer_SetHasItem()
        {
            ValueItem item1 = new ValueItem(34, -5);
            ValueItem item2 = new ValueItem(4, 4);
            ValueItem item3 = new ValueItem(9999, -2);

            HashSet<ValueItem> hashSet = new HashSet<ValueItem>(new ValueItemYEqualityComparer());
            hashSet.Add(item1);
            hashSet.Add(item2);
            hashSet.Add(item3);

            ValueItem item = new ValueItem(34, -2);
            bool removed = hashSet.Remove(item);
            Assert.True(removed); //"Error: Remove returned wrong value"

            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[] { new ValueItem(5, 4), new ValueItem(5, -5) }, hashSet.Comparer);
        }

        //Test 18: Set/Item Comparer Test 3: item same as element in set by sets comparer, different by default comparer - set contains item that is equal by default comparer
        [Fact]
        public static void HashSet_Add_DefaultComparer_SetHasItem()
        {
            ValueItem item1 = new ValueItem(34, -5);
            ValueItem item2 = new ValueItem(4, 4);
            ValueItem item3 = new ValueItem(9999, -2);

            HashSet<ValueItem> hashSet = new HashSet<ValueItem>();
            hashSet.Add(item1);
            hashSet.Add(item2);
            hashSet.Add(item3);

            ValueItem item = item3;
            bool added = hashSet.Add(item);
            Assert.False(added); //"Error: Add returned wrong value"

            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[] { item1, item2, item3 }, hashSet.Comparer);
        }

        [Fact]
        public static void HashSet_Remove_DefaultComparer_SetHasItem()
        {
            ValueItem item1 = new ValueItem(34, -5);
            ValueItem item2 = new ValueItem(4, 4);
            ValueItem item3 = new ValueItem(9999, -2);

            HashSet<ValueItem> hashSet = new HashSet<ValueItem>();
            hashSet.Add(item1);
            hashSet.Add(item2);
            hashSet.Add(item3);

            ValueItem item = item2;
            bool removed = hashSet.Remove(item);
            Assert.True(removed); //"Error: Remove returned wrong value"

            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[] { item1, item3 }, hashSet.Comparer);
        }

        //Test 19: Set/Item Comparer Test 4: item same as element in set by sets comparer, different by default comparer - set does not contain item that is equal by default comparer
        [Fact]
        public static void HashSet_Add_DefaultComparer_SetDoesNotHaveItem()
        {
            ValueItem item1 = new ValueItem(340, -5);
            ValueItem item2 = new ValueItem(4, 4);
            ValueItem item3 = new ValueItem(9999, -2);

            HashSet<ValueItem> hashSet = new HashSet<ValueItem>();
            hashSet.Add(item1);
            hashSet.Add(item2);
            hashSet.Add(item3);

            ValueItem item = new ValueItem(34, -2);
            bool added = hashSet.Add(item);
            Assert.True(added); //"Error: Add returned wrong value"

            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[] { item, item1, item2, item3 }, hashSet.Comparer);
        }

        [Fact]
        public static void HashSet_Remove_DefaultComparer_SetDoesNotHaveItem()
        {
            ValueItem item1 = new ValueItem(340, -5);
            ValueItem item2 = new ValueItem(4, 4);
            ValueItem item3 = new ValueItem(9999, -2);

            HashSet<ValueItem> hashSet = new HashSet<ValueItem>();
            hashSet.Add(item1);
            hashSet.Add(item2);
            hashSet.Add(item3);

            ValueItem item = new ValueItem(34, -2);
            bool removed = hashSet.Remove(item);
            Assert.False(removed); //"Error: Remove returned wrong value"

            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[] { item1, item2, item3 }, hashSet.Comparer);
        }

        //Test 20: Set/Item Comparer Test 5: item contains set and item in set with GetSetComparer<T> as comparer
        [Fact]
        public static void HashSet_Add_T_Comparer_SetHasItem()
        {
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
            HashSet<IEnumerable> set = new HashSet<IEnumerable>();
            HashSet<HashSet<IEnumerable>> outerSet = new HashSet<HashSet<IEnumerable>>(new SetEqualityComparer<IEnumerable>());

            set.Add(outerSet);
            outerSet.Add(itemhs1);
            outerSet.Add(itemhs2);
            outerSet.Add(set);

            HashSet<IEnumerable> item = set;
            bool added = outerSet.Add(item);
            Assert.False(added); //"Error: Add returned wrong value"

            HashSet<IEnumerable>[] expected = new HashSet<IEnumerable>[] { itemhs1, itemhs2, item };
            HashSet<IEnumerable>[] actual = new HashSet<IEnumerable>[3];
            outerSet.CopyTo(actual, 0, 3);
            Assert.Equal(3, outerSet.Count); //"Should be the same."
            HashSetTestSupport.HashSetContains(actual, expected);
        }

        [Fact]
        public static void HashSet_Remove_T_Comparer_SetHasItem()
        {
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
            HashSet<IEnumerable> set = new HashSet<IEnumerable>();
            HashSet<HashSet<IEnumerable>> outerSet = new HashSet<HashSet<IEnumerable>>(new SetEqualityComparer<IEnumerable>());

            set.Add(outerSet);
            outerSet.Add(itemhs1);
            outerSet.Add(itemhs2);
            outerSet.Add(set);

            bool removed = outerSet.Remove(set);
            Assert.True(removed); //"Error: Remove returned wrong value"

            HashSet<IEnumerable>[] expected = new HashSet<IEnumerable>[] { itemhs1, itemhs2 };
            HashSet<IEnumerable>[] actual = new HashSet<IEnumerable>[2];
            outerSet.CopyTo(actual, 0, 2);

            Assert.Equal(2, outerSet.Count); //"Expect it to be the same."
            HashSetTestSupport.HashSetContains(actual, expected);
        }

        //Test 21: Add multiple items
        [Fact]
        public static void HashSet_Add_MultipleItems()
        {
            HashSet<int> hashSet = new HashSet<int>();
            Assert.True(hashSet.Add(3)); //"Should have been added"
            Assert.True(hashSet.Add(-3)); //"Should have been added"
            Assert.True(hashSet.Add(-42)); //"Should have been added"
            Assert.True(hashSet.Add(int.MinValue)); //"Should have been added"
            Assert.True(hashSet.Add(int.MaxValue)); //"Should have been added"
            Assert.True(hashSet.Add(0)); //"Should have been added"

            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[] { int.MinValue, -42, -3, 0, 3, int.MaxValue }, EqualityComparer<int>.Default);
        }
        //Test 22: Add many items with the same hash value

        [Fact]
        public static void HashSet_Add_MultipleItems_SameHashValue()
        {
            HashSet<Item> hashSet = new HashSet<Item>(new HashAlwaysZeroItemComparer());
            Assert.True(hashSet.Add(new Item(3))); //"Should have been added."
            Assert.True(hashSet.Add(new Item(-3))); //"Should have been added"
            Assert.True(hashSet.Add(new Item(-42))); //"Should have been added"
            Assert.True(hashSet.Add(new Item(int.MinValue))); //"Should have been added"
            Assert.True(hashSet.Add(new Item(int.MaxValue))); ; //"Should have been added"
            Assert.True(hashSet.Add(new Item(0))); ; //"Should have been added"
            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(int.MinValue), new Item(-42), new Item(-3), new Item(0), new Item(3), new Item(int.MaxValue) }, hashSet.Comparer);
        }

        //Test 23: REMOVE call on each item in a multiset
        [Fact]
        public static void HashSet_Remove_MultipleItems()
        {
            HashSet<int> hashSet = new HashSet<int>(new int[] { 3, -3, -42, int.MinValue, int.MaxValue, 0 });
            Assert.True(hashSet.Remove(3)); //"Should have been removed."
            Assert.True(hashSet.Remove(-3)); //"Should have been removed."
            Assert.True(hashSet.Remove(-42)); //"Should have been removed."
            Assert.True(hashSet.Remove(int.MinValue)); //"Should have been removed."
            Assert.True(hashSet.Remove(int.MaxValue)); //"Should have been removed."
            Assert.True(hashSet.Remove(0)); //"Should have been removed."
            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[0], EqualityComparer<int>.Default);
        }

        #endregion
    }
    namespace HashSet_HashSet_Add_RemoveTests
    {
        #region Helper Classes

        public class HashAlwaysZeroItemComparer : IEqualityComparer<Item>
        {
            public bool Equals(Item x, Item y)
            {
                if ((x != null) & (y != null))
                    return (x.x == y.x);
                else
                    return ((y == null) & (x == null));
            }

            public int GetHashCode(Item x)
            {
                return 0;
            }
        }
    }
    #endregion
}
