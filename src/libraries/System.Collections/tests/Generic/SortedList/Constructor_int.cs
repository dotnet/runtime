// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;
using SortedList_SortedListUtils;

namespace SortedListCtorInt
{
    public class Driver<KeyType, ValueType>
    {
        private Test m_test;

        public Driver(Test test)
        {
            m_test = test;
        }

        public void TestVanilla(int capacity)
        {
            SortedList<KeyType, ValueType> _dic = new SortedList<KeyType, ValueType>(capacity);
            m_test.Eval(_dic.Comparer == Comparer<KeyType>.Default, String.Format("Err_54180auede! Comparer differ expected: {0} actual: {1}", Comparer<KeyType>.Default, _dic.Comparer));
            m_test.Eval(_dic.Count == 0, String.Format("Err_23497sg! Count different: {0}", _dic.Count));
            m_test.Eval(((IDictionary<KeyType, ValueType>)_dic).IsReadOnly == false, String.Format("Err_435wsdg! Count different: {0}", ((IDictionary<KeyType, ValueType>)_dic).IsReadOnly));
            m_test.Eval(_dic.Keys.Count == 0, String.Format("Err_25ag! Count different: {0}", _dic.Keys.Count));
            m_test.Eval(_dic.Values.Count == 0, String.Format("Err_23agd! Count different: {0}", _dic.Values.Count));
        }
        public void TestCanAdd(KeyType[] keys, ValueType[] values, int capacity)
        {
            SortedList<KeyType, ValueType> _dic = new SortedList<KeyType, ValueType>(capacity);
            for (int i = 0; i < keys.Length; i++)
                _dic.Add(keys[i], values[i]);
            m_test.Eval(_dic.Count == keys.Length, String.Format("Err_23497sg! Count different: {0}", _dic.Count));
            m_test.Eval(((IDictionary<KeyType, ValueType>)_dic).IsReadOnly == false, String.Format("Err_435wsdg! Count different: {0}", ((IDictionary<KeyType, ValueType>)_dic).IsReadOnly));
            m_test.Eval(_dic.Keys.Count == keys.Length, String.Format("Err_25ag! Count different: {0}", _dic.Keys.Count));
            m_test.Eval(_dic.Values.Count == keys.Length, String.Format("Err_23agd! Count different: {0}", _dic.Values.Count));
        }
        public void TestParm(int capacity)
        {
            try
            {
                SortedList<KeyType, ValueType> _dic = new SortedList<KeyType, ValueType>(capacity);
                m_test.Eval(false, "Err_23raf! Exception not thrown");
            }
            catch (ArgumentOutOfRangeException)
            {
            }
            catch (Exception ex)
            {
                m_test.Eval(false, String.Format("Err_387tsg! Wrong exception thrown: {0}", ex));
            }
        }
    }

    public class Constructor_int
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

            SimpleRef<int>[] ints = new SimpleRef<int>[] { new SimpleRef<int>(1), new SimpleRef<int>(2), new SimpleRef<int>(3) };
            SimpleRef<String>[] strings = new SimpleRef<String>[] { new SimpleRef<String>("1"), new SimpleRef<String>("2"), new SimpleRef<String>("3") };

            //Scenario 1: Vanilla - Create SortedList using a vanilla capacity value and check
            int[] validValues = { 0, 1, 2, 5, 10, 16, 32, 50, 500, 5000, 10000 };

            for (int i = 0; i < validValues.Length; i++)
            {
                driver1.TestVanilla(validValues[i]);
                driver2.TestVanilla(validValues[i]);
                driver3.TestVanilla(validValues[i]);
                driver4.TestVanilla(validValues[i]);

                driver1.TestCanAdd(ints, strings, validValues[i]);
                driver2.TestCanAdd(strings, ints, validValues[i]);
                driver3.TestCanAdd(ints, ints, validValues[i]);
                driver4.TestCanAdd(strings, strings, validValues[i]);
            }

            //Scnenario 2: Parm validation: negative
            int[] negativeValues = { -1, -2, -5, Int32.MinValue };
            for (int i = 0; i < negativeValues.Length; i++)
            {
                driver1.TestParm(negativeValues[i]);
                driver2.TestParm(negativeValues[i]);
                driver3.TestParm(negativeValues[i]);
                driver4.TestParm(negativeValues[i]);
            }

            Assert.True(test.result);
        }
    }
}