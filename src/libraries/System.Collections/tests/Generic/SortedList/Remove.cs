// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;
using SortedList_SortedListUtils;

namespace SortedListRemove
{
    public class Driver<K, V> where K : IComparableValue
    {
        private Test m_test;

        public Driver(Test test)
        {
            m_test = test;
        }

        public void BasicRemove(K[] keys, V[] values)
        {
            SortedList<K, V> tbl = new SortedList<K, V>(new ValueKeyComparer<K>());
            for (int i = 0; i < keys.Length; i++)
            {
                tbl.Add(keys[i], values[i]);
            }
            m_test.Eval(tbl.Count == keys.Length);

            for (int i = 0; i < keys.Length; i++)
            {
                m_test.Eval(tbl.Remove(keys[i]));
            }
            m_test.Eval(tbl.Count == 0);
        }

        public void RemoveNegative(K[] keys, V[] values, K[] missingkeys)
        {
            SortedList<K, V> tbl = new SortedList<K, V>(new ValueKeyComparer<K>());
            for (int i = 0; i < keys.Length; i++)
            {
                tbl.Add(keys[i], values[i]);
            }
            m_test.Eval(tbl.Count == keys.Length);

            for (int i = 0; i < missingkeys.Length; i++)
            {
                m_test.Eval(false == tbl.Remove(missingkeys[i]));
            }
            m_test.Eval(tbl.Count == keys.Length);
        }

        public void RemoveSameKey(K[] keys, V[] values, int index, int repeat)
        {
            SortedList<K, V> tbl = new SortedList<K, V>(new ValueKeyComparer<K>());
            for (int i = 0; i < keys.Length; i++)
            {
                tbl.Add(keys[i], values[i]);
            }
            m_test.Eval(tbl.Count == keys.Length);
            m_test.Eval(tbl.Remove(keys[index]));
            for (int i = 0; i < repeat; i++)
            {
                m_test.Eval(false == tbl.Remove(keys[index]));
            }
            m_test.Eval(tbl.Count == keys.Length - 1);
        }

        public void AddRemoveSameKey(K[] keys, V[] values, int index, int repeat)
        {
            SortedList<K, V> tbl = new SortedList<K, V>(new ValueKeyComparer<K>());
            for (int i = 0; i < keys.Length; i++)
            {
                tbl.Add(keys[i], values[i]);
            }
            m_test.Eval(tbl.Count == keys.Length);
            m_test.Eval(tbl.Remove(keys[index]));
            for (int i = 0; i < repeat; i++)
            {
                tbl.Add(keys[index], values[index]);
                m_test.Eval(tbl.Remove(keys[index]));
            }
            m_test.Eval(tbl.Count == keys.Length - 1);
        }


        public void RemoveValidationsRefType(K[] keys, V[] values)
        {
            SortedList<K, V> tbl = new SortedList<K, V>(new ValueKeyComparer<K>());
            for (int i = 0; i < keys.Length; i++)
            {
                tbl.Add(keys[i], values[i]);
            }

            //
            //try null key
            //
            try
            {
                tbl.Remove((K)(object)null);
                m_test.Eval(false, "Excepted ArgumentException but did not get an Exception when trying to remove null.");
            }
            catch (ArgumentException)
            {
                m_test.Eval(true);
            }
            catch (Exception E)
            {
                m_test.Eval(false, "Excepted ArgumentException but got unknown Exception when trying to remove null: " + E);
            }
        }

        public void NonGenericIDictionaryBasicRemove(K[] keys, V[] values)
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
                _idic.Remove(keys[i]);
                m_test.Eval(!_idic.Contains(keys[i]), "Expected " + keys[i] + " to not still exist, but Contains returned true.");
            }
            m_test.Eval(tbl.Count == 0);
        }

        public void NonGenericIDictionaryRemoveNegative(K[] keys, V[] values, K[] missingkeys)
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
                _idic.Remove(missingkeys[i]);
                m_test.Eval(!_idic.Contains(missingkeys[i]), "Expected " + missingkeys[i] + " to not still exist, but Contains returned true.");
            }
            m_test.Eval(tbl.Count == keys.Length);
        }

        public void NonGenericIDictionaryRemoveSameKey(K[] keys, V[] values, int index, int repeat)
        {
            SortedList<K, V> tbl = new SortedList<K, V>(new ValueKeyComparer<K>());
            IDictionary _idic = tbl;
            for (int i = 0; i < keys.Length; i++)
            {
                tbl.Add(keys[i], values[i]);
            }
            m_test.Eval(tbl.Count == keys.Length);
            _idic.Remove(keys[index]);
            m_test.Eval(!_idic.Contains(keys[index]), "Expected " + keys[index] + " to not still exist, but Contains returned true.");
            for (int i = 0; i < repeat; i++)
            {
                _idic.Remove(keys[index]);
                m_test.Eval(!_idic.Contains(keys[index]), "Expected " + keys[index] + " to not still exist, but Contains returned true.");
            }
            m_test.Eval(tbl.Count == keys.Length - 1);
        }

        public void NonGenericIDictionaryAddRemoveSameKey(K[] keys, V[] values, int index, int repeat)
        {
            SortedList<K, V> tbl = new SortedList<K, V>(new ValueKeyComparer<K>());
            IDictionary _idic = tbl;
            for (int i = 0; i < keys.Length; i++)
            {
                tbl.Add(keys[i], values[i]);
            }
            m_test.Eval(tbl.Count == keys.Length);
            _idic.Remove(keys[index]);
            m_test.Eval(!_idic.Contains(keys[index]), "Expected " + keys[index] + " to not still exist, but Contains returned true.");
            for (int i = 0; i < repeat; i++)
            {
                tbl.Add(keys[index], values[index]);
                _idic.Remove(keys[index]);
                m_test.Eval(!_idic.Contains(keys[index]), "Expected " + keys[index] + " to not still exist, but Contains returned true.");
            }
            m_test.Eval(tbl.Count == keys.Length - 1);
        }


        public void NonGenericIDictionaryRemoveValidations(K[] keys, V[] values)
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
                _idic.Remove(null);
                m_test.Eval(false, "Excepted ArgumentException but did not get an Exception when trying to remove null.");
            }
            catch (ArgumentNullException)
            {
                m_test.Eval(true);
            }
            catch (Exception E)
            {
                m_test.Eval(false, "Excepted ArgumentException but got unknown Exception when trying to remove null: " + E);
            }

            try
            {
                _idic.Remove(new Random(-55));
            }
            catch (Exception E)
            {
                m_test.Eval(false, "Excepted ArgumentException but got unknown Exception when trying to remove null: " + E);
            }
        }
    }

    public class Remove
    {
        [Fact]
        public static void RemoveMain()
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
            IntDriver.BasicRemove(intArr1, stringArr1);

            IntDriver.RemoveNegative(intArr1, stringArr1, intArr2);
            IntDriver.RemoveNegative(new RefX1<int>[] { }, new ValX1<string>[] { }, intArr2);

            IntDriver.RemoveSameKey(intArr1, stringArr1, 0, 2);
            IntDriver.RemoveSameKey(intArr1, stringArr1, 99, 3);
            IntDriver.RemoveSameKey(intArr1, stringArr1, 50, 4);
            IntDriver.RemoveSameKey(intArr1, stringArr1, 1, 5);
            IntDriver.RemoveSameKey(intArr1, stringArr1, 98, 6);

            IntDriver.AddRemoveSameKey(intArr1, stringArr1, 0, 2);
            IntDriver.AddRemoveSameKey(intArr1, stringArr1, 99, 3);
            IntDriver.AddRemoveSameKey(intArr1, stringArr1, 50, 4);
            IntDriver.AddRemoveSameKey(intArr1, stringArr1, 1, 5);
            IntDriver.AddRemoveSameKey(intArr1, stringArr1, 98, 6);

            IntDriver.RemoveValidationsRefType(intArr1, stringArr1);
            IntDriver.RemoveValidationsRefType(new RefX1<int>[] { }, new ValX1<string>[] { });

            IntDriver.NonGenericIDictionaryBasicRemove(intArr1, stringArr1);

            IntDriver.NonGenericIDictionaryRemoveNegative(intArr1, stringArr1, intArr2);
            IntDriver.NonGenericIDictionaryRemoveNegative(new RefX1<int>[] { }, new ValX1<string>[] { }, intArr2);

            IntDriver.NonGenericIDictionaryRemoveSameKey(intArr1, stringArr1, 0, 2);
            IntDriver.NonGenericIDictionaryRemoveSameKey(intArr1, stringArr1, 99, 3);
            IntDriver.NonGenericIDictionaryRemoveSameKey(intArr1, stringArr1, 50, 4);
            IntDriver.NonGenericIDictionaryRemoveSameKey(intArr1, stringArr1, 1, 5);
            IntDriver.NonGenericIDictionaryRemoveSameKey(intArr1, stringArr1, 98, 6);

            IntDriver.NonGenericIDictionaryAddRemoveSameKey(intArr1, stringArr1, 0, 2);
            IntDriver.NonGenericIDictionaryAddRemoveSameKey(intArr1, stringArr1, 99, 3);
            IntDriver.NonGenericIDictionaryAddRemoveSameKey(intArr1, stringArr1, 50, 4);
            IntDriver.NonGenericIDictionaryAddRemoveSameKey(intArr1, stringArr1, 1, 5);
            IntDriver.NonGenericIDictionaryAddRemoveSameKey(intArr1, stringArr1, 98, 6);

            IntDriver.NonGenericIDictionaryRemoveValidations(intArr1, stringArr1);
            IntDriver.NonGenericIDictionaryRemoveValidations(new RefX1<int>[] { }, new ValX1<string>[] { });

            //Val<Ref>,Ref<Val>
            StringDriver.BasicRemove(stringArr1, intArr1);

            StringDriver.RemoveNegative(stringArr1, intArr1, stringArr2);
            StringDriver.RemoveNegative(new ValX1<string>[] { }, new RefX1<int>[] { }, stringArr2);

            StringDriver.RemoveSameKey(stringArr1, intArr1, 0, 2);
            StringDriver.RemoveSameKey(stringArr1, intArr1, 99, 3);
            StringDriver.RemoveSameKey(stringArr1, intArr1, 50, 4);
            StringDriver.RemoveSameKey(stringArr1, intArr1, 1, 5);
            StringDriver.RemoveSameKey(stringArr1, intArr1, 98, 6);

            StringDriver.AddRemoveSameKey(stringArr1, intArr1, 0, 2);
            StringDriver.AddRemoveSameKey(stringArr1, intArr1, 99, 3);
            StringDriver.AddRemoveSameKey(stringArr1, intArr1, 50, 4);
            StringDriver.AddRemoveSameKey(stringArr1, intArr1, 1, 5);
            StringDriver.AddRemoveSameKey(stringArr1, intArr1, 98, 6);

            StringDriver.NonGenericIDictionaryBasicRemove(stringArr1, intArr1);

            StringDriver.NonGenericIDictionaryRemoveNegative(stringArr1, intArr1, stringArr2);
            StringDriver.NonGenericIDictionaryRemoveNegative(new ValX1<string>[] { }, new RefX1<int>[] { }, stringArr2);

            StringDriver.NonGenericIDictionaryRemoveSameKey(stringArr1, intArr1, 0, 2);
            StringDriver.NonGenericIDictionaryRemoveSameKey(stringArr1, intArr1, 99, 3);
            StringDriver.NonGenericIDictionaryRemoveSameKey(stringArr1, intArr1, 50, 4);
            StringDriver.NonGenericIDictionaryRemoveSameKey(stringArr1, intArr1, 1, 5);
            StringDriver.NonGenericIDictionaryRemoveSameKey(stringArr1, intArr1, 98, 6);

            StringDriver.NonGenericIDictionaryAddRemoveSameKey(stringArr1, intArr1, 0, 2);
            StringDriver.NonGenericIDictionaryAddRemoveSameKey(stringArr1, intArr1, 99, 3);
            StringDriver.NonGenericIDictionaryAddRemoveSameKey(stringArr1, intArr1, 50, 4);
            StringDriver.NonGenericIDictionaryAddRemoveSameKey(stringArr1, intArr1, 1, 5);
            StringDriver.NonGenericIDictionaryAddRemoveSameKey(stringArr1, intArr1, 98, 6);

            StringDriver.NonGenericIDictionaryRemoveValidations(stringArr1, intArr1);
            StringDriver.NonGenericIDictionaryRemoveValidations(new ValX1<string>[] { }, new RefX1<int>[] { });

            Assert.True(test.result);
        }
    }
}