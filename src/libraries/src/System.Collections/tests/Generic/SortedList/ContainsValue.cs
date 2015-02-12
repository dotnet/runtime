// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;
using SortedList_SortedListUtils;

namespace SortedListContainsValue
{
    public class Driver<K, V> where K : IComparableValue
    {
        public void BasicContainsValue(K[] keys, V[] values)
        {
            SortedList<K, V> tbl = new SortedList<K, V>(new ValueKeyComparer<K>());
            for (int i = 0; i < keys.Length; i++)
            {
                tbl.Add(keys[i], values[i]);
            }
            Test.Eval(tbl.Count == keys.Length);

            for (int i = 0; i < keys.Length; i++)
            {
                Test.Eval(tbl.ContainsValue(values[i]));
            }
        }

        public void ContainsValueNegative(K[] keys, V[] values, V[] missingvalues)
        {
            SortedList<K, V> tbl = new SortedList<K, V>(new ValueKeyComparer<K>());
            for (int i = 0; i < keys.Length; i++)
            {
                tbl.Add(keys[i], values[i]);
            }
            Test.Eval(tbl.Count == keys.Length);

            for (int i = 0; i < missingvalues.Length; i++)
            {
                Test.Eval(false == tbl.ContainsValue(missingvalues[i]));
            }
        }


        public void AddRemoveKeyContainsValue(K[] keys, V[] values, int index)
        {
            SortedList<K, V> tbl = new SortedList<K, V>(new ValueKeyComparer<K>());
            for (int i = 0; i < keys.Length; i++)
            {
                tbl.Add(keys[i], values[i]);
            }
            tbl.Remove(keys[index]);
            Test.Eval(false == tbl.ContainsValue(values[index]));
        }

        public void AddRemoveAddKeyContainsValue(K[] keys, V[] values, int index, int repeat)
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
                Test.Eval(tbl.ContainsValue(values[index]));
            }
        }
    }

    public class ContainsValue
    {
        [Fact]
        public static void ContainsValueMain()
        {
            Driver<RefX1<int>, ValX1<string>> IntDriver = new Driver<RefX1<int>, ValX1<string>>();
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

            Driver<ValX1<string>, RefX1<int>> StringDriver = new Driver<ValX1<string>, RefX1<int>>();
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

            IntDriver.BasicContainsValue(intArr1, stringArr1);
            IntDriver.ContainsValueNegative(intArr1, stringArr1, stringArr2);
            IntDriver.ContainsValueNegative(new RefX1<int>[] { }, new ValX1<string>[] { }, stringArr2);
            IntDriver.AddRemoveKeyContainsValue(intArr1, stringArr1, 0);
            IntDriver.AddRemoveKeyContainsValue(intArr1, stringArr1, 50);
            IntDriver.AddRemoveKeyContainsValue(intArr1, stringArr1, 99);
            IntDriver.AddRemoveAddKeyContainsValue(intArr1, stringArr1, 0, 1);
            IntDriver.AddRemoveAddKeyContainsValue(intArr1, stringArr1, 50, 2);
            IntDriver.AddRemoveAddKeyContainsValue(intArr1, stringArr1, 99, 3);


            //Val<Ref>,Ref<Val>

            StringDriver.BasicContainsValue(stringArr1, intArr1);
            StringDriver.BasicContainsValue(new ValX1<string>[] { new ValX1<string>("str") }, new RefX1<int>[] { null });
            StringDriver.ContainsValueNegative(stringArr1, intArr1, intArr2);
            StringDriver.ContainsValueNegative(new ValX1<string>[] { }, new RefX1<int>[] { }, intArr2);
            StringDriver.ContainsValueNegative(new ValX1<string>[] { }, new RefX1<int>[] { }, new RefX1<int>[] { null });
            StringDriver.AddRemoveKeyContainsValue(stringArr1, intArr1, 0);
            StringDriver.AddRemoveKeyContainsValue(stringArr1, intArr1, 50);
            StringDriver.AddRemoveKeyContainsValue(stringArr1, intArr1, 99);
            StringDriver.AddRemoveAddKeyContainsValue(stringArr1, intArr1, 0, 1);
            StringDriver.AddRemoveAddKeyContainsValue(stringArr1, intArr1, 50, 2);
            StringDriver.AddRemoveAddKeyContainsValue(stringArr1, intArr1, 99, 3);

            Assert.True(Test.result);
        }
    }
}
