// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;
using SortedList_SortedListUtils;

namespace SortedListContains
{
    public class Driver<K, V> where K : IComparableValue
    {
        private Test m_test;

        public Driver(Test test)
        {
            m_test = test;
        }

        /*public void BasicContains(K[] keys, V[] values)
        {
            SortedList<K,V> tbl = new SortedList<K,V>(new ValueKeyComparer<K>());
            for (int i=0; i<keys.Length; i++)
            {
                tbl.Add(keys[i],values[i]);
            }
            m_test.Eval(tbl.Count==keys.Length);

            for (int i=0; i<keys.Length; i++)
            {
                m_test.Eval(tbl.Contains(keys[i]));
            }
        }

        public void ContainsNegative(K[] keys, V[] values, K[] missingkeys)
        {
            SortedList<K,V> tbl = new SortedList<K,V>(new ValueKeyComparer<K>());
            for (int i=0; i<keys.Length; i++)
            {
                tbl.Add(keys[i],values[i]);
            }
            m_test.Eval(tbl.Count==keys.Length);

            for (int i=0; i<missingkeys.Length; i++)
            {
                m_test.Eval(false==tbl.Contains(missingkeys[i]));
            }
        }


        public void AddRemoveKeyContains(K[] keys, V[] values, int index)
        {
            SortedList<K,V> tbl = new SortedList<K,V>(new ValueKeyComparer<K>());
            for (int i=0; i<keys.Length; i++)
            {
                tbl.Add(keys[i],values[i]);
            }
            tbl.Remove(keys[index]);
            m_test.Eval(false==tbl.Contains(keys[index]));
        }

        public void AddRemoveAddKeyContains(K[] keys, V[] values, int index, int repeat)
        {
            SortedList<K,V> tbl = new SortedList<K,V>(new ValueKeyComparer<K>());
            for (int i=0; i<keys.Length; i++)
            {
                tbl.Add(keys[i],values[i]);
            }
            for (int i=0; i<repeat; i++)
            {
                tbl.Remove(keys[index]);
                tbl.Add(keys[index],values[index]);
                m_test.Eval(tbl.Contains(keys[index]));
            }
        }

        public void ContainsValidations(K[] keys, V[] values)
        {
            SortedList<K,V> tbl = new SortedList<K,V>(new ValueKeyComparer<K>());
            for (int i=0; i<keys.Length; i++)
            {
                tbl.Add(keys[i],values[i]);
            }

            //
            //try null key
            //
            if(!typeof(K).IsSubclassOf(typeof(System.ValueType)))
            {
                try
                {
                    tbl.Contains((K)(object)null);
                    m_test.Eval(false);
                }
                catch(ArgumentException)
                {
                    m_test.Eval(true);
                }
            }
        }*/

        public void NonGenericIDictionaryBasicContains(K[] keys, V[] values)
        {
            SortedList<K, V> tbl = new SortedList<K, V>(new ValueKeyComparer<K>());
            IDictionary _idic = tbl;
            for (int i = 0; i < keys.Length; i++)
            {
                tbl.Add(keys[i], values[i]);
            }
            m_test.Eval(tbl.Count == keys.Length);

            for (int i = 0; i < keys.Length; i++)
            {
                m_test.Eval(_idic.Contains(keys[i]));
            }
        }

        public void NonGenericIDictionaryContainsNegative(K[] keys, V[] values, K[] missingkeys)
        {
            SortedList<K, V> tbl = new SortedList<K, V>(new ValueKeyComparer<K>());
            IDictionary _idic = tbl;
            for (int i = 0; i < keys.Length; i++)
            {
                tbl.Add(keys[i], values[i]);
            }
            m_test.Eval(tbl.Count == keys.Length);

            for (int i = 0; i < missingkeys.Length; i++)
            {
                m_test.Eval(false == _idic.Contains(missingkeys[i]));
            }
        }


        public void NonGenericIDictionaryAddRemoveKeyContains(K[] keys, V[] values, int index)
        {
            SortedList<K, V> tbl = new SortedList<K, V>(new ValueKeyComparer<K>());
            IDictionary _idic = tbl;
            for (int i = 0; i < keys.Length; i++)
            {
                tbl.Add(keys[i], values[i]);
            }
            tbl.Remove(keys[index]);
            m_test.Eval(false == _idic.Contains(keys[index]));
        }

        public void NonGenericIDictionaryAddRemoveAddKeyContains(K[] keys, V[] values, int index, int repeat)
        {
            SortedList<K, V> tbl = new SortedList<K, V>(new ValueKeyComparer<K>());
            IDictionary _idic = tbl;
            for (int i = 0; i < keys.Length; i++)
            {
                tbl.Add(keys[i], values[i]);
            }
            for (int i = 0; i < repeat; i++)
            {
                tbl.Remove(keys[index]);
                tbl.Add(keys[index], values[index]);
                m_test.Eval(_idic.Contains(keys[index]));
            }
        }

        public void NonGenericIDictionaryContainsValidations(K[] keys, V[] values)
        {
            SortedList<K, V> tbl = new SortedList<K, V>(new ValueKeyComparer<K>());
            IDictionary _idic = tbl;
            for (int i = 0; i < keys.Length; i++)
            {
                tbl.Add(keys[i], values[i]);
            }

            //
            //try null key
            //
            try
            {
                _idic.Contains(null);
                m_test.Eval(false);
            }
            catch (ArgumentException)
            {
                m_test.Eval(true);
            }

            m_test.Eval(!_idic.Contains(new Random(-55)), "Err_298282haiued Expected Contains to return false with an invalid type for the key");
        }
    }

    public class Contains
    {
        [Fact]
        public static void ContainsMain()
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

            /*			IntDriver.BasicContains(intArr1,stringArr1);
                        IntDriver.ContainsNegative(intArr1,stringArr1,intArr2);
                        IntDriver.ContainsNegative(new RefX1<int>[]{},new ValX1<string>[]{},intArr2);
                        IntDriver.AddRemoveKeyContains(intArr1,stringArr1,0);
                        IntDriver.AddRemoveKeyContains(intArr1,stringArr1,50);
                        IntDriver.AddRemoveKeyContains(intArr1,stringArr1,99);
                        IntDriver.AddRemoveAddKeyContains(intArr1,stringArr1,0,1);
                        IntDriver.AddRemoveAddKeyContains(intArr1,stringArr1,50,2);
                        IntDriver.AddRemoveAddKeyContains(intArr1,stringArr1,99,3);
                        IntDriver.ContainsValidations(intArr1,stringArr1);
                        IntDriver.ContainsValidations(new RefX1<int>[]{},new ValX1<string>[]{});*/

            IntDriver.NonGenericIDictionaryBasicContains(intArr1, stringArr1);
            IntDriver.NonGenericIDictionaryContainsNegative(intArr1, stringArr1, intArr2);
            IntDriver.NonGenericIDictionaryContainsNegative(new RefX1<int>[] { }, new ValX1<string>[] { }, intArr2);
            IntDriver.NonGenericIDictionaryAddRemoveKeyContains(intArr1, stringArr1, 0);
            IntDriver.NonGenericIDictionaryAddRemoveKeyContains(intArr1, stringArr1, 50);
            IntDriver.NonGenericIDictionaryAddRemoveKeyContains(intArr1, stringArr1, 99);
            IntDriver.NonGenericIDictionaryAddRemoveAddKeyContains(intArr1, stringArr1, 0, 1);
            IntDriver.NonGenericIDictionaryAddRemoveAddKeyContains(intArr1, stringArr1, 50, 2);
            IntDriver.NonGenericIDictionaryAddRemoveAddKeyContains(intArr1, stringArr1, 99, 3);
            IntDriver.NonGenericIDictionaryContainsValidations(intArr1, stringArr1);
            IntDriver.NonGenericIDictionaryContainsValidations(new RefX1<int>[] { }, new ValX1<string>[] { });


            //Val<Ref>,Ref<Val>

            /*			StringDriver.BasicContains(stringArr1,intArr1);
                        StringDriver.ContainsNegative(stringArr1,intArr1,stringArr2);
                        StringDriver.ContainsNegative(new ValX1<string>[]{},new RefX1<int>[]{},stringArr2);
                        StringDriver.AddRemoveKeyContains(stringArr1,intArr1,0);
                        StringDriver.AddRemoveKeyContains(stringArr1,intArr1,50);
                        StringDriver.AddRemoveKeyContains(stringArr1,intArr1,99);
                        StringDriver.AddRemoveAddKeyContains(stringArr1,intArr1,0,1);
                        StringDriver.AddRemoveAddKeyContains(stringArr1,intArr1,50,2);
                        StringDriver.AddRemoveAddKeyContains(stringArr1,intArr1,99,3);
                        StringDriver.ContainsValidations(stringArr1,intArr1);
                        StringDriver.ContainsValidations(new ValX1<string>[]{},new RefX1<int>[]{});*/

            StringDriver.NonGenericIDictionaryBasicContains(stringArr1, intArr1);
            StringDriver.NonGenericIDictionaryContainsNegative(stringArr1, intArr1, stringArr2);
            StringDriver.NonGenericIDictionaryContainsNegative(new ValX1<string>[] { }, new RefX1<int>[] { }, stringArr2);
            StringDriver.NonGenericIDictionaryAddRemoveKeyContains(stringArr1, intArr1, 0);
            StringDriver.NonGenericIDictionaryAddRemoveKeyContains(stringArr1, intArr1, 50);
            StringDriver.NonGenericIDictionaryAddRemoveKeyContains(stringArr1, intArr1, 99);
            StringDriver.NonGenericIDictionaryAddRemoveAddKeyContains(stringArr1, intArr1, 0, 1);
            StringDriver.NonGenericIDictionaryAddRemoveAddKeyContains(stringArr1, intArr1, 50, 2);
            StringDriver.NonGenericIDictionaryAddRemoveAddKeyContains(stringArr1, intArr1, 99, 3);
            StringDriver.NonGenericIDictionaryContainsValidations(stringArr1, intArr1);
            StringDriver.NonGenericIDictionaryContainsValidations(new ValX1<string>[] { }, new RefX1<int>[] { });

            Assert.True(test.result);
        }
    }
}