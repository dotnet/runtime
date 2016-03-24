// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;


/// <summary>
/// System.Collections.Generic.IDictionary.Keys
/// </summary>
public class IDictionaryKeys
{
    private int c_MINI_STRING_LENGTH = 1;
    private int c_MAX_STRING_LENGTH = 20;

    public static int Main(string[] args)
    {
        IDictionaryKeys testObj = new IDictionaryKeys();
        TestLibrary.TestFramework.BeginTestCase("Testing for Property: System.Collections.Generic.IDictionary.Keys");

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

        return retVal;
    }
    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest1: Using Dictionary<TKey,TValue> which implemented the Keys property in IDictionay<TKey,TValue> and TKey is int...";
        const string c_TEST_ID = "P001";

        Dictionary<int, int> dictionary = new Dictionary<int, int>();
        int key = TestLibrary.Generator.GetInt32(-55);
        int value = TestLibrary.Generator.GetInt32(-55);
        dictionary.Add(key, value);
        key = key + 1;
        dictionary.Add(key,value);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            ICollection<int> keys = ((IDictionary<int, int>)dictionary).Keys;
            if (keys.Count != 2)
            {
                string errorDesc = "( the count of IDictionary<int, int>.Keys 2 as expected: Actual(" + keys.Count + ")";
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

            if (!keys.Contains(key))
            {
                string errorDesc = key + "should exist in (IDictionary<int, int>.Keys";
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
        const string c_TEST_DESC = "PosTest2: Using Dictionary<TKey,TValue> which implemented the Keys property in IDictionay<TKey,TValue> and TKey is String...";
        const string c_TEST_ID = "P002";

        Dictionary<String, String> dictionary = new Dictionary<String, String>();
        String key = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
        String value = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
        dictionary.Add(key, value);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            ICollection<String> keys = ((IDictionary<String, String>)dictionary).Keys;
            if (keys.Count != 1)
            {
                string errorDesc = "( the count of IDictionary<int, int>.Keys 1 as expected: Actual(" + keys.Count + ")";
                TestLibrary.TestFramework.LogError("004" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

            if (!keys.Contains(key))
            {
                string errorDesc = key + "should exist in (IDictionary<String, String>.Keys";
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
        const string c_TEST_DESC = "PosTest3: Using Dictionary<TKey,TValue> which implemented the Keys property in IDictionay<TKey,TValue> and TKey is customer class...";
        const string c_TEST_ID = "P003";

        Dictionary<MyClass, int> dictionary = new Dictionary<MyClass, int>();
        MyClass key = new MyClass();
        int value = TestLibrary.Generator.GetInt32(-55);
        dictionary.Add(key, value);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            ICollection<MyClass> keys = ((IDictionary<MyClass, int>)dictionary).Keys;
            if (keys.Count != 1)
            {
                string errorDesc = "( the count of IDictionary<int, int>.Keys 1 as expected: Actual(" + keys.Count + ")";
                TestLibrary.TestFramework.LogError("004" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

            if (!keys.Contains(key))
            {
                string errorDesc = "MyClass object should exist in (IDictionary<String, String>.Keys";
                TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, errorDesc);
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
        const string c_TEST_DESC = "PosTest4: Using Dictionary<TKey,TValue> which implemented the Keys property in IDictionay<TKey,TValue> and Keys is empty...";
        const string c_TEST_ID = "P004";

        Dictionary<int, int> dictionary = new Dictionary<int, int>();
        int key = TestLibrary.Generator.GetInt32(-55);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            ICollection<int> keys = ((IDictionary<int, int>)dictionary).Keys;
            if (keys.Count != 0)
            {
                string errorDesc = "( the count of IDictionary<int, int>.Keys 0 as expected: Actual(" + keys.Count + ")";
                TestLibrary.TestFramework.LogError("010" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

            if (keys.Contains(key))
            {
                string errorDesc = key + "should not exist in (IDictionary<int, int>.Keys";
                TestLibrary.TestFramework.LogError("011" + " TestId-" + c_TEST_ID, errorDesc);
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

    #region Help Class
    public class MyClass
    { }
    #endregion
}
