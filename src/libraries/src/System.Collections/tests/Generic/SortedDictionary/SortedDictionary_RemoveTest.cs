// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;
using SortedDictionaryTests.SortedDictionary_SortedDictionary_RemoveTest;
using SortedDictionary_SortedDictionaryUtils;

namespace SortedDictionaryTests
{
    public class SortedDictionary_RemoveTests
    {
        [Fact]
        public static void SortedDictionary_RemoveIntTest()
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

            ValX1<string>[] stringArr1 = new ValX1<string>[100];
            for (int i = 0; i < 100; i++)
            {
                stringArr1[i] = new ValX1<string>("SomeTestString" + i.ToString());
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
        }

        [Fact]
        public static void SortedDictionary_RemoveStringTest()
        {
            Driver<ValX1<string>, RefX1<int>> StringDriver = new Driver<ValX1<string>, RefX1<int>>();

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
        }

        [Fact]
        public static void SortedDictionary_RemoveTest_Negative()
        {
            Driver<ValX1<string>, RefX1<int>> StringDriver = new Driver<ValX1<string>, RefX1<int>>();
            Driver<RefX1<int>, ValX1<string>> IntDriver = new Driver<RefX1<int>, ValX1<string>>();

            RefX1<int>[] intArr1 = new RefX1<int>[100];
            for (int i = 0; i < 100; i++)
            {
                intArr1[i] = new RefX1<int>(i);
            }

            ValX1<string>[] stringArr1 = new ValX1<string>[100];
            for (int i = 0; i < 100; i++)
            {
                stringArr1[i] = new ValX1<string>("SomeTestString" + i.ToString());
            }

            IntDriver.NonGenericIDictionaryRemoveValidations(intArr1, stringArr1);
            IntDriver.NonGenericIDictionaryRemoveValidations(new RefX1<int>[] { }, new ValX1<string>[] { });

            StringDriver.NonGenericIDictionaryRemoveValidations(stringArr1, intArr1);
            StringDriver.NonGenericIDictionaryRemoveValidations(new ValX1<string>[] { }, new RefX1<int>[] { });
        }
    }
    namespace SortedDictionary_SortedDictionary_RemoveTest
    {
        /// Helper class 
        public class Driver<K, V> where K : IComparableValue
        {
            public void BasicRemove(K[] keys, V[] values)
            {
                SortedDictionary<K, V> tbl = new SortedDictionary<K, V>(new ValueKeyComparer<K>());
                for (int i = 0; i < keys.Length; i++)
                {
                    tbl.Add(keys[i], values[i]);
                }

                Assert.Equal(tbl.Count, keys.Length); //"Err_1! Expected count to be equal to keys.Length"

                for (int i = 0; i < keys.Length; i++)
                {
                    Assert.True(tbl.Remove(keys[i])); //"Err_2! Expected Remove() to return true"
                }
                Assert.Equal(tbl.Count, 0); //"Err_3! Count was expected to be zero"
            }

            public void RemoveNegative(K[] keys, V[] values, K[] missingkeys)
            {
                SortedDictionary<K, V> tbl = new SortedDictionary<K, V>(new ValueKeyComparer<K>());
                for (int i = 0; i < keys.Length; i++)
                {
                    tbl.Add(keys[i], values[i]);
                }
                Assert.Equal(tbl.Count, keys.Length); //"Err_4! Expected count to be equal to keys.Length"

                for (int i = 0; i < missingkeys.Length; i++)
                {
                    Assert.False(tbl.Remove(missingkeys[i])); //"Err_5! Expected Remove() to return false"
                }
                Assert.Equal(tbl.Count, keys.Length); //"Err_6! Expected count to be equal to keys.Length"
            }

            public void RemoveSameKey(K[] keys, V[] values, int index, int repeat)
            {
                SortedDictionary<K, V> tbl = new SortedDictionary<K, V>(new ValueKeyComparer<K>());
                for (int i = 0; i < keys.Length; i++)
                {
                    tbl.Add(keys[i], values[i]);
                }

                Assert.Equal(tbl.Count, keys.Length); //"Err_7! Expected count to be equal to keys.Length"
                Assert.True(tbl.Remove(keys[index])); //"Err_8! Expected Remove() to return true"
                for (int i = 0; i < repeat; i++)
                {
                    Assert.False(tbl.Remove(keys[index])); //"Err_9! Expected Remove() to return false"
                }

                Assert.Equal(tbl.Count, keys.Length - 1); //"Err_10! Expected count to be equal to keys.Length-1"
            }

            public void AddRemoveSameKey(K[] keys, V[] values, int index, int repeat)
            {
                SortedDictionary<K, V> tbl = new SortedDictionary<K, V>(new ValueKeyComparer<K>());
                for (int i = 0; i < keys.Length; i++)
                {
                    tbl.Add(keys[i], values[i]);
                }
                Assert.Equal(tbl.Count, keys.Length); //"Err_11! Expected count to be equal to keys.Length"
                Assert.True(tbl.Remove(keys[index])); //"Err_12! Expected Remove() to return true"

                for (int i = 0; i < repeat; i++)
                {
                    tbl.Add(keys[index], values[index]);
                    Assert.True(tbl.Remove(keys[index])); //"Err_13! Expected Remove() to return true"
                }
                Assert.Equal(tbl.Count, keys.Length - 1); //"Err_14! Expected count to be equal to keys.Length-1"
            }

            public void NonGenericIDictionaryBasicRemove(K[] keys, V[] values)
            {
                SortedDictionary<K, V> tbl = new SortedDictionary<K, V>(new ValueKeyComparer<K>());
                IDictionary _idic = tbl;
                for (int i = 0; i < keys.Length; i++)
                {
                    tbl.Add(keys[i], values[i]);
                }
                Assert.Equal(tbl.Count, keys.Length); //"Err_16! Expected count to be equal to keys.Length"

                for (int i = 0; i < keys.Length; i++)
                {
                    _idic.Remove(keys[i]);
                    Assert.False(_idic.Contains(keys[i])); //"Err_17! Expected " + keys[i] + " to not still exist, but Contains returned true."
                }
                Assert.Equal(tbl.Count, 0); //"Err_18! Count was expected to be zero"
            }

            public void NonGenericIDictionaryRemoveNegative(K[] keys, V[] values, K[] missingkeys)
            {
                SortedDictionary<K, V> tbl = new SortedDictionary<K, V>(new ValueKeyComparer<K>());
                IDictionary _idic = tbl;
                for (int i = 0; i < keys.Length; i++)
                {
                    tbl.Add(keys[i], values[i]);
                }
                Assert.Equal(tbl.Count, keys.Length); //"Err_19! Expected count to be equal to keys.Length"

                for (int i = 0; i < missingkeys.Length; i++)
                {
                    _idic.Remove(missingkeys[i]);
                    Assert.False(_idic.Contains(missingkeys[i])); //"Err_20! Expected " + missingkeys[i] + " to not still exist, but Contains returned true."
                }
                Assert.Equal(tbl.Count, keys.Length); //"Err_21! Expected count to be equal to keys.Length"
            }

            public void NonGenericIDictionaryRemoveSameKey(K[] keys, V[] values, int index, int repeat)
            {
                SortedDictionary<K, V> tbl = new SortedDictionary<K, V>(new ValueKeyComparer<K>());
                IDictionary _idic = tbl;
                for (int i = 0; i < keys.Length; i++)
                {
                    tbl.Add(keys[i], values[i]);
                }
                Assert.Equal(tbl.Count, keys.Length); //"Err_22! Expected count to be equal to keys.Length"
                _idic.Remove(keys[index]);

                Assert.False(_idic.Contains(keys[index])); //"Err_23! Expected " + keys[index] + " to not still exist, but Contains returned true."

                for (int i = 0; i < repeat; i++)
                {
                    _idic.Remove(keys[index]);
                    Assert.False(_idic.Contains(keys[index])); //"Err_24! Expected " + keys[index] + " to not still exist, but Contains returned true."
                }
                Assert.Equal(tbl.Count, keys.Length - 1); //"Err_25! Expected count to be equal to keys.Length-1"
            }

            public void NonGenericIDictionaryAddRemoveSameKey(K[] keys, V[] values, int index, int repeat)
            {
                SortedDictionary<K, V> tbl = new SortedDictionary<K, V>(new ValueKeyComparer<K>());
                IDictionary _idic = tbl;
                for (int i = 0; i < keys.Length; i++)
                {
                    tbl.Add(keys[i], values[i]);
                }

                Assert.Equal(tbl.Count, keys.Length); //"Err_26! Expected count to be equal to keys.Length"
                _idic.Remove(keys[index]);

                Assert.False(_idic.Contains(keys[index])); //"Err_27! Expected " + keys[index] + " to not still exist, but Contains returned true."

                for (int i = 0; i < repeat; i++)
                {
                    tbl.Add(keys[index], values[index]);
                    _idic.Remove(keys[index]);

                    Assert.False(_idic.Contains(keys[index])); //"Err_28! Expected " + keys[index] + " to not still exist, but Contains returned true."
                }
                Assert.Equal(tbl.Count, keys.Length - 1); //"Err_30! Expected count to be equal to keys.Length-1"
            }


            public void NonGenericIDictionaryRemoveValidations(K[] keys, V[] values)
            {
                SortedDictionary<K, V> tbl = new SortedDictionary<K, V>(new ValueKeyComparer<K>());
                IDictionary _idic = tbl;
                for (int i = 0; i < keys.Length; i++)
                {
                    tbl.Add(keys[i], values[i]);
                }

                //try null key
                Assert.Throws<ArgumentNullException>(() => _idic.Remove(null)); //"Err_31! wrong exception thrown when trying to remove null."
            }
        }
    }
}
