// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;
using SortedList_SortedListUtils;

namespace SortedListAdd
{
    public class Driver<K, V> where K : IComparableValue
    {
        private Test m_test;

        public Driver(Test test)
        {
            m_test = test;
        }

        public void BasicAdd(K[] keys, V[] values)
        {
            SortedList<K, V> tbl = new SortedList<K, V>(new ValueKeyComparer<K>());
            for (int i = 0; i < keys.Length; i++)
            {
                tbl.Add(keys[i], values[i]);
            }
            m_test.Eval(tbl.Count == keys.Length);

            for (int i = 0; i < keys.Length; i++)
            {
                V val = tbl[keys[i]];
                m_test.Eval(((null == (object)val) && (null == (object)values[i])) || (val.Equals(values[i])));
            }
        }

        public void AddSameKey //SameValue - Different Value should not matter
            (K[] keys, V[] values, int index, int repeat)
        {
            SortedList<K, V> tbl = new SortedList<K, V>(new ValueKeyComparer<K>());
            for (int i = 0; i < keys.Length; i++)
            {
                tbl.Add(keys[i], values[i]);
            }
            m_test.Eval(tbl.Count == keys.Length);
            for (int i = 0; i < repeat; i++)
            {
                try
                {
                    tbl.Add(keys[index], values[index]);
                    m_test.Eval(false);
                }
                catch (ArgumentException)
                {
                    m_test.Eval(true);
                }
            }
            m_test.Eval(tbl.Count == keys.Length);
        }

        public void AddRemoveKeyValPair(K[] keys, V[] values, int index, int repeat)
        {
            SortedList<K, V> tbl = new SortedList<K, V>(new ValueKeyComparer<K>());
            for (int i = 0; i < keys.Length; i++)
            {
                tbl.Add(keys[i], values[i]);
            }
            m_test.Eval(tbl.Count == keys.Length);
            for (int i = 0; i < repeat; i++)
            {
                V val;
                m_test.Eval(tbl.Remove(keys[index]));
                try
                {
                    val = tbl[keys[index]];
                    m_test.Eval(false);
                }
                catch (KeyNotFoundException)
                {
                    m_test.Eval(true);
                }
                tbl.Add(keys[index], values[index]);
                val = tbl[keys[index]];
                m_test.Eval(((null == (object)val) && (null == (object)values[index])) || (val.Equals(values[index])));
            }
            m_test.Eval(tbl.Count == keys.Length);
        }

        public void AddValidations(K[] keys, V[] values, K key, V value)
        {
            SortedList<K, V> tbl = new SortedList<K, V>(new ValueKeyComparer<K>());
            for (int i = 0; i < keys.Length; i++)
            {
                tbl.Add(keys[i], values[i]);
            }

            if (!(key is System.ValueType))
            {
                try
                {
                    tbl.Add(key, value);
                    m_test.Eval(false);
                }
                catch (ArgumentException)
                {
                    m_test.Eval(true);
                }
            }
        }

        public void NonGenericIDictionaryBasicAdd(K[] keys, V[] values)
        {
            SortedList<K, V> tbl = new SortedList<K, V>(new ValueKeyComparer<K>());
            IDictionary _idic = tbl;
            for (int i = 0; i < keys.Length; i++)
            {
                _idic.Add(keys[i], values[i]);
            }
            m_test.Eval(tbl.Count == keys.Length);

            for (int i = 0; i < keys.Length; i++)
            {
                V val = tbl[keys[i]];
                m_test.Eval(((null == (object)val) && (null == (object)values[i])) || (val.Equals(values[i])));
            }
        }

        public void NonGenericIDictionaryAddSameKey //SameValue - Different Value should not matter
            (K[] keys, V[] values, int index, int repeat)
        {
            SortedList<K, V> tbl = new SortedList<K, V>(new ValueKeyComparer<K>());
            IDictionary _idic = tbl;
            for (int i = 0; i < keys.Length; i++)
            {
                _idic.Add(keys[i], values[i]);
            }
            m_test.Eval(tbl.Count == keys.Length);
            for (int i = 0; i < repeat; i++)
            {
                try
                {
                    _idic.Add(keys[index], values[index]);
                    m_test.Eval(false);
                }
                catch (ArgumentException)
                {
                    m_test.Eval(true);
                }
            }
            m_test.Eval(tbl.Count == keys.Length);
        }

        public void NonGenericIDictionaryAddRemoveKeyValPair(K[] keys, V[] values, int index, int repeat)
        {
            SortedList<K, V> tbl = new SortedList<K, V>(new ValueKeyComparer<K>());
            IDictionary _idic = tbl;
            for (int i = 0; i < keys.Length; i++)
            {
                _idic.Add(keys[i], values[i]);
            }
            m_test.Eval(tbl.Count == keys.Length);
            for (int i = 0; i < repeat; i++)
            {
                V val;
                m_test.Eval(tbl.Remove(keys[index]));
                try
                {
                    val = tbl[keys[index]];
                    m_test.Eval(false);
                }
                catch (KeyNotFoundException)
                {
                    m_test.Eval(true);
                }
                _idic.Add(keys[index], values[index]);
                val = tbl[keys[index]];
                m_test.Eval(((null == (object)val) && (null == (object)values[index])) || (val.Equals(values[index])));
            }
            m_test.Eval(tbl.Count == keys.Length);
        }

        public void NonGenericIDictionaryAddValidations(K[] keys, V[] values, K key, V value)
        {
            SortedList<K, V> tbl = new SortedList<K, V>(new ValueKeyComparer<K>());
            IDictionary _idic = tbl;
            for (int i = 0; i < keys.Length; i++)
            {
                _idic.Add(keys[i], values[i]);
            }

            //
            //try null key
            //
            try
            {
                _idic.Add(null, value);
                m_test.Eval(false);
            }
            catch (ArgumentNullException)
            {
                m_test.Eval(true);
            }

            try
            {
                _idic.Add(new Random(-55), value);
                m_test.Eval(false);
            }
            catch (ArgumentException)
            {
                m_test.Eval(true);
            }

            try
            {
                _idic.Add(key, new Random(-55));
                m_test.Eval(false);
            }
            catch (ArgumentException)
            {
                m_test.Eval(true);
            }
        }
    }

    public class Add
    {
        [Fact]
        public static void MainAdd()
        {
            Test test = new Test();

            Driver<RefX1<int>, ValX1<string>> IntDriver = new Driver<RefX1<int>, ValX1<string>>(test);
            RefX1<int>[] intArr = new RefX1<int>[100];
            for (int i = 0; i < 100; i++)
            {
                intArr[i] = new RefX1<int>(i);
            }

            Driver<ValX1<string>, RefX1<int>> StringDriver = new Driver<ValX1<string>, RefX1<int>>(test);
            ValX1<string>[] stringArr = new ValX1<string>[100];
            for (int i = 0; i < 100; i++)
            {
                stringArr[i] = new ValX1<string>("SomeTestString" + i.ToString());
            }


            //Ref<val>,Val<Ref>
            IntDriver.BasicAdd(intArr, stringArr);

            IntDriver.AddSameKey(intArr, stringArr, 0, 2);
            IntDriver.AddSameKey(intArr, stringArr, 99, 3);
            IntDriver.AddSameKey(intArr, stringArr, 50, 4);
            IntDriver.AddSameKey(intArr, stringArr, 1, 5);
            IntDriver.AddSameKey(intArr, stringArr, 98, 6);

            IntDriver.AddRemoveKeyValPair(intArr, stringArr, 0, 2);
            IntDriver.AddRemoveKeyValPair(intArr, stringArr, 99, 3);
            IntDriver.AddRemoveKeyValPair(intArr, stringArr, 50, 4);
            IntDriver.AddRemoveKeyValPair(intArr, stringArr, 1, 5);
            IntDriver.AddRemoveKeyValPair(intArr, stringArr, 98, 6);

            IntDriver.AddValidations(intArr, stringArr, null, stringArr[0]);
            IntDriver.AddValidations(new RefX1<int>[] { }, new ValX1<string>[] { }, null, stringArr[0]);

            IntDriver.NonGenericIDictionaryBasicAdd(intArr, stringArr);

            IntDriver.NonGenericIDictionaryAddSameKey(intArr, stringArr, 0, 2);
            IntDriver.NonGenericIDictionaryAddSameKey(intArr, stringArr, 99, 3);
            IntDriver.NonGenericIDictionaryAddSameKey(intArr, stringArr, 50, 4);
            IntDriver.NonGenericIDictionaryAddSameKey(intArr, stringArr, 1, 5);
            IntDriver.NonGenericIDictionaryAddSameKey(intArr, stringArr, 98, 6);

            IntDriver.NonGenericIDictionaryAddRemoveKeyValPair(intArr, stringArr, 0, 2);
            IntDriver.NonGenericIDictionaryAddRemoveKeyValPair(intArr, stringArr, 99, 3);
            IntDriver.NonGenericIDictionaryAddRemoveKeyValPair(intArr, stringArr, 50, 4);
            IntDriver.NonGenericIDictionaryAddRemoveKeyValPair(intArr, stringArr, 1, 5);
            IntDriver.NonGenericIDictionaryAddRemoveKeyValPair(intArr, stringArr, 98, 6);

            IntDriver.NonGenericIDictionaryAddValidations(intArr, stringArr, null, stringArr[0]);
            IntDriver.NonGenericIDictionaryAddValidations(new RefX1<int>[] { }, new ValX1<string>[] { }, null, stringArr[0]);

            //Val<Ref>,Ref<Val>
            StringDriver.BasicAdd(stringArr, intArr);

            StringDriver.AddSameKey(stringArr, intArr, 0, 2);
            StringDriver.AddSameKey(stringArr, intArr, 99, 3);
            StringDriver.AddSameKey(stringArr, intArr, 50, 4);
            StringDriver.AddSameKey(stringArr, intArr, 1, 5);
            StringDriver.AddSameKey(stringArr, intArr, 98, 6);

            StringDriver.AddRemoveKeyValPair(stringArr, intArr, 0, 2);
            StringDriver.AddRemoveKeyValPair(stringArr, intArr, 99, 3);
            StringDriver.AddRemoveKeyValPair(stringArr, intArr, 50, 4);
            StringDriver.AddRemoveKeyValPair(stringArr, intArr, 1, 5);
            StringDriver.AddRemoveKeyValPair(stringArr, intArr, 98, 6);

            StringDriver.AddValidations(stringArr, intArr, stringArr[0], intArr[0]);
            StringDriver.AddValidations(new ValX1<string>[] { }, new RefX1<int>[] { }, stringArr[0], intArr[0]);

            StringDriver.NonGenericIDictionaryBasicAdd(stringArr, intArr);

            StringDriver.NonGenericIDictionaryAddSameKey(stringArr, intArr, 0, 2);
            StringDriver.NonGenericIDictionaryAddSameKey(stringArr, intArr, 99, 3);
            StringDriver.NonGenericIDictionaryAddSameKey(stringArr, intArr, 50, 4);
            StringDriver.NonGenericIDictionaryAddSameKey(stringArr, intArr, 1, 5);
            StringDriver.NonGenericIDictionaryAddSameKey(stringArr, intArr, 98, 6);

            StringDriver.NonGenericIDictionaryAddRemoveKeyValPair(stringArr, intArr, 0, 2);
            StringDriver.NonGenericIDictionaryAddRemoveKeyValPair(stringArr, intArr, 99, 3);
            StringDriver.NonGenericIDictionaryAddRemoveKeyValPair(stringArr, intArr, 50, 4);
            StringDriver.NonGenericIDictionaryAddRemoveKeyValPair(stringArr, intArr, 1, 5);
            StringDriver.NonGenericIDictionaryAddRemoveKeyValPair(stringArr, intArr, 98, 6);

            StringDriver.NonGenericIDictionaryAddValidations(stringArr, intArr, stringArr[0], intArr[0]);
            StringDriver.NonGenericIDictionaryAddValidations(new ValX1<string>[] { }, new RefX1<int>[] { }, stringArr[0], intArr[0]);

            Assert.True(test.result);
        }
    }
}