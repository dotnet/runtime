// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections;
using Xunit;
using SortedList_SortedListUtils;

namespace SortedListIdxKey
{
    public class Driver<K, V, R, S> where K : IPublicValue<R> where V : IPublicValue<S>
    {
        public void BasicIndexOfKey(K[] keys, V[] values)
        {
            SortedList<K, V> tbl = new SortedList<K, V>();
            for (int i = 0; i < keys.Length; i++)
            {
                tbl.Add(keys[i], values[i]);
                Test.Eval(tbl.IndexOfKey(keys[i]) == i, "BasicIndexOfKey1: Expected index of key to be " + i + " but found " + tbl.IndexOfKey(keys[i]));
                if (i + 1 < keys.Length) Test.Eval(tbl.IndexOfKey(keys[i + 1]) == -1, "BasicIndexOfKey3: Expected not to find" + keys[i + 1].publicVal + " but found it at index, " + tbl.IndexOfKey(keys[i]));
            }
            Test.Eval(tbl.Count == keys.Length);
        }

        public void IndexOfKeyNegative(K[] keys, V[] values, K[] missingkeys)
        {
            SortedList<K, V> tbl = new SortedList<K, V>();
            K tempToTest = default(K);
            for (int i = 0; i < keys.Length; i++)
            {
                tbl.Add(keys[i], values[i]);
            }
            Test.Eval(tbl.Count == keys.Length);

            try
            {
                tbl.IndexOfKey(tempToTest);
                Test.Eval(tempToTest != null, "IndexOfKeyNegative0: Expected trying to reference a key of null to generate an ArgumentNullException, but it did not.");
            }
            catch (ArgumentNullException)
            {
            }

            for (int i = 0; i < missingkeys.Length; i++)
            {
                Test.Eval(tbl.IndexOfKey(missingkeys[i]) == -1, "IndexOfKeyNegative2: Expected trying to IndexOfKey a key that hasn't been added to return -1, but it returned " + tbl.IndexOfKey(missingkeys[i]) + " for item " + missingkeys[i].publicVal);
            }
        }

        public void AddRemoveKeyIndexOfKey(K[] keys, V[] values, int index)
        {
            SortedList<K, V> tbl = new SortedList<K, V>();
            for (int i = 0; i < keys.Length; i++)
            {
                tbl.Add(keys[i], values[i]);
            }
            tbl.Remove(keys[index]);
            Test.Eval(tbl.IndexOfKey(keys[index]) == -1, "AddRemoveKeyIndexOfKey: Expected trying to IndexOfKey a key that has been removed to be -1, but it returned " + tbl.IndexOfKey(keys[index]) + " for item " + keys[index]);
        }
    }

    public class IndexOfKey
    {
        [Fact]
        public static void IndexOfKeyMain()
        {
            Driver<RefX1<int>, ValX1<string>, int, string> IntDriver = new Driver<RefX1<int>, ValX1<string>, int, string>();
            RefX1<int>[] intArr1 = new RefX1<int>[100];
            for (int i = 0; i < 100; i++)
            {
                intArr1[i] = new RefX1<int>(i);
            }

            RefX1<int>[] intArr2 = new RefX1<int>[15];
            for (int i = 0; i < 10; i++)
            {
                intArr2[i] = new RefX1<int>(i + 200);
            }
            for (int i = 10; i < 15; i++)
            {
                intArr2[i] = new RefX1<int>(i + 195);
            }

            Driver<ValX1<string>, RefX1<int>, string, int> StringDriver = new Driver<ValX1<string>, RefX1<int>, string, int>();
            ValX1<string>[] stringArr1 = new ValX1<string>[100];
            for (int i = 0; i < 100; i++)
            {
                stringArr1[i] = new ValX1<string>("SomeTestString" + (i + 100).ToString());
            }

            ValX1<string>[] stringArr2 = new ValX1<string>[15];
            for (int i = 0; i < 10; i++)
            {
                stringArr2[i] = new ValX1<string>("SomeTestString" + (i + 200));
            }
            for (int i = 10; i < 15; i++)
            {
                stringArr2[i] = new ValX1<string>("SomeTestString" + (i + 195));
            }

            //Ref<val>,Val<Ref>			
            IntDriver.BasicIndexOfKey(intArr1, stringArr1);
            Assert.True(Test.result);
            IntDriver.IndexOfKeyNegative(intArr1, stringArr1, intArr2);
            IntDriver.IndexOfKeyNegative(new RefX1<int>[0], new ValX1<string>[0], intArr2);
            IntDriver.AddRemoveKeyIndexOfKey(intArr1, stringArr1, 0);
            IntDriver.AddRemoveKeyIndexOfKey(intArr1, stringArr1, 50);
            IntDriver.AddRemoveKeyIndexOfKey(intArr1, stringArr1, 99);

            //Val<Ref>,Ref<Val>			
            StringDriver.BasicIndexOfKey(stringArr1, intArr1);
            StringDriver.IndexOfKeyNegative(stringArr1, intArr1, stringArr2);
            StringDriver.AddRemoveKeyIndexOfKey(stringArr1, intArr1, 0);
            StringDriver.AddRemoveKeyIndexOfKey(stringArr1, intArr1, 50);
            StringDriver.AddRemoveKeyIndexOfKey(stringArr1, intArr1, 99);

            intArr1 = new RefX1<int>[105];
            for (int i = 0; i < 105; i++)
            {
                intArr1[i] = new RefX1<int>(i);
            }

            RefX1<int>[] intArr3 = new RefX1<int>[15];
            for (int i = 0; i < 10; i++)
            {
                intArr3[i] = new RefX1<int>(i + 100);
            }
            for (int i = 10; i < 15; i++)
            {
                intArr3[i] = new RefX1<int>(101);
            }

            stringArr1 = new ValX1<string>[105];
            for (int i = 0; i < 100; i++)
            {
                stringArr1[i] = new ValX1<string>("SomeTestString" + i.ToString());
            }
            for (int i = 100; i < 105; i++)
            {
                stringArr1[i] = new ValX1<string>("SomeTestString11");
            }

            stringArr2 = new ValX1<string>[15];
            for (int i = 0; i < 15; i++)
            {
                stringArr2[i] = new ValX1<string>("SomeTestString" + (i + 100).ToString());
            }

            IntDriver.BasicIndexOfKey(intArr1, stringArr1);
            StringDriver.BasicIndexOfKey(stringArr2, intArr3);

            Assert.True(Test.result);
        }
    }
}