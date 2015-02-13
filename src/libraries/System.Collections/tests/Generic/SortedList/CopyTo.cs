// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;
using SortedList_SortedListUtils;

namespace SortedListCopyTo
{
    public class Driver<K, V> where K : IComparableValue
    {
        private Test m_test;

        public Driver(Test test)
        {
            m_test = test;
        }

        public void BasicCopyTo<Ex>(K[] keys, V[] values, KeyValuePair<K, V>[] target, int index, bool throws) where Ex : System.Exception
        {
            SortedList<K, V> tbl = new SortedList<K, V>(new ValueKeyComparer<K>());
            for (int i = 0; i < keys.Length; i++)
            {
                tbl.Add(keys[i], values[i]);
            }
            m_test.Eval(tbl.Count == keys.Length);

            try
            {
                ((IDictionary<K, V>)tbl).CopyTo(target, index);
                if (throws)
                {
                    m_test.Eval(false);
                }
                else
                {
                    int i = 0;
                    System.Collections.Generic.KeyValuePair<K, V>[] arr = (System.Collections.Generic.KeyValuePair<K, V>[])target;
                    foreach (System.Collections.Generic.KeyValuePair<K, V> entry in tbl)
                    {
                        m_test.Eval(((Object)entry.Key).Equals(arr[i].Key) && (((null == (object)entry.Value) && (null == (object)arr[i].Value)) || ((Object)entry.Value).Equals(arr[i].Value)));
                        i++;
                    }
                }
            }
            catch (Ex)
            {
                m_test.Eval(throws);
            }
        }
    }

    public class SpecificException : System.Exception { }

    public class CopyTo
    {
        [Fact]
        public static void CopyToMain()
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

            IntDriver.BasicCopyTo<SpecificException>(intArr, stringArr, new System.Collections.Generic.KeyValuePair<RefX1<int>, ValX1<string>>[100], 0, false);
            IntDriver.BasicCopyTo<SpecificException>(new RefX1<int>[] { }, new ValX1<string>[] { }, new System.Collections.Generic.KeyValuePair<RefX1<int>, ValX1<string>>[0], 0, false);
            IntDriver.BasicCopyTo<ArgumentOutOfRangeException>(intArr, stringArr, new System.Collections.Generic.KeyValuePair<RefX1<int>, ValX1<string>>[100], -1, true);
            IntDriver.BasicCopyTo<ArgumentNullException>(intArr, stringArr, null, 0, true);
            IntDriver.BasicCopyTo<ArgumentException>(intArr, stringArr, new System.Collections.Generic.KeyValuePair<RefX1<int>, ValX1<string>>[100], 1, true);
            //IntDriver.BasicCopyTo<ArgumentException>(intArr,stringArr,new System.Collections.Generic.KeyValuePair<RefX1<int>,ValX1<string>>[100,1],0,true);
            //IntDriver.BasicCopyTo<ArrayTypeMismatchException>(intArr,stringArr,new int[100],0,true);


            //Val<Ref>,Ref<Val>

            StringDriver.BasicCopyTo<SpecificException>(stringArr, intArr, new System.Collections.Generic.KeyValuePair<ValX1<string>, RefX1<int>>[100], 0, false);
            StringDriver.BasicCopyTo<SpecificException>(new ValX1<string>[] { }, new RefX1<int>[] { }, new System.Collections.Generic.KeyValuePair<ValX1<string>, RefX1<int>>[0], 0, false);
            //StringDriver.BasicCopyTo<ArrayTypeMismatchException>(stringArr,intArr,new string[100],0,true);

            Assert.True(test.result);
        }
    }
}
