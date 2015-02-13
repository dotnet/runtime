// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;
using SortedList_SortedListUtils;

namespace SortedListTryGetValue
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
            ValueType item;

            for (int i = 0; i < keys.Length; i++)
                _dic.Add(keys[i], values[i]);
            for (int i = 0; i < keys.Length; i++)
            {
                m_test.Eval(_dic.TryGetValue(keys[i], out item), "Err_20727ahpba!! Expected TryGetValue to return true");
                m_test.Eval(item.Equals(values[i]), String.Format("Err_3497gs! Wrong value returned expected={0} actua={1}", values[i], item));
            }
        }

        public void TestNonExistentKeys(KeyType[] keys, ValueType[] values, KeyType[] nonExistentKeys)
        {
            SortedList<KeyType, ValueType> _dic = new SortedList<KeyType, ValueType>();
            ValueType origItem = null != values && values.Length != 0 ? values[0] : default(ValueType);
            ValueType item = origItem;
            ValueType defaultItem = default(ValueType);

            for (int i = 0; i < keys.Length; i++)
                _dic.Add(keys[i], values[i]);
            for (int i = 0; i < nonExistentKeys.Length; i++)
            {
                m_test.Eval(!_dic.TryGetValue(nonExistentKeys[i], out item), "Err_8027qhapb!!! Expected TryGetValue to return false");
                m_test.Eval((defaultItem == null && item == null) || (null != item && item.Equals(defaultItem)), String.Format("Err_3707washpb!!! Expected value not to be modifed expected={0} actual={1}", origItem, item));
            }
        }

        public void TestParm(KeyType[] keys, ValueType[] values, KeyType key)
        {
            SortedList<KeyType, ValueType> _dic = new SortedList<KeyType, ValueType>();
            ValueType origItem = null != values && values.Length != 0 ? values[0] : default(ValueType);
            ValueType item = origItem;

            try
            {
                _dic.TryGetValue(key, out item);
                m_test.Eval(false, "Err_23raf! Exception not thrown");
            }
            catch (ArgumentNullException)
            {
                m_test.Eval((origItem == null && item == null) || (null != origItem && item.Equals(origItem)), String.Format("Err_709809ahpba!!! Expected value not to be modifed expected={0} actual={1}", origItem, item));
            }
            catch (Exception ex)
            {
                m_test.Eval(false, String.Format("Err_7072ahpba! Wrong exception thrown: {0}", ex));
            }
        }
    }

    public class TryGetValue
    {
        [Fact]
        public static void RunTests()
        {
            //This mostly follows the format established by the original author of these tests

            Test test = new Test();

            //Scenario 1: Vanilla - check that existing values can be accessed via the key

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

            IntDriver.TestVanilla(ints, ints);
            simpleRef.TestVanilla(simpleStrings, simpleStrings);
            simpleVal.TestVanilla(simpleInts, simpleInts);

            IntDriver.TestVanilla(new int[0], new int[0]);
            simpleRef.TestVanilla(new SimpleRef<String>[0], new SimpleRef<String>[0]);
            simpleVal.TestVanilla(new SimpleRef<int>[0], new SimpleRef<int>[0]);




            //Scenario 4: Check the returned value for non existing keys
            SimpleRef<int>[] simpleInts_1;
            SimpleRef<String>[] simpleStrings_1;
            int[] ints_1;
            SimpleRef<int>[] simpleInts_2;
            SimpleRef<String>[] simpleStrings_2;
            int[] ints_2;

            int half = count / 2;
            simpleInts_1 = new SimpleRef<int>[half];
            simpleStrings_1 = new SimpleRef<String>[half];
            ints_2 = new int[half];
            simpleInts_2 = new SimpleRef<int>[half];
            simpleStrings_2 = new SimpleRef<String>[half];
            ints_1 = new int[half];
            for (int i = 0; i < half; i++)
            {
                simpleInts_1[i] = simpleInts[i];
                simpleStrings_1[i] = simpleStrings[i];
                ints_1[i] = ints[i];

                simpleInts_2[i] = simpleInts[i + half];
                simpleStrings_2[i] = simpleStrings[i + half];
                ints_2[i] = ints[i + half];
            }

            IntDriver.TestNonExistentKeys(ints_1, ints_1, ints_2);
            simpleRef.TestNonExistentKeys(simpleStrings_1, simpleStrings_1, simpleStrings_2);
            simpleVal.TestNonExistentKeys(simpleInts_1, simpleInts_1, simpleInts_2);

            IntDriver.TestNonExistentKeys(new int[0], new int[0], ints_2);
            simpleRef.TestNonExistentKeys(new SimpleRef<String>[0], new SimpleRef<String>[0], simpleStrings_2);
            simpleVal.TestNonExistentKeys(new SimpleRef<int>[0], new SimpleRef<int>[0], simpleInts_2);

            //Scenario 5: Parm validation: null, Empty string
            simpleRef.TestParm(simpleStrings, simpleStrings, null);
            simpleVal.TestParm(simpleInts, simpleInts, null);

            simpleRef.TestParm(new SimpleRef<String>[0], new SimpleRef<String>[0], null);
            simpleVal.TestParm(new SimpleRef<int>[0], new SimpleRef<int>[0], null);

            Assert.True(test.result);
        }
    }
}