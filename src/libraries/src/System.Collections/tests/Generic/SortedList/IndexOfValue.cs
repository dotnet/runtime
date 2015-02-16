// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections;
using Xunit;
using SortedList_SortedListUtils;

namespace SortedListIdxValue
{
    public class Driver<K, V, R, S> where K : IPublicValue<R> where V : IPublicValue<S>
    {
        private Test m_test;

        public Driver(Test test)
        {
            m_test = test;
        }

        public void BasicIndexOfValue(K[] keys, V[] values)
        {
            SortedList<K, V> tbl = new SortedList<K, V>();
            for (int i = 0; i < keys.Length; i++)
            {
                tbl.Add(keys[i], values[i]);

                m_test.Eval(tbl.IndexOfValue(values[i]) == i, "BasicIndexOfValue1: Expected index of key to be " + i + " but found " + tbl.IndexOfValue(values[i]));

                if (i + 1 < keys.Length) m_test.Eval(tbl.IndexOfValue(values[i + 1]) == -1, "BasicIndexOfValue3: Expected not to find " + values[i + 1].publicVal + " but found it at index, " + tbl.IndexOfValue(values[i]));
            }
            m_test.Eval(tbl.Count == keys.Length);
        }

        public void IndexOfValueNegative(K[] keys, V[] values, V[] missingkeys)
        {
            SortedList<K, V> tbl = new SortedList<K, V>();
            V tempToTest = default(V);
            for (int i = 0; i < keys.Length; i++)
            {
                tbl.Add(keys[i], values[i]);
            }
            m_test.Eval(tbl.Count == keys.Length);

            m_test.Eval(tbl.IndexOfValue(tempToTest) == -1, "IndexOfValueNegative0: Expected trying to reference a value of null to generate a return of -1, but it did not.");

            for (int i = 0; i < missingkeys.Length; i++)
            {
                m_test.Eval(tbl.IndexOfValue(missingkeys[i]) == -1, "IndexOfValueNegative2: Expected trying to IndexOfValue a key that hasn't been added to return -1, but it returned " + tbl.IndexOfValue(missingkeys[i]) + " for item " + missingkeys[i].publicVal);
            }
        }

        public void AddRemoveKeyIndexOfValue(K[] keys, V[] values, int index)
        {
            SortedList<K, V> tbl = new SortedList<K, V>();
            for (int i = 0; i < keys.Length; i++)
            {
                tbl.Add(keys[i], values[i]);
            }
            tbl.Remove(keys[index]);
            m_test.Eval(tbl.IndexOfValue(values[index]) == -1, "AddRemoveKeyIndexOfValue: Expected trying to IndexOfValue a key that has been removed to be -1, but it returned " + tbl.IndexOfValue(values[index]) + " for item " + values[index]);
        }
    }

    public class IndexOfValue
    {
        [Fact]
        public static void IndexOfValueMain()
        {
            Test test = new Test();

            Driver<RefX1<int>, ValX1<string>, int, string> IntDriver = new Driver<RefX1<int>, ValX1<string>, int, string>(test);
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

            Driver<ValX1<string>, RefX1<int>, string, int> StringDriver = new Driver<ValX1<string>, RefX1<int>, string, int>(test);
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
            IntDriver.BasicIndexOfValue(intArr1, stringArr1);
            IntDriver.IndexOfValueNegative(intArr1, stringArr1, stringArr2);
            IntDriver.IndexOfValueNegative(new RefX1<int>[0], new ValX1<string>[0], stringArr2);
            IntDriver.AddRemoveKeyIndexOfValue(intArr1, stringArr1, 0);
            IntDriver.AddRemoveKeyIndexOfValue(intArr1, stringArr1, 50);
            IntDriver.AddRemoveKeyIndexOfValue(intArr1, stringArr1, 99);

            //Val<Ref>,Ref<Val>			
            StringDriver.BasicIndexOfValue(stringArr1, intArr1);
            StringDriver.IndexOfValueNegative(stringArr1, intArr1, intArr2);
            StringDriver.AddRemoveKeyIndexOfValue(stringArr1, intArr1, 0);
            StringDriver.AddRemoveKeyIndexOfValue(stringArr1, intArr1, 50);
            StringDriver.AddRemoveKeyIndexOfValue(stringArr1, intArr1, 99);

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
                intArr3[i] = new RefX1<int>(i);
            }

            stringArr1 = new ValX1<string>[105];
            for (int i = 0; i < 105; i++)
            {
                stringArr1[i] = new ValX1<string>("SomeTestString" + i.ToString());
            }

            stringArr2 = new ValX1<string>[15];
            for (int i = 0; i < 15; i++)
            {
                stringArr2[i] = new ValX1<string>("SomeTestString" + (i + 100).ToString());
            }

            IntDriver.BasicIndexOfValue(intArr1, stringArr1);
            StringDriver.BasicIndexOfValue(stringArr2, intArr3);

            Assert.True(test.result);
        }
    }
}