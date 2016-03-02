// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

/// <summary>
/// System.Collections.Generic.List<T>.addRange(IEnumerable)
/// </summary>
public class ListAddRange
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: The item to be added is type of byte");

        try
        {
            byte[] byteObject = new byte[1000];
            TestLibrary.Generator.GetBytes(-55, byteObject);
            List<byte> listObject = new List<byte>();
            listObject.AddRange(byteObject);
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
            string[] strArray = { "Hello", "world", "Tom", "school" };
            List<string> listObject = new List<string>(1);
            listObject.AddRange(strArray);
            if (listObject.Count != 4)
            {
                TestLibrary.TestFramework.LogError("003", "The result is not the value as expected");
                retVal = false;
            }
            for (int i = 0; i < 4; i++)
            {
                if (listObject[i] != strArray[i])
                {
                    TestLibrary.TestFramework.LogError("004", "The result is not the value as expected");
                    retVal = false;
                }
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
            MyClass myClass1 = new MyClass();
            MyClass myClass2 = new MyClass();
            MyClass[] mc ={ myClass1, myClass2 };
            List<MyClass> listObject = new List<MyClass>();
            listObject.AddRange(mc);
            if (listObject[0] != myClass1 || (listObject[1] != myClass2))
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
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: The argument is a null reference");

        try
        {
            IEnumerable<string> i = null;
            List<string> listObject = new List<string>(100);
            listObject.AddRange(i);
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
        ListAddRange test = new ListAddRange();

        TestLibrary.TestFramework.BeginTestCase("ListAddRange");

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
