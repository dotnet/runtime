// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections;
using System.Globalization;
using Xunit;
using SortedList_SortedListUtils;

namespace SortedListCtorIntIKeyComp
{
    public class Driver<KeyType, ValueType>
    {
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

        public void TestVanilla(int capacity)
        {
            SortedList<String, String> _dic;
            IComparer<String> comparer;
            IComparer<String>[] predefinedComparers = new IComparer<String>[] {
                StringComparer.CurrentCulture,
                StringComparer.CurrentCultureIgnoreCase,
                StringComparer.OrdinalIgnoreCase,
                StringComparer.Ordinal};

            foreach (IComparer<String> predefinedComparer in predefinedComparers)
            {
                _dic = new SortedList<String, String>(capacity, predefinedComparer);
                Test.Eval(_dic.Comparer == predefinedComparer, String.Format("Err_4568aijueud! Comparer differ expected: {0} actual: {1}", predefinedComparer, _dic.Comparer));
                Test.Eval(_dic.Count == 0, String.Format("Err_23497sg! Count different: {0}", _dic.Count));
                Test.Eval(((IDictionary<KeyType, ValueType>)_dic).IsReadOnly == false, String.Format("Err_435wsdg! Count different: {0}", ((IDictionary<KeyType, ValueType>)_dic).IsReadOnly));
                Test.Eval(_dic.Keys.Count == 0, String.Format("Err_25ag! Count different: {0}", _dic.Keys.Count));
                Test.Eval(_dic.Values.Count == 0, String.Format("Err_23agd! Count different: {0}", _dic.Values.Count));
            }


            //Current culture
            CultureInfo.DefaultThreadCurrentCulture = _english;
            comparer = StringComparer.CurrentCulture;
            _dic = new SortedList<String, String>(capacity, comparer);
            Test.Eval(_dic.Comparer == comparer, String.Format("Err_848652ahued! Comparer differ expected: {0} actual: {1}", comparer, _dic.Comparer));
            _dic.Add(strAE, value);
            Test.Eval(!_dic.ContainsKey(strUC4), String.Format("Err_235rdag! Wrong result returned: {0}", _dic.ContainsKey(strUC4)));

            //bug #11263 in NDPWhidbey
            CultureInfo.DefaultThreadCurrentCulture = _german;
            comparer = StringComparer.CurrentCulture;
            _dic = new SortedList<String, String>(capacity, comparer);
            Test.Eval(_dic.Comparer == comparer, String.Format("Err_54848ahuede! Comparer differ expected: {0} actual: {1}", comparer, _dic.Comparer));
            _dic.Add(strAE, value);
            Test.Eval(!_dic.ContainsKey(strUC4), String.Format("Err_23r7ag! Wrong result returned: {0}", _dic.ContainsKey(strUC4)));

            //CurrentCultureIgnoreCase
            CultureInfo.DefaultThreadCurrentCulture = _english;
            comparer = StringComparer.CurrentCultureIgnoreCase;
            _dic = new SortedList<String, String>(capacity, comparer);
            Test.Eval(_dic.Comparer == comparer, String.Format("Err_788989ajeude! Comparer differ expected: {0} actual: {1}", comparer, _dic.Comparer));
            _dic.Add(straA, value);
            Test.Eval(_dic.ContainsKey(strAa), String.Format("Err_237g! Wrong result returned: {0}", _dic.ContainsKey(strAa)));

            CultureInfo.DefaultThreadCurrentCulture = _danish;
            comparer = StringComparer.CurrentCultureIgnoreCase;
            _dic = new SortedList<String, String>(capacity, comparer);
            Test.Eval(_dic.Comparer == comparer, String.Format("Err_54878aheuid! Comparer differ expected: {0} actual: {1}", comparer, _dic.Comparer));
            _dic.Add(straA, value);
            Test.Eval(!_dic.ContainsKey(strAa), String.Format("Err_0723f! Wrong result returned: {0}", _dic.ContainsKey(strAa)));

            //OrdinalIgnoreCase
            CultureInfo.DefaultThreadCurrentCulture = _english;
            comparer = StringComparer.OrdinalIgnoreCase;
            _dic = new SortedList<String, String>(capacity, comparer);
            Test.Eval(_dic.Comparer == comparer, String.Format("Err_5588ahied! Comparer differ expected: {0} actual: {1}", comparer, _dic.Comparer));
            _dic.Add(strI, value);
            Test.Eval(!_dic.ContainsKey(strTurkishUpperI), String.Format("Err_234qf! Wrong result returned: {0}", _dic.ContainsKey(strTurkishUpperI)));

            CultureInfo.DefaultThreadCurrentCulture = _turkish;
            comparer = StringComparer.OrdinalIgnoreCase;
            _dic = new SortedList<String, String>(capacity, comparer);
            Test.Eval(_dic.Comparer == comparer, String.Format("Err_8488ahiued! Comparer differ expected: {0} actual: {1}", comparer, _dic.Comparer));
            _dic.Add(strI, value);
            Test.Eval(!_dic.ContainsKey(strTurkishUpperI), String.Format("Err_234ra7g! Wrong result returned: {0}", _dic.ContainsKey(strTurkishUpperI)));

            //Ordinal - not that many meaningful test
            CultureInfo.DefaultThreadCurrentCulture = _english;
            comparer = StringComparer.Ordinal;
            _dic = new SortedList<String, String>(capacity, comparer);
            Test.Eval(_dic.Comparer == comparer, String.Format("Err_488ahede! Comparer differ expected: {0} actual: {1}", comparer, _dic.Comparer));
            _dic.Add(strBB, value);
            Test.Eval(!_dic.ContainsKey(strbb), String.Format("Err_1244sd! Wrong result returned: {0}", _dic.ContainsKey(strbb)));

            CultureInfo.DefaultThreadCurrentCulture = _danish;
            comparer = StringComparer.Ordinal;
            _dic = new SortedList<String, String>(capacity, comparer);
            Test.Eval(_dic.Comparer == comparer, String.Format("Err_05848ahied! Comparer differ expected: {0} actual: {1}", comparer, _dic.Comparer));
            _dic.Add(strBB, value);
            Test.Eval(!_dic.ContainsKey(strbb), String.Format("Err_235aeg! Wrong result returned: {0}", _dic.ContainsKey(strbb)));
        }

        public void TestParm()
        {
            //passing null will revert to the default comparison mechanism
            SortedList<String, String> _dic;
            IComparer<String> comparer = null;
            try
            {
                CultureInfo.DefaultThreadCurrentCulture = _english;
                _dic = new SortedList<String, String>(0, comparer);
                _dic.Add(straA, value);
                Test.Eval(!_dic.ContainsKey(strAa), String.Format("Err_9237g! Wrong result returned: {0}", _dic.ContainsKey(strAa)));

                CultureInfo.DefaultThreadCurrentCulture = _danish;
                _dic = new SortedList<String, String>(comparer);
                _dic.Add(straA, value);
                Test.Eval(!_dic.ContainsKey(strAa), String.Format("Err_90723f! Wrong result returned: {0}", _dic.ContainsKey(strAa)));
            }
            catch (Exception ex)
            {
                Test.Eval(false, String.Format("Err_387tsg! Wrong exception thrown: {0}", ex));
            }

            int[] negativeValues = { -1, -2, -5, Int32.MinValue };
            comparer = StringComparer.CurrentCulture;
            for (int i = 0; i < negativeValues.Length; i++)
            {
                try
                {
                    _dic = new SortedList<String, String>(negativeValues[i], comparer);
                    Test.Eval(false, String.Format("Err_387tsg! No exception thrown"));
                }
                catch (ArgumentOutOfRangeException)
                {
                }
                catch (Exception ex)
                {
                    Test.Eval(false, String.Format("Err_387tsg! Wrong exception thrown: {0}", ex));
                }
            }
        }

        public void IkeyComparerOwnImplementation(int capacity)
        {
            //This just ensure that we can call our own implementation
            SortedList<String, String> _dic;
            IComparer<String> comparer = new MyOwnIKeyImplementation<String>();
            try
            {
                CultureInfo.DefaultThreadCurrentCulture = _english;
                _dic = new SortedList<String, String>(capacity, comparer);
                _dic.Add(straA, value);
                Test.Eval(!_dic.ContainsKey(strAa), String.Format("Err_0237g! Wrong result returned: {0}", _dic.ContainsKey(strAa)));

                CultureInfo.DefaultThreadCurrentCulture = _danish;
                _dic = new SortedList<String, String>(comparer);
                _dic.Add(straA, value);
                Test.Eval(!_dic.ContainsKey(strAa), String.Format("Err_00723f! Wrong result returned: {0}", _dic.ContainsKey(strAa)));
            }
            catch (Exception ex)
            {
                Test.Eval(false, String.Format("Err_387tsg! Wrong exception thrown: {0}", ex));
            }
        }
    }

    public class Constructor_int_IKeyComparer
    {
        [Fact]
        public static void RunTests()
        {
            //This mostly follows the format established by the original author of these tests
            //These tests mostly uses the scenarios that were used in the individual constructors

            Driver<String, String> driver1 = new Driver<String, String>();

            //Scenario 1: Pass all the enum values and ensure that the behavior is correct
            int[] validCapacityValues = { 0, 1, 2, 5, 10, 16, 32, 50, 500, 5000, 10000 };
            for (int i = 0; i < validCapacityValues.Length; i++)
                driver1.TestVanilla(validCapacityValues[i]);

            //Scenario 2: Parm validation: null for IKeyComparer and negative for capacity
            driver1.TestParm();

            //Scenario 3: Implement our own IKeyComparer and check
            for (int i = 0; i < validCapacityValues.Length; i++)
                driver1.IkeyComparerOwnImplementation(validCapacityValues[i]);

            Assert.True(Test.result);
        }
    }

    //[Serializable]
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
}