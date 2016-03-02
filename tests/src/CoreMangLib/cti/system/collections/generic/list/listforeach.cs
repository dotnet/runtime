// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

/// <summary>
/// System.Collections.Generic.List.ForEach(predicate)
/// </summary>
public class ListForEach
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: The generic type is int");

        try
        {
            int[] iArray = { 1, 9, 3, 6, -1, 8, 7, 1, 2, 4 };
            List<int> listObject = new List<int>(iArray);
            MyClass myClass = new MyClass();
            Action<int> action = new Action<int>(myClass.sumcalc);
            listObject.ForEach(action);
            if (myClass.sum != 40)
            {
                TestLibrary.TestFramework.LogError("001", "The result is not the value as expected,sum is: " + myClass.sum);
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
            string[] strArray = { "Hello", "wor", "l", "d" };
            List<string> listObject = new List<string>(strArray);
            MyClass myClass = new MyClass();
            Action<string> action = new Action<string>(myClass.joinstr);
            listObject.ForEach(action);
            if (myClass.result != "Helloworld")
            {
                TestLibrary.TestFramework.LogError("003", "The result is not the value as expected,sum is: " + myClass.sum);
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: The generic type is custom type");

        try
        {
            MyClass2 myclass1 = new MyClass2('h');
            MyClass2 myclass2 = new MyClass2('=');
            MyClass2 myclass3 = new MyClass2('&');
            MyClass2[] mc = new MyClass2[3] { myclass1, myclass2, myclass3 };
            List<MyClass2> listObject = new List<MyClass2>(mc);
            MyClass myClass = new MyClass();
            Action<MyClass2> action = new Action<MyClass2>(myClass.deletevalue);
            listObject.ForEach(action);
            for (int i = 0; i < 3; i++)
            {
                if (mc[i].value != null)
                {
                    TestLibrary.TestFramework.LogError("005", "The result is not the value as expected,sum is: " + myClass.sum);
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
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: The action is a null reference");

        try
        {
            int[] iArray = { 1, 9, 3, 6, -1, 8, 7, 1, 2, 4 };
            List<int> listObject = new List<int>(iArray);
            Action<int> action = null;
            listObject.ForEach(action);
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
        ListForEach test = new ListForEach();

        TestLibrary.TestFramework.BeginTestCase("ListForEach");

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
    public int sum = 0;
    public string result;
    public void sumcalc(int a)
    {
        sum = sum + a;
    }
    public void joinstr(string a)
    {
        result = result + a;
    }
    public void deletevalue(MyClass2 mc)
    {
        mc.value = null;
    }
}
public class MyClass2
{
    public char? value;
    public MyClass2(char c)
    {
        this.value = c;
    }
}