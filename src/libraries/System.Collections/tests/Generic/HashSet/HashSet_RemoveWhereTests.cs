// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;
using Tests.HashSet_HashSetTestSupport;
using Tests.HashSet_SetCollectionRelationshipTests;
using Tests.HashSet_SetCollectionComparerTests;

namespace Tests
{
    public class HashSet_RemoveWhereTests
    {
        #region match matches a collection of items - tests 1-57

        #region Set/Collection Relationship tests - tests 1-42

        //Test 1: Set/Collection Relationship Test 1: other is null
        [Fact]
        public static void RemoveWhere_Test1()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;
            int ret;

            SetCollectionRelationshipTests.SetupTest1(out hashSet, out other);
            PredicateMatches<Item>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<Item>.Match);

            Assert.Equal(0, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(34), new Item(4), new Item(9999) }, hashSet.Comparer);
        }

        //Test 2: Set/Collection Relationship Test 2: other is empty and set is empty
        [Fact]
        public static void RemoveWhere_Test2()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;
            int ret;

            SetCollectionRelationshipTests.SetupTest2(out hashSet, out other);
            PredicateMatches<Item>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<Item>.Match);

            Assert.Equal(0, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[0], hashSet.Comparer);
        }

        //Test 3: Set/Collection Relationship Test 3: other is empty and set is single-item
        [Fact]
        public static void RemoveWhere_Test3()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;
            int ret;

            SetCollectionRelationshipTests.SetupTest3(out hashSet, out other);
            PredicateMatches<Item>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<Item>.Match);

            Assert.Equal(0, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(1) }, hashSet.Comparer);
        }

        //Test 4: Set/Collection Relationship Test 4: other is empty and set is multi-item 
        [Fact]
        public static void RemoveWhere_Test4()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;
            int ret;

            SetCollectionRelationshipTests.SetupTest4(out hashSet, out other);
            PredicateMatches<Item>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<Item>.Match);

            Assert.Equal(0, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(1), new Item(3), new Item(-23) }, hashSet.Comparer);
        }

        //Test 5: Set/Collection Relationship Test 5: other is single-item and set is empty
        [Fact]
        public static void RemoveWhere_Test5()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;
            int ret;

            SetCollectionRelationshipTests.SetupTest5(out hashSet, out other);
            PredicateMatches<Item>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<Item>.Match);

            Assert.Equal(0, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[0], hashSet.Comparer);
        }

        //Test 6: Set/Collection Relationship Test 6: other is single-item and set is single-item with a different item
        [Fact]
        public static void RemoveWhere_Test6()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;
            int ret;

            SetCollectionRelationshipTests.SetupTest6(out hashSet, out other);
            PredicateMatches<Item>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<Item>.Match);

            Assert.Equal(0, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(34) }, hashSet.Comparer);
        }

        //Test 7: Set/Collection Relationship Test 7: other is single-item and set is single-item with the same item 
        [Fact]
        public static void RemoveWhere_Test7()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;
            int ret;

            SetCollectionRelationshipTests.SetupTest7(out hashSet, out other);
            PredicateMatches<Item>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<Item>.Match);

            Assert.Equal(1, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[0], hashSet.Comparer);
        }

        //Test 8: Set/Collection Relationship Test 8: other is single-item and set is multi-item where set and other are disjoint
        [Fact]
        public static void RemoveWhere_Test8()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;
            int ret;

            SetCollectionRelationshipTests.SetupTest8(out hashSet, out other);
            PredicateMatches<Item>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<Item>.Match);

            Assert.Equal(0, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(-23), new Item(0), new Item(234) }, hashSet.Comparer);
        }

        //Test 9: Set/Collection Relationship Test 9: other is single-item and set is multi-item where set contains other
        [Fact]
        public static void RemoveWhere_Test9()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;
            int ret;

            SetCollectionRelationshipTests.SetupTest9(out hashSet, out other);
            PredicateMatches<Item>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<Item>.Match);

            Assert.Equal(1, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(-23), new Item(0) }, hashSet.Comparer);
        }

        //Test 10: Set/Collection Relationship Test 10: other is multi-item and set is empty
        [Fact]
        public static void RemoveWhere_Test10()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;
            int ret;

            SetCollectionRelationshipTests.SetupTest10(out hashSet, out other);
            PredicateMatches<Item>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<Item>.Match);

            Assert.Equal(0, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[0], hashSet.Comparer);
        }

        //Test 11: Set/Collection Relationship Test 11: other is multi-item and set is single-item and set and other are disjoint
        [Fact]
        public static void RemoveWhere_Test11()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;
            int ret;

            SetCollectionRelationshipTests.SetupTest11(out hashSet, out other);
            PredicateMatches<Item>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<Item>.Match);

            Assert.Equal(0, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(-222) }, hashSet.Comparer);
        }

        //Test 12: Set/Collection Relationship Test 12: other is multi-item and set is single-item and other contains set
        [Fact]
        public static void RemoveWhere_Test12()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;
            int ret;

            SetCollectionRelationshipTests.SetupTest12(out hashSet, out other);
            PredicateMatches<Item>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<Item>.Match);

            Assert.Equal(1, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[0], hashSet.Comparer);
        }

        //Test 13: Set/Collection Relationship Test 13: other is multi-item and set is multi-item and set and other disjoint
        [Fact]
        public static void RemoveWhere_Test13()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;
            int ret;

            SetCollectionRelationshipTests.SetupTest13(out hashSet, out other);
            PredicateMatches<Item>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<Item>.Match);

            Assert.Equal(0, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(23), new Item(222), new Item(322) }, hashSet.Comparer);
        }

        //Test 14: Set/Collection Relationship Test 14: other is multi-item and set is multi-item and set and other overlap but are non-comparable
        [Fact]
        public static void RemoveWhere_Test14()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;
            int ret;

            SetCollectionRelationshipTests.SetupTest14(out hashSet, out other);
            PredicateMatches<Item>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<Item>.Match);

            Assert.Equal(2, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(23), new Item(222) }, hashSet.Comparer);
        }

        //Test 15: Set/Collection Relationship Test 15: other is multi-item and set is multi-item and other is a proper subset of set
        [Fact]
        public static void RemoveWhere_Test15()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;
            int ret;

            SetCollectionRelationshipTests.SetupTest15(out hashSet, out other);
            PredicateMatches<Item>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<Item>.Match);

            Assert.Equal(3, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(23), new Item(222) }, hashSet.Comparer);
        }

        //Test 16: Set/Collection Relationship Test 16: other is multi-item and set is multi-item and set is a proper subset of other
        [Fact]
        public static void RemoveWhere_Test16()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;
            int ret;

            SetCollectionRelationshipTests.SetupTest16(out hashSet, out other);
            PredicateMatches<Item>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<Item>.Match);

            Assert.Equal(3, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[0], hashSet.Comparer);
        }

        //Test 17: Set/Collection Relationship Test 17: other is multi-item and set is multi-item and set and other are equal
        [Fact]
        public static void RemoveWhere_Test17()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;
            int ret;

            SetCollectionRelationshipTests.SetupTest17(out hashSet, out other);
            PredicateMatches<Item>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<Item>.Match);

            Assert.Equal(5, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[0], hashSet.Comparer);
        }

        //Test 18: Set/Collection Relationship Test 18: other is set and set is empty
        [Fact]
        public static void RemoveWhere_Test18()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;
            int ret;

            SetCollectionRelationshipTests.SetupTest18(out hashSet, out other);
            PredicateMatches<Item>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<Item>.Match);

            Assert.Equal(0, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[0], hashSet.Comparer);
        }

        //Test 19: Set/Collection Relationship Test 19: other is set and set is single-item and set contains set
        [Fact]
        public static void RemoveWhere_Test19()
        {
            HashSet<IEnumerable> hashSet;
            IEnumerable<IEnumerable> other;
            int ret;

            SetCollectionRelationshipTests.SetupTest19(out hashSet, out other);
            PredicateMatches<IEnumerable>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<IEnumerable>.Match);

            Assert.Equal(1, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<IEnumerable>.VerifyHashSet(hashSet, new IEnumerable[0], hashSet.Comparer);
        }

        //Test 20: Set/Collection Relationship Test 20: other is set and set is single-item and set does not contain set
        [Fact]
        public static void RemoveWhere_Test20()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;
            int ret;

            SetCollectionRelationshipTests.SetupTest20(out hashSet, out other);
            PredicateMatches<Item>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<Item>.Match);

            Assert.Equal(1, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[0], hashSet.Comparer);
        }

        //Test 21: Set/Collection Relationship Test 21: other is set and set is multi-item and set contains set
        [Fact]
        public static void RemoveWhere_Test21()
        {
            HashSet<IEnumerable> hashSet;
            IEnumerable<IEnumerable> other;
            int ret;

            SetCollectionRelationshipTests.SetupTest21(out hashSet, out other);
            PredicateMatches<IEnumerable>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<IEnumerable>.Match);

            Assert.Equal(3, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<IEnumerable>.VerifyHashSet(hashSet, new IEnumerable[0], hashSet.Comparer);
        }

        //Test 22: Set/Collection Relationship Test 22: other is set and set is multi-item and set does not contain set
        [Fact]
        public static void RemoveWhere_Test22()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;
            int ret;

            SetCollectionRelationshipTests.SetupTest22(out hashSet, out other);
            PredicateMatches<Item>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<Item>.Match);

            Assert.Equal(3, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[0], hashSet.Comparer);
        }

        //Test 23: Set/Collection Relationship Test 23: item is only item in other: Item is the set and item is in the set
        [Fact]
        public static void RemoveWhere_Test23()
        {
            List<int> item1 = new List<int>(new int[] { 1, 2 });
            List<int> item2 = new List<int>(new int[] { 2, -1 });
            HashSet<IEnumerable> hashSet;
            IEnumerable<IEnumerable> other;
            int ret;

            SetCollectionRelationshipTests.SetupTest23(out hashSet, out other);
            PredicateMatches<IEnumerable>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<IEnumerable>.Match);

            Assert.Equal(1, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<IEnumerable>.VerifyHashSet(hashSet, new IEnumerable[] { item1, item2 }, hashSet.Comparer);
        }

        //Test 24: Set/Collection Relationship Test 24:  item is only item in other: Item is the set and item is not in the set
        [Fact]
        public static void RemoveWhere_Test24()
        {
            List<int> item1 = new List<int>(new int[] { 1, 2 });
            List<int> item2 = new List<int>(new int[] { 2, -1 });
            HashSet<IEnumerable> hashSet;
            IEnumerable<IEnumerable> other;
            int ret;

            SetCollectionRelationshipTests.SetupTest24(out hashSet, out other);
            PredicateMatches<IEnumerable>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<IEnumerable>.Match);

            Assert.Equal(0, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<IEnumerable>.VerifyHashSet(hashSet, new IEnumerable[] { item1, item2 }, hashSet.Comparer);
        }

        //Test 25: Set/Collection Relationship Test 25:  item is only item in other: Item is Default<T> and in set. T is a numeric type
        [Fact]
        public static void RemoveWhere_Test25()
        {
            HashSet<int> hashSet;
            IEnumerable<int> other;
            int ret;

            SetCollectionRelationshipTests.SetupTest25(out hashSet, out other);
            PredicateMatches<int>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<int>.Match);

            Assert.Equal(1, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[] { 1, 2, 3, 4, 6, 7 }, hashSet.Comparer);
        }

        //Test 26: Set/Collection Relationship Test 26:  item is only item in other: Item is Default<T> and in set. T is a reference type
        [Fact]
        public static void RemoveWhere_Test26()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;
            int ret;

            SetCollectionRelationshipTests.SetupTest26(out hashSet, out other);
            PredicateMatches<Item>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<Item>.Match);

            Assert.Equal(1, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(-3), new Item(3) }, hashSet.Comparer);
        }

        //Test 27: Set/Collection Relationship Test 27:  item is only item in other: Item is Default<T> and not in set. T is a numeric type
        [Fact]
        public static void RemoveWhere_Test27()
        {
            HashSet<int> hashSet;
            IEnumerable<int> other;
            int ret;

            SetCollectionRelationshipTests.SetupTest27(out hashSet, out other);
            PredicateMatches<int>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<int>.Match);

            Assert.Equal(0, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[] { 1, 2, 3, 4, 6, 7, 5 }, hashSet.Comparer);
        }

        //Test 28: Set/Collection Relationship Test 28:  item is only item in other: Item is Default<T> and not in set.  T is a reference type
        [Fact]
        public static void RemoveWhere_Test28()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;
            int ret;

            SetCollectionRelationshipTests.SetupTest28(out hashSet, out other);
            PredicateMatches<Item>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<Item>.Match);

            Assert.Equal(0, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(-3), new Item(3) }, hashSet.Comparer);
        }

        //Test 29: Set/Collection Relationship Test 29:  item is only item in other: Item is equal to an item in set but different.
        [Fact]
        public static void RemoveWhere_Test29()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;
            int ret;

            SetCollectionRelationshipTests.SetupTest29(out hashSet, out other);
            PredicateMatches<Item>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<Item>.Match);

            Assert.Equal(1, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(3), new Item(2) }, hashSet.Comparer);
        }

        //Test 30: Set/Collection Relationship Test 30:  item is only item in other: Item shares hash value with unequal item in set
        [Fact]
        public static void RemoveWhere_Test30()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;
            int ret;

            SetCollectionRelationshipTests.SetupTest30(out hashSet, out other);
            PredicateMatches<Item>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<Item>.Match);

            Assert.Equal(0, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(1), new Item(3), new Item(2) }, hashSet.Comparer);
        }

        //Test 31: Set/Collection Relationship Test 31:  item is only item in other: Item was previously in set but not currently
        [Fact]
        public static void RemoveWhere_Test31()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;
            int ret;

            SetCollectionRelationshipTests.SetupTest31(out hashSet, out other);
            PredicateMatches<Item>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<Item>.Match);

            Assert.Equal(0, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(3), new Item(2) }, hashSet.Comparer);
        }

        //Test 32: Set/Collection Relationship Test 32:  item is only item in other: Item was previously removed from set but in it currently
        [Fact]
        public static void RemoveWhere_Test32()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;
            int ret;

            SetCollectionRelationshipTests.SetupTest32(out hashSet, out other);
            PredicateMatches<Item>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<Item>.Match);

            Assert.Equal(1, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(3), new Item(2) }, hashSet.Comparer);
        }

        //Test 33: Set/Collection Relationship Test 33: item is one of the items in other: Item is the set and item is in the set
        [Fact]
        public static void RemoveWhere_Test33()
        {
            List<int> item1 = new List<int>(new int[] { 1, 2 });
            List<int> item2 = new List<int>(new int[] { 2, -1 });
            HashSet<IEnumerable> hashSet;
            IEnumerable<IEnumerable> other;
            int ret;

            SetCollectionRelationshipTests.SetupTest33(out hashSet, out other);
            PredicateMatches<IEnumerable>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<IEnumerable>.Match);

            Assert.Equal(1, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<IEnumerable>.VerifyHashSet(hashSet, new IEnumerable[] { item1, item2 }, hashSet.Comparer);
        }

        //Test 34: Set/Collection Relationship Test 34: item is one of the items in other: Item is the set and item is not in the set
        [Fact]
        public static void RemoveWhere_Test34()
        {
            List<int> item1 = new List<int>(new int[] { 1, 2 });
            List<int> item2 = new List<int>(new int[] { 2, -1 });
            HashSet<IEnumerable> hashSet;
            IEnumerable<IEnumerable> other;
            int ret;

            SetCollectionRelationshipTests.SetupTest34(out hashSet, out other);
            PredicateMatches<IEnumerable>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<IEnumerable>.Match);

            Assert.Equal(0, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<IEnumerable>.VerifyHashSet(hashSet, new IEnumerable[] { item1, item2 }, hashSet.Comparer);
        }

        //Test 35: Set/Collection Relationship Test 35: item is one of the items in other: Item is Default<T> and in set. T is a numeric type
        [Fact]
        public static void RemoveWhere_Test35()
        {
            HashSet<int> hashSet;
            IEnumerable<int> other;
            int ret;

            SetCollectionRelationshipTests.SetupTest35(out hashSet, out other);
            PredicateMatches<int>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<int>.Match);

            Assert.Equal(1, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[] { 1, 2, 3, 4, 6, 7 }, hashSet.Comparer);
        }

        //Test 36: Set/Collection Relationship Test 36: item is one of the items in other: Item is Default<T> and in set. T is a reference type 
        [Fact]
        public static void RemoveWhere_Test36()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;
            int ret;

            SetCollectionRelationshipTests.SetupTest36(out hashSet, out other);
            PredicateMatches<Item>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<Item>.Match);

            Assert.Equal(1, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(-3), new Item(3) }, hashSet.Comparer);
        }

        //Test 37: Set/Collection Relationship Test 37: item is one of the items in other: Item is Default<T> and not in set. T is a numeric type
        [Fact]
        public static void RemoveWhere_Test37()
        {
            HashSet<int> hashSet;
            IEnumerable<int> other;
            int ret;

            SetCollectionRelationshipTests.SetupTest37(out hashSet, out other);
            PredicateMatches<int>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<int>.Match);

            Assert.Equal(0, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[] { 1, 2, 3, 4, 6, 7, 5 }, hashSet.Comparer);
        }

        //Test 38: Set/Collection Relationship Test 38: item is one of the items in other: Item is Default<T> and not in set.  T is a reference type
        [Fact]
        public static void RemoveWhere_Test38()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;
            int ret;

            SetCollectionRelationshipTests.SetupTest38(out hashSet, out other);
            PredicateMatches<Item>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<Item>.Match);

            Assert.Equal(0, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(-3), new Item(3) }, hashSet.Comparer);
        }

        //Test 39: Set/Collection Relationship Test 39: item is one of the items in other: Item is equal to an item in set but different.
        [Fact]
        public static void RemoveWhere_Test39()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;
            int ret;

            SetCollectionRelationshipTests.SetupTest39(out hashSet, out other);
            PredicateMatches<Item>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<Item>.Match);

            Assert.Equal(1, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(2), new Item(3) }, hashSet.Comparer);
        }

        //Test 40: Set/Collection Relationship Test 40: item is one of the items in other: Item shares hash value with unequal item in set
        [Fact]
        public static void RemoveWhere_Test40()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;
            int ret;

            SetCollectionRelationshipTests.SetupTest40(out hashSet, out other);
            PredicateMatches<Item>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<Item>.Match);

            Assert.Equal(0, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(1), new Item(2), new Item(3) }, hashSet.Comparer);
        }

        //Test 41: Set/Collection Relationship Test 41: item is one of the items in other: Item was previously in set but not currently 
        [Fact]
        public static void RemoveWhere_Test41()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;
            int ret;

            SetCollectionRelationshipTests.SetupTest41(out hashSet, out other);
            PredicateMatches<Item>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<Item>.Match);

            Assert.Equal(0, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(2), new Item(3) }, hashSet.Comparer);
        }

        //Test 42: Set/Collection Relationship Test 42: item is one of the items in other: Item was previously removed from set but in it currently
        [Fact]
        public static void RemoveWhere_Test42()
        {
            HashSet<Item> hashSet;
            IEnumerable<Item> other;
            int ret;

            SetCollectionRelationshipTests.SetupTest42(out hashSet, out other);
            PredicateMatches<Item>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<Item>.Match);

            Assert.Equal(1, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<Item>.VerifyHashSet(hashSet, new Item[] { new Item(2), new Item(3) }, hashSet.Comparer);
        }

        #endregion

        #region Set/Collection Comparer Tests (tests 43-57)

        //Test 43: Set/Collection Comparer Test 1: Item is in collection: item same as element in set by default comparer, different by sets comparer - set contains item that is equal by sets comparer
        [Fact]
        public static void RemoveWhere_Test43()
        {
            HashSet<ValueItem> hashSet;
            IEnumerable<ValueItem> other;
            int ret;
            ValueItem item1 = new ValueItem(34, -5);
            ValueItem item2 = new ValueItem(4, 4);

            SetCollectionComparerTests.SetupTest1(out hashSet, out other);
            PredicateMatches<ValueItem>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<ValueItem>.Match);

            Assert.Equal(1, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[] { item1, item2 }, hashSet.Comparer);
        }

        //Test 44: Set/Collection Comparer Test 2: Item is in collection: item same as element in set by default comparer, different by sets comparer - set does not contain item that is equal by sets comparer
        [Fact]
        public static void RemoveWhere_Test44()
        {
            HashSet<ValueItem> hashSet;
            IEnumerable<ValueItem> other;
            int ret;
            ValueItem item1 = new ValueItem(34, -5);
            ValueItem item2 = new ValueItem(4, 4);
            ValueItem item3 = new ValueItem(9999, -20);

            SetCollectionComparerTests.SetupTest2(out hashSet, out other);
            PredicateMatches<ValueItem>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<ValueItem>.Match);

            Assert.Equal(0, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[] { item1, item2, item3 }, hashSet.Comparer);
        }

        //Test 45: Set/Collection Comparer Test 3: Item is in collection: item same as element in set by sets comparer, different by default comparer - set contains item that is equal by default comparer
        [Fact]
        public static void RemoveWhere_Test45()
        {
            HashSet<ValueItem> hashSet;
            IEnumerable<ValueItem> other;
            int ret;
            ValueItem item1 = new ValueItem(34, -5);
            ValueItem item2 = new ValueItem(4, 4);

            SetCollectionComparerTests.SetupTest3(out hashSet, out other);
            PredicateMatches<ValueItem>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<ValueItem>.Match);

            Assert.Equal(1, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[] { item1, item2 }, hashSet.Comparer);
        }

        //Test 46: Set/Collection Comparer Test 4: Item is in collection: item same as element in set by sets comparer, different by default comparer - set does not contain item that is equal by default comparer
        [Fact]
        public static void RemoveWhere_Test46()
        {
            HashSet<ValueItem> hashSet;
            IEnumerable<ValueItem> other;
            int ret;
            ValueItem item1 = new ValueItem(340, -5);
            ValueItem item2 = new ValueItem(4, 4);

            SetCollectionComparerTests.SetupTest4(out hashSet, out other);
            PredicateMatches<ValueItem>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<ValueItem>.Match);

            Assert.Equal(1, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[] { item1, item2 }, hashSet.Comparer);
        }

        //Test 48: Set/Collection Comparer Test 6: Item is only item in collection: item same as element in set by default comparer, different by sets comparer - set contains item that is equal by sets comparer
        [Fact]
        public static void RemoveWhere_Test48()
        {
            HashSet<ValueItem> hashSet;
            IEnumerable<ValueItem> other;
            int ret;
            ValueItem item1 = new ValueItem(34, -5);
            ValueItem item2 = new ValueItem(4, 4);

            SetCollectionComparerTests.SetupTest6(out hashSet, out other);
            PredicateMatches<ValueItem>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<ValueItem>.Match);

            Assert.Equal(1, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[] { item1, item2 }, hashSet.Comparer);
        }

        //Test 49: Set/Collection Comparer Test 7: Item is only item in collection: item same as element in set by default comparer, different by sets comparer - set does not contain item that is equal by sets comparer
        [Fact]
        public static void RemoveWhere_Test49()
        {
            HashSet<ValueItem> hashSet;
            IEnumerable<ValueItem> other;
            int ret;
            ValueItem item1 = new ValueItem(34, -5);
            ValueItem item2 = new ValueItem(4, 4);
            ValueItem item3 = new ValueItem(9999, -20);

            SetCollectionComparerTests.SetupTest7(out hashSet, out other);
            PredicateMatches<ValueItem>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<ValueItem>.Match);

            Assert.Equal(0, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[] { item1, item2, item3 }, hashSet.Comparer);
        }

        //Test 50: Set/Collection Comparer Test 8: Item is only item in collection: item same as element in set by sets comparer, different by default comparer - set contains item that is equal by default comparer
        [Fact]
        public static void RemoveWhere_Test50()
        {
            HashSet<ValueItem> hashSet;
            IEnumerable<ValueItem> other;
            int ret;
            ValueItem item1 = new ValueItem(34, -5);
            ValueItem item2 = new ValueItem(4, 4);

            SetCollectionComparerTests.SetupTest8(out hashSet, out other);
            PredicateMatches<ValueItem>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<ValueItem>.Match);

            Assert.Equal(1, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[] { item1, item2 }, hashSet.Comparer);
        }

        //Test 51: Set/Collection Comparer Test 9: Item is only item in collection: item same as element in set by sets comparer, different by default comparer - set does not contain item that is equal by default comparer
        [Fact]
        public static void RemoveWhere_Test51()
        {
            HashSet<ValueItem> hashSet;
            IEnumerable<ValueItem> other;
            int ret;
            ValueItem item1 = new ValueItem(340, -5);
            ValueItem item2 = new ValueItem(4, 4);

            SetCollectionComparerTests.SetupTest9(out hashSet, out other);
            PredicateMatches<ValueItem>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<ValueItem>.Match);

            Assert.Equal(1, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[] { item1, item2 }, hashSet.Comparer);
        }

        //Test 52: Set/Collection Comparer Test 10: Item is only item in collection: item contains set and item in set with GetSetComparer<T> as comparer
        [Fact]
        public static void RemoveWhere_Test52()
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

            HashSet<HashSet<IEnumerable>> hashSet;
            IEnumerable<HashSet<IEnumerable>> other;
            int ret;

            SetCollectionComparerTests.SetupTest10(out hashSet, out other);
            PredicateMatches<HashSet<IEnumerable>>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<HashSet<IEnumerable>>.Match);

            HashSet<IEnumerable>[] expected = new HashSet<IEnumerable>[] { itemhs1, itemhs2 };
            HashSet<IEnumerable>[] actual = new HashSet<IEnumerable>[2];
            hashSet.CopyTo(actual, 0, 2);

            Assert.Equal(1, ret); //"RemoveWhere returned the wrong value"
            Assert.Equal(2, hashSet.Count); //"Expected count to be same."
            HashSetTestSupport.HashSetContains(actual, expected);
        }

        //Test 53: Set/Collection Comparer Test 11: Item is collection: item same as element in set by default comparer, different by sets comparer - set contains item that is equal by sets comparer 
        [Fact]
        public static void RemoveWhere_Test53()
        {
            HashSet<ValueItem> hashSet;
            IEnumerable<ValueItem> other;
            int ret;
            ValueItem item1 = new ValueItem(34, -5);
            ValueItem item2 = new ValueItem(4, 4);

            SetCollectionComparerTests.SetupTest11(out hashSet, out other);
            PredicateMatches<ValueItem>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<ValueItem>.Match);

            Assert.Equal(1, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[] { item1, item2 }, hashSet.Comparer);
        }

        //Test 54: Set/Collection Comparer Test 12: Item is collection: item same as element in set by default comparer, different by sets comparer - set does not contain item that is equal by sets comparer
        [Fact]
        public static void RemoveWhere_Test54()
        {
            HashSet<ValueItem> hashSet;
            IEnumerable<ValueItem> other;
            int ret;
            ValueItem item1 = new ValueItem(34, -5);
            ValueItem item2 = new ValueItem(4, 4);
            ValueItem item3 = new ValueItem(9999, -20);

            SetCollectionComparerTests.SetupTest12(out hashSet, out other);
            PredicateMatches<ValueItem>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<ValueItem>.Match);

            Assert.Equal(0, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[] { item1, item2, item3 }, hashSet.Comparer);
        }

        //Test 55: Set/Collection Comparer Test 13: Item is collection: item same as element in set by sets comparer, different by default comparer - set contains item that is equal by default comparer
        [Fact]
        public static void RemoveWhere_Test55()
        {
            HashSet<ValueItem> hashSet;
            IEnumerable<ValueItem> other;
            int ret;
            ValueItem item1 = new ValueItem(34, -5);
            ValueItem item2 = new ValueItem(4, 4);

            SetCollectionComparerTests.SetupTest13(out hashSet, out other);
            PredicateMatches<ValueItem>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<ValueItem>.Match);

            Assert.Equal(1, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[] { item1, item2 }, hashSet.Comparer);
        }

        //Test 56: Set/Collection Comparer Test 14: Item is collection: item same as element in set by sets comparer, different by default comparer - set does not contain item that is equal by default comparer
        [Fact]
        public static void RemoveWhere_Test56()
        {
            HashSet<ValueItem> hashSet;
            IEnumerable<ValueItem> other;
            int ret;
            ValueItem item1 = new ValueItem(340, -5);
            ValueItem item2 = new ValueItem(4, 4);

            SetCollectionComparerTests.SetupTest14(out hashSet, out other);
            PredicateMatches<ValueItem>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<ValueItem>.Match);

            Assert.Equal(1, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<ValueItem>.VerifyHashSet(hashSet, new ValueItem[] { item1, item2 }, hashSet.Comparer);
        }

        //Test 57: Set/Collection Comparer Test 15: Item is collection: item contains set and item in set with GetSetComparer<T> as comparer
        [Fact]
        public static void RemoveWhere_Test57()
        {
            HashSet<HashSet<IEnumerable>> hashSet;
            IEnumerable<HashSet<IEnumerable>> other;
            int ret;

            SetCollectionComparerTests.SetupTest15(out hashSet, out other);
            PredicateMatches<HashSet<IEnumerable>>.SetMatchOn(other, hashSet.Comparer);
            ret = hashSet.RemoveWhere(PredicateMatches<HashSet<IEnumerable>>.Match);

            Assert.Equal(3, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<HashSet<IEnumerable>>.VerifyHashSet(hashSet, new HashSet<IEnumerable>[0], hashSet.Comparer);
        }

        #endregion

        #endregion

        [Fact]
        public static void RemoveWhere_Test58()
        {
            HashSet<Item> hashSet;
            int ret;

            //Test 58: match is null
            hashSet = new HashSet<Item>();
            Assert.Throws<ArgumentNullException>(() => { ret = hashSet.RemoveWhere(null); }); //"ArgumentNullException expected"

            //Test 59: match throws exception
            hashSet = new HashSet<Item>(new Item[] { new Item(0) });
            Predicate<Item> exceptionPred = (Item item) => { throw new ArithmeticException("Arithmetic exception thrown. :D"); };
            Assert.Throws<ArithmeticException>(() => { ret = hashSet.RemoveWhere(exceptionPred); }); //"ArithmeticException expected."

            //Test 65: match modifies the set: adds a different item and returns false
            HashSet<int> hashSet2 = new HashSet<int>(new int[] { -4, -3, -2, -1, 0, 1, 2, 3, 4 });
            PredicateMatch.set = hashSet2;

            Assert.Throws<OutOfMemoryException>(() => { ret = hashSet2.RemoveWhere(PredicateMatch.Match65); }); //"OutOfMemoryException expected."
        }

        //Test 60: match modifies the set: adds item being tested and returns true
        [Fact]
        public static void RemoveWhere_Test60()
        {
            HashSet<int> hashSet;
            int ret;

            hashSet = new HashSet<int>(new int[] { -4, -3, -2, -1, 0, 1, 2, 3, 4 });
            PredicateMatch.set = hashSet;
            ret = hashSet.RemoveWhere(PredicateMatch.Match60);

            Assert.Equal(9, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[0], hashSet.Comparer);
        }

        //Test 61: match modifies the set: adds item being tested and returns false
        [Fact]
        public static void RemoveWhere_Test61()
        {
            HashSet<int> hashSet;
            int ret;

            hashSet = new HashSet<int>(new int[] { -4, -3, -2, -1, 0, 1, 2, 3, 4 });
            PredicateMatch.set = hashSet;
            ret = hashSet.RemoveWhere(PredicateMatch.Match61);

            Assert.Equal(0, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[] { -4, -3, -2, -1, 0, 1, 2, 3, 4 }, hashSet.Comparer);
        }

        //Test 62: match modifies the set: removes item being tested and returns true
        [Fact]
        public static void RemoveWhere_Test62()
        {
            HashSet<int> hashSet;
            int ret;

            hashSet = new HashSet<int>(new int[] { -4, -3, -2, -1, 0, 1, 2, 3, 4 });
            PredicateMatch.set = hashSet;
            ret = hashSet.RemoveWhere(PredicateMatch.Match62);

            Assert.Equal(0, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[0], hashSet.Comparer);
        }

        //Test 63: match modifies the set: removes item being tested and returns false
        [Fact]
        public static void RemoveWhere_Test63()
        {
            HashSet<int> hashSet;
            int ret;

            hashSet = new HashSet<int>(new int[] { -4, -3, -2, -1, 0, 1, 2, 3, 4 });
            PredicateMatch.set = hashSet;
            ret = hashSet.RemoveWhere(PredicateMatch.Match63);

            Assert.Equal(0, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[0], hashSet.Comparer);
        }

        //Test 64: match modifies the set: adds a different item and returns true
        [Fact]
        public static void RemoveWhere_Test64()
        {
            HashSet<int> hashSet;
            int ret;

            hashSet = new HashSet<int>(new int[] { -4, -3, -2, -1, 0, 1, 2, 3, 4 });
            PredicateMatch.set = hashSet;
            ret = hashSet.RemoveWhere(PredicateMatch.Match64);

            Assert.Equal(10, ret); //"RemoveWhere returned the wrong value"
            HashSetTestSupport<int>.VerifyHashSet(hashSet, new int[] { 16, 7, 8, 9, 10, 11, 12, 13, 14 }, hashSet.Comparer);
        }

        //Test 66: match is non-stationary: independent of set
        [Fact]
        public static void RemoveWhere_Test66()
        {
            HashSet<int> hashSet;
            HashSet<int> hashSet2;
            int ret;

            hashSet = new HashSet<int>(new int[] { -4, -3, -2, -1, 0, 1, 2, 3, 4 });
            hashSet2 = new HashSet<int>(new int[] { -4, -3, -2, -1, 0, 1, 2, 3, 4 });
            PredicateMatch.set = hashSet;
            PredicateMatch.remove = true;
            ret = hashSet.RemoveWhere(PredicateMatch.Match66);

            Assert.Equal(5, ret); //"RemoveWhere returned the wrong value"
            Assert.Equal(4, hashSet.Count); //"RemoveWhere removed wrong number of items"
            Assert.True(hashSet.IsSubsetOf(hashSet2)); //"RemoveWhere left wrong items in set"
        }

        //Test 67: match is non-stationary: dependent on set
        [Fact]
        public static void RemoveWhere_Test67()
        {
            HashSet<int> hashSet;
            HashSet<int> hashSet2;
            int ret;

            hashSet = new HashSet<int>(new int[] { -4, -3, -2, -1, 0, 1, 2, 3, 4 });
            hashSet2 = new HashSet<int>(new int[] { -4, -3, -2, -1, 0, 1, 2, 3, 4 });
            PredicateMatch.set = hashSet;
            PredicateMatch.limit = 6;
            ret = hashSet.RemoveWhere(PredicateMatch.Match67);

            Assert.Equal(3, ret); //"RemoveWhere returned the wrong value"
            Assert.Equal(6, hashSet.Count); //"RemoveWhere removed wrong number of items"
            Assert.True(hashSet.IsSubsetOf(hashSet2)); //"RemoveWhere left wrong items in set"
        }

        #region Helper Classes

        private class PredicateMatches<T>
        {
            private static IEnumerable<T> s_match;
            private static IEqualityComparer<T> s_comparer;

            public static void SetMatchOn(IEnumerable<T> match, IEqualityComparer<T> comparer)
            {
                s_match = match;
                s_comparer = comparer;
            }

            public static bool Match(T item)
            {
                IEnumerator<T> e_match;

                if (s_match == null) return false;
                e_match = s_match.GetEnumerator();
                while (e_match.MoveNext())
                {
                    if (s_comparer.Equals(item, e_match.Current))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        // contains a bunch of predicates for tests.
        private class PredicateMatch
        {
            public static HashSet<int> set = null;
            public static bool remove = true;
            public static int limit = 0;

            public static bool Match60(int item)
            {
                set.Add(item);

                return true;
            }
            public static bool Match61(int item)
            {
                set.Add(item);

                return false;
            }
            public static bool Match62(int item)
            {
                set.Remove(item);

                return true;
            }
            public static bool Match63(int item)
            {
                set.Remove(item);

                return false;
            }
            public static bool Match64(int item)
            {
                set.Add(item + 10);

                return true;
            }
            public static bool Match65(int item)
            {
                set.Add(item + 10);

                return false;
            }
            public static bool Match66(int item)
            {
                remove = !remove;
                return !remove;
            }
            public static bool Match67(int item)
            {
                if (set == null) return false;
                return (set.Count > limit);
            }
        }

        #endregion
    }
}
