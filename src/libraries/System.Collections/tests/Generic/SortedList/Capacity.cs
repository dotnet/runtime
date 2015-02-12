// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;
using SortedList_SortedListUtils;

namespace SortedListCapacity
{
    public class Driver<KeyType, ValueType>
    {
        public void TestVanilla()
        {
            SortedList<KeyType, ValueType> _dic = new SortedList<KeyType, ValueType>();
            Test.Eval(_dic.Capacity == 0, String.Format("Err_234897agf! wrong value returned, {0}, expected: 0", _dic.Capacity));
            _dic = new SortedList<KeyType, ValueType>(0);
            Test.Eval(_dic.Capacity == 0, String.Format("Err_234897agf1! wrong value returned, {0}, expected: 0", _dic.Capacity));
            _dic = new SortedList<KeyType, ValueType>(1);
            Test.Eval(_dic.Capacity == 1, String.Format("Err_234897agf2! wrong value returned, {0}, expected: 1", _dic.Capacity));
            _dic = new SortedList<KeyType, ValueType>(16);
            Test.Eval(_dic.Capacity == 16, String.Format("Err_234897agf3! wrong value returned, {0}, expected: 16", _dic.Capacity));
            _dic = new SortedList<KeyType, ValueType>(16000);
            Test.Eval(_dic.Capacity == 16000, String.Format("Err_234897agf4! wrong value returned, {0}, expected: 16000", _dic.Capacity));
        }

        public void TestVanillaSet()
        {
            SortedList<KeyType, ValueType> _dic = new SortedList<KeyType, ValueType>();
            _dic.Capacity = 0;
            Test.Eval(_dic.Capacity == 0, String.Format("Err_b234897agf1! wrong value returned, {0}, expected: 0", _dic.Capacity));
            _dic = new SortedList<KeyType, ValueType>();
            _dic.Capacity = 1;
            Test.Eval(_dic.Capacity == 1, String.Format("Err_b234897agf2! wrong value returned, {0}, expected: 1", _dic.Capacity));
            _dic = new SortedList<KeyType, ValueType>();
            _dic.Capacity = 16;
            Test.Eval(_dic.Capacity == 16, String.Format("Err_b234897agf3! wrong value returned, {0}, expected: 16", _dic.Capacity));
            _dic = new SortedList<KeyType, ValueType>();
            _dic.Capacity = 16000;
            Test.Eval(_dic.Capacity == 16000, String.Format("Err_b234897agf4! wrong value returned, {0}, expected: 16000", _dic.Capacity));
        }

        public void TestParmValidation(KeyType[] keys, ValueType[] values)
        {
            SortedList<KeyType, ValueType> _dic = new SortedList<KeyType, ValueType>(100);
            Test.Eval(_dic.Capacity == 100, String.Format("Err_c234897agf1! wrong value returned, {0}, expected: 100", _dic.Capacity));
            _dic.Capacity = 0;
            Test.Eval(_dic.Capacity == 0, String.Format("Err_d234897agf1! wrong value returned, {0}, expected: 0", _dic.Capacity));

            try
            {
                _dic.Capacity = -1;
                Test.Eval(false, "Err_e234897agf1! Expected to throw ArgumentOutOfRangeException when trying to set Capacity to negative");
            }
            catch (ArgumentOutOfRangeException)
            {
            }

            for (int i = 0; i < keys.Length; i++)
            {
                _dic.Add(keys[i], values[i]);
                try
                {
                    _dic.Capacity = i;
                    Test.Eval(false, "Err_f234897agf1! Expected to throw ArgumentOutOfRangeException when trying to set Capacity to negative");
                }
                catch (ArgumentOutOfRangeException)
                {
                }
            }
        }

        /************************************************************************
        TestCapacity, TestCapacityPresetValue,  and TestCapacityWhenCleared make assumptions
        about the default capacity and how the collection is grown. If this changes these test cases
        will need to be updated	
        ************************************************************************/
        public void TestCapacity(KeyType[] keys, ValueType[] values)
        {
            SortedList<KeyType, ValueType> _dic = new SortedList<KeyType, ValueType>();
            int capacity = 4;
            for (int i = 0; i < keys.Length; i++)
            {
                _dic.Add(keys[i], values[i]);

                if (i == capacity) capacity *= 2;

                if (i <= capacity + 1)
                {
                    Test.Eval(_dic.Capacity == capacity, String.Format("Err_4588ahied! wrong value returned, {0}, expected: {1}", _dic.Capacity, capacity));
                }
                else
                {
                    Test.Eval(_dic.Capacity == i, String.Format("Err_54848ajide! wrong value returned, {0}, expected: {1}", _dic.Capacity, i));
                }
            }
        }


        public void TestCapacityPresetValue(KeyType[] keys, ValueType[] values, int capacity)
        {
            SortedList<KeyType, ValueType> _dic = new SortedList<KeyType, ValueType>(capacity);

            for (int i = 0; i < keys.Length; i++)
            {
                _dic.Add(keys[i], values[i]);
                //if capacity is 0, the first item added makes it 4
                if (capacity == 0) capacity = 4;
                //if the array needs to grow, it doubles the size
                if (i == capacity) capacity *= 2;

                if (i <= capacity + 1)
                {
                    Test.Eval(_dic.Capacity == capacity, String.Format("Err_4560884ahuiid! wrong value returned, {0}, expected: {1}", _dic.Capacity, capacity));
                }
                else
                {
                    Test.Eval(_dic.Capacity == i, String.Format("Err_50848ahied! wrong value returned, {0}, expected: {1}", _dic.Capacity, i));
                }
            }
        }

        public void TestCapacityWhenCleared(KeyType[] keys, ValueType[] values, int capacity)
        {
            SortedList<KeyType, ValueType> _dic = new SortedList<KeyType, ValueType>(capacity);

            for (int i = 0; i < keys.Length; i++)
            {
                _dic.Add(keys[i], values[i]);
                //if capacity is 0, the first item added makes it 4
                if (capacity == 0) capacity = 4;
                //if the array needs to grow, it doubles the size
                if (i == capacity) capacity *= 2;

                if (i <= capacity + 1)
                {
                    Test.Eval(_dic.Capacity == capacity, String.Format("Err_0058ajied! wrong value returned, {0}, expected: {1}", _dic.Capacity, capacity));
                }
                else
                {
                    Test.Eval(_dic.Capacity == i, String.Format("Err_516898ahied! wrong value returned, {0}, expected: {1}", _dic.Capacity, i));
                }
            }
            _dic.Clear();
            Test.Eval(_dic.Capacity == capacity, String.Format("Err_36695ahieid! wrong value returned, {0}, expected: {1}", _dic.Capacity, capacity));
        }
    }

    public class get_set_Capacity
    {
        [Fact]
        public static void RunTests()
        {
            Driver<string, string> driver1 = new Driver<string, string>();
            Driver<SimpleRef<int>, SimpleRef<String>> driver2 = new Driver<SimpleRef<int>, SimpleRef<String>>();
            Driver<SimpleRef<String>, SimpleRef<int>> driver3 = new Driver<SimpleRef<String>, SimpleRef<int>>();
            Driver<SimpleRef<int>, SimpleRef<int>> driver4 = new Driver<SimpleRef<int>, SimpleRef<int>>();
            Driver<SimpleRef<String>, SimpleRef<String>> driver5 = new Driver<SimpleRef<String>, SimpleRef<String>>();

            //Create a SD with default capacity and other capacities and verify
            SimpleRef<int>[] simpleInts;
            SimpleRef<String>[] simpleStrings;
            string[] strings;
            int count;

            count = 100;
            simpleInts = SortedListUtils.GetSimpleInts(count);
            simpleStrings = SortedListUtils.GetSimpleStrings(count);
            strings = new string[count];
            for (int i = 0; i < count; i++)
                strings[i] = i.ToString();

            driver1.TestVanilla();
            driver2.TestVanilla();
            driver3.TestVanilla();
            driver4.TestVanilla();
            driver5.TestVanilla();

            //Test for capacities with capacity constructor after adding items
            driver1.TestCapacity(strings, strings);
            driver2.TestCapacity(simpleInts, simpleStrings);
            driver3.TestCapacity(simpleStrings, simpleInts);
            driver4.TestCapacity(simpleInts, simpleInts);
            driver5.TestCapacity(simpleStrings, simpleStrings);

            //Test for capacity growth with set starting capacity
            driver1.TestCapacityPresetValue(strings, strings, 0);
            driver1.TestCapacityPresetValue(strings, strings, 1);
            driver1.TestCapacityPresetValue(strings, strings, 16);
            driver1.TestCapacityPresetValue(strings, strings, 20);
            driver1.TestCapacityPresetValue(strings, strings, 160);

            //Test setting capacity after default
            driver1.TestVanillaSet();
            driver2.TestVanillaSet();
            driver3.TestVanillaSet();
            driver4.TestVanillaSet();
            driver5.TestVanillaSet();

            //Test parameter validation for set
            driver1.TestParmValidation(strings, strings);
            driver2.TestParmValidation(simpleInts, simpleStrings);
            driver3.TestParmValidation(simpleStrings, simpleInts);
            driver4.TestParmValidation(simpleInts, simpleInts);
            driver5.TestParmValidation(simpleStrings, simpleStrings);

            //Test for capacity when cleared
            driver1.TestCapacityWhenCleared(strings, strings, 0);
            driver1.TestCapacityWhenCleared(strings, strings, 1);
            driver1.TestCapacityWhenCleared(strings, strings, 16);
            driver1.TestCapacityWhenCleared(strings, strings, 20);
            driver1.TestCapacityWhenCleared(strings, strings, 160);

            Assert.True(Test.result);
        }
    }
}