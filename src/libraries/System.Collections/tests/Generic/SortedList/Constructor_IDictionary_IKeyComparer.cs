// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections;
using System.Globalization;
using Xunit;
using SortedList_SortedListUtils;

namespace SortedListCtorIDicIKeyComp
{
    public class Driver<KeyType, ValueType>
    {
        private Test m_test;

        public Driver(Test test)
        {
            m_test = test;
        }

        private static CultureInfo s_english = new CultureInfo("en");
        private static CultureInfo s_german = new CultureInfo("de");
        private static CultureInfo s_danish = new CultureInfo("da");
        private static CultureInfo s_turkish = new CultureInfo("tr");

        //CompareString lcid_en-US, EMPTY_FLAGS, "AE", 0, 2, "\u00C4", 0, 1, 1, NULL_STRING
        //CompareString 0x10407, EMPTY_FLAGS, "AE", 0, 2, "\u00C4", 0, 1, 0, NULL_STRING
        //CompareString lcid_da-DK, "NORM_IGNORECASE", "aA", 0, 2, "Aa", 0, 2, -1, NULL_STRING
        //CompareString lcid_en-US, "NORM_IGNORECASE", "aA", 0, 2, "Aa", 0, 2, 0, NULL_STRING
        //CompareString lcid_tr-TR, "NORM_IGNORECASE", "\u0131", 0, 1, "\u0049", 0, 1, 0, NULL_STRING


        private const String strAE = "AE";
        private const String strUC4 = "\u00C4";
        private const String straA = "aA";
        private const String strAa = "Aa";
        private const String strI = "I";
        private const String strTurkishUpperI = "\u0131";
        private const String strBB = "BB";
        private const String strbb = "bb";

        private const String value = "Default_Value";

        public void TestEnum(IDictionary<String, String> idic)
        {
            SortedList<String, String> _dic;
            IComparer<String> comparer;
            String[] keys = new String[idic.Count];
            idic.Keys.CopyTo(keys, 0);
            String[] values = new String[idic.Count];
            idic.Values.CopyTo(values, 0);
            IComparer<String>[] predefinedComparers = new IComparer<String>[] {
                StringComparer.CurrentCulture,
                StringComparer.CurrentCultureIgnoreCase,
                StringComparer.OrdinalIgnoreCase,
                StringComparer.Ordinal};

            foreach (IComparer<String> predefinedComparer in predefinedComparers)
            {
                _dic = new SortedList<String, String>(idic, predefinedComparer);
                m_test.Eval(_dic.Comparer == predefinedComparer, String.Format("Err_4568aijueud! Comparer differ expected: {0} actual: {1}", predefinedComparer, _dic.Comparer));
                m_test.Eval(_dic.Count == idic.Count, String.Format("Err_23497sg! Count different: {0}", _dic.Count));
                m_test.Eval(_dic.Keys.Count == idic.Count, String.Format("Err_25ag! Count different: {0}", _dic.Keys.Count));
                m_test.Eval(_dic.Values.Count == idic.Count, String.Format("Err_23agd! Count different: {0}", _dic.Values.Count));
                for (int i = 0; i < idic.Keys.Count; i++)
                {
                    m_test.Eval(_dic.ContainsKey(keys[i]), String.Format("Err_234afs! key not found: {0}", keys[i]));
                    m_test.Eval(_dic.ContainsValue(values[i]), String.Format("Err_3497sg! value not found: {0}", values[i]));
                }
            }


            //Current culture
            CultureInfo.DefaultThreadCurrentCulture = s_english;
            comparer = StringComparer.CurrentCulture;
            _dic = new SortedList<String, String>(idic, comparer);
            m_test.Eval(_dic.Comparer == comparer, String.Format("Err_58484aheued! Comparer differ expected: {0} actual: {1}", comparer, _dic.Comparer));
            _dic.Add(strAE, value);
            m_test.Eval(!_dic.ContainsKey(strUC4), String.Format("Err_235rdag! Wrong result returned: {0}", _dic.ContainsKey(strUC4)));

            //bug #11263 in NDPWhidbey
            CultureInfo.DefaultThreadCurrentCulture = s_german;
            comparer = StringComparer.CurrentCulture;
            _dic = new SortedList<String, String>(idic, comparer);
            m_test.Eval(_dic.Comparer == comparer, String.Format("Err_5468ahiede! Comparer differ expected: {0} actual: {1}", comparer, _dic.Comparer));
            _dic.Add(strAE, value);
            // same result in Desktop
            m_test.Eval(!_dic.ContainsKey(strUC4), String.Format("Err_23r7ag! Wrong result returned: {0}", _dic.ContainsKey(strUC4)));

            //CurrentCultureIgnoreCase
            CultureInfo.DefaultThreadCurrentCulture = s_english;
            comparer = StringComparer.CurrentCultureIgnoreCase;
            _dic = new SortedList<String, String>(idic, comparer);
            m_test.Eval(_dic.Comparer == comparer, String.Format("Err_4488ajede! Comparer differ expected: {0} actual: {1}", comparer, _dic.Comparer));
            _dic.Add(straA, value);
            m_test.Eval(_dic.ContainsKey(strAa), String.Format("Err_237g! Wrong result returned: {0}", _dic.ContainsKey(strAa)));

            CultureInfo.DefaultThreadCurrentCulture = s_danish;
            comparer = StringComparer.CurrentCultureIgnoreCase;
            _dic = new SortedList<String, String>(idic, comparer);
            m_test.Eval(_dic.Comparer == comparer, String.Format("Err_6884ahnjed! Comparer differ expected: {0} actual: {1}", comparer, _dic.Comparer));
            _dic.Add(straA, value);
            m_test.Eval(!_dic.ContainsKey(strAa), String.Format("Err_0723f! Wrong result returned: {0}", _dic.ContainsKey(strAa)));

            //OrdinalIgnoreCase
            CultureInfo.DefaultThreadCurrentCulture = s_english;
            comparer = StringComparer.OrdinalIgnoreCase;
            _dic = new SortedList<String, String>(idic, comparer);
            m_test.Eval(_dic.Comparer == comparer, String.Format("Err_95877ahiez! Comparer differ expected: {0} actual: {1}", comparer, _dic.Comparer));
            _dic.Add(strI, value);
            m_test.Eval(!_dic.ContainsKey(strTurkishUpperI), String.Format("Err_234qf! Wrong result returned: {0}", _dic.ContainsKey(strTurkishUpperI)));

            CultureInfo.DefaultThreadCurrentCulture = s_turkish;
            comparer = StringComparer.OrdinalIgnoreCase;
            _dic = new SortedList<String, String>(idic, comparer);
            m_test.Eval(_dic.Comparer == comparer, String.Format("Err_50548haied! Comparer differ expected: {0} actual: {1}", comparer, _dic.Comparer));
            _dic.Add(strI, value);
            m_test.Eval(!_dic.ContainsKey(strTurkishUpperI), String.Format("Err_234ra7g! Wrong result returned: {0}", _dic.ContainsKey(strTurkishUpperI)));

            //Ordinal - not that many meaningful test
            CultureInfo.DefaultThreadCurrentCulture = s_english;
            comparer = StringComparer.Ordinal;
            _dic = new SortedList<String, String>(idic, comparer);
            m_test.Eval(_dic.Comparer == comparer, String.Format("Err_1407hizbd! Comparer differ expected: {0} actual: {1}", comparer, _dic.Comparer));
            _dic.Add(strBB, value);
            m_test.Eval(!_dic.ContainsKey(strbb), String.Format("Err_1244sd! Wrong result returned: {0}", _dic.ContainsKey(strbb)));

            CultureInfo.DefaultThreadCurrentCulture = s_danish;
            comparer = StringComparer.Ordinal;
            _dic = new SortedList<String, String>(idic, comparer);
            m_test.Eval(_dic.Comparer == comparer, String.Format("Err_5088aied! Comparer differ expected: {0} actual: {1}", comparer, _dic.Comparer));
            _dic.Add(strBB, value);
            m_test.Eval(!_dic.ContainsKey(strbb), String.Format("Err_235aeg! Wrong result returned: {0}", _dic.ContainsKey(strbb)));
        }

        public void TestParm()
        {
            //passing null will revert to the default comparison mechanism
            SortedList<String, String> _dic;
            IComparer<String> comparer = null;
            SortedList<String, String> dic1 = new SortedList<String, String>();
            try
            {
                CultureInfo.DefaultThreadCurrentCulture = s_english;
                _dic = new SortedList<String, String>(dic1, comparer);
                _dic.Add(straA, value);
                m_test.Eval(!_dic.ContainsKey(strAa), String.Format("Err_9237g! Wrong result returned: {0}", _dic.ContainsKey(strAa)));

                CultureInfo.DefaultThreadCurrentCulture = s_danish;
                _dic = new SortedList<String, String>(dic1, comparer);
                _dic.Add(straA, value);
                m_test.Eval(!_dic.ContainsKey(strAa), String.Format("Err_90723f! Wrong result returned: {0}", _dic.ContainsKey(strAa)));
            }
            catch (Exception ex)
            {
                m_test.Eval(false, String.Format("Err_387tsg! Wrong exception thrown: {0}", ex));
            }

            comparer = StringComparer.CurrentCulture;
            dic1 = null;
            try
            {
                CultureInfo.DefaultThreadCurrentCulture = s_english;
                _dic = new SortedList<String, String>(dic1, comparer);
                m_test.Eval(false, String.Format("Err_387tsg! exception not thrown"));
            }
            catch (ArgumentNullException)
            {
            }
            catch (Exception ex)
            {
                m_test.Eval(false, String.Format("Err_387tsg! Wrong exception thrown: {0}", ex));
            }
        }

        public void IkeyComparerOwnImplementation(IDictionary<String, String> idic)
        {
            //This just ensure that we can call our own implementation
            SortedList<String, String> _dic;
            IComparer<String> comparer = new MyOwnIKeyImplementation<String>();
            try
            {
                CultureInfo.DefaultThreadCurrentCulture = s_english;
                _dic = new SortedList<String, String>(idic, comparer);
                _dic.Add(straA, value);
                m_test.Eval(!_dic.ContainsKey(strAa), String.Format("Err_0237g! Wrong result returned: {0}", _dic.ContainsKey(strAa)));

                CultureInfo.DefaultThreadCurrentCulture = s_danish;
                _dic = new SortedList<String, String>(idic, comparer);
                _dic.Add(straA, value);
                m_test.Eval(!_dic.ContainsKey(strAa), String.Format("Err_00723f! Wrong result returned: {0}", _dic.ContainsKey(strAa)));
            }
            catch (Exception ex)
            {
                m_test.Eval(false, String.Format("Err_387tsg! Wrong exception thrown: {0}", ex));
            }
        }
    }

    public class Constructor_IDictionary_IKeyComparer
    {
        [Fact]
        public static void RunTests()
        {
            //This mostly follows the format established by the original author of these tests

            Test test = new Test();

            Driver<String, String> driver1 = new Driver<String, String>(test);
            Driver<SimpleRef<int>, SimpleRef<String>> driver2 = new Driver<SimpleRef<int>, SimpleRef<String>>(test);
            Driver<SimpleRef<String>, SimpleRef<int>> driver3 = new Driver<SimpleRef<String>, SimpleRef<int>>(test);
            Driver<SimpleRef<int>, SimpleRef<int>> driver4 = new Driver<SimpleRef<int>, SimpleRef<int>>(test);
            Driver<SimpleRef<String>, SimpleRef<String>> driver5 = new Driver<SimpleRef<String>, SimpleRef<String>>(test);

            int count;

            SimpleRef<int>[] simpleInts;
            SimpleRef<String>[] simpleStrings;
            String[] strings;

            SortedList<String, String> dic1;

            count = 10;
            strings = new String[count];
            for (int i = 0; i < count; i++)
                strings[i] = i.ToString();
            simpleInts = GetSimpleInts(count);
            simpleStrings = GetSimpleStrings(count);

            dic1 = FillValues(strings, strings);


            //Scenario 1: Pass all the enum values and ensure that the behavior is correct
            driver1.TestEnum(dic1);

            //Scenario 2: Parm validation: null
            driver1.TestParm();

            //Scenario 3: Implement our own IKeyComparer and check
            driver1.IkeyComparerOwnImplementation(dic1);

            //Scenario 5: ensure that SortedList items from the passed IDictionary object use the interface IKeyComparer's Equals and GetHashCode APIs. 
            //Ex. Pass the case invariant IKeyComparer and check
            //@TODO!!!

            //Scenario 6: Contradictory values and check: ex. IDictionary is case insensitive but IKeyComparer is not 

            Assert.True(test.result);
        }

        private static SortedList<KeyType, ValueType> FillValues<KeyType, ValueType>(KeyType[] keys, ValueType[] values)
        {
            SortedList<KeyType, ValueType> _dic = new SortedList<KeyType, ValueType>();
            for (int i = 0; i < keys.Length; i++)
                _dic.Add(keys[i], values[i]);
            return _dic;
        }

        private static SimpleRef<int>[] GetSimpleInts(int count)
        {
            SimpleRef<int>[] simpleInts = new SimpleRef<int>[count];
            for (int i = 0; i < count; i++)
                simpleInts[i] = new SimpleRef<int>(i);
            return simpleInts;
        }

        private static SimpleRef<String>[] GetSimpleStrings(int count)
        {
            SimpleRef<String>[] simpleStrings = new SimpleRef<String>[count];
            for (int i = 0; i < count; i++)
                simpleStrings[i] = new SimpleRef<String>(i.ToString());
            return simpleStrings;
        }
    }

    //[Serializable]
    internal class MyOwnIKeyImplementation<KeyType> : IComparer<KeyType> where KeyType : IComparable<KeyType>
    {
        public int GetHashCode(KeyType key)
        {
            if (null == key)
                return 0;

            //We cannot get the hascode that is culture aware here since TextInfo doesn't expose this functionality publicly
            return key.GetHashCode();
        }

        public int Compare(KeyType key1, KeyType key2)
        {
            if (null == key1)
            {
                if (null == key2)
                    return 0;

                return -1;
            }

            return key1.CompareTo(key2);
        }

        public bool Equals(KeyType key1, KeyType key2)
        {
            if (null == key1)
                return null == key2;

            return key1.Equals(key2);
        }
    }
}