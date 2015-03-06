// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;
using SortedList_SortedListUtils;

namespace SortedListNoGenIDicEnum
{
    public class Driver<K, V> where K : IComparableValue
    {
        private Test m_test;

        public Driver(Test test)
        {
            m_test = test;
        }

        public void GetEnumeratorBasic(K[] keys, V[] values)
        {
            SortedList<K, V> tbl = new SortedList<K, V>(new TestSortedList<K, V>(keys, values), new ValueKeyComparer<K>());
            IDictionary dictionaryIEnumerable = tbl;
            IDictionaryEnumerator Enum = dictionaryIEnumerable.GetEnumerator();
            //There are no guarantees on the order of elements in the HT
            List<K> kls = new List<K>();
            for (int i = 0; i < keys.Length; i++)
                kls.Add(keys[i]);
            List<V> vls = new List<V>();
            for (int i = 0; i < keys.Length; i++)
                vls.Add(values[i]);
            for (int i = 0; i < keys.Length; i++)
            {
                m_test.Eval(Enum.MoveNext(), "Err_19191haiede Expected MoveNext to return true");
                DictionaryEntry entry = Enum.Entry;

                m_test.Eval(kls.Contains((K)entry.Key), "Err_21988932haiue Expected Contains with key " + entry.Key + " to return true");
                kls.Remove((K)Enum.Key);
                m_test.Eval(vls.Contains((V)entry.Value), "Err_2919ahied Epected Contains with value " + entry.Value + " to return true");
                vls.Remove((V)Enum.Value);
            }
            for (int i = 0; i < keys.Length; i++)
                kls.Add(keys[i]);
            for (int i = 0; i < keys.Length; i++)
                vls.Add(values[i]);
            foreach (Object entry in dictionaryIEnumerable)
            {
                m_test.Eval(kls.Contains((K)((DictionaryEntry)entry).Key), "Err_789322ahie Expected Contains with key " + (K)((DictionaryEntry)entry).Key + " to return true");
                kls.Remove((K)((DictionaryEntry)entry).Key);
                m_test.Eval(vls.Contains((V)((DictionaryEntry)entry).Value), "Err_668851ajied Epected Contains with value " + (V)((DictionaryEntry)entry).Value + " to return true");
                vls.Remove((V)((DictionaryEntry)entry).Value);
            }
            kls = new List<K>();
            for (int i = 0; i < keys.Length; i++)
                kls.Add(keys[i]);
            vls = new List<V>();
            for (int i = 0; i < keys.Length; i++)
                vls.Add(values[i]);
            m_test.Eval(Enum.MoveNext() == false, "Err_29829anie Expected MoveNext to return false");
        }
        public void GetEnumeratorValidations(K[] keys, V[] values, K nkey, V nvalue)
        {
            SortedList<K, V> tbl = new SortedList<K, V>(new TestSortedList<K, V>(keys, values), new ValueKeyComparer<K>());
            IDictionary dictionaryIEnumerable = tbl;
            IDictionaryEnumerator Enum = dictionaryIEnumerable.GetEnumerator();
            DictionaryEntry expectedDictionaryEntry;

            Object entry;

            try
            {
                entry = Enum.Entry;
            }
            catch (Exception)
            {
            }

            for (int i = 0; i < keys.Length; i++)
            {
                m_test.Eval(Enum.MoveNext(), "Err_19273ajeid Expected MoveNext to return true");
                entry = Enum.Entry;
                //			m_test.Eval(entry.Key.Equals(keys[i]) && ( ((null==(object)entry.Value)&&(null==(object)values[i])) || entry.Value.Equals(values[i])));
            }
            m_test.Eval(Enum.MoveNext() == false, "Err_32218ahied Expected MoveNext to return false");

            try
            {
                entry = Enum.Entry;
                m_test.Eval(false, "Err_8795188ahied Expected Entry to throw InvalidOperationException");
            }
            catch (InvalidOperationException)
            {
            }

            try
            {
                entry = Enum.Key;
                m_test.Eval(false, "Err_584848ajeoia Expected Key to throw InvalidOperationException");
            }
            catch (InvalidOperationException)
            {
            }

            try
            {
                entry = Enum.Value;
                m_test.Eval(false, "Err_681588aiepad Expected Value to throw InvalidOperationException");
            }
            catch (InvalidOperationException)
            {
            }

            Enum = dictionaryIEnumerable.GetEnumerator();
            Enum.MoveNext();
            tbl.Add(nkey, nvalue);
            tbl.Remove(nkey);

            if (keys.Length != 0)
            {
                entry = Enum.Entry;
                expectedDictionaryEntry = new DictionaryEntry(keys[0], values[0]);
                m_test.Eval(entry.Equals(expectedDictionaryEntry), "Err_845418aheid Expected DictionaryEntry={0} actual={1}", expectedDictionaryEntry, entry);


                entry = Enum.Key;
                m_test.Eval(entry.Equals(keys[0]), "Err_56688ajioed Expected Key={0} actual={1}", keys[0], entry);

                entry = Enum.Value;
                m_test.Eval(entry.Equals(values[0]), "Err_5888ajoed Expected Value={0} actual={1}", values[0], entry);
            }
            else
            {
                try
                {
                    entry = Enum.Entry;
                    m_test.Eval(false, "Err_845418aheid Expected Entry to throw InvalidOperationException");
                }
                catch (InvalidOperationException)
                {
                }

                try
                {
                    entry = Enum.Key;
                    m_test.Eval(false, "Err_56688ajioed Expected Key to throw InvalidOperationException");
                }
                catch (InvalidOperationException)
                {
                }

                try
                {
                    entry = Enum.Value;
                    m_test.Eval(false, "Err_5888ajoed Expected Value to throw InvalidOperationException");
                }
                catch (InvalidOperationException)
                {
                }
            }
        }
    }

    public class Enumerator2
    {
        [Fact]
        public static void NOGenIDicEnumMain()
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


            //Ref<Val>,Val<Ref>
            IntDriver.GetEnumeratorBasic(intArr, stringArr);
            IntDriver.GetEnumeratorBasic(new RefX1<int>[0], new ValX1<string>[0]);
            IntDriver.GetEnumeratorValidations(intArr, stringArr, new RefX1<int>(1000), new ValX1<string>("1000"));
            IntDriver.GetEnumeratorValidations(new RefX1<int>[0], new ValX1<string>[0], new RefX1<int>(1000), new ValX1<string>("1000"));


            //Val<Ref>,Ref<Val>
            StringDriver.GetEnumeratorBasic(stringArr, intArr);
            StringDriver.GetEnumeratorBasic(new ValX1<string>[0], new RefX1<int>[0]);
            StringDriver.GetEnumeratorValidations(stringArr, intArr, new ValX1<string>("1000"), new RefX1<int>(1000));
            StringDriver.GetEnumeratorValidations(new ValX1<string>[0], new RefX1<int>[0], new ValX1<string>("1000"), new RefX1<int>(1000));

            Assert.True(test.result);
        }
    }
}