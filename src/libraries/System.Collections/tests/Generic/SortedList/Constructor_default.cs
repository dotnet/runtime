// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;
using SortedList_SortedListUtils;

namespace SortedListCtor
{
    public class Driver<KeyType, ValueType>
    {
        public void TestVanilla()
        {
            SortedList<KeyType, ValueType> _dic = new SortedList<KeyType, ValueType>();
            Test.Eval(_dic.Comparer == Comparer<KeyType>.Default, String.Format("Err_54180auede! Comparer differ expected: {0} actual: {1}", Comparer<KeyType>.Default, _dic.Comparer));
            Test.Eval(_dic.Count == 0, String.Format("Err_23497sg! Count different: {0}", _dic.Count));
            Test.Eval(((IDictionary<KeyType, ValueType>)_dic).IsReadOnly == false, String.Format("Err_435wsdg! Count different: {0}", ((IDictionary<KeyType, ValueType>)_dic).IsReadOnly));
            Test.Eval(_dic.Keys.Count == 0, String.Format("Err_25ag! Count different: {0}", _dic.Keys.Count));
            Test.Eval(_dic.Values.Count == 0, String.Format("Err_23agd! Count different: {0}", _dic.Values.Count));
        }
        public void TestCanAdd(KeyType[] keys, ValueType[] values)
        {
            SortedList<KeyType, ValueType> _dic = new SortedList<KeyType, ValueType>();
            for (int i = 0; i < keys.Length; i++)
                _dic.Add(keys[i], values[i]);
            Test.Eval(_dic.Count == keys.Length, String.Format("Err_23497sg! Count different: {0}", _dic.Count));
            Test.Eval(((IDictionary<KeyType, ValueType>)_dic).IsReadOnly == false, String.Format("Err_435wsdg! Count different: {0}", ((IDictionary<KeyType, ValueType>)_dic).IsReadOnly));
            Test.Eval(_dic.Keys.Count == keys.Length, String.Format("Err_25ag! Count different: {0}", _dic.Keys.Count));
            Test.Eval(_dic.Values.Count == keys.Length, String.Format("Err_23agd! Count different: {0}", _dic.Values.Count));
        }
    }

    public class Constructor_default
    {
        [Fact]
        public static void RunTests()
        {
            //This mostly follows the format established by the original author of these tests

            Driver<SimpleRef<int>, SimpleRef<String>> driver1 = new Driver<SimpleRef<int>, SimpleRef<String>>();
            Driver<SimpleRef<String>, SimpleRef<int>> driver2 = new Driver<SimpleRef<String>, SimpleRef<int>>();
            Driver<SimpleRef<int>, SimpleRef<int>> driver3 = new Driver<SimpleRef<int>, SimpleRef<int>>();
            Driver<SimpleRef<String>, SimpleRef<String>> driver4 = new Driver<SimpleRef<String>, SimpleRef<String>>();

            SimpleRef<int>[] ints = new SimpleRef<int>[] { new SimpleRef<int>(1), new SimpleRef<int>(2), new SimpleRef<int>(3) };
            SimpleRef<String>[] strings = new SimpleRef<String>[] { new SimpleRef<String>("1"), new SimpleRef<String>("2"), new SimpleRef<String>("3") };

            //Scenario 1: Vanilla - Create a SortedList and check that default properties are set

            driver1.TestVanilla();
            driver2.TestVanilla();
            driver3.TestVanilla();
            driver4.TestVanilla();

            //Scenario 2: Vanilla - Make sure that we can add key-value pairs to this

            driver1.TestCanAdd(ints, strings);
            driver2.TestCanAdd(strings, ints);
            driver3.TestCanAdd(ints, ints);
            driver4.TestCanAdd(strings, strings);

            Assert.True(Test.result);
        }
    }
}
