// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;
using SL = SortedList_SortedListUtils;
using SortedList_ICollection;
using TestSupport.Common_TestSupport;
using TestSupport.Collections.SortedList_GenericICollectionTest;
using TestSupport.Collections.SortedList_GenericIEnumerableTest;

namespace SortedListKeys
{
    public class Driver<KeyType, ValueType>
    {
        private Test m_test;

        public Driver(Test test)
        {
            m_test = test;
        }

        public void TestVanilla(KeyType[] keys, ValueType[] values)
        {
            SortedList<KeyType, ValueType> _dic = new SortedList<KeyType, ValueType>();
            for (int i = 0; i < keys.Length - 1; i++)
                _dic.Add(keys[i], values[i]);
            ICollection<KeyType> _col = _dic.Keys;
            m_test.Eval(_col.Count == _dic.Count, String.Format("Err_3497gs! Not equal {0}", _col.Count));
            IEnumerator<KeyType> _enum = _col.GetEnumerator();
            int count = 0;
            while (_enum.MoveNext())
            {
                m_test.Eval(_dic.ContainsKey(_enum.Current), String.Format("Err_3497gs! Not equal {0}", _dic.ContainsKey(_enum.Current)));
                count++;
            }
            m_test.Eval(count == _dic.Count, String.Format("Err_3497gs! Not equal {0}", count));
            KeyType[] _keys = new KeyType[_dic.Count];
            _col.CopyTo(_keys, 0);
            for (int i = 0; i < keys.Length - 1; i++)
                m_test.Eval(_dic.ContainsKey(_keys[i]), String.Format("Err_3497gs! Not equal {0}", _dic.ContainsKey(_keys[i])));

            count = 0;
            foreach (KeyType currKey in _dic.Keys)
            {
                m_test.Eval(_dic.ContainsKey(currKey), String.Format("Err_53497gs! Not equal {0}", _dic.ContainsKey(currKey)));
                count++;
            }
            m_test.Eval(count == _dic.Count, String.Format("Err_3497gs! Not equal {0}", count));

            try
            {
                //The behavior here is undefined as long as we don't AV were fine
                KeyType item = _enum.Current;
            }
            catch (Exception) { }

            if (keys.Length > 0)
            {
                _dic.Add(keys[keys.Length - 1], values[values.Length - 1]);

                try
                {
                    _enum.MoveNext();
                    m_test.Eval(false, "Expected InvalidOperationException, but got no exception.");
                }
                catch (InvalidOperationException)
                {
                }
                catch (Exception E)
                {
                    m_test.Eval(false, "Expected InvalidOperationException, but got unknown exception: " + E);
                }
            }
        }

        public void TestModify(KeyType[] keys, ValueType[] values, KeyType[] newKeys)
        {
            SortedList<KeyType, ValueType> _dic = new SortedList<KeyType, ValueType>();
            for (int i = 0; i < keys.Length; i++)
                _dic.Add(keys[i], values[i]);
            ICollection<KeyType> _col = _dic.Keys;
            for (int i = 0; i < keys.Length; i++)
                _dic.Remove(keys[i]);

            m_test.Eval(_col.Count == 0, String.Format("Err_3497gs! Not equal {0}", _col.Count));
            IEnumerator<KeyType> _enum = _col.GetEnumerator();
            int count = 0;
            while (_enum.MoveNext())
            {
                m_test.Eval(_dic.ContainsKey(_enum.Current), String.Format("Err_3497gs! Not equal {0}", _dic.ContainsKey(_enum.Current)));
                count++;
            }
            m_test.Eval(count == 0, String.Format("Err_3497gs! Not equal {0}", count));

            for (int i = 0; i < keys.Length; i++)
                _dic.Add(newKeys[i], values[i]);

            m_test.Eval(_col.Count == _dic.Count, String.Format("Err_3497gs! Not equal {0}", _col.Count));
            _enum = _col.GetEnumerator();
            count = 0;
            while (_enum.MoveNext())
            {
                m_test.Eval(_dic.ContainsKey(_enum.Current), String.Format("Err_3497gs! Not equal {0}", _dic.ContainsKey(_enum.Current)));
                count++;
            }
            m_test.Eval(count == _dic.Count, String.Format("Err_3497gs! Not equal {0}", count));
            KeyType[] _keys = new KeyType[_dic.Count];
            _col.CopyTo(_keys, 0);
            for (int i = 0; i < keys.Length; i++)
                m_test.Eval(_dic.ContainsKey(_keys[i]), String.Format("Err_3497gs! Not equal {0}", _dic.ContainsKey(_keys[i])));
        }

        /**	public void TestNonExistentKeys(KeyType[] keys, ValueType[] values, KeyType[] nonExistentKeys)
            {
                SortedList<KeyType, ValueType> _dic = new SortedList<KeyType, ValueType>();
                for(int i=0; i<keys.Length; i++)
                    _dic.Add(keys[i], values[i]);
                for(int i=0; i<nonExistentKeys.Length; i++)
                {
                    try{
                        ValueType v = _dic[nonExistentKeys[i]];
                        m_test.Eval(false, "Err_23raf! Exception not thrown");
                    }catch(ArgumentException){
                    }catch(Exception ex){
                        m_test.Eval(false, String.Format("Err_387tsg! Wrong exception thrown: {0}", ex));
                    }
                }
            }

            public void TestParm(KeyType[] keys, ValueType[] values, KeyType value)
            {
                SortedList<KeyType, ValueType> _dic = new SortedList<KeyType, ValueType>();
                try{
                    ValueType v = _dic[value];
                    m_test.Eval(false, "Err_23raf! Exception not thrown");
                }catch(ArgumentNullException){
                }catch(Exception ex){
                    m_test.Eval(false, String.Format("Err_387tsg! Wrong exception thrown: {0}", ex));
                }
            }
        **/

        public void NonGenericIDictionaryTestVanilla(KeyType[] keys, ValueType[] values)
        {
            SortedList<KeyType, ValueType> _dic = new SortedList<KeyType, ValueType>();
            IDictionary _idic = _dic;
            for (int i = 0; i < keys.Length - 1; i++)
                _dic.Add(keys[i], values[i]);
            ICollection _col = _idic.Keys;
            m_test.Eval(_col.Count == _dic.Count, String.Format("Err_3497gs! Not equal {0}", _col.Count));
            IEnumerator _enum = _col.GetEnumerator();
            int count = 0;
            while (_enum.MoveNext())
            {
                m_test.Eval(_dic.ContainsKey((KeyType)_enum.Current), String.Format("Err_3497gs! Not equal {0}", _dic.ContainsKey((KeyType)_enum.Current)));
                count++;
            }
            m_test.Eval(count == _dic.Count, String.Format("Err_3497gs! Not equal {0}", count));
            KeyType[] _keys = new KeyType[_dic.Count];
            _col.CopyTo(_keys, 0);
            for (int i = 0; i < keys.Length - 1; i++)
                m_test.Eval(_dic.ContainsKey(_keys[i]), String.Format("Err_3497gs! Not equal {0}", _dic.ContainsKey(_keys[i])));

            _enum.Reset();

            count = 0;
            while (_enum.MoveNext())
            {
                m_test.Eval(_dic.ContainsKey((KeyType)_enum.Current), String.Format("Err_3497gs! Not equal {0}", _dic.ContainsKey((KeyType)_enum.Current)));
                count++;
            }
            m_test.Eval(count == _dic.Count, String.Format("Err_3497gs! Not equal {0}", count));
            _keys = new KeyType[_dic.Count];
            _col.CopyTo(_keys, 0);
            for (int i = 0; i < keys.Length - 1; i++)
                m_test.Eval(_dic.ContainsKey(_keys[i]), String.Format("Err_3497gs! Not equal {0}", _dic.ContainsKey(_keys[i])));

            try
            {
                _dic.ContainsKey((KeyType)_enum.Current);
                m_test.Eval(false, "Expected InvalidOperationException, but got no exception.");
            }
            catch (InvalidOperationException)
            {
            }
            catch (Exception E)
            {
                m_test.Eval(false, "Expected InvalidOperationException, but got unknown exception: " + E);
            }


            if (keys.Length > 0)
            {
                _dic.Add(keys[keys.Length - 1], values[values.Length - 1]);

                try
                {
                    _enum.MoveNext();
                    m_test.Eval(false, "Expected InvalidOperationException, but got no exception.");
                }
                catch (InvalidOperationException)
                {
                }
                catch (Exception E)
                {
                    m_test.Eval(false, "Expected InvalidOperationException, but got unknown exception: " + E);
                }

                try
                {
                    _enum.Reset();
                    m_test.Eval(false, "Expected InvalidOperationException, but got no exception.");
                }
                catch (InvalidOperationException)
                {
                }
                catch (Exception E)
                {
                    m_test.Eval(false, "Expected InvalidOperationException, but got unknown exception: " + E);
                }
            }
        }

        public void NonGenericIDictionaryTestModify(KeyType[] keys, ValueType[] values, KeyType[] newKeys)
        {
            SortedList<KeyType, ValueType> _dic = new SortedList<KeyType, ValueType>();
            IDictionary _idic = _dic;
            for (int i = 0; i < keys.Length; i++)
                _dic.Add(keys[i], values[i]);
            ICollection _col = _idic.Keys;
            for (int i = 0; i < keys.Length; i++)
                _dic.Remove(keys[i]);

            m_test.Eval(_col.Count == 0, String.Format("Err_3497gs! Not equal {0}", _col.Count));
            IEnumerator _enum = _col.GetEnumerator();
            int count = 0;
            while (_enum.MoveNext())
            {
                m_test.Eval(_dic.ContainsKey((KeyType)_enum.Current), String.Format("Err_3497gs! Not equal {0}", _dic.ContainsKey((KeyType)_enum.Current)));
                count++;
            }
            m_test.Eval(count == 0, String.Format("Err_3497gs! Not equal {0}", count));

            for (int i = 0; i < keys.Length; i++)
                _dic.Add(newKeys[i], values[i]);

            m_test.Eval(_col.Count == _dic.Count, String.Format("Err_3497gs! Not equal {0}", _col.Count));
            _enum = _col.GetEnumerator();
            count = 0;
            while (_enum.MoveNext())
            {
                m_test.Eval(_dic.ContainsKey((KeyType)_enum.Current), String.Format("Err_3497gs! Not equal {0}", _dic.ContainsKey((KeyType)_enum.Current)));
                count++;
            }
            m_test.Eval(count == _dic.Count, String.Format("Err_3497gs! Not equal {0}", count));
            KeyType[] _keys = new KeyType[_dic.Count];
            _col.CopyTo(_keys, 0);
            for (int i = 0; i < keys.Length; i++)
                m_test.Eval(_dic.ContainsKey(_keys[i]), String.Format("Err_3497gs! Not equal {0}", _dic.ContainsKey(_keys[i])));
        }

        public void TestVanillaIListReturned(KeyType[] keys, ValueType[] values, KeyType valueNotInList)
        {
            SortedList<KeyType, ValueType> _dic = new SortedList<KeyType, ValueType>();
            IList<KeyType> _ilist;
            for (int i = 0; i < keys.Length; i++)
                _dic.Add(keys[i], values[i]);
            _ilist = _dic.Keys;
            //IsReadOnly
            m_test.Eval(_ilist.IsReadOnly == true, "Expected IsReadOnly of IList of Values to be true, but found " + _ilist.IsReadOnly);

            //This get
            for (int i = 0; i < keys.Length; i++)
            {
                m_test.Eval(Array.IndexOf(keys, _ilist[i]) != -1, "Expected This at " + i + " to be found in original array , but it was not");
            }

            try
            {
                Console.WriteLine(_ilist[-1]);
                m_test.Eval(false, "Expected ArgumentOutOfRangeException, but found value of " + _ilist[-1]);
            }
            catch (ArgumentOutOfRangeException)
            {
            }
            catch (Exception E)
            {
                m_test.Eval(false, "Expected ArgumentOutOfRangeException, but found " + E);
            }

            try
            {
                Console.WriteLine(_ilist[keys.Length]);
                m_test.Eval(false, "Expected ArgumentOutOfRangeException, but found value of " + _ilist[keys.Length]);
            }
            catch (ArgumentOutOfRangeException)
            {
            }
            catch (Exception E)
            {
                m_test.Eval(false, "Expected ArgumentOutOfRangeException, but found " + E);
            }

            //Add
            try
            {
                _ilist.Add(keys[keys.Length - 1]);
                m_test.Eval(false, "Expected NotSupportedException, but was able to Add a value with no key");
            }
            catch (NotSupportedException)
            {
            }
            catch (Exception E)
            {
                m_test.Eval(false, "Expected NotSupportedException, but found " + E);
            }

            //Clear
            try
            {
                _ilist.Clear();
                m_test.Eval(false, "Expected NotSupportedException, but was able to Clear a value list");
            }
            catch (NotSupportedException)
            {
            }
            catch (Exception E)
            {
                m_test.Eval(false, "Expected NotSupportedException, but found " + E);
            }

            //Contains
            for (int i = 0; i < keys.Length; i++)
            {
                m_test.Eval(_ilist.Contains(keys[i]), "Expected Contains of item " + i + " with value " + keys[i] + " to return true, but found false");
            }

            //IndexOf
            for (int i = 0; i < keys.Length; i++)
            {
                m_test.Eval(_ilist.IndexOf(keys[i]) < keys.Length && _ilist.IndexOf(keys[i]) >= 0, "Expected IndexOf of item " + i + " with value " + keys[i] + " to return something within the allowed length but found " + _ilist.IndexOf(keys[i]));
            }

            m_test.Eval(_ilist.IndexOf(valueNotInList) == -1, "Expected IndexOf of item not in list, " + valueNotInList + " to return -1, but found " + _ilist.IndexOf(valueNotInList));

            //if(!typeof(KeyType).IsSubclassOf(typeof(System.ValueType)))
            if (!(keys[0] is System.ValueType))
            {
                try
                {
                    _ilist.IndexOf((KeyType)(Object)null);
                    m_test.Eval(false, "Expected ArgumentNullException when attempting to find IndexOf for null, but did not get one.");
                }
                catch (ArgumentNullException)
                {
                }
                catch (Exception E)
                {
                    m_test.Eval(false, "Expected ArgumentNullException when attempting to find IndexOf for null, but got unknown exception: " + E);
                }
            }

            //Insert
            try
            {
                _ilist.Insert(0, keys[keys.Length - 1]);
                m_test.Eval(false, "Expected NotSupportedException, but was able to Insert a value with no key");
            }
            catch (NotSupportedException)
            {
            }
            catch (Exception E)
            {
                m_test.Eval(false, "Expected NotSupportedException, but found " + E);
            }

            //Remove
            try
            {
                _ilist.Remove(keys[keys.Length - 1]);
                m_test.Eval(false, "Expected NotSupportedException, but was able to Insert a value with no key");
            }
            catch (NotSupportedException)
            {
            }
            catch (Exception E)
            {
                m_test.Eval(false, "Expected NotSupportedException, but found " + E);
            }

            //RemoveAt
            try
            {
                _ilist.RemoveAt(0);
                m_test.Eval(false, "Expected NotSupportedException, but was able to Insert a value with no key");
            }
            catch (NotSupportedException)
            {
            }
            catch (Exception E)
            {
                m_test.Eval(false, "Expected NotSupportedException, but found " + E);
            }

            //This set		
            try
            {
                _ilist[keys.Length - 1] = keys[keys.Length - 1];
                m_test.Eval(false, "Expected NotSupportedException, but was able to assign via This a value with no key");
            }
            catch (NotSupportedException)
            {
            }
            catch (Exception E)
            {
                m_test.Eval(false, "Expected NotSupportedException, but found " + E);
            }

            try
            {
                _ilist[-1] = keys[keys.Length - 1];
                m_test.Eval(false, "Expected NotSupportedException, but found value of " + _ilist[-1]);
            }
            catch (NotSupportedException)
            {
            }
            catch (Exception E)
            {
                m_test.Eval(false, "Expected NotSupportedException, but found " + E);
            }

            try
            {
                _ilist[keys.Length] = keys[keys.Length - 1];
                m_test.Eval(false, "Expected NotSupportedException, but found value of " + _ilist[keys.Length]);
            }
            catch (NotSupportedException)
            {
            }
            catch (Exception E)
            {
                m_test.Eval(false, "Expected NotSupportedException, but found " + E);
            }
        }

        public void TestVanillaICollectionReturned(KeyType[] keys, ValueType[] values)
        {
            SortedList<KeyType, ValueType> _dic = new SortedList<KeyType, ValueType>();
            KeyType[] arrayToCheck = new KeyType[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                arrayToCheck[i] = keys[i];
                _dic.Add(keys[i], values[i]);
            }
            Array.Sort(arrayToCheck);
            var tester = new ICollectionTester<KeyType>();
            tester.RunTest(m_test, ((IDictionary)_dic).Keys, keys.Length, false, ((IDictionary)_dic).SyncRoot, arrayToCheck);
        }

        public bool VerifyICollection_T(GenerateItem<KeyType> keyGenerator, GenerateItem<ValueType> valueGenerator, int numItems)
        {
            Dictionary<KeyType, ValueType> d = new Dictionary<KeyType, ValueType>();
            KeyType[] keys = new KeyType[numItems];
            ICollection_T_Test<KeyType> iCollectionTest;
            bool retValue = true;

            for (int i = 0; i < numItems; ++i)
            {
                keys[i] = keyGenerator();
                d.Add(keys[i], valueGenerator());
            }

            iCollectionTest = new ICollection_T_Test<KeyType>(m_test, d.Keys, keyGenerator, keys, true);


            iCollectionTest.ItemsMustBeUnique = true;
            iCollectionTest.ItemsMustBeNonNull = default(KeyType) == null;
            iCollectionTest.CollectionOrder = TestSupport.CollectionOrder.Unspecified;

            retValue &= m_test.Eval(iCollectionTest.RunAllTests(), "Err_98382apeuie System.Collections.Generic.ICollection<KeyType> tests FAILED");

            return retValue;
        }
    }

    public class get_Keys
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
        public static void RunTests()
        {
            Test test = new Test();

            IntGenerator intGenerator = new IntGenerator();
            StringGenerator stringGenerator = new StringGenerator();

            intGenerator.NextValue();
            stringGenerator.NextValue();

            //This mostly follows the format established by the original author of these tests

            //Scenario 1: Vanilla - fill in an SortedList with 10 keys and check this property

            Driver<int, int> IntDriver = new Driver<int, int>(test);
            Driver<SL.SimpleRef<String>, SL.SimpleRef<String>> simpleRef = new Driver<SL.SimpleRef<String>, SL.SimpleRef<String>>(test);
            Driver<SL.SimpleRef<int>, SL.SimpleRef<int>> simpleVal = new Driver<SL.SimpleRef<int>, SL.SimpleRef<int>>(test);

            SL.SimpleRef<int>[] simpleInts;
            SL.SimpleRef<String>[] simpleStrings;
            int[] ints;
            int count;

            count = 1000;
            simpleInts = SL.SortedListUtils.GetSimpleInts(count);
            simpleStrings = SL.SortedListUtils.GetSimpleStrings(count);
            ints = new int[count];
            for (int i = 0; i < count; i++)
                ints[i] = i;

            IntDriver.TestVanilla(ints, ints);
            simpleRef.TestVanilla(simpleStrings, simpleStrings);
            simpleVal.TestVanilla(simpleInts, simpleInts);
            IntDriver.NonGenericIDictionaryTestVanilla(ints, ints);
            simpleRef.NonGenericIDictionaryTestVanilla(simpleStrings, simpleStrings);
            simpleVal.NonGenericIDictionaryTestVanilla(simpleInts, simpleInts);
            IntDriver.TestVanillaIListReturned(ints, ints, -1);
            simpleRef.TestVanillaIListReturned(simpleStrings, simpleStrings, new SL.SimpleRef<string>("bozo"));
            simpleVal.TestVanillaIListReturned(simpleInts, simpleInts, new SL.SimpleRef<int>(-1));
            IntDriver.TestVanillaICollectionReturned(ints, ints);
            simpleRef.TestVanillaICollectionReturned(simpleStrings, simpleStrings);
            simpleVal.TestVanillaICollectionReturned(simpleInts, simpleInts);

            //Scenario 2: Check for an empty SortedList
            IntDriver.TestVanilla(new int[0], new int[0]);
            simpleRef.TestVanilla(new SL.SimpleRef<String>[0], new SL.SimpleRef<String>[0]);
            simpleVal.TestVanilla(new SL.SimpleRef<int>[0], new SL.SimpleRef<int>[0]);
            IntDriver.NonGenericIDictionaryTestVanilla(new int[0], new int[0]);
            simpleRef.NonGenericIDictionaryTestVanilla(new SL.SimpleRef<String>[0], new SL.SimpleRef<String>[0]);
            simpleVal.NonGenericIDictionaryTestVanilla(new SL.SimpleRef<int>[0], new SL.SimpleRef<int>[0]);


            //Scenario 3: Check the underlying reference. Change the SortedList afterwards and examine ICollection keys and make sure that the 
            //change is reflected
            SL.SimpleRef<int>[] simpleInts_1;
            SL.SimpleRef<String>[] simpleStrings_1;
            int[] ints_1;
            SL.SimpleRef<int>[] simpleInts_2;
            SL.SimpleRef<String>[] simpleStrings_2;
            int[] ints_2;

            int half = count / 2;
            simpleInts_1 = new SL.SimpleRef<int>[half];
            simpleStrings_1 = new SL.SimpleRef<String>[half];
            ints_2 = new int[half];
            simpleInts_2 = new SL.SimpleRef<int>[half];
            simpleStrings_2 = new SL.SimpleRef<String>[half];
            ints_1 = new int[half];
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

            //Scenario 4: Change keys via ICollection (how?) and examine SortedList
            //How indeed?

            //Verify ICollection<K> through ICollection testing suite
            Driver<int, string> intStringDriver = new Driver<int, string>(test);
            Driver<string, int> stringIntDriver = new Driver<string, int>(test);

            test.Eval(intStringDriver.VerifyICollection_T(new GenerateItem<int>(intGenerator.NextValue),
                new GenerateItem<string>(stringGenerator.NextValue), 0),
                "Err_085184aehdke Test Int32, String Empty Dictionary FAILED\n");

            test.Eval(intStringDriver.VerifyICollection_T(new GenerateItem<int>(intGenerator.NextValue),
                new GenerateItem<string>(stringGenerator.NextValue), 1),
                "Err_05164anhekjd Test Int32, String Dictionary with 1 item FAILED\n");

            test.Eval(intStringDriver.VerifyICollection_T(new GenerateItem<int>(intGenerator.NextValue),
                new GenerateItem<string>(stringGenerator.NextValue), 16),
                "Err_1088ajeid Test Int32, String Dictionary with 16 items FAILED\n");

            test.Eval(stringIntDriver.VerifyICollection_T(new GenerateItem<string>(stringGenerator.NextValue),
                new GenerateItem<int>(intGenerator.NextValue), 0),
                "Err_31288ajkekd Test String, Int32 Empty Dictionary FAILED\n");

            test.Eval(stringIntDriver.VerifyICollection_T(new GenerateItem<string>(stringGenerator.NextValue),
                new GenerateItem<int>(intGenerator.NextValue), 1),
                "Err_0215548aheuid Test String, Int32 Dictionary with 1 item FAILED\n");

            test.Eval(stringIntDriver.VerifyICollection_T(new GenerateItem<string>(stringGenerator.NextValue),
                new GenerateItem<int>(intGenerator.NextValue), 16),
                "Err_21057ajeipzd Test String, Int32 Dictionary with 16 items FAILED\n");

            Assert.True(test.Pass);
        }
    }
}