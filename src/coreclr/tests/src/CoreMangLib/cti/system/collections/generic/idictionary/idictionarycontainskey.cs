// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;


/// <summary>
/// System.Collections.Generic.IDictionary.ContainsKey(TKey)
/// </summary>
public class IDictionaryContainsKey
{
    private int c_MINI_STRING_LENGTH = 1;
    private int c_MAX_STRING_LENGTH = 20;

    public static int Main(string[] args)
    {
        IDictionaryContainsKey testObj = new IDictionaryContainsKey();
        TestLibrary.TestFramework.BeginTestCase("Testing for Methord: System.Collections.Generic.IDictionary.ContainsKey(TKey)");

        if (testObj.RunTests())
        {
            TestLibrary.TestFramework.EndTestCase();
            TestLibrary.TestFramework.LogInformation("PASS");
            return 100;
        }
        else
        {
            TestLibrary.TestFramework.EndTestCase();
            TestLibrary.TestFramework.LogInformation("FAIL");
            return 0;
        }
    }

    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;
        retVal = PosTest5() && retVal;

        TestLibrary.TestFramework.LogInformation("[Netativ]");
        retVal = NegTest1() && retVal;

        return retVal;
    }
    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest1: Using Dictionary<TKey,TValue> which implemented the ContainsKey method in IDictionay<TKey,TValue> and TKey is int...";
        const string c_TEST_ID = "P001";

        Dictionary<int, int> dictionary = new Dictionary<int, int>();
        int key = TestLibrary.Generator.GetInt32(-55);
        int value = TestLibrary.Generator.GetInt32(-55);
        ((IDictionary<int, int>)dictionary).Add(key, value);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {

            if (!((IDictionary<int, int>)dictionary).ContainsKey(key))
            {
                string errorDesc = "Value is not false as expected: Actual is true";
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unecpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest2: Using Dictionary<TKey,TValue> which implemented the ContainsKey method in IDictionay<TKey,TValue> and TKey is String...";
        const string c_TEST_ID = "P002";

        Dictionary<String, String> dictionary = new Dictionary<String, String>();
        String key = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
        String value = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
        dictionary.Add(key, value);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (!((IDictionary<String, String>)dictionary).ContainsKey(key))
            {
                string errorDesc = "Value is not false as expected: Actual is true";
                TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unecpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest3: Using Dictionary<TKey,TValue> which implemented the ContainsKey method in IDictionay<TKey,TValue> and TKey is customer class...";
        const string c_TEST_ID = "P003";

        Dictionary<MyClass, MyClass> dictionary = new Dictionary<MyClass, MyClass>();
        MyClass key = new MyClass();
        MyClass value = new MyClass();
        dictionary.Add(key, value);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {

            if (!((IDictionary<MyClass, MyClass>)dictionary).ContainsKey(key))
            {
                string errorDesc = "Value is not false as expected: Actual is true";
                TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unecpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest4: Using Dictionary<TKey,TValue> which implemented the ContainsKey method in IDictionay<TKey,TValue> and TKey isn't contained in IDictionary...";
        const string c_TEST_ID = "P004";

        Dictionary<String, String> dictionary = new Dictionary<String, String>();
        String key = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (((IDictionary<String, String>)dictionary).ContainsKey(key))
            {
                string errorDesc = "Value is not true as expected: Actual is false";
                TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unecpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest5: Using customer class which implemented the ContainsKey method in IDictionay<TKey,TValue>...";
        const string c_TEST_ID = "P005";

        MyDictionary<int, int> dictionary = new MyDictionary<int, int>();
        int key = TestLibrary.Generator.GetInt32(-55);
        int value = TestLibrary.Generator.GetInt32(-55);
        dictionary.Add(key, value);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {

            if (!((IDictionary<int, int>)dictionary).ContainsKey(key))
            {
                string errorDesc = "Value is not false as expected: Actual is true";
                TestLibrary.TestFramework.LogError("009" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unecpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "NegTest1: Using Dictionary<TKey,TValue> which implemented the ContainsKey method in IDictionay<TKey,TValue> and Key is a null reference...";
        const string c_TEST_ID = "N001";


        Dictionary<String, int> dictionary = new Dictionary<String, int>();
        String key = null;
        int value = TestLibrary.Generator.GetInt32(-55);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            ((IDictionary<String, int>)dictionary).ContainsKey(key);
            TestLibrary.TestFramework.LogError("011" + "TestId-" + c_TEST_ID, "The ArgumentNullException was not thrown as expected");
            retVal = false;
        }
        catch (ArgumentNullException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Help Class
    public class MyDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private int count;
        private int capacity = 10;
        public bool readOnly = false;
        private KeyValuePair<TKey, TValue>[] keyvaluePair;

        public MyDictionary()
        {
            count = 0;
            keyvaluePair = new KeyValuePair<TKey, TValue>[capacity];
        }
        #region IDictionary<TKey,TValue> Members

        public void Add(TKey key, TValue value)
        {
            if (readOnly)
                throw new NotSupportedException();

            if (ContainsKey(key))
                throw new ArgumentException();
            try
            {
                KeyValuePair<TKey, TValue> pair = new KeyValuePair<TKey, TValue>(key, value);
                keyvaluePair[count] = pair;
                count++;
            }
            catch (Exception en)
            {
                throw en;
            }
        }

        public bool ContainsKey(TKey key)
        {
            bool exist = false;

            if (key == null)
                throw new ArgumentNullException();

            foreach (KeyValuePair<TKey, TValue> pair in keyvaluePair)
            {
                if (pair.Key != null && pair.Key.Equals(key))
                {
                    exist = true;
                }
            }

            return exist;
        }

        public ICollection<TKey> Keys
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public bool Remove(TKey key)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public ICollection<TValue> Values
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public TValue this[TKey key]
        {
            get
            {
                if (!ContainsKey(key))

                    throw new KeyNotFoundException();


                int index = -1;
                for (int j = 0; j < count; j++)
                {
                    KeyValuePair<TKey, TValue> pair = keyvaluePair[j];
                    if (pair.Key.Equals(key))
                    {
                        index = j;
                        break;
                    }
                }

                return keyvaluePair[index].Value;

            }
            set
            {
                if (readOnly)
                   throw new NotSupportedException();

                if (ContainsKey(key))
                {
                    int index = -1;
                    for (int j = 0; j < count; j++)
                    {
                        KeyValuePair<TKey, TValue> pair = keyvaluePair[j];
                        if (pair.Key.Equals(key))
                        {
                            index = j;
                            break;
                        }
                    }

                    KeyValuePair<TKey, TValue> newpair = new KeyValuePair<TKey, TValue>(key, value);
                    keyvaluePair[index] = newpair;

                }
                else
                {
                    KeyValuePair<TKey, TValue> pair = new KeyValuePair<TKey, TValue>(key, value);
                    keyvaluePair[count] = pair;
                    count++;
                }
            }
        }

        #endregion

        #region ICollection<KeyValuePair<TKey,TValue>> Members

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void Clear()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public int Count
        {
            get { return count; }
        }

        public bool IsReadOnly
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion

        #region IEnumerable<KeyValuePair<TKey,TValue>> Members

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion
    }
    public class MyClass
    { }
    #endregion
}
