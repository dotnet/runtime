// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;
using SortedDictionaryTests.SortedDictionary_SortedDictionary_ValueCollection;
using SortedDictionary_SortedDictionaryUtils;

namespace SortedDictionaryTests
{
    public class SortedDictionary_ValueCollectionTests
    {
        public class IntGenerator
        {
            private int _index;

            public IntGenerator()
            {
                _index = 0;
            }

            public int NextValue()
            {
                return _index++;
            }

            public Object NextValueObject()
            {
                return (Object)NextValue();
            }
        }

        public class StringGenerator
        {
            private int _index;

            public StringGenerator()
            {
                _index = 0;
            }

            public String NextValue()
            {
                return (_index++).ToString();
            }

            public Object NextValueObject()
            {
                return (Object)NextValue();
            }
        }

        [Fact]
        public static void ValueCollectionTest1()
        {
            IntGenerator intGenerator = new IntGenerator();
            StringGenerator stringGenerator = new StringGenerator();

            intGenerator.NextValue();
            stringGenerator.NextValue();

            //Scenario 1: Vanilla - fill in SortedDictionary with 10 keys and check this property
            Driver<int, int> IntDriver = new Driver<int, int>();
            Driver<SimpleRef<String>, SimpleRef<String>> simpleRef = new Driver<SimpleRef<String>, SimpleRef<String>>();
            Driver<SimpleRef<int>, SimpleRef<int>> simpleVal = new Driver<SimpleRef<int>, SimpleRef<int>>();

            int count = 100;
            SimpleRef<int>[] simpleInts = SortedDictionaryUtils.GetSimpleInts(count);
            SimpleRef<String>[] simpleStrings = SortedDictionaryUtils.GetSimpleStrings(count);
            int[] ints = new int[count];

            for (int i = 0; i < count; i++)
                ints[i] = i;

            IntDriver.TestVanilla(ints, ints);
            simpleRef.TestVanilla(simpleStrings, simpleStrings);
            simpleVal.TestVanilla(simpleInts, simpleInts);
            IntDriver.NonGenericIDictionaryTestVanilla(ints, ints);
            simpleRef.NonGenericIDictionaryTestVanilla(simpleStrings, simpleStrings);
            simpleVal.NonGenericIDictionaryTestVanilla(simpleInts, simpleInts);

            //Scenario 2: Check for an empty SortedDictionary
            IntDriver.TestVanilla(new int[0], new int[0]);
            simpleRef.TestVanilla(new SimpleRef<String>[0], new SimpleRef<String>[0]);
            simpleVal.TestVanilla(new SimpleRef<int>[0], new SimpleRef<int>[0]);
            IntDriver.NonGenericIDictionaryTestVanilla(new int[0], new int[0]);
            simpleRef.NonGenericIDictionaryTestVanilla(new SimpleRef<String>[0], new SimpleRef<String>[0]);
            simpleVal.NonGenericIDictionaryTestVanilla(new SimpleRef<int>[0], new SimpleRef<int>[0]);

            //Scenario 3: Check the underlying reference. Change the SortedDictionary afterwards and examine ICollection keys and make sure that the 
            //change is reflected
            int half = count / 2;
            SimpleRef<int>[] simpleInts_1 = new SimpleRef<int>[half];
            SimpleRef<String>[] simpleStrings_1 = new SimpleRef<String>[half];
            SimpleRef<int>[] simpleInts_2 = new SimpleRef<int>[half];
            SimpleRef<String>[] simpleStrings_2 = new SimpleRef<String>[half];

            int[] ints_1 = new int[half];
            int[] ints_2 = new int[half];


            for (int i = 0; i < half; i++)
            {
                simpleInts_1[i] = simpleInts[i];
                simpleStrings_1[i] = simpleStrings[i];
                ints_1[i] = ints[i];

                simpleInts_2[i] = simpleInts[i + half];
                simpleStrings_2[i] = simpleStrings[i + half];
                ints_2[i] = ints[i + half];
            }

            IntDriver.TestModify(ints_1, ints_1, ints_2);
            simpleRef.TestModify(simpleStrings_1, simpleStrings_1, simpleStrings_2);
            simpleVal.TestModify(simpleInts_1, simpleInts_1, simpleInts_2);
            IntDriver.NonGenericIDictionaryTestModify(ints_1, ints_1, ints_2);
            simpleRef.NonGenericIDictionaryTestModify(simpleStrings_1, simpleStrings_1, simpleStrings_2);
            simpleVal.NonGenericIDictionaryTestModify(simpleInts_1, simpleInts_1, simpleInts_2);
        }

        [Fact]
        public static void SortedDictionary_ValueCollectionTest_Negative()
        {
            IntGenerator intGenerator = new IntGenerator();
            StringGenerator stringGenerator = new StringGenerator();

            intGenerator.NextValue();
            stringGenerator.NextValue();

            //Scenario 1: Vanilla - fill in SortedDictionary with 10 keys and check this property
            Driver<int, int> IntDriver = new Driver<int, int>();
            Driver<SimpleRef<String>, SimpleRef<String>> simpleRef = new Driver<SimpleRef<String>, SimpleRef<String>>();
            Driver<SimpleRef<int>, SimpleRef<int>> simpleVal = new Driver<SimpleRef<int>, SimpleRef<int>>();

            int count = 100;
            SimpleRef<int>[] simpleInts = SortedDictionaryUtils.GetSimpleInts(count);
            SimpleRef<String>[] simpleStrings = SortedDictionaryUtils.GetSimpleStrings(count);
            int[] ints = new int[count];

            for (int i = 0; i < count; i++)
                ints[i] = i;

            IntDriver.TestVanilla_Negative(ints, ints);
            simpleRef.TestVanilla_Negative(simpleStrings, simpleStrings);
            simpleVal.TestVanilla_Negative(simpleInts, simpleInts);
            IntDriver.NonGenericIDictionaryTestVanilla_Negative(ints, ints);
            simpleRef.NonGenericIDictionaryTestVanilla_Negative(simpleStrings, simpleStrings);
            simpleVal.NonGenericIDictionaryTestVanilla_Negative(simpleInts, simpleInts);


            //Scenario 2: Check for an empty SortedDictionary
            IntDriver.TestVanilla_Negative(new int[0], new int[0]);
            simpleRef.TestVanilla_Negative(new SimpleRef<String>[0], new SimpleRef<String>[0]);
            simpleVal.TestVanilla_Negative(new SimpleRef<int>[0], new SimpleRef<int>[0]);
            IntDriver.NonGenericIDictionaryTestVanilla_Negative(new int[0], new int[0]);
            simpleRef.NonGenericIDictionaryTestVanilla_Negative(new SimpleRef<String>[0], new SimpleRef<String>[0]);
            simpleVal.NonGenericIDictionaryTestVanilla_Negative(new SimpleRef<int>[0], new SimpleRef<int>[0]);
        }

        [Fact]
        public static void SortedDictionary_ValueCollectionTest2()
        {
            IntGenerator intGenerator = new IntGenerator();
            StringGenerator stringGenerator = new StringGenerator();

            intGenerator.NextValue();
            stringGenerator.NextValue();

            Driver<int, int> intDriver = new Driver<int, int>();
            Driver<SimpleRef<String>, SimpleRef<String>> simpleRef = new Driver<SimpleRef<String>, SimpleRef<String>>();
            Driver<SimpleRef<int>, SimpleRef<int>> simpleVal = new Driver<SimpleRef<int>, SimpleRef<int>>();

            //Scenario 3: Check the underlying reference. Change the SortedDictionary afterwards and examine ICollection keys and make sure that the 
            //change is reflected
            int count = 100;
            SimpleRef<int>[] simpleInts = SortedDictionaryUtils.GetSimpleInts(count);
            SimpleRef<String>[] simpleStrings = SortedDictionaryUtils.GetSimpleStrings(count);
            int[] ints = new int[count];
            int half = count / 2;
            SimpleRef<int>[] simpleInts_1 = new SimpleRef<int>[half];
            SimpleRef<String>[] simpleStrings_1 = new SimpleRef<String>[half];
            SimpleRef<int>[] simpleInts_2 = new SimpleRef<int>[half];
            SimpleRef<String>[] simpleStrings_2 = new SimpleRef<String>[half];

            for (int i = 0; i < count; i++)
                ints[i] = i;

            int[] ints_1 = new int[half];
            int[] ints_2 = new int[half];

            for (int i = 0; i < half; i++)
            {
                simpleInts_1[i] = simpleInts[i];
                simpleStrings_1[i] = simpleStrings[i];
                ints_1[i] = ints[i];

                simpleInts_2[i] = simpleInts[i + half];
                simpleStrings_2[i] = simpleStrings[i + half];
                ints_2[i] = ints[i + half];
            }

            intDriver.TestModify(ints_1, ints_1, ints_2);
            simpleRef.TestModify(simpleStrings_1, simpleStrings_1, simpleStrings_2);
            simpleVal.TestModify(simpleInts_1, simpleInts_1, simpleInts_2);
            intDriver.NonGenericIDictionaryTestModify(ints_1, ints_1, ints_2);
            simpleRef.NonGenericIDictionaryTestModify(simpleStrings_1, simpleStrings_1, simpleStrings_2);

            simpleVal.NonGenericIDictionaryTestModify(simpleInts_1, simpleInts_1, simpleInts_2);
        }
    }
    namespace SortedDictionary_SortedDictionary_ValueCollection
    {
        public class Driver<KeyType, ValueType>
        {
            public void TestVanilla(KeyType[] keys, ValueType[] values)
            {
                SortedDictionary<KeyType, ValueType> _dic = new SortedDictionary<KeyType, ValueType>();

                for (int i = 0; i < keys.Length - 1; i++)
                    _dic.Add(keys[i], values[i]);

                SortedDictionary<KeyType, ValueType>.ValueCollection _col = new SortedDictionary<KeyType, ValueType>.ValueCollection(_dic);

                Assert.Equal(_col.Count, _dic.Count); //"Err_1! Count not equal"

                IEnumerator<ValueType> _enum = _col.GetEnumerator();

                int count = 0;

                while (_enum.MoveNext())
                {
                    Assert.True(_dic.ContainsValue(_enum.Current)); //"Err_2! Expected key to be present"
                    count++;
                }

                Assert.Equal(count, _dic.Count); //"Err_3! Count not equal"

                ValueType[] _values = new ValueType[_dic.Count];

                _col.CopyTo(_values, 0);


                for (int i = 0; i < values.Length - 1; i++)
                    Assert.True(_dic.ContainsValue(_values[i])); //"Err_4! Expected key to be present"

                count = 0;

                foreach (ValueType currValue in _dic.Values)
                {
                    Assert.True(_dic.ContainsValue(currValue)); //"Err_5! Expected key to be present"
                    count++;
                }
                Assert.Equal(count, _dic.Count); //"Err_! Count not equal"

                try
                {
                    //The behavior here is undefined as long as we don't AV we're fine
                    ValueType item = _enum.Current;
                }
                catch (Exception) { }
            }

            // verify we get InvalidOperationException when we call MoveNext() after adding a key
            public void TestVanilla_Negative(KeyType[] keys, ValueType[] values)
            {
                SortedDictionary<KeyType, ValueType> _dic = new SortedDictionary<KeyType, ValueType>();

                for (int i = 0; i < keys.Length - 1; i++)
                    _dic.Add(keys[i], values[i]);

                SortedDictionary<KeyType, ValueType>.ValueCollection _col = new SortedDictionary<KeyType, ValueType>.ValueCollection(_dic);
                IEnumerator<ValueType> _enum = _col.GetEnumerator();

                if (keys.Length > 0)
                {
                    _dic.Add(keys[keys.Length - 1], values[values.Length - 1]);

                    Assert.Throws<InvalidOperationException>((() => _enum.MoveNext())); //"Err_6! Expected InvalidOperationException."
                }
            }

            public void TestModify(KeyType[] keys, ValueType[] values, ValueType[] newValues)
            {
                SortedDictionary<KeyType, ValueType> _dic = new SortedDictionary<KeyType, ValueType>();
                for (int i = 0; i < keys.Length; i++)
                {
                    _dic.Add(keys[i], values[i]);
                }

                SortedDictionary<KeyType, ValueType>.ValueCollection _col = new SortedDictionary<KeyType, ValueType>.ValueCollection(_dic);

                for (int i = 0; i < keys.Length; i++)
                    _dic.Remove(keys[i]);

                Assert.Equal(_col.Count, 0); //"Err_7! Count not equal"
                IEnumerator<ValueType> _enum = _col.GetEnumerator();

                int count = 0;
                while (_enum.MoveNext())
                {
                    Assert.True(_dic.ContainsValue(_enum.Current)); //"Err_8! Expected key to be present"
                    count++;
                }
                Assert.Equal(count, 0); //"Err_9! Count not equal"

                for (int i = 0; i < keys.Length; i++)
                    _dic[keys[i]] = newValues[i];


                Assert.Equal(_col.Count, _dic.Count); //"Err_10! Count not equal"
                _enum = _col.GetEnumerator();
                count = 0;
                while (_enum.MoveNext())
                {
                    Assert.True(_dic.ContainsValue(_enum.Current)); //"Err_11! Expected key to be present"
                    count++;
                }
                Assert.Equal(count, _dic.Count); //"Err_12! Count not equal"

                ValueType[] _values = new ValueType[_dic.Count];
                _col.CopyTo(_values, 0);

                for (int i = 0; i < keys.Length; i++)
                    Assert.True(_dic.ContainsValue(_values[i])); //"Err_13! Expected key to be present"
            }

            public void NonGenericIDictionaryTestVanilla(KeyType[] keys, ValueType[] values)
            {
                SortedDictionary<KeyType, ValueType> _dic = new SortedDictionary<KeyType, ValueType>();
                IDictionary _idic = _dic;

                for (int i = 0; i < keys.Length - 1; i++)
                    _dic.Add(keys[i], values[i]);

                SortedDictionary<KeyType, ValueType>.ValueCollection _col = new SortedDictionary<KeyType, ValueType>.ValueCollection(_dic);

                Assert.Equal(_col.Count, _dic.Count); //"Err_14! Count not equal"
                IEnumerator _enum = _col.GetEnumerator();
                int count = 0;
                while (_enum.MoveNext())
                {
                    Assert.True(_dic.ContainsValue((ValueType)_enum.Current)); //"Err_15! Expected key to be present"
                    count++;
                }
                Assert.Equal(count, _dic.Count); //"Err_16! Count not equal"

                ValueType[] _values = new ValueType[_dic.Count];
                _col.CopyTo(_values, 0);

                for (int i = 0; i < keys.Length - 1; i++)
                    Assert.True(_dic.ContainsValue(_values[i])); //"Err_17! Expected key to be present"

                _enum.Reset();

                count = 0;
                while (_enum.MoveNext())
                {
                    Assert.True(_dic.ContainsValue((ValueType)_enum.Current)); //"Err_18! Expected key to be present"
                    count++;
                }
                Assert.Equal(count, _dic.Count); //"Err_19! Count not equal"

                _values = new ValueType[_dic.Count];
                _col.CopyTo(_values, 0);

                for (int i = 0; i < keys.Length - 1; i++)
                    Assert.True(_dic.ContainsValue(_values[i])); //"Err_20! Expected key to be present"
            }

            public void NonGenericIDictionaryTestVanilla_Negative(KeyType[] keys, ValueType[] values)
            {
                SortedDictionary<KeyType, ValueType> _dic = new SortedDictionary<KeyType, ValueType>();
                IDictionary _idic = _dic;

                for (int i = 0; i < keys.Length - 1; i++)
                    _dic.Add(keys[i], values[i]);
                SortedDictionary<KeyType, ValueType>.ValueCollection _col = new SortedDictionary<KeyType, ValueType>.ValueCollection(_dic);
                IEnumerator _enum = _col.GetEnumerator();

                // get to the end
                while (_enum.MoveNext()) { }

                Assert.Throws<InvalidOperationException>((() => _dic.ContainsValue((ValueType)_enum.Current))); //"Err_21! Expected InvalidOperationException."


                if (keys.Length > 0)
                {
                    _dic.Add(keys[keys.Length - 1], values[values.Length - 1]);

                    Assert.Throws<InvalidOperationException>((() => _enum.MoveNext())); //"Err_22! Expected InvalidOperationException."
                    Assert.Throws<InvalidOperationException>((() => _enum.Reset())); //"Err_23! Expected InvalidOperationException."
                }
            }

            public void NonGenericIDictionaryTestModify(KeyType[] keys, ValueType[] values, ValueType[] newValues)
            {
                SortedDictionary<KeyType, ValueType> _dic = new SortedDictionary<KeyType, ValueType>();
                IDictionary _idic = _dic;
                for (int i = 0; i < keys.Length; i++)
                    _dic.Add(keys[i], values[i]);

                SortedDictionary<KeyType, ValueType>.ValueCollection _col = new SortedDictionary<KeyType, ValueType>.ValueCollection(_dic);

                for (int i = 0; i < keys.Length; i++)
                    _dic.Remove(keys[i]);

                Assert.Equal(_col.Count, 0); //"Err_24! Expected count to be zero"
                IEnumerator _enum = _col.GetEnumerator();
                int count = 0;
                while (_enum.MoveNext())
                {
                    Assert.True(_dic.ContainsValue((ValueType)_enum.Current)); //"Err_! Expected key to be present"
                    count++;
                }
                Assert.Equal(count, 0); //"Err_25! Expected count to be zero"

                for (int i = 0; i < keys.Length; i++)
                    _dic[keys[i]] = newValues[i];


                Assert.Equal(_col.Count, _dic.Count); //"Err_26! Count not equal"
                _enum = _col.GetEnumerator();
                count = 0;
                while (_enum.MoveNext())
                {
                    Assert.True(_dic.ContainsValue((ValueType)_enum.Current)); //"Err_27! Expected key to be present"
                    count++;
                }
                Assert.Equal(count, _dic.Count); //"Err_28! Count not equal"
                ValueType[] _values = new ValueType[_dic.Count];
                _col.CopyTo(_values, 0);

                for (int i = 0; i < keys.Length; i++)
                    Assert.True(_dic.ContainsValue(_values[i])); //"Err_29! Expected key to be present"
            }
        }
    }
}

