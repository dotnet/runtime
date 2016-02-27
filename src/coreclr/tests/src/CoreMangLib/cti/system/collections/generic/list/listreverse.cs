// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

/// <summary>
/// System.Collections.Generic.List.Reverse
/// </summary>
public class ListReverse
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: The generic type is byte");

        try
        {
            byte[] byArray = new byte[1000];
            TestLibrary.Generator.GetBytes(-55, byArray);
            List<byte> listObject = new List<byte>(byArray);
            byte[] expected = this.reverse<byte>(byArray);
            listObject.Reverse();
            for (int i = 0; i < 1000; i++)
            {
                if (listObject[i] != expected[i])
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: The generic type is type of string");

        try
        {
            string[] strArray = { "dog", "apple", "joke", "banana", "chocolate", "dog", "food", "Microsoft" };
            List<string> listObject = new List<string>(strArray);
            listObject.Reverse();
            string[] expected = this.reverse<string>(strArray);
            for (int i = 0; i < 8; i++)
            {
                if (listObject[i] != expected[i])
                {
                    TestLibrary.TestFramework.LogError("003", "The result is not the value as expected,i is: " + i);
                    retVal = false;
                }
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
            MyClass myclass4 = new MyClass();
            MyClass[] mc = new MyClass[4] { myclass1, myclass2, myclass3, myclass4 };
            List<MyClass> listObject = new List<MyClass>(mc);
            listObject.Reverse();
            MyClass[] expected = new MyClass[4] { myclass4, myclass3, myclass2, myclass1 };
            for (int i = 0; i < 4; i++)
            {
                if (listObject[i] != expected[i])
                {
                    TestLibrary.TestFramework.LogError("005", "The result is not the value as expected,i is: " + i);
                    retVal = false;
                }
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

        TestLibrary.TestFramework.BeginScenario("PosTest4: The list has no element");

        try
        {
            List<int> listObject = new List<int>();
            listObject.Reverse();
            if (listObject.Count != 0)
            {
                TestLibrary.TestFramework.LogError("007", "The result is not the value as expected,count is: " + listObject.Count);
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
    #endregion

    #region Nagetive Test Cases
    #endregion
    #endregion

    public static int Main()
    {
        ListReverse test = new ListReverse();

        TestLibrary.TestFramework.BeginTestCase("ListReverse");

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
    #region useful method
    private T[] reverse<T>(T[] arrayT)
    {
        T temp;
        int times = arrayT.Length / 2;
        for (int i = 0; i < times; i++)
        {
            temp = arrayT[i];
            arrayT[i] = arrayT[arrayT.Length - 1 - i];
            arrayT[arrayT.Length - 1 - i] = temp;
        }
        return arrayT;
    }
    #endregion
}
public class MyClass
{
}
