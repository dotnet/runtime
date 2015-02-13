// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;
using SortedList_SortedListUtils;

namespace SortedListThis
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
            for (int i = 0; i < keys.Length; i++)
                m_test.Eval(_dic[keys[i]].Equals(values[i]), String.Format("Err_3497gs! Not equal {0}", i));
        }

        public void TestModify(KeyType[] keys, ValueType[] values)
        {
            SortedList<KeyType, ValueType> _dic = new SortedList<KeyType, ValueType>();
            int half = keys.Length / 2;
            for (int i = 0; i < half; i++)
                _dic.Add(keys[i], values[i]);
            for (int i = 0; i < half; i++)
                _dic[keys[i]] = values[i + half];
            for (int i = 0; i < half; i++)
                m_test.Eval(_dic[keys[i]].Equals(values[i + half]), String.Format("Err_3497gs! Not equal {0}", i));
        }

        public void TestNonExistentKeys(KeyType[] keys, ValueType[] values, KeyType[] nonExistentKeys)
        {
            SortedList<KeyType, ValueType> _dic = new SortedList<KeyType, ValueType>();
            for (int i = 0; i < keys.Length; i++)
                _dic.Add(keys[i], values[i]);
            for (int i = 0; i < nonExistentKeys.Length; i++)
            {
                try
                {
                    ValueType v = _dic[nonExistentKeys[i]];
                    m_test.Eval(false, "Err_23raf! Exception not thrown");
                }
                catch (KeyNotFoundException)
                {
                }
                catch (Exception ex)
                {
                    m_test.Eval(false, String.Format("Err_207092ahpsh! Wrong exception thrown: {0}", ex));
                }
            }
        }

        public void TestParm(KeyType[] keys, ValueType[] values)
        {
            SortedList<KeyType, ValueType> _dic = new SortedList<KeyType, ValueType>();
            try
            {
                ValueType v = _dic[(KeyType)(Object)null];
                m_test.Eval(false, "Err_23raf! Exception not thrown");
            }
            catch (ArgumentNullException)
            {
            }
            catch (Exception ex)
            {
                m_test.Eval(false, String.Format("Err_387tsg! Wrong exception thrown: {0}", ex));
            }

            try
            {
                _dic[(KeyType)(Object)null] = values[0];
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

        public void NonGenericIDictionaryTestVanilla(KeyType[] keys, ValueType[] values)
        {
            SortedList<KeyType, ValueType> _dic = new SortedList<KeyType, ValueType>();
            IDictionary _idic = _dic;
            for (int i = 0; i < keys.Length; i++)
                _dic.Add(keys[i], values[i]);
            for (int i = 0; i < keys.Length; i++)
                m_test.Eval(_idic[keys[i]].Equals(values[i]), String.Format("Err_3497gs! Not equal {0}", i));
        }

        public void NonGenericIDictionaryTestModify(KeyType[] keys, ValueType[] values)
        {
            SortedList<KeyType, ValueType> _dic = new SortedList<KeyType, ValueType>();
            IDictionary _idic = _dic;
            int half = keys.Length / 2;
            for (int i = 0; i < half; i++)
                _dic.Add(keys[i], values[i]);
            for (int i = 0; i < half; i++)
                _idic[keys[i]] = values[i + half];
            for (int i = 0; i < half; i++)
                m_test.Eval(_idic[keys[i]].Equals(values[i + half]), String.Format("Err_3497gs! Not equal {0}", i));

            //[] Verify value can be null
            if (null == default(ValueType))
            {
                _idic[keys[0]] = null;
                m_test.Eval(_idic[keys[0]] == null, "Err_05848aheiiud! Not equal");
            }
        }

        public void NonGenericIDictionaryTestNonExistentKeys(KeyType[] keys, ValueType[] values, KeyType[] nonExistentKeys)
        {
            SortedList<KeyType, ValueType> _dic = new SortedList<KeyType, ValueType>();
            IDictionary _idic = _dic;
            for (int i = 0; i < keys.Length; i++)
                _dic.Add(keys[i], values[i]);
            for (int i = 0; i < nonExistentKeys.Length; i++)
            {
                try
                {
                    Object v = _idic[nonExistentKeys[i]];
                    m_test.Eval(v == null,
                        String.Format("Err_5467ahbpa! Expected non existant key to return null actual idic[{0}]={1} at index={2}",
                        nonExistentKeys[i], _idic[nonExistentKeys[i]], i));
                }
                catch (Exception ex)
                {
                    m_test.Eval(false, String.Format("Err_564894ahpa! Unexpected exception thrown: {0}", ex));
                }
            }
        }

        public void NonGenericIDictionaryTestParm(KeyType[] keys, ValueType[] values)
        {
            SortedList<KeyType, ValueType> _dic = new SortedList<KeyType, ValueType>();
            IDictionary _idic = _dic;

            try
            {
                Object v = _idic[null];
                m_test.Eval(false, "Err_21518ahied! Exception not thrown");
            }
            catch (ArgumentNullException)
            {
            }
            catch (Exception ex)
            {
                m_test.Eval(false, String.Format("Err_546188ahed! Wrong exception thrown: {0}", ex));
            }

            try
            {
                Object v = _idic[new Random(-55)];
                m_test.Eval(null == v, "Err_23raf! Expected Indexer to return null with a key that was an invalid type actual {0}", v);
            }
            catch (Exception ex)
            {
                m_test.Eval(false, String.Format("Err_387tsg! Wrong exception thrown: {0}", ex));
            }

            try
            {
                _idic[new Random(-55)] = values[0];
                m_test.Eval(false, "Err_23raf! Exception not thrown");
            }
            catch (ArgumentException)
            {
            }
            catch (Exception ex)
            {
                m_test.Eval(false, String.Format("Err_387tsg! Wrong exception thrown: {0}", ex));
            }

            try
            {
                _idic[keys[0]] = new Random(-55);
                m_test.Eval(false, "Err_23raf! Exception not thrown");
            }
            catch (ArgumentException)
            {
            }
            catch (Exception ex)
            {
                m_test.Eval(false, String.Format("Err_387tsg! Wrong exception thrown: {0}", ex));
            }


            try
            {
                _idic[null] = values[0];
                m_test.Eval(false, "Err_10518ajieud! Exception not thrown");
            }
            catch (ArgumentNullException)
            {
            }
            catch (Exception ex)
            {
                m_test.Eval(false, String.Format("Err_05158ahied! Wrong exception thrown: {0}", ex));
            }

            if (null != default(ValueType))
            {
                try
                {
                    _idic[keys[0]] = null;
                    m_test.Eval(false, "Err_021558aheid! Expected exception to be thrown when setting null a values of the collection are a value type");
                }
                catch (ArgumentException)
                {
                }
                catch (Exception ex)
                {
                    m_test.Eval(false, String.Format("Err_205484ajhied! Wrong exception thrown: {0}", ex));
                }
            }
        }
    }

    public class get_set_This
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
            IntDriver.NonGenericIDictionaryTestVanilla(ints, ints);
            simpleRef.NonGenericIDictionaryTestVanilla(simpleStrings, simpleStrings);
            simpleVal.NonGenericIDictionaryTestVanilla(simpleInts, simpleInts);

            //Scenario 2: Check that the existing values can be modified via the key
            //Scneario 3: Check that non-existing values are created via set 
            IntDriver.TestModify(ints, ints);
            simpleRef.TestModify(simpleStrings, simpleStrings);
            simpleVal.TestModify(simpleInts, simpleInts);
            IntDriver.NonGenericIDictionaryTestModify(ints, ints);
            simpleRef.NonGenericIDictionaryTestModify(simpleStrings, simpleStrings);
            simpleVal.NonGenericIDictionaryTestModify(simpleInts, simpleInts);

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
            IntDriver.NonGenericIDictionaryTestNonExistentKeys(ints_1, ints_1, ints_2);
            simpleRef.NonGenericIDictionaryTestNonExistentKeys(simpleStrings_1, simpleStrings_1, simpleStrings_2);
            simpleVal.NonGenericIDictionaryTestNonExistentKeys(simpleInts_1, simpleInts_1, simpleInts_2);

            //Scenario 5: Parm validation: null, Empty string
            simpleRef.TestParm(simpleStrings, simpleStrings);
            simpleVal.TestParm(simpleInts, simpleInts);
            simpleRef.NonGenericIDictionaryTestParm(simpleStrings, simpleStrings);
            simpleVal.NonGenericIDictionaryTestParm(simpleInts, simpleInts);
            IntDriver.NonGenericIDictionaryTestParm(ints_1, ints_2);

            Assert.True(test.result);
        }
    }
}