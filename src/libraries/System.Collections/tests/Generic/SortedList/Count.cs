// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;
using SortedList_SortedListUtils;

namespace SortedListCount
{
    public class Driver<KeyType, ValueType>
    {
        private Test m_test;

        public Driver(Test test)
        {
            m_test = test;
        }

        public void TestVanilla(KeyType[] keys, ValueType[] values)
        {
            SortedList<KeyType, ValueType> _dic = new SortedList<KeyType, ValueType>();
            for (int i = 0; i < keys.Length; i++)
                _dic.Add(keys[i], values[i]);
            m_test.Eval(keys.Length == _dic.Count, String.Format("Err_234897agf! wrong value returned, {0}, expected: {1}", keys.Length, _dic.Count));
        }

        public void TestAddClear(KeyType[] keys, ValueType[] values)
        {
            SortedList<KeyType, ValueType> _dic = new SortedList<KeyType, ValueType>();
            for (int i = 0; i < keys.Length; i++)
                _dic.Add(keys[i], values[i]);
            m_test.Eval(keys.Length == _dic.Count, String.Format("Err_234af! wrong value returned, {0}, expected: {1}", keys.Length, _dic.Count));
            _dic.Clear();
            m_test.Eval(_dic.Count == 0, String.Format("Err_234ag! wrong value returned, {0}, expected: {1}", 0, _dic.Count));
            for (int i = 0; i < keys.Length; i++)
                _dic.Add(keys[i], values[i]);
            m_test.Eval(keys.Length == _dic.Count, String.Format("Err_234af! wrong value returned, {0}, expected: {1}", keys.Length, _dic.Count));
        }


        public void TestAddRemove(KeyType[] keys, ValueType[] values)
        {
            SortedList<KeyType, ValueType> _dic = new SortedList<KeyType, ValueType>();
            for (int i = 0; i < keys.Length; i++)
                _dic.Add(keys[i], values[i]);
            m_test.Eval(keys.Length == _dic.Count, String.Format("Err_234af! wrong value returned, {0}, expected: {1}", keys.Length, _dic.Count));

            for (int i = 0; i < keys.Length / 2; i++)
                _dic.Remove(keys[i]);
            m_test.Eval((keys.Length - keys.Length / 2) == _dic.Count, String.Format("Err_234af! wrong value returned, {0}, expected: {1}", (keys.Length - keys.Length / 2), _dic.Count));

            for (int i = 0; i < keys.Length / 2; i++)
                _dic.Add(keys[i], values[i]);
            m_test.Eval(keys.Length == _dic.Count, String.Format("Err_234af! wrong value returned, {0}, expected: {1}", keys.Length, _dic.Count));

            for (int i = 0; i < keys.Length; i++)
                _dic.Remove(keys[i]);
            m_test.Eval(0 == _dic.Count, String.Format("Err_234af! wrong value returned, {0}, expected: {1}", 0, _dic.Count));

            for (int i = 0; i < keys.Length; i++)
                _dic.Add(keys[i], values[i]);
            m_test.Eval(keys.Length == _dic.Count, String.Format("Err_234af! wrong value returned, {0}, expected: {1}", keys.Length, _dic.Count));
        }
    }

    public class get_Count
    {
        [Fact]
        public static void RunTests()
        {
            //This mostly follows the format established by the original author of these tests
            //Also this property is tested elsewhere in most of the other test cases

            Test test = new Test();

            Driver<int, int> driver1 = new Driver<int, int>(test);
            Driver<SimpleRef<int>, SimpleRef<String>> driver2 = new Driver<SimpleRef<int>, SimpleRef<String>>(test);
            Driver<SimpleRef<String>, SimpleRef<int>> driver3 = new Driver<SimpleRef<String>, SimpleRef<int>>(test);
            Driver<SimpleRef<int>, SimpleRef<int>> driver4 = new Driver<SimpleRef<int>, SimpleRef<int>>(test);
            Driver<SimpleRef<String>, SimpleRef<String>> driver5 = new Driver<SimpleRef<String>, SimpleRef<String>>(test);

            //Scenario 1: Vanilla - Add 10 values and check that count is correct

            SimpleRef<int>[] simpleInts;
            SimpleRef<String>[] simpleStrings;
            int[] ints;
            int count;

            count = 10;
            simpleInts = SortedListUtils.GetSimpleInts(count);
            simpleStrings = SortedListUtils.GetSimpleStrings(count);
            ints = new int[count];
            for (int i = 0; i < count; i++)
                ints[i] = i;


            driver1.TestVanilla(ints, ints);
            driver2.TestVanilla(simpleInts, simpleStrings);
            driver3.TestVanilla(simpleStrings, simpleInts);
            driver4.TestVanilla(simpleInts, simpleInts);
            driver5.TestVanilla(simpleStrings, simpleStrings);

            //Scenario 2: Add, Clear and check
            driver1.TestAddClear(ints, ints);
            driver2.TestAddClear(simpleInts, simpleStrings);
            driver3.TestAddClear(simpleStrings, simpleInts);
            driver4.TestAddClear(simpleInts, simpleInts);
            driver5.TestAddClear(simpleStrings, simpleStrings);

            //Scenario 3: Stress and check: A large number of values and check
            count = 1000;
            simpleInts = SortedListUtils.GetSimpleInts(count);
            simpleStrings = SortedListUtils.GetSimpleStrings(count);
            ints = new int[count];
            for (int i = 0; i < count; i++)
                ints[i] = i;

            driver1.TestAddClear(ints, ints);
            driver2.TestAddClear(simpleInts, simpleStrings);
            driver3.TestAddClear(simpleStrings, simpleInts);
            driver4.TestAddClear(simpleInts, simpleInts);
            driver5.TestAddClear(simpleStrings, simpleStrings);


            //Scenario 4: Add, remove and clear combinations
            driver1.TestAddRemove(ints, ints);
            driver2.TestAddRemove(simpleInts, simpleStrings);
            driver3.TestAddRemove(simpleStrings, simpleInts);
            driver4.TestAddRemove(simpleInts, simpleInts);
            driver5.TestAddRemove(simpleStrings, simpleStrings);

            Assert.True(test.result);
        }
    }
}