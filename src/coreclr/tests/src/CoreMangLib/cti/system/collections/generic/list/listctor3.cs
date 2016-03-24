// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

/// <summary>
/// System.Collections.Generic.List<T>.Ctor(Int32)
/// </summary>
public class ListCtor3
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;

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
            int i = TestLibrary.Generator.GetInt16(-55);
            List<int> listObject = new List<int>(i);
            if (listObject == null)
            {
                TestLibrary.TestFramework.LogError("001", "The constructor does not work well");
                retVal = false;
            }
            if (listObject.Capacity != i)
            {
                TestLibrary.TestFramework.LogError("002", string.Format("The result is not the value as expected,capacity is {0},i is{1}", listObject.Capacity, i));
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
            int i = TestLibrary.Generator.GetInt16(-55);
            List<string> listObject = new List<string>(i);
            if (listObject == null)
            {
                TestLibrary.TestFramework.LogError("004", "The constructor does not work well");
                retVal = false;
            }
            if (listObject.Capacity != i)
            {
                TestLibrary.TestFramework.LogError("005", string.Format("The result is not the value as expected,capacity is {0},i is{1}", listObject.Capacity, i));
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
            int i = TestLibrary.Generator.GetInt16(-55);
            List<MyClass> listObject = new List<MyClass>(i);
            if (listObject == null)
            {
                TestLibrary.TestFramework.LogError("007", "The constructor does not work well");
                retVal = false;
            }
            if (listObject.Capacity != i)
            {
                TestLibrary.TestFramework.LogError("008", string.Format("The result is not the value as expected,capacity is {0},i is{1}", listObject.Capacity, i));
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
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: The argument is a negative number");

        try
        {
            int i = (-1) * (TestLibrary.Generator.GetInt16(-55));
            List<char> listObject = new List<char>(i);
            TestLibrary.TestFramework.LogError("101", "The ArgumentOutOfRangeException was not thrown as expected");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
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
        ListCtor3 test = new ListCtor3();

        TestLibrary.TestFramework.BeginTestCase("ListCtor3");

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
