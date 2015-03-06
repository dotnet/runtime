// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Tests.HashSet_HashSetTestSupport;
using Tests.HashSet_SetCollectionDuplicateItemTests;

namespace Tests
{
    namespace HashSet_SetCollectionDuplicateItemTests
    {    //Test framework for testing set and collection
         //interaction when the collection contains duplicates
         //of the same item
        public class SetCollectionDuplicateItemTests
        {
            //Test 1: other collection is multi-item with duplicates, set is empty
            public static void SetupTest1(out HashSet<ValueItem> set, out IEnumerable<ValueItem> other)
            {
                List<ValueItem> list = new List<ValueItem>();

                ValueItem item1 = new ValueItem(34, -5);
                ValueItem item2 = new ValueItem(4, 4);
                ValueItem item3 = new ValueItem(9999, -2);
                ValueItem item4 = new ValueItem(99, -2);

                list.Add(item1);
                list.Add(item2);
                list.Add(item2);
                list.Add(item3);
                list.Add(item4);
                list.Add(item4);

                set = new HashSet<ValueItem>();
                other = list;
            }

            //Test 2: other collection is multi-item with duplicates, set contains a single item not in other
            public static void SetupTest2(out HashSet<ValueItem> set, out IEnumerable<ValueItem> other)
            {
                List<ValueItem> list = new List<ValueItem>();

                ValueItem item1 = new ValueItem(34, -5);
                ValueItem item2 = new ValueItem(4, 4);
                ValueItem item3 = new ValueItem(9999, -2);
                ValueItem item4 = new ValueItem(99, -2);
                ValueItem item5 = new ValueItem(101, 101);

                list.Add(item1);
                list.Add(item2);
                list.Add(item2);
                list.Add(item3);
                list.Add(item4);
                list.Add(item4);

                set = new HashSet<ValueItem>();
                set.Add(item5);

                other = list;
            }

            //Test 3: other collection is multi-item with duplicates, set contains a single item that is in other but not a duplicate in other
            public static void SetupTest3(out HashSet<ValueItem> set, out IEnumerable<ValueItem> other)
            {
                List<ValueItem> list = new List<ValueItem>();

                ValueItem item1 = new ValueItem(34, -5);
                ValueItem item2 = new ValueItem(4, 4);
                ValueItem item3 = new ValueItem(9999, -2);
                ValueItem item4 = new ValueItem(99, -2);

                list.Add(item1);
                list.Add(item2);
                list.Add(item2);
                list.Add(item3);
                list.Add(item4);
                list.Add(item4);

                set = new HashSet<ValueItem>();
                set.Add(item3);

                other = list;
            }

            //Test 4: other collection is multi-item with duplicates, set contains a single item that is a duplicate in other
            public static void SetupTest4(out HashSet<ValueItem> set, out IEnumerable<ValueItem> other)
            {
                List<ValueItem> list = new List<ValueItem>();

                ValueItem item1 = new ValueItem(34, -5);
                ValueItem item2 = new ValueItem(4, 4);
                ValueItem item3 = new ValueItem(9999, -2);
                ValueItem item4 = new ValueItem(99, -2);

                list.Add(item1);
                list.Add(item2);
                list.Add(item2);
                list.Add(item3);
                list.Add(item4);
                list.Add(item4);

                set = new HashSet<ValueItem>();
                set.Add(item2);

                other = list;
            }

            //Test 5: other collection is multi-item with duplicates, set is multi-item as well, set and other are disjoint
            public static void SetupTest5(out HashSet<ValueItem> set, out IEnumerable<ValueItem> other)
            {
                List<ValueItem> list = new List<ValueItem>();

                ValueItem itemo1 = new ValueItem(34, -5);
                ValueItem itemo2 = new ValueItem(4, 4);
                ValueItem itemo3 = new ValueItem(9999, -2);
                ValueItem itemo4 = new ValueItem(99, -2);
                ValueItem items1 = new ValueItem(-34, 5);
                ValueItem items2 = new ValueItem(-4, -4);
                ValueItem items3 = new ValueItem(-9999, 2);
                ValueItem items4 = new ValueItem(-99, 2);

                list.Add(itemo1);
                list.Add(itemo2);
                list.Add(itemo2);
                list.Add(itemo3);
                list.Add(itemo4);
                list.Add(itemo4);

                set = new HashSet<ValueItem>();
                set.Add(items1);
                set.Add(items2);
                set.Add(items3);
                set.Add(items4);

                other = list;
            }

            //Test 6: other collection is multi-item with duplicates, set is multi-item as well, set and other overlap but are non-comparable, the overlap contains duplicate items from other
            public static void SetupTest6(out HashSet<ValueItem> set, out IEnumerable<ValueItem> other)
            {
                List<ValueItem> list = new List<ValueItem>();

                ValueItem itemo1 = new ValueItem(34, -5);
                ValueItem itemo2 = new ValueItem(4, 4);
                ValueItem itemo3 = new ValueItem(9999, -2);
                ValueItem itemo4 = new ValueItem(99, -2);
                ValueItem items1 = new ValueItem(-34, 5);
                ValueItem items2 = new ValueItem(-4, -4);
                ValueItem items3 = new ValueItem(-9999, 2);
                ValueItem items4 = new ValueItem(-99, 2);

                list.Add(itemo1);
                list.Add(itemo2);
                list.Add(itemo2);
                list.Add(itemo3);
                list.Add(itemo4);
                list.Add(itemo4);
                list.Add(items2);
                list.Add(items4);

                set = new HashSet<ValueItem>();
                set.Add(items1);
                set.Add(items2);
                set.Add(items3);
                set.Add(items4);
                set.Add(itemo2);
                set.Add(itemo3);
                set.Add(itemo4);

                other = list;
            }

            //Test 7: other collection is multi-item with duplicates, set is multi-item as well, set and other overlap but are non-comparable, the overlap does not contain duplicate items from other
            public static void SetupTest7(out HashSet<ValueItem> set, out IEnumerable<ValueItem> other)
            {
                List<ValueItem> list = new List<ValueItem>();

                ValueItem itemo1 = new ValueItem(34, -5);
                ValueItem itemo2 = new ValueItem(4, 4);
                ValueItem itemo3 = new ValueItem(9999, -2);
                ValueItem itemo4 = new ValueItem(99, -2);
                ValueItem items1 = new ValueItem(-34, 5);
                ValueItem items2 = new ValueItem(-4, -4);
                ValueItem items3 = new ValueItem(-9999, 2);
                ValueItem items4 = new ValueItem(-99, 2);

                list.Add(itemo1);
                list.Add(itemo2);
                list.Add(itemo2);
                list.Add(itemo3);
                list.Add(itemo4);
                list.Add(itemo4);
                list.Add(items2);
                list.Add(items4);

                set = new HashSet<ValueItem>();
                set.Add(items1);
                set.Add(items2);
                set.Add(items3);
                set.Add(items4);
                set.Add(itemo3);

                other = list;
            }

            //Test 8: other collection is multi-item with duplicates, set is multi-item as well, other is a proper subset of set
            public static void SetupTest8(out HashSet<ValueItem> set, out IEnumerable<ValueItem> other)
            {
                List<ValueItem> list = new List<ValueItem>();

                ValueItem itemo1 = new ValueItem(34, -5);
                ValueItem itemo2 = new ValueItem(4, 4);
                ValueItem itemo3 = new ValueItem(9999, -2);
                ValueItem itemo4 = new ValueItem(99, -2);
                ValueItem items1 = new ValueItem(-34, 5);
                ValueItem items2 = new ValueItem(-4, -4);
                ValueItem items3 = new ValueItem(-9999, 2);
                ValueItem items4 = new ValueItem(-99, 2);

                list.Add(itemo1);
                list.Add(itemo2);
                list.Add(itemo2);
                list.Add(itemo3);
                list.Add(itemo4);
                list.Add(itemo4);

                set = new HashSet<ValueItem>();
                set.Add(items1);
                set.Add(items2);
                set.Add(items3);
                set.Add(items4);
                set.Add(itemo1);
                set.Add(itemo2);
                set.Add(itemo3);
                set.Add(itemo4);

                other = list;
            }

            //Test 9: other collection is multi-item with duplicates, set is multi-item as well, set is a proper subset of other, set contains duplicate items from other
            public static void SetupTest9(out HashSet<ValueItem> set, out IEnumerable<ValueItem> other)
            {
                List<ValueItem> list = new List<ValueItem>();

                ValueItem itemo1 = new ValueItem(34, -5);
                ValueItem itemo2 = new ValueItem(4, 4);
                ValueItem itemo3 = new ValueItem(9999, -2);
                ValueItem itemo4 = new ValueItem(99, -2);
                ValueItem items1 = new ValueItem(-34, 5);
                ValueItem items2 = new ValueItem(-4, -4);
                ValueItem items3 = new ValueItem(-9999, 2);
                ValueItem items4 = new ValueItem(-99, 2);

                list.Add(itemo1);
                list.Add(itemo2);
                list.Add(itemo2);
                list.Add(itemo3);
                list.Add(itemo4);
                list.Add(itemo4);
                list.Add(items1);
                list.Add(items2);
                list.Add(items2);
                list.Add(items3);
                list.Add(items4);
                list.Add(items4);

                set = new HashSet<ValueItem>();
                set.Add(items1);
                set.Add(items2);
                set.Add(items3);
                set.Add(items4);

                other = list;
            }

            //Test 10: other collection is multi-item with duplicates, set is multi-item as well, set is a proper subset of other, set does not contain duplicate items from other
            public static void SetupTest10(out HashSet<ValueItem> set, out IEnumerable<ValueItem> other)
            {
                List<ValueItem> list = new List<ValueItem>();

                ValueItem itemo1 = new ValueItem(34, -5);
                ValueItem itemo2 = new ValueItem(4, 4);
                ValueItem itemo3 = new ValueItem(9999, -2);
                ValueItem itemo4 = new ValueItem(99, -2);
                ValueItem items1 = new ValueItem(-34, 5);
                ValueItem items2 = new ValueItem(-4, -4);
                ValueItem items3 = new ValueItem(-9999, 2);
                ValueItem items4 = new ValueItem(-99, 2);

                list.Add(itemo1);
                list.Add(itemo2);
                list.Add(itemo2);
                list.Add(itemo3);
                list.Add(itemo4);
                list.Add(itemo4);
                list.Add(items1);
                list.Add(items2);
                list.Add(items3);
                list.Add(items4);

                set = new HashSet<ValueItem>();
                set.Add(items1);
                set.Add(items2);
                set.Add(items3);
                set.Add(items4);

                other = list;
            }

            //Test 11: other collection is multi-item with duplicates, set is multi-item as well, set and other are equal
            public static void SetupTest11(out HashSet<ValueItem> set, out IEnumerable<ValueItem> other)
            {
                List<ValueItem> list = new List<ValueItem>();

                ValueItem itemo1 = new ValueItem(34, -5);
                ValueItem itemo2 = new ValueItem(4, 4);
                ValueItem itemo3 = new ValueItem(9999, -2);
                ValueItem itemo4 = new ValueItem(99, -2);
                ValueItem items1 = new ValueItem(-34, 5);
                ValueItem items2 = new ValueItem(-4, -4);
                ValueItem items3 = new ValueItem(-9999, 2);
                ValueItem items4 = new ValueItem(-99, 2);

                list.Add(itemo1);
                list.Add(itemo2);
                list.Add(itemo2);
                list.Add(itemo3);
                list.Add(itemo4);
                list.Add(itemo4);
                list.Add(items1);
                list.Add(items2);
                list.Add(items2);
                list.Add(items3);
                list.Add(items4);
                list.Add(items4);

                set = new HashSet<ValueItem>();
                set.Add(items1);
                set.Add(items2);
                set.Add(items3);
                set.Add(items4);
                set.Add(itemo1);
                set.Add(itemo2);
                set.Add(itemo3);
                set.Add(itemo4);

                other = list;
            }

            //Test 12: other contains duplicates by sets comparer but not by default comparer
            public static void SetupTest12(out HashSet<ValueItem> set, out IEnumerable<ValueItem> other)
            {
                List<ValueItem> list = new List<ValueItem>();

                ValueItem itemo1 = new ValueItem(34, -5);
                ValueItem itemo2 = new ValueItem(4, -4);
                ValueItem itemo3 = new ValueItem(9999, -12);
                ValueItem itemo4 = new ValueItem(99, -5);
                ValueItem items1 = new ValueItem(-34, 5);
                ValueItem items2 = new ValueItem(-4, -4);
                ValueItem items3 = new ValueItem(-9999, 2);
                ValueItem items4 = new ValueItem(-99, -2);

                list.Add(itemo1);
                list.Add(itemo2);
                list.Add(itemo3);
                list.Add(itemo4);

                set = new HashSet<ValueItem>(new ValueItemYEqualityComparer());
                set.Add(items1);
                set.Add(items2);
                set.Add(items3);
                set.Add(items4);

                other = list;
            }

            //Test 13: other contains duplicates by default comparer but not by sets comparer
            public static void SetupTest13(out HashSet<ValueItem> set, out IEnumerable<ValueItem> other)
            {
                List<ValueItem> list = new List<ValueItem>();

                ValueItem itemo1 = new ValueItem(34, -5);
                ValueItem itemo2 = new ValueItem(4, 4);
                ValueItem itemo3 = new ValueItem(-9999, -12);
                ValueItem itemo4 = new ValueItem(34, 12);
                ValueItem items1 = new ValueItem(-34, 5);
                ValueItem items2 = new ValueItem(-4, -4);
                ValueItem items3 = new ValueItem(-9999, 2);
                ValueItem items4 = new ValueItem(-99, -2);

                list.Add(itemo1);
                list.Add(itemo2);
                list.Add(itemo3);
                list.Add(itemo4);

                set = new HashSet<ValueItem>(new ValueItemYEqualityComparer());
                set.Add(items1);
                set.Add(items2);
                set.Add(items3);
                set.Add(items4);

                other = list;
            }

            //Test 14: set contains duplicate items by default comparer, those items also in other
            public static void SetupTest14(out HashSet<ValueItem> set, out IEnumerable<ValueItem> other)
            {
                List<ValueItem> list = new List<ValueItem>();

                ValueItem itemo1 = new ValueItem(-34, -5);
                ValueItem itemo2 = new ValueItem(4, 4);
                ValueItem itemo3 = new ValueItem(9999, -12);
                ValueItem itemo4 = new ValueItem(-99, 12);
                ValueItem items1 = new ValueItem(-34, 5);
                ValueItem items2 = new ValueItem(-99, -4);
                ValueItem items3 = new ValueItem(-34, 2);
                ValueItem items4 = new ValueItem(-99, -2);

                list.Add(itemo1);
                list.Add(itemo2);
                list.Add(itemo3);
                list.Add(itemo4);

                set = new HashSet<ValueItem>(new ValueItemYEqualityComparer());
                set.Add(items1);
                set.Add(items2);
                set.Add(items3);
                set.Add(items4);

                other = list;
            }

            //Test 15: set contains duplicate items by default comparer, one of those items also in other
            public static void SetupTest15(out HashSet<ValueItem> set, out IEnumerable<ValueItem> other)
            {
                List<ValueItem> list = new List<ValueItem>();

                ValueItem itemo1 = new ValueItem(34, -5);
                ValueItem itemo2 = new ValueItem(4, 4);
                ValueItem itemo3 = new ValueItem(9999, -12);
                ValueItem itemo4 = new ValueItem(-99, 12);
                ValueItem items1 = new ValueItem(-34, 5);
                ValueItem items2 = new ValueItem(-99, -4);
                ValueItem items3 = new ValueItem(-34, 2);
                ValueItem items4 = new ValueItem(-99, -2);

                list.Add(itemo1);
                list.Add(itemo2);
                list.Add(itemo3);
                list.Add(itemo4);

                set = new HashSet<ValueItem>(new ValueItemYEqualityComparer());
                set.Add(items1);
                set.Add(items2);
                set.Add(items3);
                set.Add(items4);

                other = list;
            }

            //Test 16: set contains duplicate items by default comparer, those items not in other
            public static void SetupTest16(out HashSet<ValueItem> set, out IEnumerable<ValueItem> other)
            {
                List<ValueItem> list = new List<ValueItem>();

                ValueItem itemo1 = new ValueItem(34, -5);
                ValueItem itemo2 = new ValueItem(4, 4);
                ValueItem itemo3 = new ValueItem(9999, -12);
                ValueItem itemo4 = new ValueItem(99, 12);
                ValueItem items1 = new ValueItem(-34, 5);
                ValueItem items2 = new ValueItem(-99, -4);
                ValueItem items3 = new ValueItem(-34, 2);
                ValueItem items4 = new ValueItem(-99, -2);

                list.Add(itemo1);
                list.Add(itemo2);
                list.Add(itemo3);
                list.Add(itemo4);

                set = new HashSet<ValueItem>(new ValueItemYEqualityComparer());
                set.Add(items1);
                set.Add(items2);
                set.Add(items3);
                set.Add(items4);

                other = list;
            }
        }
    }
}
