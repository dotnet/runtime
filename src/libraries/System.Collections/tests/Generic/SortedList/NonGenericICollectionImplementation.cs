// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;
using SortedList_NonGenericICollectionImplementation;
using SL = SortedList_SortedListUtils;
using SortedList_ICollection;
using TestSupport.Common_TestSupport;

namespace SortedList_NonGenericICollectionImplementation
{
    public class Driver<T>
    {
        private Test m_test;

        public Driver(Test test)
        {
            m_test = test;
        }

        public static SortedList<int, V> BuildSortedList<V>(V[] builtFrom)
        {
            SortedList<int, V> builtSortedList = new SortedList<int, V>(builtFrom.Length);

            for (int i = 0; i < builtFrom.Length; i++)
            {
                builtSortedList.Add(i, builtFrom[i]);
            }

            return builtSortedList;
        }

        public void TestICollection(T[] items)
        {
            SortedList<int, T> _dictionary = BuildSortedList(items);
            KeyValuePair<int, T>[] arrayToCheck = new KeyValuePair<int, T>[items.Length];
            ((ICollection<KeyValuePair<int, T>>)_dictionary).CopyTo(arrayToCheck, 0);
#if ONLYZEROBOUND
        ICollectionTester<KeyValuePair<int, T>>.RunTest(_dictionary, items.Length, false,
            ((System.Collections.ICollection)_dictionary).SyncRoot, arrayToCheck, false, true);
#else
            var tester = new ICollectionTester<KeyValuePair<int, T>>();
            tester.RunTest(m_test, _dictionary, items.Length, false,
              ((System.Collections.ICollection)_dictionary).SyncRoot, arrayToCheck, false, false);
#endif

            m_test.Eval(((System.Collections.ICollection)_dictionary).SyncRoot.GetType() == typeof(Object),
                "Err_47235fsd! Expected SyncRoot to be an object actual={0}",
                ((System.Collections.ICollection)_dictionary).SyncRoot.GetType());
        }
    }
}
public class NonGenericICollection
{
    public static T[] GenerateArray<T>(int size)
    {
        T[] newArray = new T[size];
        for (int i = 0; i < size; i++)
        {
            //#if DESKTOP
            newArray[i] = (T)System.Convert.ChangeType((System.Object)i, typeof(T));
            //#else
            //          newArray[i] = (T)System.Convert.ChangeType((System.Object)i, typeof(T), null);
            //#endif
        }
        return newArray;
    }

    [Fact]
    public static void RunTests()
    {
        //Scenario 1: Verify that ICollection implementation works properly using ICollection common tester

        Test test = new Test();

        Driver<int> IntDriver = new Driver<int>(test);
        IntDriver.TestICollection(GenerateArray<int>(10));

        Driver<string> StringDriver = new Driver<string>(test);
        StringDriver.TestICollection(GenerateArray<string>(1000));

        Assert.True(test.Pass);
    }
}
