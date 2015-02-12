// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Tests.HashSet_HashSetTestSupport;
using Tests.HashSet_SetCollectionRelationshipTests;

namespace Tests
{
    namespace HashSet_SetCollectionRelationshipTests
    {    //Test framework for testing collection in various relationships
         //with respect to the HashSet
        public class SetCollectionRelationshipTests
        {
            //Test 1
            //  other is null
            public static void SetupTest1(out HashSet<Item> set, out IEnumerable<Item> other)
            {
                Item item1 = new Item(34);
                Item item2 = new Item(4);
                Item item3 = new Item(9999);

                set = new HashSet<Item>(new ItemEqualityComparer());
                set.Add(item1);
                set.Add(item2);
                set.Add(item3);

                other = null;
            }

            //Test 2
            // other is empty and set is empty
            public static void SetupTest2(out HashSet<Item> set, out IEnumerable<Item> other)
            {
                set = new HashSet<Item>(new ItemEqualityComparer());
                other = new List<Item>();
            }

            //Test 3
            //  other is empty and set is single-item
            public static void SetupTest3(out HashSet<Item> set, out IEnumerable<Item> other)
            {
                Item item1 = new Item(1);

                set = new HashSet<Item>(new ItemEqualityComparer());
                set.Add(item1);

                other = new List<Item>();
            }

            //Test 4
            //  other is empty and set is multi-item
            public static void SetupTest4(out HashSet<Item> set, out IEnumerable<Item> other)
            {
                Item item1 = new Item(1);
                Item item2 = new Item(3);
                Item item3 = new Item(-23);

                set = new HashSet<Item>(new ItemEqualityComparer());
                set.Add(item1);
                set.Add(item2);
                set.Add(item3);

                other = new List<Item>();
            }

            //Test 5
            //  other is single-item and set is empty
            public static void SetupTest5(out HashSet<Item> set, out IEnumerable<Item> other)
            {
                Item item1 = new Item(-23);

                set = new HashSet<Item>(new ItemEqualityComparer());

                List<Item> list = new List<Item>();
                list.Add(item1);
                other = list;
            }

            //Test 6
            //  other is single-item and set is single-item with a different item
            public static void SetupTest6(out HashSet<Item> set, out IEnumerable<Item> other)
            {
                Item item1 = new Item(-23);
                Item item2 = new Item(34);

                set = new HashSet<Item>(new ItemEqualityComparer());
                set.Add(item2);

                List<Item> list = new List<Item>();
                list.Add(item1);
                other = list;
            }

            //Test 7
            //  other is single-item and set is single-item with the same item
            public static void SetupTest7(out HashSet<Item> set, out IEnumerable<Item> other)
            {
                Item item1 = new Item(-23);

                set = new HashSet<Item>(new ItemEqualityComparer());
                set.Add(item1);

                List<Item> list = new List<Item>();
                list.Add(item1);
                other = list;
            }

            //Test 8
            //  other is single-item and set is multi-item where set and other are disjoint
            public static void SetupTest8(out HashSet<Item> set, out IEnumerable<Item> other)
            {
                Item item1 = new Item(-23);
                Item item2 = new Item(234);
                Item item3 = new Item(0);
                Item item4 = new Item(233);

                set = new HashSet<Item>(new ItemEqualityComparer());
                set.Add(item1);
                set.Add(item2);
                set.Add(item3);

                List<Item> list = new List<Item>();
                list.Add(item4);
                other = list;
            }

            //Test 9
            //  other is single-item and set is multi-item where set contains other
            public static void SetupTest9(out HashSet<Item> set, out IEnumerable<Item> other)
            {
                Item item1 = new Item(-23);
                Item item2 = new Item(234);
                Item item3 = new Item(0);

                set = new HashSet<Item>(new ItemEqualityComparer());
                set.Add(item1);
                set.Add(item2);
                set.Add(item3);

                List<Item> list = new List<Item>();
                list.Add(item2);
                other = list;
            }

            //Test 10
            //  other is multi-item and set is empty
            public static void SetupTest10(out HashSet<Item> set, out IEnumerable<Item> other)
            {
                Item item1 = new Item(-23);
                Item item2 = new Item(234);
                Item item3 = new Item(0);

                set = new HashSet<Item>(new ItemEqualityComparer());

                List<Item> list = new List<Item>();
                list.Add(item1);
                list.Add(item2);
                list.Add(item3);
                other = list;
            }

            //Test 11
            //  other is multi-item and set is single-item and set and other are disjoint
            public static void SetupTest11(out HashSet<Item> set, out IEnumerable<Item> other)
            {
                Item item1 = new Item(-23);
                Item item2 = new Item(234);
                Item item3 = new Item(0);
                Item item4 = new Item(-222);

                set = new HashSet<Item>(new ItemEqualityComparer());
                set.Add(item4);

                List<Item> list = new List<Item>();
                list.Add(item1);
                list.Add(item2);
                list.Add(item3);
                other = list;
            }

            //Test 12
            //  other is multi-item and set is single-item and other contains set
            public static void SetupTest12(out HashSet<Item> set, out IEnumerable<Item> other)
            {
                Item item1 = new Item(-23);
                Item item2 = new Item(234);
                Item item3 = new Item(0);

                set = new HashSet<Item>(new ItemEqualityComparer());
                set.Add(item3);

                List<Item> list = new List<Item>();
                list.Add(item1);
                list.Add(item2);
                list.Add(item3);
                other = list;
            }

            //Test 13
            //  other is multi-item and set is multi-item and set and other disjoint
            public static void SetupTest13(out HashSet<Item> set, out IEnumerable<Item> other)
            {
                Item item1 = new Item(-23);
                Item item2 = new Item(234);
                Item item3 = new Item(0);
                Item item4 = new Item(23);
                Item item5 = new Item(222);
                Item item6 = new Item(322);

                set = new HashSet<Item>(new ItemEqualityComparer());
                set.Add(item4);
                set.Add(item5);
                set.Add(item6);

                List<Item> list = new List<Item>();
                list.Add(item1);
                list.Add(item2);
                list.Add(item3);
                other = list;
            }

            //Test 14
            //  other is multi-item and set is multi-item and set and other overlap but are non-comparable
            public static void SetupTest14(out HashSet<Item> set, out IEnumerable<Item> other)
            {
                Item item1 = new Item(-23);
                Item item2 = new Item(234);
                Item item3 = new Item(0);
                Item item4 = new Item(23);
                Item item5 = new Item(222);

                set = new HashSet<Item>(new ItemEqualityComparer());
                set.Add(item4);
                set.Add(item1);
                set.Add(item2);
                set.Add(item5);

                List<Item> list = new List<Item>();
                list.Add(item1);
                list.Add(item2);
                list.Add(item3);
                other = list;
            }

            //Test 15
            //  other is multi-item and set is multi-item and other is a proper subset of set
            public static void SetupTest15(out HashSet<Item> set, out IEnumerable<Item> other)
            {
                Item item1 = new Item(-23);
                Item item2 = new Item(234);
                Item item3 = new Item(0);
                Item item4 = new Item(23);
                Item item5 = new Item(222);

                set = new HashSet<Item>(new ItemEqualityComparer());
                set.Add(item4);
                set.Add(item1);
                set.Add(item2);
                set.Add(item3);
                set.Add(item5);

                List<Item> list = new List<Item>();
                list.Add(item1);
                list.Add(item2);
                list.Add(item3);
                other = list;
            }

            //Test 16
            //  other is multi-item and set is multi-item and set is a proper subset of other
            public static void SetupTest16(out HashSet<Item> set, out IEnumerable<Item> other)
            {
                Item item1 = new Item(-23);
                Item item2 = new Item(234);
                Item item3 = new Item(0);
                Item item4 = new Item(23);
                Item item5 = new Item(222);

                set = new HashSet<Item>(new ItemEqualityComparer());
                set.Add(item4);
                set.Add(item3);
                set.Add(item5);

                List<Item> list = new List<Item>();
                list.Add(item1);
                list.Add(item2);
                list.Add(item3);
                list.Add(item4);
                list.Add(item5);
                other = list;
            }

            //Test 17
            //  other is multi-item and set is multi-item and set and other are equal
            public static void SetupTest17(out HashSet<Item> set, out IEnumerable<Item> other)
            {
                Item item1 = new Item(-23);
                Item item2 = new Item(234);
                Item item3 = new Item(0);
                Item item4 = new Item(23);
                Item item5 = new Item(222);

                set = new HashSet<Item>(new ItemEqualityComparer());
                set.Add(item2);
                set.Add(item1);
                set.Add(item4);
                set.Add(item3);
                set.Add(item5);

                List<Item> list = new List<Item>();
                list.Add(item1);
                list.Add(item2);
                list.Add(item3);
                list.Add(item4);
                list.Add(item5);
                other = list;
            }

            //Test 18
            //  other is set and set is empty
            public static void SetupTest18(out HashSet<Item> set, out IEnumerable<Item> other)
            {
                set = new HashSet<Item>();
                other = set;
            }

            //Test 19
            //  other is set and set is single-item and set contains set
            public static void SetupTest19(out HashSet<IEnumerable> set, out IEnumerable<IEnumerable> other)
            {
                IEnumerableEqualityComparer comparer = new IEnumerableEqualityComparer();
                set = new HashSet<IEnumerable>(comparer);
                comparer.setSelf(set);
                set.Add(set);
                other = set;
            }

            //Test 20
            //  other is set and set is single-item and set does not contain set
            public static void SetupTest20(out HashSet<Item> set, out IEnumerable<Item> other)
            {
                Item item = new Item(22);

                set = new HashSet<Item>(new ItemEqualityComparer());
                set.Add(item);

                other = set;
            }

            //Test 21
            //  other is set and set is multi-item and set contains set
            public static void SetupTest21(out HashSet<IEnumerable> set, out IEnumerable<IEnumerable> other)
            {
                List<int> item1 = new List<int>(new int[] { 1, 3, 5, -2 });
                List<int> item2 = new List<int>(new int[] { 1, -3, 5, -2 });

                IEnumerableEqualityComparer comparer = new IEnumerableEqualityComparer();
                set = new HashSet<IEnumerable>(comparer);
                comparer.setSelf(set);
                set.Add(item1);
                set.Add(item2);
                set.Add(set);

                other = set;
            }

            //Test 22
            //  other is set and set is multi-item and set does not contain set
            public static void SetupTest22(out HashSet<Item> set, out IEnumerable<Item> other)
            {
                Item item1 = new Item(22);
                Item item2 = new Item(-2);
                Item item3 = new Item(-2222);

                set = new HashSet<Item>(new ItemEqualityComparer());
                set.Add(item1);
                set.Add(item2);
                set.Add(item3);

                other = set;
            }

            #region special items tests where item is the only item in other - Test 23-32
            //Test 23 - Special Item Test 1
            public static void SetupTest23(out HashSet<IEnumerable> set, out IEnumerable<IEnumerable> other)
            {
                IEnumerable item1;

                SpecialItemTests.SetupTest1(out set, out item1);

                List<IEnumerable> list = new List<IEnumerable>();
                list.Add(item1);
                other = list;
            }

            //Test 24 - Special Item Test 2
            public static void SetupTest24(out HashSet<IEnumerable> set, out IEnumerable<IEnumerable> other)
            {
                IEnumerable item1;

                SpecialItemTests.SetupTest2(out set, out item1);

                List<IEnumerable> list = new List<IEnumerable>();
                list.Add(item1);
                other = list;
            }

            //Test 25 - Special Item Test 3
            public static void SetupTest25(out HashSet<int> set, out IEnumerable<int> other)
            {
                int item1;

                SpecialItemTests.SetupTest3(out set, out item1);

                List<int> list = new List<int>();
                list.Add(item1);
                other = list;
            }

            //Test 26 - Special Item Test 4
            public static void SetupTest26(out HashSet<Item> set, out IEnumerable<Item> other)
            {
                Item item1;

                SpecialItemTests.SetupTest4(out set, out item1);

                List<Item> list = new List<Item>();
                list.Add(item1);
                other = list;
            }

            //Test 27 - Special Item Test 5
            public static void SetupTest27(out HashSet<int> set, out IEnumerable<int> other)
            {
                int item1;

                SpecialItemTests.SetupTest5(out set, out item1);

                List<int> list = new List<int>();
                list.Add(item1);
                other = list;
            }

            //Test 28 - Special Item Test 6
            public static void SetupTest28(out HashSet<Item> set, out IEnumerable<Item> other)
            {
                Item item1;

                SpecialItemTests.SetupTest6(out set, out item1);

                List<Item> list = new List<Item>();
                list.Add(item1);
                other = list;
            }

            //Test 29 - Special Item Test 7
            public static void SetupTest29(out HashSet<Item> set, out IEnumerable<Item> other)
            {
                Item item1;

                SpecialItemTests.SetupTest7(out set, out item1);

                List<Item> list = new List<Item>();
                list.Add(item1);
                other = list;
            }

            //Test 30 - Special Item Test 8
            public static void SetupTest30(out HashSet<Item> set, out IEnumerable<Item> other)
            {
                Item item1;

                SpecialItemTests.SetupTest8(out set, out item1);

                List<Item> list = new List<Item>();
                list.Add(item1);
                other = list;
            }

            //Test 31 - Special Item Test 9
            public static void SetupTest31(out HashSet<Item> set, out IEnumerable<Item> other)
            {
                Item item1;

                SpecialItemTests.SetupTest9(out set, out item1);

                List<Item> list = new List<Item>();
                list.Add(item1);
                other = list;
            }

            //Test 32 - Special Item Test 10
            public static void SetupTest32(out HashSet<Item> set, out IEnumerable<Item> other)
            {
                Item item1;

                SpecialItemTests.SetupTest10(out set, out item1);

                List<Item> list = new List<Item>();
                list.Add(item1);
                other = list;
            }

            #endregion

            #region special items tests where item is one of the items in other - Test 33-42
            //Test 33 - Special Item Test 1
            public static void SetupTest33(out HashSet<IEnumerable> set, out IEnumerable<IEnumerable> other)
            {
                IEnumerable item1;
                IEnumerable item2 = new List<int>(new int[] { 1, 4, 5, 6, 8 });
                IEnumerable item3 = new List<int>(new int[] { -1, 4, -5, 6, -8 });

                SpecialItemTests.SetupTest1(out set, out item1);

                List<IEnumerable> list = new List<IEnumerable>();
                list.Add(item2);
                list.Add(item1);
                list.Add(item3);
                other = list;
            }

            //Test 34 - Special Item Test 2
            public static void SetupTest34(out HashSet<IEnumerable> set, out IEnumerable<IEnumerable> other)
            {
                IEnumerable item1;
                IEnumerable item2 = new List<int>(new int[] { 1, 4, 5, 6, 8 });
                IEnumerable item3 = new List<int>(new int[] { -1, 4, -5, 6, -8 });

                SpecialItemTests.SetupTest2(out set, out item1);

                List<IEnumerable> list = new List<IEnumerable>();
                list.Add(item2);
                list.Add(item1);
                list.Add(item3);
                other = list;
            }

            //Test 35 - Special Item Test 3
            public static void SetupTest35(out HashSet<int> set, out IEnumerable<int> other)
            {
                int item1;
                int item2 = -111;
                int item3 = 111;

                SpecialItemTests.SetupTest3(out set, out item1);

                List<int> list = new List<int>();
                list.Add(item1);
                list.Add(item2);
                list.Add(item3);
                other = list;
            }

            //Test 36 - Special Item Test 4
            public static void SetupTest36(out HashSet<Item> set, out IEnumerable<Item> other)
            {
                Item item1;
                Item item2 = new Item(292);
                Item item3 = new Item(-222);

                SpecialItemTests.SetupTest4(out set, out item1);

                List<Item> list = new List<Item>();
                list.Add(item1);
                list.Add(item2);
                list.Add(item3);
                other = list;
            }

            //Test 37 - Special Item Test 5
            public static void SetupTest37(out HashSet<int> set, out IEnumerable<int> other)
            {
                int item1;
                int item2 = -111;
                int item3 = 111;

                SpecialItemTests.SetupTest5(out set, out item1);

                List<int> list = new List<int>();
                list.Add(item1);
                list.Add(item2);
                list.Add(item3);
                other = list;
            }

            //Test 38 - Special Item Test 6
            public static void SetupTest38(out HashSet<Item> set, out IEnumerable<Item> other)
            {
                Item item1;
                Item item2 = new Item(292);
                Item item3 = new Item(-222);

                SpecialItemTests.SetupTest6(out set, out item1);

                List<Item> list = new List<Item>();
                list.Add(item1);
                list.Add(item2);
                list.Add(item3);
                other = list;
            }

            //Test 39 - Special Item Test 7
            public static void SetupTest39(out HashSet<Item> set, out IEnumerable<Item> other)
            {
                Item item1;
                Item item2 = new Item(292);
                Item item3 = new Item(-222);

                SpecialItemTests.SetupTest7(out set, out item1);

                List<Item> list = new List<Item>();
                list.Add(item1);
                list.Add(item2);
                list.Add(item3);
                other = list;
            }

            //Test 40 - Special Item Test 8
            public static void SetupTest40(out HashSet<Item> set, out IEnumerable<Item> other)
            {
                Item item1;
                Item item2 = new Item(292);
                Item item3 = new Item(-222);

                SpecialItemTests.SetupTest8(out set, out item1);

                List<Item> list = new List<Item>();
                list.Add(item1);
                list.Add(item2);
                list.Add(item3);
                other = list;
            }

            //Test 41 - Special Item Test 9
            public static void SetupTest41(out HashSet<Item> set, out IEnumerable<Item> other)
            {
                Item item1;
                Item item2 = new Item(292);
                Item item3 = new Item(-222);

                SpecialItemTests.SetupTest9(out set, out item1);

                List<Item> list = new List<Item>();
                list.Add(item1);
                list.Add(item2);
                list.Add(item3);
                other = list;
            }

            //Test 42 - Special Item Test 10
            public static void SetupTest42(out HashSet<Item> set, out IEnumerable<Item> other)
            {
                Item item1;
                Item item2 = new Item(292);
                Item item3 = new Item(-222);

                SpecialItemTests.SetupTest10(out set, out item1);

                List<Item> list = new List<Item>();
                list.Add(item1);
                list.Add(item2);
                list.Add(item3);
                other = list;
            }

            #endregion
        }
        //Test Framework for setting up tests where an Item in the test is special
        public class SpecialItemTests
        {
            //Test 1
            //  Item is the set and item is in the set
            public static void SetupTest1(out HashSet<IEnumerable> set, out IEnumerable item)
            {
                List<int> item1 = new List<int>(new int[] { 1, 2 });
                List<int> item2 = new List<int>(new int[] { 2, -1 });

                IEnumerableEqualityComparer comparer = new IEnumerableEqualityComparer();
                set = new HashSet<IEnumerable>(comparer);
                comparer.setSelf(set);
                set.Add(item1);
                set.Add(item2);
                set.Add(set);

                item = set;
            }

            //Test 2
            //  Item is the set and item is not in the set
            public static void SetupTest2(out HashSet<IEnumerable> set, out IEnumerable item)
            {
                List<int> item1 = new List<int>(new int[] { 1, 2 });
                List<int> item2 = new List<int>(new int[] { 2, -1 });

                IEnumerableEqualityComparer comparer = new IEnumerableEqualityComparer();
                set = new HashSet<IEnumerable>(comparer);
                comparer.setSelf(set);
                set.Add(item1);
                set.Add(item2);

                item = set;
            }

            //Test 3
            //  Item is Default<T> and in set. T is a numeric type
            public static void SetupTest3(out HashSet<int> set, out int item)
            {
                int x = 0;

                set = new HashSet<int>(new int[] { 1, 2, 3, 4, x, 6, 7 });
                item = x;
            }

            //Test 4
            //  Item is Default<T> and in set. T is a reference type
            public static void SetupTest4(out HashSet<Item> set, out Item item)
            {
                Item x = null;
                Item item1 = new Item(3);
                Item item2 = new Item(-3);

                set = new HashSet<Item>(new ItemEqualityComparer());
                set.Add(item1);
                set.Add(item2);
                set.Add(x);

                item = x;
            }

            //Test 5
            //  Item is Default<T> and not in set. T is a numeric type
            public static void SetupTest5(out HashSet<int> set, out int item)
            {
                int x = 0;

                set = new HashSet<int>(new int[] { 1, 2, 3, 4, 5, 6, 7 });
                item = x;
            }

            //Test 6
            //  Item is Default<T> and not in set.  T is a reference type
            public static void SetupTest6(out HashSet<Item> set, out Item item)
            {
                Item x = null;
                Item item1 = new Item(3);
                Item item2 = new Item(-3);

                set = new HashSet<Item>(new ItemEqualityComparer());
                set.Add(item1);
                set.Add(item2);

                item = x;
            }

            //Test 7
            //  Item is equal to an item in set but different.
            public static void SetupTest7(out HashSet<Item> set, out Item item)
            {
                Item x1 = new Item(1);
                Item x2 = new Item(1);
                Item item1 = new Item(2);
                Item item2 = new Item(3);

                set = new HashSet<Item>(new ItemEqualityComparer());
                set.Add(x1);
                set.Add(item1);
                set.Add(item2);

                item = x2;
            }

            //Test 8
            //  Item shares hash value with unequal item in set
            public static void SetupTest8(out HashSet<Item> set, out Item item)
            {
                Item x1 = new Item(1);
                Item x2 = new Item(-1);
                Item item1 = new Item(2);
                Item item2 = new Item(3);

                set = new HashSet<Item>(new ItemAbsoluteEqualityComparer());
                set.Add(x1);
                set.Add(item1);
                set.Add(item2);

                item = x2;
            }

            //Test 9
            //  Item was previously in set but not currently
            public static void SetupTest9(out HashSet<Item> set, out Item item)
            {
                Item x1 = new Item(1);
                Item item1 = new Item(2);
                Item item2 = new Item(3);

                set = new HashSet<Item>(new ItemEqualityComparer());
                set.Add(x1);
                set.Add(item1);
                set.Add(item2);
                set.Remove(x1);

                item = x1;
            }

            //Test 10
            //  Item was previously removed from set but in it currently
            public static void SetupTest10(out HashSet<Item> set, out Item item)
            {
                Item x1 = new Item(1);
                Item item1 = new Item(2);
                Item item2 = new Item(3);

                set = new HashSet<Item>(new ItemEqualityComparer());
                set.Add(x1);
                set.Add(item1);
                set.Add(item2);
                set.Remove(x1);
                set.Add(x1);

                item = x1;
            }
        }
    }
}
