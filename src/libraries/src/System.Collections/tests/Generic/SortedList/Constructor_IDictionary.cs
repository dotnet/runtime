// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;
using SortedList_SortedListUtils;

namespace SortedListCtorIDic
{
    public class Driver<KeyType, ValueType>
    {
        private Test m_test;

        public Driver(Test test)
        {
            m_test = test;
        }

        public void TestVanilla(IDictionary<KeyType, ValueType> SortedList)
        {
            SortedList<KeyType, ValueType> _dic = new SortedList<KeyType, ValueType>(SortedList);
            m_test.Eval(_dic.Comparer == Comparer<KeyType>.Default, String.Format("Err_54180auede! Comparer differ expected: {0} actual: {1}", Comparer<KeyType>.Default, _dic.Comparer));
            m_test.Eval(_dic.Count == SortedList.Count, String.Format("Err_23497sg! Count different: {0}", _dic.Count));
            m_test.Eval(((IDictionary<KeyType, ValueType>)_dic).IsReadOnly == false, String.Format("Err_435wsdg! Count different: {0}", ((IDictionary<KeyType, ValueType>)_dic).IsReadOnly));
            m_test.Eval(_dic.Keys.Count == SortedList.Count, String.Format("Err_25ag! Count different: {0}", _dic.Keys.Count));
            m_test.Eval(_dic.Values.Count == SortedList.Count, String.Format("Err_23agd! Count different: {0}", _dic.Values.Count));
        }

        public void TestParm(IDictionary<KeyType, ValueType> SortedList)
        {
            try
            {
                SortedList<KeyType, ValueType> _dic = new SortedList<KeyType, ValueType>(SortedList);
                m_test.Eval(false, "Err_23raf! Exception not thrown");
            }
            catch (ArgumentNullException)
            {
            }
            catch (Exception ex)
            {
                m_test.Eval(false, String.Format("Err_387tsg! Wrong exception thrown: {0}", ex));
            }
        }
    }

    public class Constructor_IDictionary
    {
        [Fact]
        public static void RunTests()
        {
            //This mostly follows the format established by the original author of these tests

            Test test = new Test();

            Driver<SimpleRef<int>, SimpleRef<String>> driver1 = new Driver<SimpleRef<int>, SimpleRef<String>>(test);
            Driver<SimpleRef<String>, SimpleRef<int>> driver2 = new Driver<SimpleRef<String>, SimpleRef<int>>(test);
            Driver<SimpleRef<int>, SimpleRef<int>> driver3 = new Driver<SimpleRef<int>, SimpleRef<int>>(test);
            Driver<SimpleRef<String>, SimpleRef<String>> driver4 = new Driver<SimpleRef<String>, SimpleRef<String>>(test);

            int count;

            SimpleRef<int>[] ints;
            SimpleRef<String>[] strings;

            SortedList<SimpleRef<int>, SimpleRef<String>> dic1;
            SortedList<SimpleRef<String>, SimpleRef<int>> dic2;
            SortedList<SimpleRef<int>, SimpleRef<int>> dic3;
            SortedList<SimpleRef<String>, SimpleRef<String>> dic4;

            //Scenario 1: Vanilla - Create SortedList using another SortedList with 10 key-values and check
            count = 10;
            ints = GetSimpleInts(count);
            strings = GetSimpleStrings(count);

            dic1 = FillValues(ints, strings);
            dic2 = FillValues(strings, ints);
            dic3 = FillValues(ints, ints);
            dic4 = FillValues(strings, strings);

            driver1.TestVanilla(dic1);
            driver2.TestVanilla(dic2);
            driver3.TestVanilla(dic3);
            driver4.TestVanilla(dic4);

            //Scenario 2: SortedList with 100 entries
            count = 100;
            ints = GetSimpleInts(count);
            strings = GetSimpleStrings(count);

            dic1 = FillValues(ints, strings);
            dic2 = FillValues(strings, ints);
            dic3 = FillValues(ints, ints);
            dic4 = FillValues(strings, strings);

            driver1.TestVanilla(dic1);
            driver2.TestVanilla(dic2);
            driver3.TestVanilla(dic3);
            driver4.TestVanilla(dic4);

            //Scenario 3: Implement our own type that implements IDictionary<KeyType, ValueType> and test

            TestSortedList<SimpleRef<int>, SimpleRef<String>> myDic1 = new TestSortedList<SimpleRef<int>, SimpleRef<String>>(ints, strings);
            TestSortedList<SimpleRef<String>, SimpleRef<int>> myDic2 = new TestSortedList<SimpleRef<String>, SimpleRef<int>>(strings, ints);
            TestSortedList<SimpleRef<int>, SimpleRef<int>> myDic3 = new TestSortedList<SimpleRef<int>, SimpleRef<int>>(ints, ints);
            TestSortedList<SimpleRef<String>, SimpleRef<String>> myDic4 = new TestSortedList<SimpleRef<String>, SimpleRef<String>>(strings, strings);

            driver1.TestVanilla(myDic1);
            driver2.TestVanilla(myDic2);
            driver3.TestVanilla(myDic3);
            driver4.TestVanilla(myDic4);

            //Scenario 4: Parm validation: null, empty SortedList
            driver1.TestParm(null);
            driver2.TestParm(null);
            driver3.TestParm(null);
            driver4.TestParm(null);

            driver1.TestVanilla(new SortedList<SimpleRef<int>, SimpleRef<String>>());
            driver2.TestVanilla(new SortedList<SimpleRef<String>, SimpleRef<int>>());
            driver3.TestVanilla(new SortedList<SimpleRef<int>, SimpleRef<int>>());
            driver4.TestVanilla(new SortedList<SimpleRef<String>, SimpleRef<String>>());

            Assert.True(test.result);
        }

        private static SortedList<KeyType, ValueType> FillValues<KeyType, ValueType>(KeyType[] keys, ValueType[] values)
        {
            SortedList<KeyType, ValueType> _dic = new SortedList<KeyType, ValueType>();
            for (int i = 0; i < keys.Length; i++)
                _dic.Add(keys[i], values[i]);
            return _dic;
        }

        private static SimpleRef<int>[] GetSimpleInts(int count)
        {
            SimpleRef<int>[] ints = new SimpleRef<int>[count];
            for (int i = 0; i < count; i++)
                ints[i] = new SimpleRef<int>(i);
            return ints;
        }

        private static SimpleRef<String>[] GetSimpleStrings(int count)
        {
            SimpleRef<String>[] strings = new SimpleRef<String>[count];
            for (int i = 0; i < count; i++)
                strings[i] = new SimpleRef<String>(i.ToString());
            return strings;
        }
    }
}