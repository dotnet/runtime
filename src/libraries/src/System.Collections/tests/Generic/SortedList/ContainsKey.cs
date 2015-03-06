// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;
using SortedList_SortedListUtils;

namespace SortedListContainsKey
{
    public class Driver<K, V> where K : IComparableValue
    {
        private Test m_test;

        public Driver(Test test)
        {
            m_test = test;
        }

        public void BasicContainsKey(K[] keys, V[] values)
        {
            SortedList<K, V> tbl = new SortedList<K, V>(new ValueKeyComparer<K>());
            for (int i = 0; i < keys.Length; i++)
            {
                tbl.Add(keys[i], values[i]);
            }
            m_test.Eval(tbl.Count == keys.Length);

            for (int i = 0; i < keys.Length; i++)
            {
                m_test.Eval(tbl.ContainsKey(keys[i]));
            }
        }

        public void ContainsKeyNegative(K[] keys, V[] values, K[] missingkeys)
        {
            SortedList<K, V> tbl = new SortedList<K, V>(new ValueKeyComparer<K>());
            for (int i = 0; i < keys.Length; i++)
            {
                tbl.Add(keys[i], values[i]);
            }
            m_test.Eval(tbl.Count == keys.Length);

            for (int i = 0; i < missingkeys.Length; i++)
            {
                m_test.Eval(false == tbl.ContainsKey(missingkeys[i]));
            }
        }


        public void AddRemoveKeyContainsKey(K[] keys, V[] values, int index)
        {
            SortedList<K, V> tbl = new SortedList<K, V>(new ValueKeyComparer<K>());
            for (int i = 0; i < keys.Length; i++)
            {
                tbl.Add(keys[i], values[i]);
            }
            tbl.Remove(keys[index]);
            m_test.Eval(false == tbl.ContainsKey(keys[index]));
        }

        public void AddRemoveAddKeyContainsKey(K[] keys, V[] values, int index, int repeat)
        {
            SortedList<K, V> tbl = new SortedList<K, V>(new ValueKeyComparer<K>());
            for (int i = 0; i < keys.Length; i++)
            {
                tbl.Add(keys[i], values[i]);
            }
            for (int i = 0; i < repeat; i++)
            {
                tbl.Remove(keys[index]);
                tbl.Add(keys[index], values[index]);
                m_test.Eval(tbl.ContainsKey(keys[index]));
            }
        }

        public void ContainsKeyValidationsRefType(K[] keys, V[] values)
        {
            SortedList<K, V> tbl = new SortedList<K, V>(new ValueKeyComparer<K>());
            for (int i = 0; i < keys.Length; i++)
            {
                tbl.Add(keys[i], values[i]);
            }

            try
            {
                tbl.ContainsKey((K)(object)null);
                m_test.Eval(false);
            }
            catch (ArgumentException)
            {
                m_test.Eval(true);
            }
        }
    }

    public class ContainsKey
    {
        [Fact]
        public static void ContainKeyMain()
        {
            Test test = new Test();

            Driver<RefX1<int>, ValX1<string>> IntDriver = new Driver<RefX1<int>, ValX1<string>>(test);
            RefX1<int>[] intArr1 = new RefX1<int>[100];
            for (int i = 0; i < 100; i++)
            {
                intArr1[i] = new RefX1<int>(i);
            }

            RefX1<int>[] intArr2 = new RefX1<int>[10];
            for (int i = 0; i < 10; i++)
            {
                intArr2[i] = new RefX1<int>(i + 100);
            }

            Driver<ValX1<string>, RefX1<int>> StringDriver = new Driver<ValX1<string>, RefX1<int>>(test);
            ValX1<string>[] stringArr1 = new ValX1<string>[100];
            for (int i = 0; i < 100; i++)
            {
                stringArr1[i] = new ValX1<string>("SomeTestString" + i.ToString());
            }

            ValX1<string>[] stringArr2 = new ValX1<string>[10];
            for (int i = 0; i < 10; i++)
            {
                stringArr2[i] = new ValX1<string>("SomeTestString" + (i + 100).ToString());
            }


            //Ref<val>,Val<Ref>

            IntDriver.BasicContainsKey(intArr1, stringArr1);
            IntDriver.ContainsKeyNegative(intArr1, stringArr1, intArr2);
            IntDriver.ContainsKeyNegative(new RefX1<int>[] { }, new ValX1<string>[] { }, intArr2);
            IntDriver.AddRemoveKeyContainsKey(intArr1, stringArr1, 0);
            IntDriver.AddRemoveKeyContainsKey(intArr1, stringArr1, 50);
            IntDriver.AddRemoveKeyContainsKey(intArr1, stringArr1, 99);
            IntDriver.AddRemoveAddKeyContainsKey(intArr1, stringArr1, 0, 1);
            IntDriver.AddRemoveAddKeyContainsKey(intArr1, stringArr1, 50, 2);
            IntDriver.AddRemoveAddKeyContainsKey(intArr1, stringArr1, 99, 3);
            IntDriver.ContainsKeyValidationsRefType(intArr1, stringArr1);
            IntDriver.ContainsKeyValidationsRefType(new RefX1<int>[] { }, new ValX1<string>[] { });


            //Val<Ref>,Ref<Val>

            StringDriver.BasicContainsKey(stringArr1, intArr1);
            StringDriver.ContainsKeyNegative(stringArr1, intArr1, stringArr2);
            StringDriver.ContainsKeyNegative(new ValX1<string>[] { }, new RefX1<int>[] { }, stringArr2);
            StringDriver.AddRemoveKeyContainsKey(stringArr1, intArr1, 0);
            StringDriver.AddRemoveKeyContainsKey(stringArr1, intArr1, 50);
            StringDriver.AddRemoveKeyContainsKey(stringArr1, intArr1, 99);
            StringDriver.AddRemoveAddKeyContainsKey(stringArr1, intArr1, 0, 1);
            StringDriver.AddRemoveAddKeyContainsKey(stringArr1, intArr1, 50, 2);
            StringDriver.AddRemoveAddKeyContainsKey(stringArr1, intArr1, 99, 3);

            Assert.True(test.result);
        }
    }
}