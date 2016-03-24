// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

/// <summary>
/// System.Collections.Generic.List<T>.add(T)
/// </summary>
public class ListAdd
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

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: The item to be added is type of byte");

        try
        {
            byte[] byteObject = new byte[1000];
            TestLibrary.Generator.GetBytes(-55, byteObject);
            List<byte> listObject = new List<byte>();
            for (int i = 0; i < 1000; i++)
            {
                listObject.Add(byteObject[i]);
            }
            for (int i = 0; i < 1000; i++)
            {
                if (listObject[i] != byteObject[i])
                {
                    TestLibrary.TestFramework.LogError("001", "The result is not the value as expected,i is: " + i);
                    retVal = false;
                }
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: The item to be added is type of string");

        try
        {
            string[] strArray = { "Hello" };
            List<string> listObject = new List<string>(strArray);
            string str1 = "World";
            listObject.Add(str1);
            if (listObject.Count != 2)
            {
                TestLibrary.TestFramework.LogError("003", "The result is not the value as expected");
                retVal = false;
            }
            if (listObject[1] != "World")
            {
                TestLibrary.TestFramework.LogError("004", "The result is not the value as expected");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("005", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: The item to be added is a custom type");

        try
        {
            MyClass myClass = new MyClass();
            List<MyClass> listObject = new List<MyClass>();
            listObject.Add(myClass);
            if (listObject[0] != myClass)
            {
                TestLibrary.TestFramework.LogError("006", "The result is not the value as expected");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("007", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Add null object to the list");

        try
        {
            List<string> listObject = new List<string>(1);
            listObject.Add(null);
            if (listObject[0] != null)
            {
                TestLibrary.TestFramework.LogError("008", "The result is not the value as expected");
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
    #endregion
    #endregion

    public static int Main()
    {
        ListAdd test = new ListAdd();

        TestLibrary.TestFramework.BeginTestCase("ListAdd");

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
