// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections;
using System.Globalization;
using Xunit;
using SortedList_SortedListUtils;

namespace SortedListCtorIntComp
{
    public class Driver<KeyType, ValueType>
    {
        private Test m_test;

        public Driver(Test test)
        {
            m_test = test;
        }

        private CultureInfo _english = new CultureInfo("en");
        private CultureInfo _german = new CultureInfo("de");
        private CultureInfo _danish = new CultureInfo("da");
        private CultureInfo _turkish = new CultureInfo("tr");

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

        private static IComparer<String>[] s_predefinedComparers = new IComparer<String>[] {
                StringComparer.CurrentCulture,
                StringComparer.CurrentCultureIgnoreCase,
                StringComparer.OrdinalIgnoreCase,
                StringComparer.Ordinal};

        public void TestEnum(int capacity)
        {
            SortedList<String, String> _dic;
            IComparer<String> comparer;
            foreach (var comparison in s_predefinedComparers)
            {
                _dic = new SortedList<String, String>(capacity, comparison);
                m_test.Eval(_dic.Count == 0, String.Format("Err_23497sg! Count different: {0}", _dic.Count));
                m_test.Eval(_dic.Keys.Count == 0, String.Format("Err_25ag! Count different: {0}", _dic.Keys.Count));
                m_test.Eval(_dic.Values.Count == 0, String.Format("Err_23agd! Count different: {0}", _dic.Values.Count));
            }


            //Current culture
            CultureInfo.DefaultThreadCurrentCulture = _english;
            comparer = StringComparer.CurrentCulture;
            _dic = new SortedList<String, String>(capacity, comparer);
            _dic.Add(strAE, value);
            m_test.Eval(!_dic.ContainsKey(strUC4), String.Format("Err_235rdag! Wrong result returned: {0}", _dic.ContainsKey(strUC4)));

            //bug #11263 in NDPWhidbey
            CultureInfo.DefaultThreadCurrentCulture = _german;
            _dic = new SortedList<String, String>(capacity, StringComparer.CurrentCulture);
            _dic.Add(strAE, value);
            m_test.Eval(!_dic.ContainsKey(strUC4), String.Format("Err_23r7ag! Wrong result returned: {0}", _dic.ContainsKey(strUC4)));

            //CurrentCultureIgnoreCase
            CultureInfo.DefaultThreadCurrentCulture = _english;
            _dic = new SortedList<String, String>(capacity, StringComparer.CurrentCultureIgnoreCase);
            _dic.Add(straA, value);
            m_test.Eval(_dic.ContainsKey(strAa), String.Format("Err_237g! Wrong result returned: {0}", _dic.ContainsKey(strAa)));

            CultureInfo.DefaultThreadCurrentCulture = _danish;
            _dic = new SortedList<String, String>(capacity, StringComparer.CurrentCultureIgnoreCase);
            _dic.Add(straA, value);
            m_test.Eval(!_dic.ContainsKey(strAa), String.Format("Err_0723f! Wrong result returned: {0}", _dic.ContainsKey(strAa)));

            //InvariantCultureIgnoreCase
            CultureInfo.DefaultThreadCurrentCulture = _english;
            _dic = new SortedList<String, String>(capacity, StringComparer.OrdinalIgnoreCase);
            _dic.Add(strI, value);
            m_test.Eval(!_dic.ContainsKey(strTurkishUpperI), String.Format("Err_234qf! Wrong result returned: {0}", _dic.ContainsKey(strTurkishUpperI)));

            CultureInfo.DefaultThreadCurrentCulture = _turkish;
            _dic = new SortedList<String, String>(capacity, StringComparer.OrdinalIgnoreCase);
            _dic.Add(strI, value);
            m_test.Eval(!_dic.ContainsKey(strTurkishUpperI), String.Format("Err_234ra7g! Wrong result returned: {0}", _dic.ContainsKey(strTurkishUpperI)));

            //Ordinal - not that many meaningful test
            CultureInfo.DefaultThreadCurrentCulture = _english;
            _dic = new SortedList<String, String>(capacity, StringComparer.Ordinal);
            _dic.Add(strBB, value);
            m_test.Eval(!_dic.ContainsKey(strbb), String.Format("Err_1244sd! Wrong result returned: {0}", _dic.ContainsKey(strbb)));

            CultureInfo.DefaultThreadCurrentCulture = _danish;
            _dic = new SortedList<String, String>(capacity, StringComparer.Ordinal);
            _dic.Add(strBB, value);
            m_test.Eval(!_dic.ContainsKey(strbb), String.Format("Err_235aeg! Wrong result returned: {0}", _dic.ContainsKey(strbb)));
        }

        public void TestParm()
        {
            //passing null will revert to the default comparison mechanism
            SortedList<String, String> _dic;
            IComparer<String> comparer;

            int[] negativeValues = { -1, -2, -5, Int32.MinValue };
            comparer = StringComparer.CurrentCulture;
            for (int i = 0; i < negativeValues.Length; i++)
            {
                try
                {
                    _dic = new SortedList<String, String>(negativeValues[i], comparer);
                    m_test.Eval(false, String.Format("Err_387tsg! No exception thrown"));
                }
                catch (ArgumentOutOfRangeException)
                {
                }
                catch (Exception ex)
                {
                    m_test.Eval(false, String.Format("Err_387tsg! Wrong exception thrown: {0}", ex));
                }
            }
        }

        public void NonStringImplementation(int capacity, KeyType key, ValueType value)
        {
            SortedList<KeyType, ValueType> _dic;
            IComparer<KeyType> comparer = new MyOwnIKeyImplementation<KeyType>();
            _dic = new SortedList<KeyType, ValueType>(capacity, comparer);
            m_test.Eval(_dic.Count == 0, String.Format("Err_23497sg! Count different: {0}", _dic.Count));
            m_test.Eval(_dic.Keys.Count == 0, String.Format("Err_25ag! Count different: {0}", _dic.Keys.Count));
            m_test.Eval(_dic.Values.Count == 0, String.Format("Err_23agd! Count different: {0}", _dic.Values.Count));
            _dic.Add(key, value);
            //m_test.Eval(_dic[key].Equals(value), String.Format("Err_234e7af! Result different: {0}", _dic[key]));
        }
    }

    public class Constructor_int_StringComparer
    {
        [Fact]
        public static void RunTests()
        {
            //This mostly follows the format established by the original author of these tests

            Test test = new Test();

            Driver<String, String> driver1 = new Driver<String, String>(test);

            //Scenario 1: Pass all the enum values and ensure that the behavior is correct
            int[] validCapacityValues = { 0, 1, 2, 5, 10, 16, 32, 50, 500, 5000, 10000 };
            for (int i = 0; i < validCapacityValues.Length; i++)
                driver1.TestEnum(validCapacityValues[i]);

            //Scenario 2: Parm validation: other enum values and invalid capacity values
            driver1.TestParm();

            //Scenario 3: Non-string implementations and check
            Driver<SimpleRef<int>, SimpleRef<String>> driver2 = new Driver<SimpleRef<int>, SimpleRef<String>>(test);
            Driver<SimpleRef<String>, SimpleRef<int>> driver3 = new Driver<SimpleRef<String>, SimpleRef<int>>(test);
            Driver<SimpleRef<int>, SimpleRef<int>> driver4 = new Driver<SimpleRef<int>, SimpleRef<int>>(test);
            Driver<SimpleRef<String>, SimpleRef<String>> driver5 = new Driver<SimpleRef<String>, SimpleRef<String>>(test);

            for (int i = 0; i < validCapacityValues.Length; i++)
            {
                driver1.NonStringImplementation(validCapacityValues[i], "Any", "Value");
                driver2.NonStringImplementation(validCapacityValues[i], new SimpleRef<int>(1), new SimpleRef<string>("1"));
                driver3.NonStringImplementation(validCapacityValues[i], new SimpleRef<string>("1"), new SimpleRef<int>(1));
                driver4.NonStringImplementation(validCapacityValues[i], new SimpleRef<int>(1), new SimpleRef<int>(1));
                driver5.NonStringImplementation(validCapacityValues[i], new SimpleRef<string>("1"), new SimpleRef<string>("1"));
            }

            Assert.True(test.result);
        }
    }

    internal class MyOwnIKeyImplementation<KeyType> : IComparer<KeyType>
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
            return key1 == null ? (key2 == null ? 0 : -1) : (key2 == null ? 1 : key1.GetHashCode());
            //if (null == key1)
            //{
            //    if (null == key2)
            //        return 0;

            //    return -1;
            //}

            //return key1.CompareTo(key2);
        }

        public bool Equals(KeyType key1, KeyType key2)
        {
            if (null == key1)
                return null == key2;

            return key1.Equals(key2);
        }
    }
}