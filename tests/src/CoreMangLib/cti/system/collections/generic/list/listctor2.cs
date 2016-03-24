// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// System.Collections.Generic.List<T>.Ctor(IEnumerable<T>)
/// </summary>
public class ListCtor2
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

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: The genaric type is a value type");

        try
        {
            int[] intArray = new int[5] { 1, 2, 3, 4, 5 };
            List<int> listObject = new List<int>(intArray);
            if (listObject == null)
            {
                TestLibrary.TestFramework.LogError("001", "The constructor does not work well");
                retVal = false;
            }
            if (listObject.Count != 5)
            {
                TestLibrary.TestFramework.LogError("002", "The result is not the value as expected");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: The generic type is a reference type");

        try
        {
            string[] stringArray = { "Hello", "world", "thanks", "school" };
            List<string> listObject = new List<string>(stringArray);
            if (listObject == null)
            {
                TestLibrary.TestFramework.LogError("004", "The constructor does not work well");
                retVal = false;
            }
            if (listObject.Count != 4)
            {
                TestLibrary.TestFramework.LogError("005", "The result is not the value as expected");
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

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: The generic type is a custom type");

        try
        {
            int length = TestLibrary.Generator.GetByte(-55);
            MyClass[] myClass = new MyClass[length];
            List<MyClass> listObject = new List<MyClass>(myClass);
            if (listObject == null)
            {
                TestLibrary.TestFramework.LogError("007", "The constructor does not work well");
                retVal = false;
            }
            if (listObject.Count != length)
            {
                TestLibrary.TestFramework.LogError("008", "The result is not the value as expected,the count is: " + listObject.Count + ",The length is: " + length);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("009", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Using a list to construct another list");

        try
        {
            int[] iArray = new int[10] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0 };
            List<int> listObject1 = new List<int>(iArray);
            List<int> listObject2 = new List<int>(listObject1);
            if (listObject2 == null)
            {
                TestLibrary.TestFramework.LogError("010", "The constructor does not work well");
                retVal = false;
            }
            if (listObject2.Count != 10)
            {
                TestLibrary.TestFramework.LogError("011", "The result is not the value as expected");
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
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: The argument is a null reference");

        try
        {
            IEnumerable<char> i = null;
            List<char> listObject = new List<char>(i);
            TestLibrary.TestFramework.LogError("101", "The ArgumentNullException was not thrown as expected");
            retVal = false;
        }
        catch (ArgumentNullException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        ListCtor2 test = new ListCtor2();

        TestLibrary.TestFramework.BeginTestCase("ListCtor2");

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
}
public class MyClass
{
}
