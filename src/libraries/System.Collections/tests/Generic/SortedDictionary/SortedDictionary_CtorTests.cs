// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Xunit;
using SortedDictionaryTests.SortedDictionary_SortedDictionary_CtorTests;
using SortedDictionary_SortedDictionaryUtils;

namespace SortedDictionaryTests
{
    internal class MyOwnIKeyImplementation<KeyType> : IComparer<KeyType>
    {
        public int GetHashCode(KeyType key)
        {
            //We cannot get the hascode that is culture aware here since TextInfo doesn't expose this functionality publicly
            return key.GetHashCode();
        }

        public int Compare(KeyType key1, KeyType key2)
        {
            //We cannot get the hascode that is culture aware here since TextInfo doesn't expose this functionality publicly
            return key1.GetHashCode();
        }

        public bool Equals(KeyType key1, KeyType key2)
        {
            return key1.Equals(key2);
        }
    }
    namespace SortedDictionary_SortedDictionary_CtorTests
    {
        public class SortedDictionary_CtorTests
        {
            [Fact]
            public static void SortedDictionary_DefaultCtorTest()
            {
                Driver<SimpleRef<int>, SimpleRef<String>> driver1 = new Driver<SimpleRef<int>, SimpleRef<String>>();
                Driver<SimpleRef<String>, SimpleRef<int>> driver2 = new Driver<SimpleRef<String>, SimpleRef<int>>();
                Driver<SimpleRef<int>, SimpleRef<int>> driver3 = new Driver<SimpleRef<int>, SimpleRef<int>>();
                Driver<SimpleRef<String>, SimpleRef<String>> driver4 = new Driver<SimpleRef<String>, SimpleRef<String>>();

                SimpleRef<int>[] ints = new SimpleRef<int>[] { new SimpleRef<int>(1), new SimpleRef<int>(2), new SimpleRef<int>(3) };
                SimpleRef<String>[] strings = new SimpleRef<String>[] { new SimpleRef<String>("1"), new SimpleRef<String>("2"), new SimpleRef<String>("3") };

                //Scenario 1: Vanilla - Create a SortedDictionary and check that default properties are set
                driver1.TestVanilla();
                driver2.TestVanilla();
                driver3.TestVanilla();
                driver4.TestVanilla();

                //Scenario 2: Vanilla - Make sure that we can add key-value pairs to this
                driver1.TestCanAdd(ints, strings);
                driver2.TestCanAdd(strings, ints);
                driver3.TestCanAdd(ints, ints);
                driver4.TestCanAdd(strings, strings);
            }

            [Fact]
            public static void SortedDictionary_IDictionaryCtorTest()
            {
                Driver<SimpleRef<int>, SimpleRef<String>> driver1 = new Driver<SimpleRef<int>, SimpleRef<String>>();
                Driver<SimpleRef<String>, SimpleRef<int>> driver2 = new Driver<SimpleRef<String>, SimpleRef<int>>();
                Driver<SimpleRef<int>, SimpleRef<int>> driver3 = new Driver<SimpleRef<int>, SimpleRef<int>>();
                Driver<SimpleRef<String>, SimpleRef<String>> driver4 = new Driver<SimpleRef<String>, SimpleRef<String>>();

                int count;

                SimpleRef<int>[] ints;
                SimpleRef<String>[] strings;

                SortedDictionary<SimpleRef<int>, SimpleRef<String>> dic1;
                SortedDictionary<SimpleRef<String>, SimpleRef<int>> dic2;
                SortedDictionary<SimpleRef<int>, SimpleRef<int>> dic3;
                SortedDictionary<SimpleRef<String>, SimpleRef<String>> dic4;

                //Scenario 1: Vanilla - Create SortedDictionary using another SortedDictionary with 10 key-values and check
                count = 10;
                ints = GetSimpleInts(count);
                strings = GetSimpleStrings(count);

                dic1 = FillValues(ints, strings);
                dic2 = FillValues(strings, ints);
                dic3 = FillValues(ints, ints);
                dic4 = FillValues(strings, strings);

                driver1.TestVanillaIDictionary(dic1);
                driver2.TestVanillaIDictionary(dic2);
                driver3.TestVanillaIDictionary(dic3);
                driver4.TestVanillaIDictionary(dic4);

                //Scenario 2: SortedDictionary with 100 entries
                count = 100;
                ints = GetSimpleInts(count);
                strings = GetSimpleStrings(count);

                dic1 = FillValues(ints, strings);
                dic2 = FillValues(strings, ints);
                dic3 = FillValues(ints, ints);
                dic4 = FillValues(strings, strings);

                driver1.TestVanillaIDictionary(dic1);
                driver2.TestVanillaIDictionary(dic2);
                driver3.TestVanillaIDictionary(dic3);
                driver4.TestVanillaIDictionary(dic4);

                //Scenario 3: Implement our own type that implements IDictionary<KeyType, ValueType> and test
                TestSortedDictionary<SimpleRef<int>, SimpleRef<String>> myDic1 = new TestSortedDictionary<SimpleRef<int>, SimpleRef<String>>(ints, strings);
                TestSortedDictionary<SimpleRef<String>, SimpleRef<int>> myDic2 = new TestSortedDictionary<SimpleRef<String>, SimpleRef<int>>(strings, ints);
                TestSortedDictionary<SimpleRef<int>, SimpleRef<int>> myDic3 = new TestSortedDictionary<SimpleRef<int>, SimpleRef<int>>(ints, ints);
                TestSortedDictionary<SimpleRef<String>, SimpleRef<String>> myDic4 = new TestSortedDictionary<SimpleRef<String>, SimpleRef<String>>(strings, strings);

                driver1.TestVanillaIDictionary(myDic1);
                driver2.TestVanillaIDictionary(myDic2);
                driver3.TestVanillaIDictionary(myDic3);
                driver4.TestVanillaIDictionary(myDic4);

                driver1.TestVanillaIDictionary(new SortedDictionary<SimpleRef<int>, SimpleRef<String>>());
                driver2.TestVanillaIDictionary(new SortedDictionary<SimpleRef<String>, SimpleRef<int>>());
                driver3.TestVanillaIDictionary(new SortedDictionary<SimpleRef<int>, SimpleRef<int>>());
                driver4.TestVanillaIDictionary(new SortedDictionary<SimpleRef<String>, SimpleRef<String>>());
            }

            [Fact]
            public static void SortedDictionary_IDictionaryIKeyComparerCtorTest()
            {
                Driver<String, String> driver = new Driver<String, String>();

                int count;

                SimpleRef<int>[] simpleInts;
                SimpleRef<String>[] simpleStrings;
                String[] strings;

                SortedDictionary<String, String> dic1;

                count = 10;
                strings = new String[count];
                for (int i = 0; i < count; i++)
                    strings[i] = i.ToString();
                simpleInts = GetSimpleInts(count);
                simpleStrings = GetSimpleStrings(count);

                dic1 = FillValues(strings, strings);

                // Scenario 1: Pass all the enum values and ensure that the behavior is correct
                driver.TestEnumIDictionary_IKeyComparer(dic1);

                // Scenario 2: Implement our own IKeyComparer and check
                driver.IkeyComparerOwnImplementation(dic1);

                //Scenario 3: ensure that SortedDictionary items from the passed IDictionary object use the interface IKeyComparer's Equals and GetHashCode APIs. 
                //Ex. Pass the case invariant IKeyComparer and check
                //@TODO!!!

                //Scenario 4: Contradictory values and check: ex. IDictionary is case insensitive but IKeyComparer is not 
            }

            [Fact]
            public static void SortedDictionary_IKeyComparerCtorTest()
            {
                Driver<String, String> driver1 = new Driver<String, String>();

                //Scenario 1: Pass all the enum values and ensure that the behavior is correct
                driver1.TestEnumIKeyComparer();

                //Scenario 2: Parm validation: null
                // If comparer is null, this constructor uses the default generic equality comparer, Comparer<T>.Default. 
                driver1.TestParmIKeyComparer();

                //Scenario 3: Implement our own IKeyComparer and check
                driver1.IkeyComparerOwnImplementation(null);
            }

            [Fact]
            public static void SortedDictionary_IDictionaryIKeyComparerCtorTest_Negative()
            {
                Driver<String, String> driver = new Driver<String, String>();

                //Param validation: null
                driver.TestParmIDictionaryIKeyComparer();
            }

            [Fact]
            public static void SortedDictionary_IDictionaryCtorTest_Negative()
            {
                Driver<SimpleRef<int>, SimpleRef<String>> driver1 = new Driver<SimpleRef<int>, SimpleRef<String>>();
                Driver<SimpleRef<String>, SimpleRef<int>> driver2 = new Driver<SimpleRef<String>, SimpleRef<int>>();
                Driver<SimpleRef<int>, SimpleRef<int>> driver3 = new Driver<SimpleRef<int>, SimpleRef<int>>();
                Driver<SimpleRef<String>, SimpleRef<String>> driver4 = new Driver<SimpleRef<String>, SimpleRef<String>>();

                //Scenario 4: Parm validation: null, empty SortedDictionary
                driver1.TestParmIDictionary(null);
                driver2.TestParmIDictionary(null);
                driver3.TestParmIDictionary(null);
                driver4.TestParmIDictionary(null);
            }


            private static SortedDictionary<KeyType, ValueType> FillValues<KeyType, ValueType>(KeyType[] keys, ValueType[] values)
            {
                SortedDictionary<KeyType, ValueType> _dic = new SortedDictionary<KeyType, ValueType>();
                for (int i = 0; i < keys.Length; i++)
                    _dic.Add(keys[i], values[i]);
                return _dic;
            }

            private static SimpleRef<int>[] GetSimpleInts(int count)
            {
                SimpleRef<int>[] ints = new SimpleRef<int>[count];
                for (int i = 0; i < count; i++)
                    ints[i] = new SimpleRef<int>(i);
                return ints;
            }

            private static SimpleRef<String>[] GetSimpleStrings(int count)
            {
                SimpleRef<String>[] strings = new SimpleRef<String>[count];
                for (int i = 0; i < count; i++)
                    strings[i] = new SimpleRef<String>(i.ToString());
                return strings;
            }
        }
        /// Helper class 
        public class Driver<KeyType, ValueType>
        {
            private CultureInfo _english = new CultureInfo("en");
            private CultureInfo _german = new CultureInfo("de");
            private CultureInfo _danish = new CultureInfo("da");
            private CultureInfo _turkish = new CultureInfo("tr");

            private const String strAE = "AE";
            private const String strUC4 = "\u00C4";
            private const String straA = "aA";
            private const String strAa = "Aa";
            private const String strI = "I";
            private const String strTurkishUpperI = "\u0131";
            private const String strBB = "BB";
            private const String strbb = "bb";

            private const String value = "Default_Value";

            public void TestVanilla()
            {
                SortedDictionary<KeyType, ValueType> _dic = new SortedDictionary<KeyType, ValueType>();
                Assert.Equal(_dic.Comparer, Comparer<KeyType>.Default); //"Err_001! Comparer differ"
                Assert.Equal(_dic.Count, 0); //"Err_002! Count is different"
                Assert.False(((IDictionary<KeyType, ValueType>)_dic).IsReadOnly); //"Err_003! Dictionary is not readonly"
                Assert.Equal(_dic.Keys.Count, 0); //"Err_004! Key count is different"
                Assert.Equal(_dic.Values.Count, 0); //"Err_005! Values count is different"
            }

            public void TestVanillaIDictionary(IDictionary<KeyType, ValueType> SortedDictionary)
            {
                SortedDictionary<KeyType, ValueType> _dic = new SortedDictionary<KeyType, ValueType>(SortedDictionary);
                Assert.Equal(_dic.Comparer, Comparer<KeyType>.Default); //"Err_006! Comparer differ"
                Assert.Equal(_dic.Count, SortedDictionary.Count); //"Err_007! Count is different"
                Assert.False(((IDictionary<KeyType, ValueType>)_dic).IsReadOnly); //"Err_008! Dictionary is not readonly"
                Assert.Equal(_dic.Keys.Count, SortedDictionary.Count); //"Err_009! Key count is different"
                Assert.Equal(_dic.Values.Count, SortedDictionary.Count); //"Err_010! Values count is different"
            }

            public void TestParmIDictionary(IDictionary<KeyType, ValueType> SortedDictionary)
            {
                Assert.Throws<ArgumentNullException>(() => new SortedDictionary<KeyType, ValueType>(SortedDictionary)); //"Err_011! wrong exception thrown."
            }


            public void TestCanAdd(KeyType[] keys, ValueType[] values)
            {
                SortedDictionary<KeyType, ValueType> _dic = new SortedDictionary<KeyType, ValueType>();

                for (int i = 0; i < keys.Length; i++)
                    _dic.Add(keys[i], values[i]);

                Assert.Equal(_dic.Count, keys.Length); //"Err_012! Count is different"
                Assert.False(((IDictionary<KeyType, ValueType>)_dic).IsReadOnly); //"Err_013! Dictionary is not readonly"
                Assert.Equal(_dic.Keys.Count, keys.Length); //"Err_014! Keys count is different"
                Assert.Equal(_dic.Values.Count, values.Length); //"Err_015! values count is different"
            }

            public void TestEnumIDictionary_IKeyComparer(IDictionary<String, String> idic)
            {
                SortedDictionary<String, String> _dic;
                IComparer<String> comparer;
                IComparer<String>[] predefinedComparers = new IComparer<String>[] {
                StringComparer.CurrentCulture,
                StringComparer.CurrentCultureIgnoreCase,
                StringComparer.Ordinal};

                foreach (IComparer<String> predefinedComparer in predefinedComparers)
                {
                    _dic = new SortedDictionary<String, String>(predefinedComparer);
                    Assert.Equal(_dic.Comparer, predefinedComparer); //"Err_016! Comparers differ"
                    Assert.Equal(_dic.Count, 0); //"Err_017! Count is different"
                    Assert.False(((IDictionary<KeyType, ValueType>)_dic).IsReadOnly); //"Err_018! Dictionary is not readonly"
                    Assert.Equal(_dic.Keys.Count, 0); //"Err_019! Count is different"
                    Assert.Equal(_dic.Values.Count, 0); //"Err_020! Count is different"
                }

                //Current culture
                CultureInfo.DefaultThreadCurrentCulture = _english;
                comparer = StringComparer.CurrentCulture;
                _dic = new SortedDictionary<String, String>(comparer);
                Assert.Equal(_dic.Comparer, comparer); //"Err_021! Comparer is different"

                _dic.Add(strAE, value);

                Assert.False(_dic.ContainsKey(strUC4)); //"Err_022! Expected that _dic.ContainsKey(strUC4) would return false"

                //CurrentCultureIgnoreCase
                CultureInfo.DefaultThreadCurrentCulture = _english;
                comparer = StringComparer.CurrentCultureIgnoreCase;
                _dic = new SortedDictionary<String, String>(comparer);
                Assert.Equal(_dic.Comparer, comparer); //"Err_025! Comparer is different"

                _dic.Add(straA, value);
                Assert.True(_dic.ContainsKey(strAa)); //"Err_026! Expected that _dic.ContainsKey(strAa) would return true"

                CultureInfo.DefaultThreadCurrentCulture = _danish;
                comparer = StringComparer.CurrentCultureIgnoreCase;
                _dic = new SortedDictionary<String, String>(comparer);
                Assert.Equal(_dic.Comparer, comparer); //"Err_027! Comparer is different"

                _dic.Add(straA, value);
                Assert.False(_dic.ContainsKey(strAa)); //"Err_028! Expected that _dic.ContainsKey(strAa) would return false"

                //Ordinal
                CultureInfo.DefaultThreadCurrentCulture = _english;
                comparer = StringComparer.Ordinal;
                _dic = new SortedDictionary<String, String>(comparer);
                Assert.Equal(_dic.Comparer, comparer); //"Err_029! Comparer is different"

                _dic.Add(strBB, value);
                Assert.False(_dic.ContainsKey(strbb)); //"Err_030! Expected that _dic.ContainsKey(strbb) would return false"

                CultureInfo.DefaultThreadCurrentCulture = _danish;
                comparer = StringComparer.Ordinal;
                _dic = new SortedDictionary<String, String>(comparer);
                Assert.Equal(_dic.Comparer, comparer); //"Err_031! Comparer is different"
                _dic.Add(strBB, value);
                Assert.False(_dic.ContainsKey(strbb)); //"Err_032! Expected that _dic.ContainsKey(strbb) would return false"
            }

            public void TestEnumIKeyComparer()
            {
                SortedDictionary<String, String> _dic;
                IComparer<String> comparer;
                IComparer<String>[] predefinedComparers = new IComparer<String>[] {
                StringComparer.CurrentCulture,
                StringComparer.CurrentCultureIgnoreCase,
                StringComparer.Ordinal};

                foreach (IComparer<String> predefinedComparer in predefinedComparers)
                {
                    _dic = new SortedDictionary<String, String>(predefinedComparer);
                    Assert.Equal(_dic.Comparer, predefinedComparer); //"Err_033! Comparer is different"
                    Assert.Equal(_dic.Count, 0); //"Err_034! Count is different"
                    Assert.False(((IDictionary<KeyType, ValueType>)_dic).IsReadOnly); //"Err_035! Dictionary is not readonly"
                    Assert.Equal(_dic.Keys.Count, 0); //"Err_036! Count is different"
                    Assert.Equal(_dic.Values.Count, 0); //"Err_037! Count is different"
                }

                //Current culture
                CultureInfo.DefaultThreadCurrentCulture = _english;
                comparer = StringComparer.CurrentCulture;
                _dic = new SortedDictionary<String, String>(comparer);
                Assert.Equal(_dic.Comparer, comparer); //"Err_038! Comparer is different"
                _dic.Add(strAE, value);
                Assert.False(_dic.ContainsKey(strUC4)); //"Err_039! Expected that _dic.ContainsKey(strUC4) would return false"

                //CurrentCultureIgnoreCase
                CultureInfo.DefaultThreadCurrentCulture = _english;
                comparer = StringComparer.CurrentCultureIgnoreCase;
                _dic = new SortedDictionary<String, String>(comparer);
                Assert.Equal(_dic.Comparer, comparer); //"Err_040! Comparer is different"
                _dic.Add(straA, value);
                Assert.True(_dic.ContainsKey(strAa)); //"Err_041! Expected that _dic.ContainsKey(strAa) would return true"

                CultureInfo.DefaultThreadCurrentCulture = _danish;
                comparer = StringComparer.CurrentCultureIgnoreCase;
                _dic = new SortedDictionary<String, String>(comparer);
                Assert.Equal(_dic.Comparer, comparer); //"Err_042! Comparer is different"
                _dic.Add(straA, value);
                Assert.False(_dic.ContainsKey(strAa)); //"Err_043! Expected that _dic.ContainsKey(strAa) would return false"

                //Ordinal
                CultureInfo.DefaultThreadCurrentCulture = _english;
                comparer = StringComparer.Ordinal;
                _dic = new SortedDictionary<String, String>(comparer);
                Assert.Equal(_dic.Comparer, comparer); //"Err_044! Comparer is different"
                _dic.Add(strBB, value);
                Assert.False(_dic.ContainsKey(strbb)); //"Err_045! Expected that _dic.ContainsKey(strbb) would return false"

                CultureInfo.DefaultThreadCurrentCulture = _danish;
                comparer = StringComparer.Ordinal;
                _dic = new SortedDictionary<String, String>(comparer);
                Assert.Equal(_dic.Comparer, comparer); //"Err_046! Comparer is different"
                _dic.Add(strBB, value);
                Assert.False(_dic.ContainsKey(strbb)); //"Err_047! Expected that _dic.ContainsKey(strbb) would return false"
            }

            public void TestParmIDictionaryIKeyComparer()
            {
                //passing null will revert to the default comparison mechanism
                SortedDictionary<String, String> _dic;
                IComparer<String> comparer = null;
                SortedDictionary<String, String> dic1 = new SortedDictionary<String, String>();

                CultureInfo.DefaultThreadCurrentCulture = _english;
                _dic = new SortedDictionary<String, String>(dic1, comparer);
                _dic.Add(straA, value);
                Assert.False(_dic.ContainsKey(strAa)); //"Err_048! Expected that _dic.ContainsKey(strAa) would return false"

                CultureInfo.DefaultThreadCurrentCulture = _danish;
                _dic = new SortedDictionary<String, String>(dic1, comparer);
                _dic.Add(straA, value);
                Assert.False(_dic.ContainsKey(strAa)); //"Err_049! Expected that _dic.ContainsKey(strAa) would return false"

                comparer = StringComparer.CurrentCulture;
                dic1 = null;

                CultureInfo.DefaultThreadCurrentCulture = _english;

                Assert.Throws<ArgumentNullException>(() => new SortedDictionary<String, String>(dic1, comparer)); //"Err_050! wrong exception thrown."
            }


            public void TestParmIKeyComparer()
            {
                IComparer<KeyType> comparer = null;

                SortedDictionary<KeyType, ValueType> _dic = new SortedDictionary<KeyType, ValueType>(comparer);

                Assert.Equal(_dic.Comparer, Comparer<KeyType>.Default); //"Err_051! Comparer differ"
                Assert.Equal(_dic.Count, 0); //"Err_052! Count is different"
                Assert.False(((IDictionary<KeyType, ValueType>)_dic).IsReadOnly); //"Err_053! Dictionary is not readonly"
                Assert.Equal(_dic.Keys.Count, 0); //"Err_054! Key count is different"
                Assert.Equal(_dic.Values.Count, 0); //"Err_055! Values count is different"
            }

            public void IkeyComparerOwnImplementation(IDictionary<String, String> idic)
            {
                //This just ensures that we can call our own implementation
                SortedDictionary<String, String> _dic;
                IComparer<String> comparer = new MyOwnIKeyImplementation<String>();

                CultureInfo.DefaultThreadCurrentCulture = _english;
                if (idic == null)
                {
                    _dic = new SortedDictionary<String, String>(comparer);
                }
                else
                {
                    _dic = new SortedDictionary<String, String>(idic, comparer);
                }

                _dic.Add(straA, value);
                Assert.False(_dic.ContainsKey(strAa)); //"Err_056! Expected that _dic.ContainsKey(strAa) would return false"

                CultureInfo.DefaultThreadCurrentCulture = _danish;

                if (idic == null)
                {
                    _dic = new SortedDictionary<String, String>(comparer);
                }
                else
                {
                    _dic = new SortedDictionary<String, String>(idic, comparer);
                }

                _dic.Add(straA, value);
                Assert.False(_dic.ContainsKey(strAa)); //"Err_057! Expected that _dic.ContainsKey(strAa) would return false"
            }
        }
    }
}
