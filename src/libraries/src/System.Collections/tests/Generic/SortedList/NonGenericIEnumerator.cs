// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;
using SortedList_SortedListUtils;

namespace SortedListNoGenIEnum
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
            IEnumerable enumerable = tbl;
            IEnumerator Enum = enumerable.GetEnumerator();
            //There are no guarantees on the order of elements in the HT
            List<K> kls = new List<K>();
            for (int i = 0; i < keys.Length; i++)
                kls.Add(keys[i]);
            List<V> vls = new List<V>();
            for (int i = 0; i < keys.Length; i++)
                vls.Add(values[i]);
            for (int i = 0; i < keys.Length; i++)
            {
                m_test.Eval(Enum.MoveNext());
                Object entry = Enum.Current;

                m_test.Eval(kls.Contains(((KeyValuePair<K, V>)entry).Key));
                kls.Remove(((KeyValuePair<K, V>)entry).Key);
                m_test.Eval(vls.Contains(((KeyValuePair<K, V>)entry).Value));
                vls.Remove(((KeyValuePair<K, V>)entry).Value);
            }

            for (int i = 0; i < keys.Length; i++)
                kls.Add(keys[i]);
            for (int i = 0; i < keys.Length; i++)
                vls.Add(values[i]);
            foreach (Object entry in enumerable)
            {
                m_test.Eval(kls.Contains(((KeyValuePair<K, V>)entry).Key));
                kls.Remove(((KeyValuePair<K, V>)entry).Key);
                m_test.Eval(vls.Contains(((KeyValuePair<K, V>)entry).Value));
                vls.Remove(((KeyValuePair<K, V>)entry).Value);
            }

            kls = new List<K>();
            for (int i = 0; i < keys.Length; i++)
                kls.Add(keys[i]);
            vls = new List<V>();
            for (int i = 0; i < keys.Length; i++)
                vls.Add(values[i]);
            m_test.Eval(Enum.MoveNext() == false);
        }
        public void GetEnumeratorValidations(K[] keys, V[] values, K nkey, V nvalue)
        {
            SortedList<K, V> tbl = new SortedList<K, V>(new TestSortedList<K, V>(keys, values), new ValueKeyComparer<K>());
            IEnumerable enumerable = tbl;
            IEnumerator Enum = enumerable.GetEnumerator();
            Object entry;

            try
            {
                entry = Enum.Current;
                m_test.Eval(false);
            }
            catch (InvalidOperationException)
            {
                m_test.Eval(true);
            }
            catch (Exception)
            {
                m_test.Eval(false);
            }

            Enum.Reset();

            for (int i = 0; i < keys.Length; i++)
            {
                m_test.Eval(Enum.MoveNext());
                entry = Enum.Current;
                //			m_test.Eval(entry.Key.Equals(keys[i]) && ( ((null==(object)entry.Value)&&(null==(object)values[i])) || entry.Value.Equals(values[i])));
            }
            m_test.Eval(Enum.MoveNext() == false);

            try
            {
                entry = Enum.Current;
                m_test.Eval(false);
            }
            catch (InvalidOperationException)
            {
                m_test.Eval(true);
            }
            catch (Exception)
            {
                m_test.Eval(false);
            }

            Enum = enumerable.GetEnumerator();
            Enum.MoveNext();
            tbl.Add(nkey, nvalue);

            try
            {
                Enum.MoveNext();
                m_test.Eval(false);
            }
            catch (InvalidOperationException)
            {
                m_test.Eval(true);
            }
            catch (Exception)
            {
                m_test.Eval(false);
            }
            try
            {
                Enum.Reset();
                m_test.Eval(false);
            }
            catch (InvalidOperationException)
            {
                m_test.Eval(true);
            }
            catch (Exception)
            {
                m_test.Eval(false);
            }
        }
    }

    public class Enumerator2
    {
        [Fact]
        public static void NoGenIEnumMain()
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