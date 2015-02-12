// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;
using Tests.HashSet_HashSetTestSupport;
using Tests.HashSet_SetCollectionComparerTests;

namespace Tests
{
    namespace HashSet_SetCollectionComparerTests
    {    //Test framework for testing the use of comparers when
         //determining the relationship between a collection and a set
        public class SetCollectionComparerTests
        {
            #region Set/Item Comparer Tests where Item is in collection. Test 1-5

            //Test 1 - Set/Item Comparer Test 1
            public static void SetupTest1(out HashSet<ValueItem> set, out IEnumerable<ValueItem> other)
            {
                List<ValueItem> list = new List<ValueItem>();
                ValueItem item1 = new ValueItem(134, -15);
                ValueItem item2 = new ValueItem(14, 14);
                ValueItem item3 = new ValueItem(19999, -12);
                ValueItem innerItem;

                SetItemComparerTests.SetupTest1(out set, out innerItem);

                list.Add(item1);
                list.Add(item2);
                list.Add(item3);
                list.Add(innerItem);

                other = list;
            }

            //Test 2 - Set/Item Comparer Test 2
            public static void SetupTest2(out HashSet<ValueItem> set, out IEnumerable<ValueItem> other)
            {
                List<ValueItem> list = new List<ValueItem>();
                ValueItem item1 = new ValueItem(134, -15);
                ValueItem item2 = new ValueItem(14, 14);
                ValueItem item3 = new ValueItem(19999, -12);
                ValueItem innerItem;

                SetItemComparerTests.SetupTest2(out set, out innerItem);

                list.Add(item1);
                list.Add(item2);
                list.Add(item3);
                list.Add(innerItem);

                other = list;
            }

            //Test 3 - Set/Item Comparer Test 3
            public static void SetupTest3(out HashSet<ValueItem> set, out IEnumerable<ValueItem> other)
            {
                List<ValueItem> list = new List<ValueItem>();
                ValueItem item1 = new ValueItem(134, -15);
                ValueItem item2 = new ValueItem(14, 14);
                ValueItem item3 = new ValueItem(19999, -12);
                ValueItem innerItem;

                SetItemComparerTests.SetupTest3(out set, out innerItem);

                list.Add(item1);
                list.Add(item2);
                list.Add(item3);
                list.Add(innerItem);

                other = list;
            }

            //Test 4 - Set/Item Comparer Test 4
            public static void SetupTest4(out HashSet<ValueItem> set, out IEnumerable<ValueItem> other)
            {
                List<ValueItem> list = new List<ValueItem>();
                ValueItem item1 = new ValueItem(134, -15);
                ValueItem item2 = new ValueItem(14, 14);
                ValueItem item3 = new ValueItem(19999, -12);
                ValueItem innerItem;

                SetItemComparerTests.SetupTest4(out set, out innerItem);

                list.Add(item1);
                list.Add(item2);
                list.Add(item3);
                list.Add(innerItem);

                other = list;
            }

            //Test 5 - Set/Item Comparer Test 5
            public static void SetupTest5(out HashSet<HashSet<IEnumerable>> set, out IEnumerable<HashSet<IEnumerable>> other)
            {
                List<HashSet<IEnumerable>> other2 = new List<HashSet<IEnumerable>>();

                HashSet<IEnumerable> hs1 = new HashSet<IEnumerable>(new ValueItem[] { new ValueItem(10, 10), new ValueItem(20, 20), new ValueItem(30, 30), new ValueItem(40, 40) }); ;
                HashSet<IEnumerable> hs2 = new HashSet<IEnumerable>(new ValueItem[] { new ValueItem(1, 1), new ValueItem(2, 2), new ValueItem(3, 3), new ValueItem(4, 4) }); ;
                HashSet<IEnumerable> hs3 = new HashSet<IEnumerable>(new ValueItem[] { new ValueItem(-1, -1), new ValueItem(-2, -2), new ValueItem(-3, -3), new ValueItem(-4, -4) }); ;
                HashSet<IEnumerable> innerItem;

                SetItemComparerTests.SetupTest5(out set, out innerItem);

                other2.Add(hs1);
                other2.Add(hs2);
                other2.Add(hs3);
                other2.Add(innerItem);

                other = other2;
            }

            #endregion

            #region Set/Item Comparer Tests where Item is the only item in collection. Test 6-10
            //Test 6 - Set/Item Comparer Test 1
            public static void SetupTest6(out HashSet<ValueItem> set, out IEnumerable<ValueItem> other)
            {
                List<ValueItem> list = new List<ValueItem>();
                ValueItem innerItem;

                SetItemComparerTests.SetupTest1(out set, out innerItem);

                list.Add(innerItem);
                other = list;
            }

            //Test 7 - Set/Item Comparer Test 2
            public static void SetupTest7(out HashSet<ValueItem> set, out IEnumerable<ValueItem> other)
            {
                List<ValueItem> list = new List<ValueItem>();
                ValueItem innerItem;

                SetItemComparerTests.SetupTest2(out set, out innerItem);

                list.Add(innerItem);
                other = list;
            }

            //Test 8 - Set/Item Comparer Test 3
            public static void SetupTest8(out HashSet<ValueItem> set, out IEnumerable<ValueItem> other)
            {
                List<ValueItem> list = new List<ValueItem>();
                ValueItem innerItem;

                SetItemComparerTests.SetupTest3(out set, out innerItem);

                list.Add(innerItem);
                other = list;
            }

            //Test 9 - Set/Item Comparer Test 4
            public static void SetupTest9(out HashSet<ValueItem> set, out IEnumerable<ValueItem> other)
            {
                List<ValueItem> list = new List<ValueItem>();
                ValueItem innerItem;

                SetItemComparerTests.SetupTest4(out set, out innerItem);

                list.Add(innerItem);
                other = list;
            }

            //Test 10 - Set/Item Comparer Test 5
            public static void SetupTest10(out HashSet<HashSet<IEnumerable>> set, out IEnumerable<HashSet<IEnumerable>> other)
            {
                List<HashSet<IEnumerable>> other2 = new List<HashSet<IEnumerable>>();
                HashSet<IEnumerable> innerItem;

                SetItemComparerTests.SetupTest5(out set, out innerItem);

                other2.Add(innerItem);
                other = other2;
            }

            #endregion

            #region Set/Item Comparer Tests where collection is the item. Test 11-15

            //Test 11 - Set/Item Comparer Test 1
            public static void SetupTest11(out HashSet<ValueItem> set, out IEnumerable<ValueItem> other)
            {
                ValueItem innerItem;

                SetItemComparerTests.SetupTest1(out set, out innerItem);

                other = innerItem;
            }

            //Test 12 - Set/Item Comparer Test 2
            public static void SetupTest12(out HashSet<ValueItem> set, out IEnumerable<ValueItem> other)
            {
                ValueItem innerItem;

                SetItemComparerTests.SetupTest2(out set, out innerItem);

                other = innerItem;
            }

            //Test 13 - Set/Item Comparer Test 3
            public static void SetupTest13(out HashSet<ValueItem> set, out IEnumerable<ValueItem> other)
            {
                ValueItem innerItem;

                SetItemComparerTests.SetupTest3(out set, out innerItem);

                other = innerItem;
            }

            //Test 14 - Set/Item Comparer Test 4
            public static void SetupTest14(out HashSet<ValueItem> set, out IEnumerable<ValueItem> other)
            {
                ValueItem innerItem;

                SetItemComparerTests.SetupTest4(out set, out innerItem);

                other = innerItem;
            }

            //Test 15 - Set/Item Comparer Test 5
            public static void SetupTest15(out HashSet<HashSet<IEnumerable>> set, out IEnumerable<HashSet<IEnumerable>> other)
            {
                HashSet<IEnumerable> innerItem;

                SetItemComparerTests.SetupTest5(out set, out innerItem);

                other = set;
            }

            #endregion
        }
        //Test framework for testing the use of comparers when
        //determining the relationship between an item and a set
        public class SetItemComparerTests
        {
            //Test 1
            // item same as element in set by default comparer, different by sets comparer - set contains item that is equal by sets comparer
            public static void SetupTest1(out HashSet<ValueItem> set, out ValueItem item)
            {
                set = new HashSet<ValueItem>(new ValueItemYEqualityComparer());
                set.Add(new ValueItem(34, -5));
                set.Add(new ValueItem(4, 4));
                set.Add(new ValueItem(9999, -2));

                item = new ValueItem(34, -2);
            }

            //Test 2
            // item same as element in set by default comparer, different by sets comparer - set does not contain item that is equal by sets comparer
            public static void SetupTest2(out HashSet<ValueItem> set, out ValueItem item)
            {
                set = new HashSet<ValueItem>(new ValueItemYEqualityComparer());
                set.Add(new ValueItem(34, -5));
                set.Add(new ValueItem(4, 4));
                set.Add(new ValueItem(9999, -20));

                item = new ValueItem(34, -2);
            }

            //Test 3
            // item same as element in set by sets comparer, different by default comparer - set contains item that is equal by default comparer
            public static void SetupTest3(out HashSet<ValueItem> set, out ValueItem item)
            {
                set = new HashSet<ValueItem>(new ValueItemYEqualityComparer());
                set.Add(new ValueItem(34, -5));
                set.Add(new ValueItem(4, 4));
                set.Add(new ValueItem(9999, -2));

                item = new ValueItem(34, -2);
            }

            //Test 4
            // item same as element in set by sets comparer, different by default comparer - set does not contain item that is equal by default comparer
            public static void SetupTest4(out HashSet<ValueItem> set, out ValueItem item)
            {
                ValueItem item1 = new ValueItem(340, -5);
                ValueItem item2 = new ValueItem(4, 4);
                ValueItem item3 = new ValueItem(9999, -2);

                set = new HashSet<ValueItem>(new ValueItemYEqualityComparer());
                set.Add(item1);
                set.Add(item2);
                set.Add(item3);

                item = new ValueItem(34, -2);
            }

            //Test 5
            // item contains set and item in set with GetSetComparer<T> as comparer
            public static void SetupTest5(out HashSet<HashSet<IEnumerable>> outerSet, out HashSet<IEnumerable> item)
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
                outerSet = new HashSet<HashSet<IEnumerable>>(new SetEqualityComparer<IEnumerable>());

                set.Add(outerSet);
                outerSet.Add(itemhs1);
                outerSet.Add(itemhs2);
                outerSet.Add(set);

                item = set;
            }
        }
    }
}
