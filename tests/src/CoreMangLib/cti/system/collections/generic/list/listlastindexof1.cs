// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

/// <summary>
/// System.Collections.Generic.List.LastIndexOf(T item)
/// </summary>
public class ListLastIndexOf1
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;
        retVal = PosTest5() && retVal;
        retVal = PosTest6() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: The generic type is int");

        try
        {
            int[] iArray = new int[1000];
            for (int i = 0; i < 1000; i++)
            {
                iArray[i] = i;
            }
            List<int> listObject = new List<int>(iArray);
            int ob = this.GetInt32(0, 1000);
            int result = listObject.LastIndexOf(ob);
            if (result != ob)
            {
                TestLibrary.TestFramework.LogError("001", "The result is not the value as expected,result is: " + result);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: The generic type is type of string");

        try
        {
            string[] strArray = { "apple", "banana", "dog", "chocolate", "dog", "food" };
            List<string> listObject = new List<string>(strArray);
            int result = listObject.LastIndexOf("dog");
            if (result != 4)
            {
                TestLibrary.TestFramework.LogError("003", "The result is not the value as expected,result is: " + result);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: The generic type is a custom type");

        try
        {
            MyClass myclass1 = new MyClass();
            MyClass myclass2 = new MyClass();
            MyClass myclass3 = new MyClass();
            MyClass[] mc = new MyClass[5] { myclass1, myclass2, myclass3, myclass3, myclass2 };
            List<MyClass> listObject = new List<MyClass>(mc);
            int result = listObject.LastIndexOf(myclass3);
            if (result != 3)
            {
                TestLibrary.TestFramework.LogError("005", "The result is not the value as expected,result is: " + result);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: There are many element in the list with the same value");

        try
        {
            string[] strArray = { "apple", "banana", "chocolate", "banana", "banana", "dog", "banana", "food" };
            List<string> listObject = new List<string>(strArray);
            int result = listObject.LastIndexOf("banana");
            if (result != 6)
            {
                TestLibrary.TestFramework.LogError("007", "The result is not the value as expected,result is: " + result);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest5: Do not find the element ");

        try
        {
            int[] iArray = { 1, 9, 3, 6, -1, 8, 7, 1, 2, 4 };
            List<int> listObject = new List<int>(iArray);
            int result = listObject.LastIndexOf(-10000);
            if (result != -1)
            {
                TestLibrary.TestFramework.LogError("009", "The result is not the value as expected,result is: " + result);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest6()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest6: The argument is a null reference");

        try
        {
            string[] strArray = { "apple", "banana", "chocolate" };
            List<string> listObject = new List<string>(strArray);
            int result = listObject.LastIndexOf(null);
            if (result != -1)
            {
                TestLibrary.TestFramework.LogError("011", "The result is not the value as expected,result is: " + result);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    #endregion
    #endregion

    public static int Main()
    {
        ListLastIndexOf1 test = new ListLastIndexOf1();

        TestLibrary.TestFramework.BeginTestCase("ListLastIndexOf1");

        if (test.RunTests())
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
    private Int32 GetInt32(Int32 minValue, Int32 maxValue)
    {
        try
        {
            if (minValue == maxValue)
            {
                return minValue;
            }
            if (minValue < maxValue)
            {
                return minValue + TestLibrary.Generator.GetInt32(-55) % (maxValue - minValue);
            }
        }
        catch
        {
            throw;
        }

        return minValue;
    }
}
public class MyClass
{
}
