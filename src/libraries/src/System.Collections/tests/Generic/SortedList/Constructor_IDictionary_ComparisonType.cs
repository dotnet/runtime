// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections;
using System.Globalization;
using Xunit;
using SortedList_SortedListUtils;

namespace SortedListCtorIDicComp
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

        public void TestEnum(IDictionary<String, String> idic)
        {
            SortedList<String, String> _dic;
            String[] keys = new String[idic.Count];
            idic.Keys.CopyTo(keys, 0);
            String[] values = new String[idic.Count];
            idic.Values.CopyTo(values, 0);

            IComparer<String>[] predefinedComparers = new IComparer<String>[] {
                StringComparer.CurrentCulture,
                StringComparer.CurrentCultureIgnoreCase,
                StringComparer.Ordinal,
                StringComparer.OrdinalIgnoreCase};

            foreach (StringComparer comparison in predefinedComparers)
            {
                _dic = new SortedList<String, String>(idic, comparison);
                m_test.Eval(_dic.Count == idic.Count, String.Format("Err_23497sg! Count different: {0}", _dic.Count));
                m_test.Eval(_dic.Keys.Count == idic.Count, String.Format("Err_25ag! Count different: {0}", _dic.Keys.Count));
                m_test.Eval(_dic.Values.Count == idic.Count, String.Format("Err_23agd! Count different: {0}", _dic.Values.Count));

                for (int i = 0; i < idic.Keys.Count; i++)
                {
                    m_test.Eval(_dic.ContainsKey(keys[i]), String.Format("Err_234afs! key not found: {0}", keys[i]));
                    m_test.Eval(_dic.ContainsValue(values[i]), String.Format("Err_3497sg! value not found: {0}", values[i]));
                }
            }

            IComparer<String> comparer;
            //Current culture
            CultureInfo.DefaultThreadCurrentCulture = _english;
            comparer = StringComparer.CurrentCulture;
            _dic = new SortedList<String, String>(idic, comparer);
            _dic.Add(strAE, value);
            m_test.Eval(!_dic.ContainsKey(strUC4), String.Format("Err_235rdag! Wrong result returned: {0}", _dic.ContainsKey(strUC4)));

            //bug #11263 in NDPWhidbey
            CultureInfo.DefaultThreadCurrentCulture = _german;
            _dic = new SortedList<String, String>(idic, StringComparer.CurrentCulture);
            _dic.Add(strAE, value);
            // 
            m_test.Eval(!_dic.ContainsKey(strUC4), String.Format("Err_23r7ag! Wrong result returned: {0}", _dic.ContainsKey(strUC4)));

            //CurrentCultureIgnoreCase
            CultureInfo.DefaultThreadCurrentCulture = _english;
            _dic = new SortedList<String, String>(idic, StringComparer.CurrentCultureIgnoreCase);
            _dic.Add(straA, value);
            m_test.Eval(_dic.ContainsKey(strAa), String.Format("Err_237g! Wrong result returned: {0}", _dic.ContainsKey(strAa)));

            CultureInfo.DefaultThreadCurrentCulture = _danish;
            _dic = new SortedList<String, String>(idic, StringComparer.CurrentCultureIgnoreCase);
            _dic.Add(straA, value);
            m_test.Eval(!_dic.ContainsKey(strAa), String.Format("Err_0723f! Wrong result returned: {0}", _dic.ContainsKey(strAa)));

            // was for InvariantCulrureIgnoreCase
            CultureInfo.DefaultThreadCurrentCulture = _english;
            _dic = new SortedList<String, String>(idic, StringComparer.OrdinalIgnoreCase);
            _dic.Add(strI, value);
            m_test.Eval(!_dic.ContainsKey(strTurkishUpperI), String.Format("Err_234qf! Wrong result returned: {0}", _dic.ContainsKey(strTurkishUpperI)));

            CultureInfo.DefaultThreadCurrentCulture = _turkish;
            _dic = new SortedList<String, String>(idic, StringComparer.OrdinalIgnoreCase);
            _dic.Add(strI, value);
            m_test.Eval(!_dic.ContainsKey(strTurkishUpperI), String.Format("Err_234ra7g! Wrong result returned: {0}", _dic.ContainsKey(strTurkishUpperI)));

            //Ordinal - not that many meaningful test
            CultureInfo.DefaultThreadCurrentCulture = _english;
            _dic = new SortedList<String, String>(idic, StringComparer.Ordinal);
            _dic.Add(strBB, value);
            m_test.Eval(!_dic.ContainsKey(strbb), String.Format("Err_1244sd! Wrong result returned: {0}", _dic.ContainsKey(strbb)));

            CultureInfo.DefaultThreadCurrentCulture = _danish;
            _dic = new SortedList<String, String>(idic, StringComparer.Ordinal);
            _dic.Add(strBB, value);
            m_test.Eval(!_dic.ContainsKey(strbb), String.Format("Err_235aeg! Wrong result returned: {0}", _dic.ContainsKey(strbb)));
        }

        public void TestParm()
        {
            //passing null will revert to the default comparison mechanism
            SortedList<String, String> _dic;
            SortedList<String, String> dic1 = new SortedList<String, String>();
            dic1 = null;
            try
            {
                CultureInfo.DefaultThreadCurrentCulture = _english;
                _dic = new SortedList<String, String>(dic1, StringComparer.CurrentCulture);
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

        public void VerifyProperSortUsedForCasedStrings(IDictionary<string, string> idic, StringComparer originalComparer, CultureInfo originalCulture)
        {
            SortedList<string, string> _dic;
            SortedList<string, string> originalDic;
            originalDic = new SortedList<string, string>(idic, originalComparer);
            _dic = new SortedList<string, string>(originalDic, StringComparer.Ordinal);

            CultureInfo.DefaultThreadCurrentCulture = _english;
            m_test.Eval(_dic.Count == idic.Count, String.Format("Err_23497sg! Count different: {0}", _dic.Count));
            m_test.Eval(_dic.Keys.Count == idic.Count, String.Format("Err_25ag! Count different: {0}", _dic.Keys.Count));
            m_test.Eval(_dic.Values.Count == idic.Count, String.Format("Err_23agd! Count different: {0}", _dic.Values.Count));
            for (int i = 0; i < idic.Keys.Count; i++)
            {
                m_test.Eval(_dic.Keys[i] == Constructor_IDictionary_StringComparer.stringsOrdinal[i], "VerifyProperSortUsedForCasedStrings1: OriginalCulture/StringComparer" + originalCulture + "/" + originalComparer + "Expected key " + i + " to be " + Constructor_IDictionary_StringComparer.stringsOrdinal[i] + ", but found " + _dic.Keys[i]);
            }

            CultureInfo.DefaultThreadCurrentCulture = originalCulture;
            originalDic = new SortedList<string, string>(idic, originalComparer);
            CultureInfo.DefaultThreadCurrentCulture = _danish;
            _dic = new SortedList<string, string>(originalDic, StringComparer.Ordinal);
            m_test.Eval(_dic.Count == idic.Count, String.Format("Err_23497sg! Count different: {0}", _dic.Count));
            m_test.Eval(_dic.Keys.Count == idic.Count, String.Format("Err_25ag! Count different: {0}", _dic.Keys.Count));
            m_test.Eval(_dic.Values.Count == idic.Count, String.Format("Err_23agd! Count different: {0}", _dic.Values.Count));
            for (int i = 0; i < idic.Keys.Count; i++)
            {
                m_test.Eval(_dic.Keys[i] == Constructor_IDictionary_StringComparer.stringsOrdinal[i], "VerifyProperSortUsedForCasedStrings2: OriginalCulture/StringComparer" + originalCulture + "/" + originalComparer + "Expected key " + i + " to be " + Constructor_IDictionary_StringComparer.stringsOrdinal[i] + ", but found " + _dic.Keys[i]);
            }

            CultureInfo.DefaultThreadCurrentCulture = originalCulture;
            originalDic = new SortedList<string, string>(idic, originalComparer);
            CultureInfo.DefaultThreadCurrentCulture = _english;
            _dic = new SortedList<string, string>(originalDic, StringComparer.CurrentCulture);
            m_test.Eval(_dic.Count == idic.Count, String.Format("Err_23497sg! Count different: {0}", _dic.Count));
            m_test.Eval(_dic.Keys.Count == idic.Count, String.Format("Err_25ag! Count different: {0}", _dic.Keys.Count));
            m_test.Eval(_dic.Values.Count == idic.Count, String.Format("Err_23agd! Count different: {0}", _dic.Values.Count));
            for (int i = 0; i < idic.Keys.Count; i++)
            {
                m_test.Eval(_dic.Keys[i] == Constructor_IDictionary_StringComparer.stringsEnCulture[i], "VerifyProperSortUsedForCasedStrings3: OriginalCulture/StringComparer" + originalCulture + "/" + originalComparer + "Expected key " + i + " to be " + Constructor_IDictionary_StringComparer.stringsEnCulture[i] + ", but found " + _dic.Keys[i]);
            }

            CultureInfo.DefaultThreadCurrentCulture = originalCulture;
            originalDic = new SortedList<string, string>(idic, originalComparer);
            CultureInfo.DefaultThreadCurrentCulture = _danish;
            _dic = new SortedList<string, string>(originalDic, StringComparer.CurrentCulture);
            m_test.Eval(_dic.Count == idic.Count, String.Format("Err_23497sg! Count different: {0}", _dic.Count));
            m_test.Eval(_dic.Keys.Count == idic.Count, String.Format("Err_25ag! Count different: {0}", _dic.Keys.Count));
            m_test.Eval(_dic.Values.Count == idic.Count, String.Format("Err_23agd! Count different: {0}", _dic.Values.Count));
            for (int i = 0; i < idic.Keys.Count; i++)
            {
                m_test.Eval(_dic.Keys[i] == Constructor_IDictionary_StringComparer.stringsDaCulture[i], "VerifyProperSortUsedForCasedStrings4: OriginalCulture/StringComparer" + originalCulture + "/" + originalComparer + "Expected key " + i + " to be " + Constructor_IDictionary_StringComparer.stringsDaCulture[i] + ", but found " + _dic.Keys[i]);
            }
        }

        public void VerifyProperSortUsedForNonCasedStrings(IDictionary<string, string> idic, StringComparer originalComparer, CultureInfo originalCulture)
        {
            SortedList<string, string> _dic;
            SortedList<string, string> originalDic;

            CultureInfo.DefaultThreadCurrentCulture = originalCulture;
            originalDic = new SortedList<string, string>(idic, originalComparer);
            CultureInfo.DefaultThreadCurrentCulture = _english;
            _dic = new SortedList<string, string>(originalDic, StringComparer.Ordinal);
            m_test.Eval(_dic.Count == idic.Count, String.Format("Err_23497sg! Count different: {0}", _dic.Count));
            m_test.Eval(_dic.Keys.Count == idic.Count, String.Format("Err_25ag! Count different: {0}", _dic.Keys.Count));
            m_test.Eval(_dic.Values.Count == idic.Count, String.Format("Err_23agd! Count different: {0}", _dic.Values.Count));
            for (int i = 0; i < idic.Keys.Count; i++)
            {
                m_test.Eval(_dic.Keys[i] == Constructor_IDictionary_StringComparer.stringsNoCaseOrdinal[i], "VerifyProperSortUsedForNonCasedStrings1: OriginalCulture/StringComparer" + originalCulture + "/" + originalComparer + "Expected key " + i + " to be " + Constructor_IDictionary_StringComparer.stringsNoCaseOrdinal[i] + ", but found " + _dic.Keys[i]);
            }

            CultureInfo.DefaultThreadCurrentCulture = originalCulture;
            originalDic = new SortedList<string, string>(idic, originalComparer);
            CultureInfo.DefaultThreadCurrentCulture = _danish;
            _dic = new SortedList<string, string>(originalDic, StringComparer.Ordinal);
            m_test.Eval(_dic.Count == idic.Count, String.Format("Err_23497sg! Count different: {0}", _dic.Count));
            m_test.Eval(_dic.Keys.Count == idic.Count, String.Format("Err_25ag! Count different: {0}", _dic.Keys.Count));
            m_test.Eval(_dic.Values.Count == idic.Count, String.Format("Err_23agd! Count different: {0}", _dic.Values.Count));
            for (int i = 0; i < idic.Keys.Count; i++)
            {
                m_test.Eval(_dic.Keys[i] == Constructor_IDictionary_StringComparer.stringsNoCaseOrdinal[i], "VerifyProperSortUsedForNonCasedStrings2: OriginalCulture/StringComparer" + originalCulture + "/" + originalComparer + "Expected key " + i + " to be " + Constructor_IDictionary_StringComparer.stringsNoCaseOrdinal[i] + ", but found " + _dic.Keys[i]);
            }

            CultureInfo.DefaultThreadCurrentCulture = originalCulture;
            originalDic = new SortedList<string, string>(idic, originalComparer);
            CultureInfo.DefaultThreadCurrentCulture = _english;
            _dic = new SortedList<string, string>(originalDic, StringComparer.CurrentCulture);
            m_test.Eval(_dic.Count == idic.Count, String.Format("Err_23497sg! Count different: {0}", _dic.Count));
            m_test.Eval(_dic.Keys.Count == idic.Count, String.Format("Err_25ag! Count different: {0}", _dic.Keys.Count));
            m_test.Eval(_dic.Values.Count == idic.Count, String.Format("Err_23agd! Count different: {0}", _dic.Values.Count));
            for (int i = 0; i < idic.Keys.Count; i++)
            {
                m_test.Eval(_dic.Keys[i] == Constructor_IDictionary_StringComparer.stringsNoCaseEnCulture[i], "VerifyProperSortUsedForNonCasedStrings3: OriginalCulture/StringComparer" + originalCulture + "/" + originalComparer + "Expected key " + i + " to be " + Constructor_IDictionary_StringComparer.stringsNoCaseEnCulture[i] + ", but found " + _dic.Keys[i]);
            }

            CultureInfo.DefaultThreadCurrentCulture = originalCulture;
            originalDic = new SortedList<string, string>(idic, originalComparer);
            CultureInfo.DefaultThreadCurrentCulture = _danish;
            _dic = new SortedList<string, string>(originalDic, StringComparer.CurrentCulture);
            m_test.Eval(_dic.Count == idic.Count, String.Format("Err_23497sg! Count different: {0}", _dic.Count));
            m_test.Eval(_dic.Keys.Count == idic.Count, String.Format("Err_25ag! Count different: {0}", _dic.Keys.Count));
            m_test.Eval(_dic.Values.Count == idic.Count, String.Format("Err_23agd! Count different: {0}", _dic.Values.Count));
            for (int i = 0; i < idic.Keys.Count; i++)
            {
                m_test.Eval(_dic.Keys[i] == Constructor_IDictionary_StringComparer.stringsNoCaseDaCulture[i], "VerifyProperSortUsedForNonCasedStrings4: OriginalCulture/StringComparer" + originalCulture + "/" + originalComparer + "Expected key " + i + " to be " + Constructor_IDictionary_StringComparer.stringsNoCaseDaCulture[i] + ", but found " + _dic.Keys[i]);
            }

            CultureInfo.DefaultThreadCurrentCulture = originalCulture;
            originalDic = new SortedList<string, string>(idic, originalComparer);
            CultureInfo.DefaultThreadCurrentCulture = _english;
            _dic = new SortedList<string, string>(originalDic, StringComparer.CurrentCultureIgnoreCase);
            try
            {
                _dic.Add("apple", "apple");
                m_test.Eval(false, "VerifyProperSortUsedForNonCasedStrings7: OriginalCulture/StringComparer" + originalCulture + "/" + originalComparer + "Expected adding apple to IgnoreCase that contains Apple to throw ArgumentException but it did not.");
            }
            catch (ArgumentException)
            {
            }
            m_test.Eval(_dic.Count == idic.Count, String.Format("Err_23497sg! Count different: {0}", _dic.Count));
            m_test.Eval(_dic.Keys.Count == idic.Count, String.Format("Err_25ag! Count different: {0}", _dic.Keys.Count));
            m_test.Eval(_dic.Values.Count == idic.Count, String.Format("Err_23agd! Count different: {0}", _dic.Values.Count));
            for (int i = 0; i < idic.Keys.Count; i++)
            {
                m_test.Eval(_dic.Keys[i] == Constructor_IDictionary_StringComparer.stringsNoCaseEnCulture[i], "VerifyProperSortUsedForNonCasedStrings8: OriginalCulture/StringComparer" + originalCulture + "/" + originalComparer + "Expected key " + i + " to be " + Constructor_IDictionary_StringComparer.stringsNoCaseEnCulture[i] + ", but found " + _dic.Keys[i]);
            }

            CultureInfo.DefaultThreadCurrentCulture = originalCulture;
            originalDic = new SortedList<string, string>(idic, originalComparer);
            CultureInfo.DefaultThreadCurrentCulture = _danish;
            _dic = new SortedList<string, string>(originalDic, StringComparer.CurrentCultureIgnoreCase);
            try
            {
                _dic.Add("apple", "apple");
                m_test.Eval(false, "VerifyProperSortUsedForNonCasedStrings9: OriginalCulture/StringComparer" + originalCulture + "/" + originalComparer + "Expected adding apple to IgnoreCase that contains Apple to throw ArgumentException but it did not.");
            }
            catch (ArgumentException)
            {
            }
            m_test.Eval(_dic.Count == idic.Count, String.Format("Err_23497sg! Count different: {0}", _dic.Count));
            m_test.Eval(_dic.Keys.Count == idic.Count, String.Format("Err_25ag! Count different: {0}", _dic.Keys.Count));
            m_test.Eval(_dic.Values.Count == idic.Count, String.Format("Err_23agd! Count different: {0}", _dic.Values.Count));
            for (int i = 0; i < idic.Keys.Count; i++)
            {
                m_test.Eval(_dic.Keys[i] == Constructor_IDictionary_StringComparer.stringsNoCaseDaCulture[i], "VerifyProperSortUsedForNonCasedStrings10: OriginalCulture/StringComparer" + originalCulture + "/" + originalComparer + "Expected key " + i + " to be " + Constructor_IDictionary_StringComparer.stringsNoCaseDaCulture[i] + ", but found " + _dic.Keys[i]);
            }
        }
    }

    public class Constructor_IDictionary_StringComparer
    {
        private static CultureInfo s_english = new CultureInfo("en");
        private static CultureInfo s_german = new CultureInfo("de");
        private static CultureInfo s_danish = new CultureInfo("da");
        private static CultureInfo s_turkish = new CultureInfo("tr");

        private static int s_count = 14;
        static public String[] strings = new String[s_count];
        static public String[] stringsOrdinal = new String[s_count];
        static public String[] stringsEnCulture = new String[s_count];
        static public String[] stringsDaCulture = new String[s_count];
        static public String[] stringsNoCase = new String[s_count];
        static public String[] stringsNoCaseOrdinal = new String[s_count];
        static public String[] stringsNoCaseEnCulture = new String[s_count];
        static public String[] stringsNoCaseDaCulture = new String[s_count];

        [Fact]
        [ActiveIssue(846, PlatformID.AnyUnix)]
        public static void RunTests()
        {
            //This mostly follows the format established by the original author of these tests

            Test test = new Test();

            Driver<String, String> driver1 = new Driver<String, String>(test);
            Driver<SimpleRef<int>, SimpleRef<String>> driver2 = new Driver<SimpleRef<int>, SimpleRef<String>>(test);
            Driver<SimpleRef<String>, SimpleRef<int>> driver3 = new Driver<SimpleRef<String>, SimpleRef<int>>(test);
            Driver<SimpleRef<int>, SimpleRef<int>> driver4 = new Driver<SimpleRef<int>, SimpleRef<int>>(test);
            Driver<SimpleRef<String>, SimpleRef<String>> driver5 = new Driver<SimpleRef<String>, SimpleRef<String>>(test);

            SimpleRef<int>[] simpleInts;
            SimpleRef<String>[] simpleStrings;

            SortedList<String, String> dic1;
            SortedList<SimpleRef<int>, SimpleRef<String>> dic2;
            SortedList<SimpleRef<String>, SimpleRef<int>> dic3;
            SortedList<SimpleRef<int>, SimpleRef<int>> dic4;
            SortedList<SimpleRef<String>, SimpleRef<String>> dic5;

            for (int i = 0; i < s_count; i++)
            {
                strings[i] = i.ToString();
                stringsOrdinal[i] = i.ToString();
                stringsEnCulture[i] = i.ToString();
                stringsDaCulture[i] = i.ToString();
                stringsNoCase[i] = i.ToString();
                stringsNoCaseOrdinal[i] = i.ToString();
                stringsNoCaseEnCulture[i] = i.ToString();
                stringsNoCaseDaCulture[i] = i.ToString();
            }
            strings[10] = "Apple";
            strings[11] = "\u00C6ble";
            strings[12] = "Zebra";
            strings[13] = "apple";
            stringsOrdinal[10] = "Apple";
            stringsOrdinal[11] = "Zebra";
            stringsOrdinal[12] = "apple";
            stringsOrdinal[13] = "\u00C6ble";
            stringsEnCulture[10] = "\u00C6ble";
            stringsEnCulture[11] = "apple";
            stringsEnCulture[12] = "Apple";
            stringsEnCulture[13] = "Zebra";
            stringsDaCulture[10] = "apple";
            stringsDaCulture[11] = "Apple";
            stringsDaCulture[12] = "Zebra";
            stringsDaCulture[13] = "\u00C6ble";

            stringsNoCase[10] = "Apple";
            stringsNoCase[11] = "\u00C6ble";
            stringsNoCase[12] = "Zebra";
            stringsNoCase[13] = "aapple";
            stringsNoCaseOrdinal[10] = "Apple";
            stringsNoCaseOrdinal[11] = "Zebra";
            stringsNoCaseOrdinal[12] = "aapple";
            stringsNoCaseOrdinal[13] = "\u00C6ble";
            stringsNoCaseEnCulture[10] = "aapple";
            stringsNoCaseEnCulture[11] = "\u00C6ble";
            stringsNoCaseEnCulture[12] = "Apple";
            stringsNoCaseEnCulture[13] = "Zebra";
            stringsNoCaseDaCulture[10] = "Apple";
            stringsNoCaseDaCulture[11] = "Zebra";
            stringsNoCaseDaCulture[12] = "\u00C6ble";
            stringsNoCaseDaCulture[13] = "aapple";

            simpleInts = GetSimpleInts(s_count);
            simpleStrings = GetSimpleStrings(s_count);

            simpleStrings[10].Val = "Apple";
            simpleStrings[11].Val = "\u00C6ble";
            simpleStrings[12].Val = "Zebra";
            simpleStrings[13].Val = "aapple";

            dic1 = FillValues(stringsNoCase, stringsNoCase);
            dic2 = FillValues(simpleInts, simpleStrings);
            dic3 = FillValues(simpleStrings, simpleInts);
            dic4 = FillValues(simpleInts, simpleInts);
            dic5 = FillValues(simpleStrings, simpleStrings);


            //Scenario 1: Pass all the enum values and ensure that the behavior is correct
            driver1.TestEnum(dic1);

            //Scenario 2: Parm validation: null
            driver1.TestParm();

            //Scenario 3: Non-string implementations and check

            //Scenario 4: ensure that SortedList items from the passed IDictionary object use the interface IKeyComparer's Equals and GetHashCode APIs. 
            //Ex. Pass the case invariant IKeyComparer and check
            //@TODO!!!

            //Scenario 5: Contradictory values and check: ex. IDictionary is case insensitive but IKeyComparer is not

            //Add SortedList that uses different type of StringComparer and verify
            SortedList<String, String> dicComparison1 = new SortedList<String, String>(StringComparer.CurrentCulture);
            SortedList<String, String> dicComparison2 = new SortedList<String, String>(StringComparer.CurrentCultureIgnoreCase);
            SortedList<String, String> dicComparison4 = new SortedList<String, String>(StringComparer.OrdinalIgnoreCase);
            SortedList<String, String> dicComparison5 = new SortedList<String, String>(StringComparer.Ordinal);

            dicComparison1 = FillValues(stringsNoCase, stringsNoCase);
            dicComparison2 = FillValues(stringsNoCase, stringsNoCase);
            dicComparison4 = FillValues(stringsNoCase, stringsNoCase);
            dicComparison5 = FillValues(stringsNoCase, stringsNoCase);

            //Add Dictionary that uses different type of StringComparer and verify
            Dictionary<String, String> justDicComparison1 = new Dictionary<String, String>();
            Dictionary<String, String> justDicComparison2 = new Dictionary<String, String>();

            justDicComparison1 = FillDictionaryValues(strings, strings);
            justDicComparison2 = FillDictionaryValues(stringsNoCase, stringsNoCase);
            //english original
            driver1.VerifyProperSortUsedForCasedStrings(justDicComparison1, StringComparer.CurrentCulture, s_english);
            driver1.VerifyProperSortUsedForCasedStrings(justDicComparison1, StringComparer.CurrentCultureIgnoreCase, s_english);
            driver1.VerifyProperSortUsedForCasedStrings(justDicComparison1, StringComparer.OrdinalIgnoreCase, s_english);
            driver1.VerifyProperSortUsedForCasedStrings(justDicComparison1, StringComparer.Ordinal, s_english);
            //danish original
            driver1.VerifyProperSortUsedForCasedStrings(justDicComparison1, StringComparer.CurrentCulture, s_danish);
            driver1.VerifyProperSortUsedForCasedStrings(justDicComparison1, StringComparer.CurrentCultureIgnoreCase, s_danish);
            driver1.VerifyProperSortUsedForCasedStrings(justDicComparison1, StringComparer.OrdinalIgnoreCase, s_danish);
            driver1.VerifyProperSortUsedForCasedStrings(justDicComparison1, StringComparer.Ordinal, s_danish);
            // german original
            driver1.VerifyProperSortUsedForCasedStrings(justDicComparison1, StringComparer.CurrentCulture, s_german);
            driver1.VerifyProperSortUsedForCasedStrings(justDicComparison1, StringComparer.CurrentCultureIgnoreCase, s_german);
            driver1.VerifyProperSortUsedForCasedStrings(justDicComparison1, StringComparer.OrdinalIgnoreCase, s_german);
            driver1.VerifyProperSortUsedForCasedStrings(justDicComparison1, StringComparer.Ordinal, s_german);
            // turkish original
            driver1.VerifyProperSortUsedForCasedStrings(justDicComparison1, StringComparer.CurrentCulture, s_turkish);
            driver1.VerifyProperSortUsedForCasedStrings(justDicComparison1, StringComparer.CurrentCultureIgnoreCase, s_turkish);
            driver1.VerifyProperSortUsedForCasedStrings(justDicComparison1, StringComparer.OrdinalIgnoreCase, s_turkish);
            driver1.VerifyProperSortUsedForCasedStrings(justDicComparison1, StringComparer.Ordinal, s_turkish);

            ////english original
            driver1.VerifyProperSortUsedForNonCasedStrings(justDicComparison2, StringComparer.CurrentCulture, s_english);
            driver1.VerifyProperSortUsedForNonCasedStrings(justDicComparison2, StringComparer.CurrentCultureIgnoreCase, s_english);
            driver1.VerifyProperSortUsedForNonCasedStrings(justDicComparison2, StringComparer.OrdinalIgnoreCase, s_english);
            driver1.VerifyProperSortUsedForNonCasedStrings(justDicComparison2, StringComparer.Ordinal, s_english);
            //danish original
            driver1.VerifyProperSortUsedForNonCasedStrings(justDicComparison2, StringComparer.CurrentCulture, s_danish);
            driver1.VerifyProperSortUsedForNonCasedStrings(justDicComparison2, StringComparer.CurrentCultureIgnoreCase, s_danish);
            driver1.VerifyProperSortUsedForNonCasedStrings(justDicComparison2, StringComparer.OrdinalIgnoreCase, s_danish);
            driver1.VerifyProperSortUsedForNonCasedStrings(justDicComparison2, StringComparer.Ordinal, s_danish);
            // german original
            driver1.VerifyProperSortUsedForNonCasedStrings(justDicComparison2, StringComparer.CurrentCulture, s_german);
            driver1.VerifyProperSortUsedForNonCasedStrings(justDicComparison2, StringComparer.CurrentCultureIgnoreCase, s_german);
            driver1.VerifyProperSortUsedForNonCasedStrings(justDicComparison2, StringComparer.OrdinalIgnoreCase, s_german);
            driver1.VerifyProperSortUsedForNonCasedStrings(justDicComparison2, StringComparer.Ordinal, s_german);
            // turkish original
            driver1.VerifyProperSortUsedForNonCasedStrings(justDicComparison2, StringComparer.CurrentCulture, s_turkish);
            driver1.VerifyProperSortUsedForNonCasedStrings(justDicComparison2, StringComparer.CurrentCultureIgnoreCase, s_turkish);
            driver1.VerifyProperSortUsedForNonCasedStrings(justDicComparison2, StringComparer.OrdinalIgnoreCase, s_turkish);
            driver1.VerifyProperSortUsedForNonCasedStrings(justDicComparison2, StringComparer.Ordinal, s_turkish);

            Assert.True(test.result);
        }

        private static SortedList<KeyType, ValueType> FillValues<KeyType, ValueType>(KeyType[] keys, ValueType[] values)
        {
            SortedList<KeyType, ValueType> _dic = new SortedList<KeyType, ValueType>();
            for (int i = 0; i < keys.Length; i++)
                _dic.Add(keys[i], values[i]);
            return _dic;
        }

        private static Dictionary<KeyType, ValueType> FillDictionaryValues<KeyType, ValueType>(KeyType[] keys, ValueType[] values)
        {
            Dictionary<KeyType, ValueType> _dic = new Dictionary<KeyType, ValueType>();
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
}