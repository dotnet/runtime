// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;
using SortedList_SortedListUtils;

namespace SortedListMiscProp
{
    public class Driver<KeyType, ValueType>
    {
        private Test m_test;

        public Driver(Test test)
        {
            m_test = test;
        }

        public void TestReadOnly(KeyType[] keys, ValueType[] values)
        {
            SortedList<KeyType, ValueType> _dic = new SortedList<KeyType, ValueType>();
            m_test.Eval(false == ((IDictionary<KeyType, ValueType>)_dic).IsReadOnly, String.Format("Err_345sdg! wrong value returned, {0}", ((IDictionary<KeyType, ValueType>)_dic).IsReadOnly));
            for (int i = 0; i < keys.Length; i++)
                _dic.Add(keys[i], values[i]);
            m_test.Eval(false == ((IDictionary<KeyType, ValueType>)_dic).IsReadOnly, String.Format("Err_0347tsg! wrong value returned, {0}", ((IDictionary<KeyType, ValueType>)_dic).IsReadOnly));
            _dic.Clear();
            m_test.Eval(false == ((IDictionary<KeyType, ValueType>)_dic).IsReadOnly, String.Format("Err-34sg! wrong value returned, {0}", ((IDictionary<KeyType, ValueType>)_dic).IsReadOnly));
        }

        public void NonGenericIDictionaryTestFixedSize(KeyType[] keys, ValueType[] values)
        {
            SortedList<KeyType, ValueType> _dic = new SortedList<KeyType, ValueType>();
            IDictionary _idic = _dic;
            m_test.Eval(false == _idic.IsFixedSize, String.Format("Err_3234897agf! wrong value returned, {0}", _idic.IsFixedSize));
            for (int i = 0; i < keys.Length; i++)
                _dic.Add(keys[i], values[i]);
            m_test.Eval(false == _idic.IsFixedSize, String.Format("Err_33467tsgb! wrong value returned, {0}", _idic.IsFixedSize));
            _dic.Clear();
            m_test.Eval(false == _idic.IsFixedSize, String.Format("Err_33467tsgb! wrong value returned, {0}", _idic.IsFixedSize));
        }

        public void NonGenericIDictionaryTestReadOnly(KeyType[] keys, ValueType[] values)
        {
            SortedList<KeyType, ValueType> _dic = new SortedList<KeyType, ValueType>();
            IDictionary _idic = _dic;
            m_test.Eval(false == _idic.IsReadOnly, String.Format("Err_4345sdg! wrong value returned, {0}", _idic.IsReadOnly));
            for (int i = 0; i < keys.Length; i++)
                _dic.Add(keys[i], values[i]);
            m_test.Eval(false == _idic.IsReadOnly, String.Format("Err_40347tsg! wrong value returned, {0}", _idic.IsReadOnly));
            _dic.Clear();
            m_test.Eval(false == _idic.IsReadOnly, String.Format("Err-434sg! wrong value returned, {0}", _idic.IsReadOnly));
        }
    }

    public class MiscProperties
    {
        [Fact]
        public static void RunTests()
        {
            //This mostly follows the format established by the original author of these tests

            Test test = new Test();

            Driver<int, int> IntDriver = new Driver<int, int>(test);
            Driver<SimpleRef<String>, SimpleRef<String>> simpleRef = new Driver<SimpleRef<String>, SimpleRef<String>>(test);
            Driver<SimpleRef<int>, SimpleRef<int>> simpleVal = new Driver<SimpleRef<int>, SimpleRef<int>>(test);

            SimpleRef<int>[] simpleInts;
            SimpleRef<String>[] simpleStrings;
            int[] ints;
            int count;

            count = 1000;
            simpleInts = SortedListUtils.GetSimpleInts(count);
            simpleStrings = SortedListUtils.GetSimpleStrings(count);
            ints = new int[count];
            for (int i = 0; i < count; i++)
                ints[i] = i;


            //IsFixedSize
            //Scenario 1: Vanilla - Ensure that this returns false for an empty SortedList
            //Scenario 2: Do item manipulations in the SortedList and ensure that this property is still false
            IntDriver.NonGenericIDictionaryTestFixedSize(ints, ints);
            simpleRef.NonGenericIDictionaryTestFixedSize(simpleStrings, simpleStrings);
            simpleVal.NonGenericIDictionaryTestFixedSize(simpleInts, simpleInts);

            //IsReadOnly
            //Scenario 1: Vanilla - Ensure that this returns false for an empty SortedList
            //Scenario 2: Do item manipulations in the SortedList and ensure that this property is still false
            IntDriver.TestReadOnly(ints, ints);
            simpleRef.TestReadOnly(simpleStrings, simpleStrings);
            simpleVal.TestReadOnly(simpleInts, simpleInts);
            IntDriver.NonGenericIDictionaryTestReadOnly(ints, ints);
            simpleRef.NonGenericIDictionaryTestReadOnly(simpleStrings, simpleStrings);
            simpleVal.NonGenericIDictionaryTestReadOnly(simpleInts, simpleInts);

            //Scenario 1: Check for properties and methods that return a reference to the underlying SortedList, the SyncRoot property is the same as 
            //the original SortedList
            //@NOTE: After the modified design, SortedList doesn't have any members that return a reference to the underlying SortedList

            Assert.True(test.result);
        }
    }
}