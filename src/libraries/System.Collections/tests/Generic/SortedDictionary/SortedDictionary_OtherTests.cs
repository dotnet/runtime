// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;
using SortedDictionaryTests.SortedDictionary_SortedDictionary_OtherTests;
using SortedDictionary_SortedDictionaryUtils;

namespace SortedDictionaryTests
{
    namespace SortedDictionary_SortedDictionary_OtherTests
    {
        public class SortedDictionary_CtorTests
        {
            [Fact]
            public static void SortedDictionary_ClearTest()
            {
                Driver<RefX1<int>, ValX1<string>> IntDriver = new Driver<RefX1<int>, ValX1<string>>();
                RefX1<int>[] intArr = new RefX1<int>[100];
                for (int i = 0; i < 100; i++)
                {
                    intArr[i] = new RefX1<int>(i);
                }

                Driver<ValX1<string>, RefX1<int>> StringDriver = new Driver<ValX1<string>, RefX1<int>>();
                ValX1<string>[] stringArr = new ValX1<string>[100];
                for (int i = 0; i < 100; i++)
                {
                    stringArr[i] = new ValX1<string>("SomeTestString" + i.ToString());
                }

                IntDriver.Clear(intArr, stringArr, 1);
                IntDriver.Clear(intArr, stringArr, 10);
                IntDriver.Clear(new RefX1<int>[] { }, new ValX1<string>[] { }, 1);
                IntDriver.Clear(new RefX1<int>[] { }, new ValX1<string>[] { }, 10);

                StringDriver.Clear(stringArr, intArr, 1);
                StringDriver.Clear(stringArr, intArr, 10);
                StringDriver.Clear(new ValX1<string>[] { }, new RefX1<int>[] { }, 1);
                StringDriver.Clear(new ValX1<string>[] { }, new RefX1<int>[] { }, 10);

                IntDriver.NonGenericIDictionaryClear(intArr, stringArr, 1);
                IntDriver.NonGenericIDictionaryClear(intArr, stringArr, 10);
                IntDriver.NonGenericIDictionaryClear(new RefX1<int>[] { }, new ValX1<string>[] { }, 1);
                IntDriver.NonGenericIDictionaryClear(new RefX1<int>[] { }, new ValX1<string>[] { }, 10);

                StringDriver.NonGenericIDictionaryClear(stringArr, intArr, 1);
                StringDriver.NonGenericIDictionaryClear(stringArr, intArr, 10);
                StringDriver.NonGenericIDictionaryClear(new ValX1<string>[] { }, new RefX1<int>[] { }, 1);
                StringDriver.NonGenericIDictionaryClear(new ValX1<string>[] { }, new RefX1<int>[] { }, 10);
            }

            [Fact]
            public static void SortedDictionary_ConatinsValueTest()
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
            }

            [Fact]
            public static void SortedDictionary_GetEnumeratorTest()
            {
                Driver<RefX1<int>, ValX1<string>> IntDriver = new Driver<RefX1<int>, ValX1<string>>();
                RefX1<int>[] intArr = new RefX1<int>[100];
                for (int i = 0; i < 100; i++)
                {
                    intArr[i] = new RefX1<int>(i);
                }

                Driver<ValX1<string>, RefX1<int>> StringDriver = new Driver<ValX1<string>, RefX1<int>>();
                ValX1<string>[] stringArr = new ValX1<string>[100];
                for (int i = 0; i < 100; i++)
                {
                    stringArr[i] = new ValX1<string>("SomeTestString" + i.ToString());
                }

                //Ref<Val>,Val<Ref>
                IntDriver.GetEnumeratorBasic(intArr, stringArr);
                IntDriver.GetEnumeratorBasic(new RefX1<int>[0], new ValX1<string>[0]);

                //Val<Ref>,Ref<Val>
                StringDriver.GetEnumeratorBasic(stringArr, intArr);
                StringDriver.GetEnumeratorBasic(new ValX1<string>[0], new RefX1<int>[0]);
            }

            [Fact]
            public static void SortedDictionary_GetEnumeratorTest_Negative()
            {
                Driver<RefX1<int>, ValX1<string>> IntDriver = new Driver<RefX1<int>, ValX1<string>>();
                RefX1<int>[] intArr = new RefX1<int>[100];
                for (int i = 0; i < 100; i++)
                {
                    intArr[i] = new RefX1<int>(i);
                }

                Driver<ValX1<string>, RefX1<int>> StringDriver = new Driver<ValX1<string>, RefX1<int>>();
                ValX1<string>[] stringArr = new ValX1<string>[100];
                for (int i = 0; i < 100; i++)
                {
                    stringArr[i] = new ValX1<string>("SomeTestString" + i.ToString());
                }


                //Ref<Val>,Val<Ref>
                IntDriver.GetEnumeratorValidations(intArr, stringArr, new RefX1<int>(1000), new ValX1<string>("1000"));
                IntDriver.GetEnumeratorValidations(new RefX1<int>[0], new ValX1<string>[0], new RefX1<int>(1000), new ValX1<string>("1000"));


                //Val<Ref>,Ref<Val>
                StringDriver.GetEnumeratorValidations(new ValX1<string>[0], new RefX1<int>[0], new ValX1<string>("1000"), new RefX1<int>(1000));
            }
        }
        // helper class
        public class Driver<K, V> where K : IComparableValue
        {
            // Clear helpers
            public void Clear(K[] keys, V[] values, int repeat)
            {
                SortedDictionary<K, V> tbl = new SortedDictionary<K, V>(new ValueKeyComparer<K>());

                tbl.Clear();
                Assert.Equal(tbl.Count, 0); //"Err_1! Count was expected to be zero"

                for (int i = 0; i < keys.Length; i++)
                {
                    tbl.Add(keys[i], values[i]);
                }
                for (int i = 0; i < repeat; i++)
                {
                    tbl.Clear();
                    Assert.Equal(tbl.Count, 0); //"Err_2! Count was expected to be zero"
                }
            }

            public void NonGenericIDictionaryClear(K[] keys, V[] values, int repeat)
            {
                SortedDictionary<K, V> tbl = new SortedDictionary<K, V>(new ValueKeyComparer<K>());
                IDictionary _idic = tbl;

                _idic.Clear();

                Assert.Equal(tbl.Count, 0); //"Err_3! Count was expected to be zero"

                for (int i = 0; i < keys.Length; i++)
                {
                    tbl.Add(keys[i], values[i]);
                }
                for (int i = 0; i < repeat; i++)
                {
                    _idic.Clear();

                    Assert.Equal(tbl.Count, 0); //"Err_4! Count was expected to be zero"
                }
            }

            // ContainsValue helpers
            public void BasicContainsValue(K[] keys, V[] values)
            {
                SortedDictionary<K, V> tbl = new SortedDictionary<K, V>(new ValueKeyComparer<K>());
                for (int i = 0; i < keys.Length; i++)
                {
                    tbl.Add(keys[i], values[i]);
                }

                Assert.Equal(tbl.Count, keys.Length); //"Err_5! Expected count to be equal to keys.Length"

                for (int i = 0; i < keys.Length; i++)
                {
                    Assert.True(tbl.ContainsValue(values[i])); //"Err_6! Expected to get true"
                }
            }

            public void ContainsValueNegative(K[] keys, V[] values, V[] missingvalues)
            {
                SortedDictionary<K, V> tbl = new SortedDictionary<K, V>(new ValueKeyComparer<K>());
                for (int i = 0; i < keys.Length; i++)
                {
                    tbl.Add(keys[i], values[i]);
                }
                Assert.Equal(tbl.Count, keys.Length); //"Err_7! Expected count to be equal to keys.Length"

                for (int i = 0; i < missingvalues.Length; i++)
                {
                    Assert.False(tbl.ContainsValue(missingvalues[i])); //"Err_8! Expected to get false"
                }
            }

            public void AddRemoveKeyContainsValue(K[] keys, V[] values, int index)
            {
                SortedDictionary<K, V> tbl = new SortedDictionary<K, V>(new ValueKeyComparer<K>());
                for (int i = 0; i < keys.Length; i++)
                {
                    tbl.Add(keys[i], values[i]);
                }
                tbl.Remove(keys[index]);
                Assert.False(tbl.ContainsValue(values[index])); //"Err_9! Expected to get false"
            }

            public void AddRemoveAddKeyContainsValue(K[] keys, V[] values, int index, int repeat)
            {
                SortedDictionary<K, V> tbl = new SortedDictionary<K, V>(new ValueKeyComparer<K>());
                for (int i = 0; i < keys.Length; i++)
                {
                    tbl.Add(keys[i], values[i]);
                }
                for (int i = 0; i < repeat; i++)
                {
                    tbl.Remove(keys[index]);
                    tbl.Add(keys[index], values[index]);
                    Assert.True(tbl.ContainsValue(values[index])); //"Err_10! Expected to get true"
                }
            }

            // GetEnumerators helpers
            public void GetEnumeratorBasic(K[] keys, V[] values)
            {
                SortedDictionary<K, V> tbl = new SortedDictionary<K, V>(new TestSortedDictionary<K, V>(keys, values), new ValueKeyComparer<K>());
                IEnumerator<KeyValuePair<K, V>> Enum = ((IDictionary<K, V>)tbl).GetEnumerator();

                //There are no guarantees on the order of elements in the HT
                List<K> kls = new List<K>();

                for (int i = 0; i < keys.Length; i++)
                    kls.Add(keys[i]);

                List<V> vls = new List<V>();

                for (int i = 0; i < keys.Length; i++)
                    vls.Add(values[i]);

                for (int i = 0; i < keys.Length; i++)
                {
                    Assert.True(Enum.MoveNext()); //"Err_11!  Enumerator unexpectedly did not move"
                    KeyValuePair<K, V> entry = Enum.Current;

                    Assert.True(kls.Contains(entry.Key)); //"Err_12! Current enum key " + entry.Key + " not found in dictionary"
                    kls.Remove(entry.Key);
                    Assert.True(vls.Contains(entry.Value)); //"Err_13!  Current enum value " + entry.Value + " not found in dictionary"
                    vls.Remove(entry.Value);
                }

                for (int i = 0; i < keys.Length; i++)
                    kls.Add(keys[i]);

                for (int i = 0; i < keys.Length; i++)
                    vls.Add(values[i]);

                foreach (KeyValuePair<K, V> entry in tbl)
                {
                    Assert.True(kls.Contains(entry.Key)); //"Err_14! Current enum key " + entry.Key + " not found in dictionary"
                    kls.Remove(entry.Key);
                    Assert.True(vls.Contains(entry.Value)); //"Err_15! Current enum value " + entry.Value + " not found in dictionary"
                    vls.Remove(entry.Value);
                }

                kls = new List<K>();
                for (int i = 0; i < keys.Length; i++)
                    kls.Add(keys[i]);

                vls = new List<V>();
                for (int i = 0; i < keys.Length; i++)
                    vls.Add(values[i]);

                Assert.True(Enum.MoveNext() == false); //"Err_16! Enumerator unexpectedly moved"
            }


            public void GetEnumeratorValidations(K[] keys, V[] values, K nkey, V nvalue)
            {
                SortedDictionary<K, V> tbl = new SortedDictionary<K, V>(new TestSortedDictionary<K, V>(keys, values), new ValueKeyComparer<K>());

                IEnumerator<KeyValuePair<K, V>> Enum = tbl.GetEnumerator();

                KeyValuePair<K, V> entry;
                K currKey;
                V currValue;

                try
                {
                    //The behavior here is undefined
                    entry = Enum.Current;
                }
                catch (Exception) { }

                Assert.Throws<InvalidOperationException>((() => entry = (KeyValuePair<K, V>)((IEnumerator)Enum).Current)); //"5- Enum.Current did not throw invalid operation exception when positioned before collection"

                for (int i = 0; i < keys.Length; i++)
                {
                    Assert.True(Enum.MoveNext()); //"Err_17! Expected MoveNext() to return true"
                }

                Assert.False(Enum.MoveNext()); //"Err_18! Enumerator unexpectedly moved"

                try
                {
                    //The behavior here is undefined
                    entry = Enum.Current;
                }
                catch (Exception) { }

                try
                {
                    //The behavior here is undefined
                    currKey = Enum.Current.Key;
                }
                catch (Exception) { }

                try
                {
                    //The behavior here is undefined
                    currValue = Enum.Current.Value;
                }
                catch (Exception) { }


                Assert.Throws<InvalidOperationException>((() => entry = (KeyValuePair<K, V>)((IEnumerator)Enum).Current)); //"Err_19! Enum.Current did not throw invalid operation exception when positioned after collection"


                Enum = ((IDictionary<K, V>)tbl).GetEnumerator();
                Enum.MoveNext();
                tbl.Add(nkey, nvalue);

                Assert.Throws<InvalidOperationException>((() => Enum.MoveNext())); //"Err_20! Enum.MoveNext did not throw expected invalid operation exception after collection was modified"

                tbl.Remove(nkey);

                // Test Reset
                Enum = tbl.GetEnumerator();
                Enum.Reset(); Enum.Reset(); Enum.Reset();

                for (int i = 0; i < keys.Length; i++)
                {
                    Enum.MoveNext();
                    entry = Enum.Current;
                }


                Enum = tbl.GetEnumerator();

                Enum.Reset();

                for (int i = 0; i < keys.Length; i++)
                {
                    Assert.True(Enum.MoveNext()); //"Err_21! Enumerator unexpectedly did not move"

                    entry = Enum.Current;

                    Assert.True(entry.Key.Equals(keys[i]) && (((null == (object)entry.Value) && (null == (object)values[i])) || entry.Value.Equals(values[i])),
                                  "Err_22! Actual: " + entry.Key.ToString() + ", " + entry.Value.ToString() + " Expected: " + keys[i].ToString() + ", " + values[i].ToString());

                    entry = (KeyValuePair<K, V>)((IEnumerator)Enum).Current;

                    Assert.True(entry.Key.Equals(keys[i]) && (((null == (object)entry.Value) && (null == (object)values[i])) || entry.Value.Equals(values[i])),
                                  "Err_23! Enum.Current expected to be " + keys[i] + ", " + values[i] + ". Actually " + entry.Key + ", " + entry.Value);

                    entry = (KeyValuePair<K, V>)((IEnumerator)Enum).Current;

                    Assert.True(entry.Key.Equals(keys[i]) && (((null == (object)entry.Value) && (null == (object)values[i])) || entry.Value.Equals(values[i])),
                        "Err_24! Enum.Current expected to be " + keys[i] + ", " + values[i] + ". Actually " + entry.Key + ", " + entry.Value);
                }

                // Iterate through half the collection, reset and then enumerate through it all
                Enum.Reset();
                for (int i = 0; i < keys.Length / 2; i++)
                {
                    Assert.True(Enum.MoveNext()); //"Err_25! Enumerator unexpectedly did not move"
                    entry = Enum.Current;

                    Assert.True(entry.Key.Equals(keys[i]) && (((null == (object)entry.Value) && (null == (object)values[i])) || entry.Value.Equals(values[i])),
                                   "Err_26! Enum.Current expected to be " + keys[i] + ", " + values[i] + ". Actually " + entry.Key + ", " + entry.Value);

                    entry = (KeyValuePair<K, V>)((IEnumerator)Enum).Current;

                    Assert.True(entry.Key.Equals(keys[i]) && (((null == (object)entry.Value) && (null == (object)values[i])) || entry.Value.Equals(values[i])),
                                    "Err_27! Enum.Current expected to be " + keys[i] + ", " + values[i] + ". Actually " + entry.Key + ", " + entry.Value);

                    entry = (KeyValuePair<K, V>)((IEnumerator)Enum).Current;

                    Assert.True(entry.Key.Equals(keys[i]) && (((null == (object)entry.Value) && (null == (object)values[i])) || entry.Value.Equals(values[i])),
                                    "Err_28! Enum.Current expected to be " + keys[i] + ", " + values[i] + ". Actually " + entry.Key + ", " + entry.Value);
                }

                Enum.Reset(); Enum.Reset();

                for (int i = 0; i < keys.Length; i++)
                {
                    Assert.True(Enum.MoveNext()); //"Err_29! Enumerator unexpectedly did not move"
                    entry = Enum.Current;

                    Assert.True(entry.Key.Equals(keys[i]) && (((null == (object)entry.Value) && (null == (object)values[i])) || entry.Value.Equals(values[i])),
                                     "Err_30! Enum.Current expected to be " + keys[i] + ", " + values[i] + ". Actually " + entry.Key + ", " + entry.Value);

                    entry = (KeyValuePair<K, V>)((IEnumerator)Enum).Current;

                    Assert.True(entry.Key.Equals(keys[i]) && (((null == (object)entry.Value) && (null == (object)values[i])) || entry.Value.Equals(values[i])),
                                     "Err_31! Enum.Current expected to be " + keys[i] + ", " + values[i] + ". Actually " + entry.Key + ", " + entry.Value);

                    entry = (KeyValuePair<K, V>)((IEnumerator)Enum).Current;

                    Assert.True(entry.Key.Equals(keys[i]) && (((null == (object)entry.Value) && (null == (object)values[i])) || entry.Value.Equals(values[i])),
                                    "Err_32! Enum.Current expected to be " + keys[i] + ", " + values[i] + ". Actually " + entry.Key + ", " + entry.Value);
                }

                tbl.Add(nkey, nvalue);
                Enum = tbl.GetEnumerator();
                Enum.Reset();
                Enum.MoveNext();
                KeyValuePair<K, V> item = Enum.Current;
                tbl.Remove(nkey);
                tbl.Add(nkey, nvalue);

                entry = Enum.Current;

                Assert.True(entry.Key.Equals(item.Key) && (((null == (object)entry.Value) && (null == (object)item.Value)) || entry.Value.Equals(item.Value)),
                             "Err_33! Enum.Current expected to be " + item.Key + ", " + item.Value + ". Actually " + entry.Key + ", " + entry.Value);

                entry = (KeyValuePair<K, V>)((IEnumerator)Enum).Current;

                Assert.True(entry.Key.Equals(item.Key) && (((null == (object)entry.Value) && (null == (object)item.Value)) || entry.Value.Equals(item.Value)),
                             "Err_34! Enum.Current expected to be " + item.Key + ", " + item.Value + ". Actually " + entry.Key + ", " + entry.Value);

                entry = (KeyValuePair<K, V>)((IEnumerator)Enum).Current;

                Assert.True(entry.Key.Equals(item.Key) && (((null == (object)entry.Value) && (null == (object)item.Value)) || entry.Value.Equals(item.Value)),
                                "Err_35! Enum.Current expected to be " + item.Key + ", " + item.Value + ". Actually " + entry.Key + ", " + entry.Value);

                Assert.Throws<InvalidOperationException>((() => Enum.Reset())); //"Err_36! Enumerator.Reset did not throw expected invalid operation exception when called after the collection was modified"
            }
        }
    }
}
