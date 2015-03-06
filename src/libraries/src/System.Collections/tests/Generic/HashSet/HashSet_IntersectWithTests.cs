// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;
using Tests.HashSet_HashSetTestSupport;
using Tests.HashSet_SetCollectionRelationshipTests;
using Tests.HashSet_SetCollectionComparerTests;
using Tests.HashSet_SetCollectionDuplicateItemTests;

namespace Tests
{
    public class HashSet_IntersectWithTests
    {
        #region Set/Collection Relationship Tests (tests 1-42)
        //Test 1: other is null
        [Fact]
        public static void IsIntersectWith_Test1()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;

            SetCollectionRelationshipTests.SetupTest1(out hashSet, out other);
            Assert.Throws<ArgumentNullException>(() => hashSet.IntersectWith(other)); //"ArgumenNullException expected."
        }

        //Test 2: other is empty and set is empty
        [Fact]
        public static void IsIntersectWith_Test2()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;

            SetCollectionRelationshipTests.SetupTest2(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[0], hashSet.Comparer);
        }

        //Test 3: Set/Collection Relationship Test 3: other is empty and set is single-item
        [Fact]
        public static void IsIntersectWith_Test3()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;

            SetCollectionRelationshipTests.SetupTest3(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[0], hashSet.Comparer);
        }

        //Test 4: Set/Collection Relationship Test 4: other is empty and set is multi-item 
        [Fact]
        public static void IsIntersectWith_Test4()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;

            SetCollectionRelationshipTests.SetupTest4(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[0], hashSet.Comparer);
        }

        //Test 5: Set/Collection Relationship Test 5: other is single-item and set is empty
        [Fact]
        public static void IsIntersectWith_Test5()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;

            SetCollectionRelationshipTests.SetupTest5(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[0], hashSet.Comparer);
        }

        //Test 6: Set/Collection Relationship Test 6: other is single-item and set is single-item with a different item
        [Fact]
        public static void IsIntersectWith_Test6()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;

            SetCollectionRelationshipTests.SetupTest6(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[0], hashSet.Comparer);
        }

        //Test 7: Set/Collection Relationship Test 7: other is single-item and set is single-item with the same item 
        [Fact]
        public static void IsIntersectWith_Test7()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;

            SetCollectionRelationshipTests.SetupTest7(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(-23) }, hashSet.Comparer);
        }

        //Test 8: Set/Collection Relationship Test 8: other is single-item and set is multi-item where set and other are disjoint
        [Fact]
        public static void IsIntersectWith_Test8()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;

            SetCollectionRelationshipTests.SetupTest8(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[0], hashSet.Comparer);
        }

        //Test 9: Set/Collection Relationship Test 9: other is single-item and set is multi-item where set contains other
        [Fact]
        public static void IsIntersectWith_Test9()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;

            SetCollectionRelationshipTests.SetupTest9(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(234) }, hashSet.Comparer);
        }

        //Test 10: Set/Collection Relationship Test 10: other is multi-item and set is empty
        [Fact]
        public static void IsIntersectWith_Test10()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;

            SetCollectionRelationshipTests.SetupTest10(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[0], hashSet.Comparer);
        }

        //Test 11: Set/Collection Relationship Test 11: other is multi-item and set is single-item and set and other are disjoint
        [Fact]
        public static void IsIntersectWith_Test11()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;

            SetCollectionRelationshipTests.SetupTest11(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[0], hashSet.Comparer);
        }

        //Test 12: Set/Collection Relationship Test 12: other is multi-item and set is single-item and other contains set
        [Fact]
        public static void IsIntersectWith_Test12()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;

            SetCollectionRelationshipTests.SetupTest12(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(0) }, hashSet.Comparer);
        }

        //Test 13: Set/Collection Relationship Test 13: other is multi-item and set is multi-item and set and other disjoint
        [Fact]
        public static void IsIntersectWith_Test13()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;

            SetCollectionRelationshipTests.SetupTest13(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[0], hashSet.Comparer);
        }

        //Test 14: Set/Collection Relationship Test 14: other is multi-item and set is multi-item and set and other overlap but are non-comparable
        [Fact]
        public static void IsIntersectWith_Test14()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;

            SetCollectionRelationshipTests.SetupTest14(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(-23), new Item(234) }, hashSet.Comparer);
        }

        //Test 15: Set/Collection Relationship Test 15: other is multi-item and set is multi-item and other is a proper subset of set
        [Fact]
        public static void IsIntersectWith_Test15()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;

            SetCollectionRelationshipTests.SetupTest15(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(-23), new Item(0), new Item(234) }, hashSet.Comparer);
        }

        //Test 16: Set/Collection Relationship Test 16: other is multi-item and set is multi-item and set is a proper subset of other
        [Fact]
        public static void IsIntersectWith_Test16()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;

            SetCollectionRelationshipTests.SetupTest16(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(0), new Item(23), new Item(222) }, hashSet.Comparer);
        }

        //Test 17: Set/Collection Relationship Test 17: other is multi-item and set is multi-item and set and other are equal
        [Fact]
        public static void IsIntersectWith_Test17()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;

            SetCollectionRelationshipTests.SetupTest17(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(-23), new Item(0), new Item(23), new Item(222), new Item(234) }, hashSet.Comparer);
        }

        //Test 18: Set/Collection Relationship Test 18: other is set and set is empty
        [Fact]
        public static void IsIntersectWith_Test18()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;

            SetCollectionRelationshipTests.SetupTest18(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[0], hashSet.Comparer);
        }

        //Test 19: Set/Collection Relationship Test 19: other is set and set is single-item and set contains set
        [Fact]
        public static void IsIntersectWith_Test19()
        {
            HashSet<IEnumerable> hashSet;
            IEnumerable<IEnumerable> other;

            SetCollectionRelationshipTests.SetupTest19(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<IEnumerable>.VerifyHashSet(hashSet, new IEnumerable[] { other }, hashSet.Comparer);
        }

        //Test 20: Set/Collection Relationship Test 20: other is set and set is single-item and set does not contain set
        [Fact]
        public static void IsIntersectWith_Test20()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;

            SetCollectionRelationshipTests.SetupTest20(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(22) }, hashSet.Comparer);
        }

        //Test 21: Set/Collection Relationship Test 21: other is set and set is multi-item and set contains set
        [Fact]
        public static void IsIntersectWith_Test21()
        {
            List<int> item1 = new List<int>(new int[] { 1, 3, 5, -2 });
            List<int> item2 = new List<int>(new int[] { 1, -3, 5, -2 });
            HashSet<IEnumerable> hashSet;
            IEnumerable<IEnumerable> other;

            SetCollectionRelationshipTests.SetupTest21(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<IEnumerable>.VerifyHashSet(hashSet, new IEnumerable[] { other, item1, item2 }, hashSet.Comparer);
        }

        //Test 22: Set/Collection Relationship Test 22: other is set and set is multi-item and set does not contain set
        [Fact]
        public static void IsIntersectWith_Test22()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;

            SetCollectionRelationshipTests.SetupTest22(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(-2222), new Item(-2), new Item(22) }, hashSet.Comparer);
        }

        //Test 23: Set/Collection Relationship Test 23: item is only item in other: Item is the set and item is in the set
        [Fact]
        public static void IsIntersectWith_Test23()
        {
            HashSet<IEnumerable> hashSet;
            IEnumerable<IEnumerable> other;

            SetCollectionRelationshipTests.SetupTest23(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<IEnumerable>.VerifyHashSet(hashSet, new IEnumerable[] { hashSet }, hashSet.Comparer);
        }

        //Test 24: Set/Collection Relationship Test 24:  item is only item in other: Item is the set and item is not in the set
        [Fact]
        public static void IsIntersectWith_Test24()
        {
            HashSet<IEnumerable> hashSet;
            IEnumerable<IEnumerable> other;

            SetCollectionRelationshipTests.SetupTest24(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<IEnumerable>.VerifyHashSet(hashSet, new IEnumerable[0], hashSet.Comparer);
        }

        //Test 25: Set/Collection Relationship Test 25:  item is only item in other: Item is Default<T> and in set. T is a numeric type
        [Fact]
        public static void IsIntersectWith_Test25()
        {
            HashSet<int> hashSet;
            IEnumerable<int> other;

            SetCollectionRelationshipTests.SetupTest25(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[] { 0 }, hashSet.Comparer);
        }

        //Test 26: Set/Collection Relationship Test 26:  item is only item in other: Item is Default<T> and in set. T is a reference type
        [Fact]
        public static void IsIntersectWith_Test26()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;

            SetCollectionRelationshipTests.SetupTest26(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { null }, hashSet.Comparer);
        }

        //Test 27: Set/Collection Relationship Test 27:  item is only item in other: Item is Default<T> and not in set. T is a numeric type
        [Fact]
        public static void IsIntersectWith_Test27()
        {
            HashSet<int> hashSet;
            IEnumerable<int> other;

            SetCollectionRelationshipTests.SetupTest27(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[0], hashSet.Comparer);
        }

        //Test 28: Set/Collection Relationship Test 28:  item is only item in other: Item is Default<T> and not in set.  T is a reference type
        [Fact]
        public static void IsIntersectWith_Test28()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;

            SetCollectionRelationshipTests.SetupTest28(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[0], hashSet.Comparer);
        }

        //Test 29: Set/Collection Relationship Test 29:  item is only item in other: Item is equal to an item in set but different.
        [Fact]
        public static void IsIntersectWith_Test29()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;

            SetCollectionRelationshipTests.SetupTest29(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(1) }, hashSet.Comparer);
        }

        //Test 30: Set/Collection Relationship Test 30:  item is only item in other: Item shares hash value with unequal item in set
        [Fact]
        public static void IsIntersectWith_Test30()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;

            SetCollectionRelationshipTests.SetupTest30(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[0], hashSet.Comparer);
        }

        //Test 31: Set/Collection Relationship Test 31:  item is only item in other: Item was previously in set but not currently
        [Fact]
        public static void IsIntersectWith_Test31()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;

            SetCollectionRelationshipTests.SetupTest31(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[0], hashSet.Comparer);
        }

        //Test 32: Set/Collection Relationship Test 32:  item is only item in other: Item was previously removed from set but in it currently
        [Fact]
        public static void IsIntersectWith_Test32()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;

            SetCollectionRelationshipTests.SetupTest32(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(1) }, hashSet.Comparer);
        }

        //Test 33: Set/Collection Relationship Test 33: item is one of the items in other: Item is the set and item is in the set
        [Fact]
        public static void IsIntersectWith_Test33()
        {
            HashSet<IEnumerable> hashSet;
            IEnumerable<IEnumerable> other;

            SetCollectionRelationshipTests.SetupTest33(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<IEnumerable>.VerifyHashSet(hashSet, new IEnumerable[] { hashSet }, hashSet.Comparer);
        }

        //Test 34: Set/Collection Relationship Test 34: item is one of the items in other: Item is the set and item is not in the set
        [Fact]
        public static void IsIntersectWith_Test34()
        {
            HashSet<IEnumerable> hashSet;
            IEnumerable<IEnumerable> other;

            SetCollectionRelationshipTests.SetupTest34(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<IEnumerable>.VerifyHashSet(hashSet, new IEnumerable[0], hashSet.Comparer);
        }

        //Test 35: Set/Collection Relationship Test 35: item is one of the items in other: Item is Default<T> and in set. T is a numeric type
        [Fact]
        public static void IsIntersectWith_Test35()
        {
            HashSet<int> hashSet;
            IEnumerable<int> other;

            SetCollectionRelationshipTests.SetupTest35(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[] { 0 }, hashSet.Comparer);
        }

        //Test 36: Set/Collection Relationship Test 36: item is one of the items in other: Item is Default<T> and in set. T is a reference type 
        [Fact]
        public static void IsIntersectWith_Test36()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;

            SetCollectionRelationshipTests.SetupTest36(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { null }, hashSet.Comparer);
        }

        //Test 37: Set/Collection Relationship Test 37: item is one of the items in other: Item is Default<T> and not in set. T is a numeric type
        [Fact]
        public static void IsIntersectWith_Test37()
        {
            HashSet<int> hashSet;
            IEnumerable<int> other;

            SetCollectionRelationshipTests.SetupTest37(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[0], hashSet.Comparer);
        }

        //Test 38: Set/Collection Relationship Test 38: item is one of the items in other: Item is Default<T> and not in set.  T is a reference type
        [Fact]
        public static void IsIntersectWith_Test38()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;

            SetCollectionRelationshipTests.SetupTest38(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[0], hashSet.Comparer);
        }

        //Test 39: Set/Collection Relationship Test 39: item is one of the items in other: Item is equal to an item in set but different.
        [Fact]
        public static void IsIntersectWith_Test39()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;

            SetCollectionRelationshipTests.SetupTest39(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(1) }, hashSet.Comparer);
        }

        //Test 40: Set/Collection Relationship Test 40: item is one of the items in other: Item shares hash value with unequal item in set
        [Fact]
        public static void IsIntersectWith_Test40()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;

            SetCollectionRelationshipTests.SetupTest40(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[0], hashSet.Comparer);
        }

        //Test 41: Set/Collection Relationship Test 41: item is one of the items in other: Item was previously in set but not currently 
        [Fact]
        public static void IsIntersectWith_Test41()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;

            SetCollectionRelationshipTests.SetupTest41(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[0], hashSet.Comparer);
        }

        //Test 42: Set/Collection Relationship Test 42: item is one of the items in other: Item was previously removed from set but in it currently
        [Fact]
        public static void IsIntersectWith_Test42()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;

            SetCollectionRelationshipTests.SetupTest42(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(1) }, hashSet.Comparer);
        }
        #endregion

        #region Set/Collection Comparer Tests (tests 43-57)
        //Test 43: Set/Collection Comparer Test 1: Item is in collection: item same as element in set by default comparer, different by sets comparer - set contains item that is equal by sets comparer
        [Fact]
        public static void IsIntersectWith_Test43()
        {
            HashSet<ValueItem> hashSet;
            IEnumerable<ValueItem> other;
            ValueItem item3 = new ValueItem(9999, -2);

            SetCollectionComparerTests.SetupTest1(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[] { item3 }, hashSet.Comparer);
        }

        //Test 44: Set/Collection Comparer Test 2: Item is in collection: item same as element in set by default comparer, different by sets comparer - set does not contain item that is equal by sets comparer
        [Fact]
        public static void IsIntersectWith_Test44()
        {
            HashSet<ValueItem> hashSet;
            IEnumerable<ValueItem> other;

            SetCollectionComparerTests.SetupTest2(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[0], hashSet.Comparer);
        }

        //Test 45: Set/Collection Comparer Test 3: Item is in collection: item same as element in set by sets comparer, different by default comparer - set contains item that is equal by default comparer
        [Fact]
        public static void IsIntersectWith_Test45()
        {
            HashSet<ValueItem> hashSet;
            IEnumerable<ValueItem> other;
            ValueItem item3 = new ValueItem(9999, -2);

            SetCollectionComparerTests.SetupTest3(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[] { item3 }, hashSet.Comparer);
        }

        //Test 46: Set/Collection Comparer Test 4: Item is in collection: item same as element in set by sets comparer, different by default comparer - set does not contain item that is equal by default comparer
        [Fact]
        public static void IsIntersectWith_Test46()
        {
            HashSet<ValueItem> hashSet;
            IEnumerable<ValueItem> other;
            ValueItem item3 = new ValueItem(9999, -2);

            SetCollectionComparerTests.SetupTest4(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[] { item3 }, hashSet.Comparer);
        }

        //Test 48: Set/Collection Comparer Test 6: Item is only item in collection: item same as element in set by default comparer, different by sets comparer - set contains item that is equal by sets comparer
        [Fact]
        public static void IsIntersectWith_Test48()
        {
            HashSet<ValueItem> hashSet;
            IEnumerable<ValueItem> other;
            ValueItem item3 = new ValueItem(9999, -2);

            SetCollectionComparerTests.SetupTest6(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[] { item3 }, hashSet.Comparer);
        }

        //Test 49: Set/Collection Comparer Test 7: Item is only item in collection: item same as element in set by default comparer, different by sets comparer - set does not contain item that is equal by sets comparer
        [Fact]
        public static void IsIntersectWith_Test49()
        {
            HashSet<ValueItem> hashSet;
            IEnumerable<ValueItem> other;

            SetCollectionComparerTests.SetupTest7(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[0], hashSet.Comparer);
        }

        //Test 50: Set/Collection Comparer Test 8: Item is only item in collection: item same as element in set by sets comparer, different by default comparer - set contains item that is equal by default comparer
        [Fact]
        public static void IsIntersectWith_Test50()
        {
            HashSet<ValueItem> hashSet;
            IEnumerable<ValueItem> other;
            ValueItem item3 = new ValueItem(9999, -2);

            SetCollectionComparerTests.SetupTest8(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[] { item3 }, hashSet.Comparer);
        }

        //Test 51: Set/Collection Comparer Test 9: Item is only item in collection: item same as element in set by sets comparer, different by default comparer - set does not contain item that is equal by default comparer
        [Fact]
        public static void IsIntersectWith_Test51()
        {
            HashSet<ValueItem> hashSet;
            IEnumerable<ValueItem> other;
            ValueItem item3 = new ValueItem(9999, -2);

            SetCollectionComparerTests.SetupTest9(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[] { item3 }, hashSet.Comparer);
        }

        //Test 52: Set/Collection Comparer Test 10: Item is only item in collection: item contains set and item in set with GetSetComparer<T> as comparer
        [Fact]
        public static void IsIntersectWith_Test52()
        {
            HashSet<IEnumerable> inneritem = new HashSet<IEnumerable>();

            HashSet<HashSet<IEnumerable>> hashSet;
            IEnumerable<HashSet<IEnumerable>> other;

            SetCollectionComparerTests.SetupTest10(out hashSet, out other);
            inneritem.Add(hashSet);
            hashSet.IntersectWith(other);

            HashSet<IEnumerable>[] expected = new HashSet<IEnumerable>[] { inneritem };
            HashSet<IEnumerable>[] actual = new HashSet<IEnumerable>[1];
            hashSet.CopyTo(actual, 0, 1);

            Assert.Equal(1, hashSet.Count); //"Should be equal"
            HashSetTestSupport.HashSetContains(actual, expected);
        }

        //Test 53: Set/Collection Comparer Test 11: Item is collection: item same as element in set by default comparer, different by sets comparer - set contains item that is equal by sets comparer 
        [Fact]
        public static void IsIntersectWith_Test53()
        {
            HashSet<ValueItem> hashSet;
            IEnumerable<ValueItem> other;
            ValueItem item3 = new ValueItem(9999, -2);

            SetCollectionComparerTests.SetupTest11(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[] { item3 }, hashSet.Comparer);
        }

        //Test 54: Set/Collection Comparer Test 12: Item is collection: item same as element in set by default comparer, different by sets comparer - set does not contain item that is equal by sets comparer
        [Fact]
        public static void IsIntersectWith_Test54()
        {
            HashSet<ValueItem> hashSet;
            IEnumerable<ValueItem> other;

            SetCollectionComparerTests.SetupTest12(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[0], hashSet.Comparer);
        }

        //Test 55: Set/Collection Comparer Test 13: Item is collection: item same as element in set by sets comparer, different by default comparer - set contains item that is equal by default comparer
        [Fact]
        public static void IsIntersectWith_Test55()
        {
            HashSet<ValueItem> hashSet;
            IEnumerable<ValueItem> other;
            ValueItem item3 = new ValueItem(9999, -2);

            SetCollectionComparerTests.SetupTest13(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[] { item3 }, hashSet.Comparer);
        }

        //Test 56: Set/Collection Comparer Test 14: Item is collection: item same as element in set by sets comparer, different by default comparer - set does not contain item that is equal by default comparer
        [Fact]
        public static void IsIntersectWith_Test56()
        {
            HashSet<ValueItem> hashSet;
            IEnumerable<ValueItem> other;
            ValueItem item3 = new ValueItem(9999, -2);

            SetCollectionComparerTests.SetupTest14(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[] { item3 }, hashSet.Comparer);
        }

        //Test 57: Set/Collection Comparer Test 15: Item is collection: item contains set and item in set with GetSetComparer<T> as comparer
        [Fact]
        public static void IsIntersectWith_Test57()
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
            HashSet<IEnumerable> inneritem = new HashSet<IEnumerable>();

            HashSet<HashSet<IEnumerable>> hashSet;
            IEnumerable<HashSet<IEnumerable>> other;

            SetCollectionComparerTests.SetupTest15(out hashSet, out other);
            inneritem.Add(hashSet);
            hashSet.IntersectWith(other);

            HashSet<IEnumerable>[] expected = new HashSet<IEnumerable>[] { itemhs1, itemhs2, inneritem };
            HashSet<IEnumerable>[] actual = new HashSet<IEnumerable>[3];
            hashSet.CopyTo(actual, 0, 3);

            Assert.Equal(3, hashSet.Count); //"Should be equal"
            HashSetTestSupport.HashSetContains(actual, expected);
        }
        #endregion

        #region Set/Collection Duplicate Item Tests (tests 58-73)
        //Test 58: Set/Collection Duplicate Item Test 1: other collection is multi-item with duplicates, set is empty
        [Fact]
        public static void IsIntersectWith_Test58()
        {
            HashSet<ValueItem> hashSet;
            IEnumerable<ValueItem> other;

            SetCollectionDuplicateItemTests.SetupTest1(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[0], hashSet.Comparer);
        }

        //Test 59: Set/Collection Duplicate Item Test 2: other collection is multi-item with duplicates, set contains a single item not in other
        [Fact]
        public static void IsIntersectWith_Test59()
        {
            HashSet<ValueItem> hashSet;
            IEnumerable<ValueItem> other;

            SetCollectionDuplicateItemTests.SetupTest2(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[0], hashSet.Comparer);
        }

        //Test 60: Set/Collection Duplicate Item Test 3: other collection is multi-item with duplicates, set contains a single item that is in other but not a duplicate in other
        [Fact]
        public static void IsIntersectWith_Test60()
        {
            HashSet<ValueItem> hashSet;
            IEnumerable<ValueItem> other;
            ValueItem item3 = new ValueItem(9999, -2);

            SetCollectionDuplicateItemTests.SetupTest3(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[] { item3 }, hashSet.Comparer);
        }

        //Test 61: Set/Collection Duplicate Item Test 4: other collection is multi-item with duplicates, set contains a single item that is a duplicate in other
        [Fact]
        public static void IsIntersectWith_Test61()
        {
            HashSet<ValueItem> hashSet;
            IEnumerable<ValueItem> other;
            ValueItem item2 = new ValueItem(4, 4);

            SetCollectionDuplicateItemTests.SetupTest4(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[] { item2 }, hashSet.Comparer);
        }

        //Test 62: Set/Collection Duplicate Item Test 5: other collection is multi-item with duplicates, set is multi-item as well, set and other are disjoint
        [Fact]
        public static void IsIntersectWith_Test62()
        {
            HashSet<ValueItem> hashSet;
            IEnumerable<ValueItem> other;

            SetCollectionDuplicateItemTests.SetupTest5(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[0], hashSet.Comparer);
        }

        //Test 63: Set/Collection Duplicate Item Test 6: other collection is multi-item with duplicates, set is multi-item as well, set and other overlap but are non-comparable, the overlap contains duplicate items from other
        [Fact]
        public static void IsIntersectWith_Test63()
        {
            HashSet<ValueItem> hashSet;
            IEnumerable<ValueItem> other;
            ValueItem itemo2 = new ValueItem(4, 4);
            ValueItem itemo3 = new ValueItem(9999, -2);
            ValueItem itemo4 = new ValueItem(99, -2);
            ValueItem items2 = new ValueItem(-4, -4);
            ValueItem items4 = new ValueItem(-99, 2);

            SetCollectionDuplicateItemTests.SetupTest6(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[] { items2, items4, itemo2, itemo3, itemo4 }, hashSet.Comparer);
        }

        //Test 64: Set/Collection Duplicate Item Test 7: other collection is multi-item with duplicates, set is multi-item as well, set and other overlap but are non-comparable, the overlap does not contain duplicate items from other
        [Fact]
        public static void IsIntersectWith_Test64()
        {
            HashSet<ValueItem> hashSet;
            IEnumerable<ValueItem> other;
            ValueItem itemo3 = new ValueItem(9999, -2);
            ValueItem items2 = new ValueItem(-4, -4);
            ValueItem items4 = new ValueItem(-99, 2);

            SetCollectionDuplicateItemTests.SetupTest7(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[] { items2, items4, itemo3 }, hashSet.Comparer);
        }

        //Test 65: Set/Collection Duplicate Item Test 8: other collection is multi-item with duplicates, set is multi-item as well, other is a proper subset of set
        [Fact]
        public static void IsIntersectWith_Test65()
        {
            HashSet<ValueItem> hashSet;
            IEnumerable<ValueItem> other;
            ValueItem itemo1 = new ValueItem(34, -5);
            ValueItem itemo2 = new ValueItem(4, 4);
            ValueItem itemo3 = new ValueItem(9999, -2);
            ValueItem itemo4 = new ValueItem(99, -2);

            SetCollectionDuplicateItemTests.SetupTest8(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[] { itemo1, itemo2, itemo3, itemo4 }, hashSet.Comparer);
        }

        //Test 66: Set/Collection Duplicate Item Test 9: other collection is multi-item with duplicates, set is multi-item as well, set is a proper subset of other, set contains duplicate items from other
        [Fact]
        public static void IsIntersectWith_Test66()
        {
            HashSet<ValueItem> hashSet;
            IEnumerable<ValueItem> other;
            ValueItem items1 = new ValueItem(-34, 5);
            ValueItem items2 = new ValueItem(-4, -4);
            ValueItem items3 = new ValueItem(-9999, 2);
            ValueItem items4 = new ValueItem(-99, 2);

            SetCollectionDuplicateItemTests.SetupTest9(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[] { items1, items2, items3, items4 }, hashSet.Comparer);
        }

        //Test 67: Set/Collection Duplicate Item Test 10: other collection is multi-item with duplicates, set is multi-item as well, set is a proper subset of other, set does not contain duplicate items from other
        [Fact]
        public static void IsIntersectWith_Test67()
        {
            HashSet<ValueItem> hashSet;
            IEnumerable<ValueItem> other;
            ValueItem items1 = new ValueItem(-34, 5);
            ValueItem items2 = new ValueItem(-4, -4);
            ValueItem items3 = new ValueItem(-9999, 2);
            ValueItem items4 = new ValueItem(-99, 2);

            SetCollectionDuplicateItemTests.SetupTest10(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[] { items1, items2, items3, items4 }, hashSet.Comparer);
        }

        //Test 68: Set/Collection Duplicate Item Test 11: other collection is multi-item with duplicates, set is multi-item as well, set and other are equal 
        [Fact]
        public static void IsIntersectWith_Test68()
        {
            HashSet<ValueItem> hashSet;
            IEnumerable<ValueItem> other;
            ValueItem itemo1 = new ValueItem(34, -5);
            ValueItem itemo2 = new ValueItem(4, 4);
            ValueItem itemo3 = new ValueItem(9999, -2);
            ValueItem itemo4 = new ValueItem(99, -2);
            ValueItem items1 = new ValueItem(-34, 5);
            ValueItem items2 = new ValueItem(-4, -4);
            ValueItem items3 = new ValueItem(-9999, 2);
            ValueItem items4 = new ValueItem(-99, 2);

            SetCollectionDuplicateItemTests.SetupTest11(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[] { items1, items2, items3, items4, itemo1, itemo2, itemo3, itemo4 }, hashSet.Comparer);
        }

        //Test 69: Set/Collection Duplicate Item Test 12: other contains duplicates by sets comparer but not by default comparer
        [Fact]
        public static void IsIntersectWith_Test69()
        {
            HashSet<ValueItem> hashSet;
            IEnumerable<ValueItem> other;
            ValueItem items2 = new ValueItem(-4, -4);

            SetCollectionDuplicateItemTests.SetupTest12(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[] { items2 }, hashSet.Comparer);
        }

        //Test 70: Set/Collection Duplicate Item Test 13: other contains duplicates by default comparer but not by sets comparer
        [Fact]
        public static void IsIntersectWith_Test70()
        {
            HashSet<ValueItem> hashSet;
            IEnumerable<ValueItem> other;

            SetCollectionDuplicateItemTests.SetupTest13(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[0], hashSet.Comparer);
        }

        //Test 71: Set/Collection Duplicate Item Test 14: set contains duplicate items by default comparer, those items also in other
        [Fact]
        public static void IsIntersectWith_Test71()
        {
            HashSet<ValueItem> hashSet;
            IEnumerable<ValueItem> other;

            SetCollectionDuplicateItemTests.SetupTest14(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[0], hashSet.Comparer);
        }

        //Test 72: Set/Collection Duplicate Item Test 15: set contains duplicate items by default comparer, one of those items also in other
        [Fact]
        public static void IsIntersectWith_Test72()
        {
            HashSet<ValueItem> hashSet;
            IEnumerable<ValueItem> other;

            SetCollectionDuplicateItemTests.SetupTest15(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[0], hashSet.Comparer);
        }

        //Test 73: Set/Collection Duplicate Item Test 16: set contains duplicate items by default comparer, those items not in other
        [Fact]
        public static void IsIntersectWith_Test73()
        {
            HashSet<ValueItem> hashSet;
            IEnumerable<ValueItem> other;

            SetCollectionDuplicateItemTests.SetupTest16(out hashSet, out other);
            hashSet.IntersectWith(other);

            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[0], hashSet.Comparer);
        }

        #endregion
    }
}
