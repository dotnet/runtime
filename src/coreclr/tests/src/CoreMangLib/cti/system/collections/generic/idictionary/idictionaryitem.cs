// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;


/// <summary>
/// System.Collections.Generic.IDictionary.Item(TKey)
/// </summary>
public class IDictionaryItem
{
    private int c_MINI_STRING_LENGTH = 1;
    private int c_MAX_STRING_LENGTH = 20;

    public static int Main(string[] args)
    {
        IDictionaryItem testObj = new IDictionaryItem();
        TestLibrary.TestFramework.BeginTestCase("Testing for Property: System.Collections.Generic.IDictionary.Item(TKey)");

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

        TestLibrary.TestFramework.LogInformation("[Netativ]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;

        return retVal;
    }
    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest1: Using Dictionary<TKey,TValue> which implemented the item property in IDictionay<TKey,TValue> and TKey is int...";
        const string c_TEST_ID = "P001";

        Dictionary<int, int> dictionary = new Dictionary<int, int>();
        int key = TestLibrary.Generator.GetInt32(-55);
        int value = TestLibrary.Generator.GetInt32(-55);
        int newValue = TestLibrary.Generator.GetInt32(-55);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            ((IDictionary<int, int>)dictionary)[key] = value;
            if (((IDictionary<int, int>)dictionary)[key] != value)
            {
                string errorDesc = "Value is not " + value + " as expected: Actual(" + dictionary[key] + ")";
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

            ((IDictionary<int, int>)dictionary)[key] = newValue;
            if (((IDictionary<int, int>)dictionary)[key] != newValue)
            {
                string errorDesc = "Value is not " + newValue + " as expected: Actual(" + dictionary[key] + ")";
                TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003", "Unecpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest2: Using Dictionary<TKey,TValue> which implemented the item property in IDictionay<TKey,TValue> and TKey is String...";
        const string c_TEST_ID = "P002";

        Dictionary<String, String> dictionary = new Dictionary<String, String>();
        String key = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
        String value = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
        dictionary.Add(key, value);

        String newValue = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (((IDictionary<String, String>)dictionary)[key] != value)
            {
                string errorDesc = "Value is not " + value + " as expected: Actual(" + dictionary[key] + ")";
                TestLibrary.TestFramework.LogError("004" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

            ((IDictionary<String, String>)dictionary)[key] = newValue;
            if (((IDictionary<String, String>)dictionary)[key] != newValue)
            {
                string errorDesc = "Value is not " + newValue + " as expected: Actual(" + dictionary[key] + ")";
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

    public bool PosTest3()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest3: Using Dictionary<TKey,TValue> which implemented the item property in IDictionay<TKey,TValue> and TKey is customer class...";
        const string c_TEST_ID = "P003";

        Dictionary<MyClass, int> dictionary = new Dictionary<MyClass, int>();
        MyClass key = new MyClass();
        int value = TestLibrary.Generator.GetInt32(-55);
        dictionary.Add(key, value);

        int newValue = TestLibrary.Generator.GetInt32(-55);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (((IDictionary<MyClass, int>)dictionary)[key] != value)
            {
                string errorDesc = "Value is not " + value + " as expected: Actual(" + dictionary[key] + ")";
                TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

            ((IDictionary<MyClass, int>)dictionary)[key] = newValue;
            if (((IDictionary<MyClass, int>)dictionary)[key] != newValue)
            {
                string errorDesc = "Value is not " + newValue + " as expected: Actual(" + dictionary[key] + ")";
                TestLibrary.TestFramework.LogError("008" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("009", "Unecpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest4: Using customer class which implemented the item property in IDictionay<TKey,TValue>...";
        const string c_TEST_ID = "P004";

        MyDictionary<int, int> dictionary = new MyDictionary<int, int>();
        int key = TestLibrary.Generator.GetInt32(-55);
        int value = TestLibrary.Generator.GetInt32(-55);
        int newValue = TestLibrary.Generator.GetInt32(-55);
        dictionary.Add(key, value);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {

            if (((IDictionary<int, int>)dictionary)[key] != value)
            {
                string errorDesc = "Value is not " + value + " as expected: Actual(" + dictionary[key] + ")";
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

            ((IDictionary<int, int>)dictionary)[key] = newValue;
            if (((IDictionary<int, int>)dictionary)[key] != newValue)
            {
                string errorDesc = "Value is not " + newValue + " as expected: Actual(" + dictionary[key] + ")";
                TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "Unecpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "NegTest1: Using Dictionary<TKey,TValue> which implemented the item property in IDictionay<TKey,TValue> and Key is a null reference...";
        const string c_TEST_ID = "N001";


        Dictionary<String, int> dictionary = new Dictionary<String, int>();
        String key = null;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            int value = ((IDictionary<String, int>)dictionary)[key];
            TestLibrary.TestFramework.LogError("013" + "TestId-" + c_TEST_ID, "The ArgumentNullException was not thrown as expected");
            retVal = false;
        }
        catch (ArgumentNullException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;
        const string c_TEST_DESC = "NegTest2: Using Dictionary<TKey,TValue> which implemented the item property in IDictionay<TKey,TValue> and Key is a null reference...";
        const string c_TEST_ID = "N002";


        Dictionary<String, int> dictionary = new Dictionary<String, int>();
        String key = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            int value = ((IDictionary<String, int>)dictionary)[key];
            TestLibrary.TestFramework.LogError("015" + "TestId-" + c_TEST_ID, "The KeyNotFoundException was not thrown as expected");
            retVal = false;
        }
        catch (KeyNotFoundException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("016", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest3()
    {
        bool retVal = true;
        const string c_TEST_DESC = "NegTest3: Using user-defined class which implemented the add method in IDictionay<TKey,TValue> and ReadOnly is true";
        const string c_TEST_ID = "N003";

        MyDictionary<String, int> dictionary = new MyDictionary<String, int>();
        String key = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
        int value = TestLibrary.Generator.GetInt32(-55);
        int newValue = TestLibrary.Generator.GetInt32(-55);
        dictionary.Add(key, value);

        dictionary.readOnly = true;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            ((IDictionary<String, int>)dictionary)[key] = newValue;
            TestLibrary.TestFramework.LogError("017" + " TestId-" + c_TEST_ID, "The NotSupportedException was not thrown as expected");
            retVal = false;
        }
        catch (NotSupportedException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("018", "Unexpected exception: " + e);
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
                   throw  new NotSupportedException();

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
