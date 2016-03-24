// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

/// <summary>
/// System.Collections.Generic.List.Reverse(int32,int32)
/// </summary>
public class ListReverse2
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
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: The generic type is byte");

        try
        {
            byte[] byArray = new byte[100];
            TestLibrary.Generator.GetBytes(-55, byArray);
            List<byte> listObject = new List<byte>(byArray);
            byte[] expected = this.reverse<byte>(byArray);
            listObject.Reverse(10, 80);
            for (int i = 0; i < 100; i++)
            {
                if ((i < 10) || (i > 89))
                {
                    if (listObject[i] != byArray[i])
                    {
                        TestLibrary.TestFramework.LogError("001", "The result is not the value as expected,i is: " + i);
                        retVal = false;
                    }
                }
                else
                {
                    if (listObject[i] != expected[i])
                    {
                        TestLibrary.TestFramework.LogError("002", "The result is not the value as expected,i is: " + i);
                        retVal = false;
                    }
                }
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: The generic type is type of string");

        try
        {
            string[] strArray = { "dog", "apple", "joke", "banana", "chocolate", "dog", "food", "Microsoft" };
            List<string> listObject = new List<string>(strArray);
            listObject.Reverse(2, 5);
            string[] expected = { "dog", "apple", "food", "dog", "chocolate", "banana", "joke", "Microsoft" };
            for (int i = 0; i < 8; i++)
            {
                if (listObject[i] != expected[i])
                {
                    TestLibrary.TestFramework.LogError("004", "The result is not the value as expected,i is: " + i);
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: The generic type is a custom type");

        try
        {
            MyClass myclass1 = new MyClass();
            MyClass myclass2 = new MyClass();
            MyClass myclass3 = new MyClass();
            MyClass myclass4 = new MyClass();
            MyClass[] mc = new MyClass[4] { myclass1, myclass2, myclass3, myclass4 };
            List<MyClass> listObject = new List<MyClass>(mc);
            listObject.Reverse(0, 2);
            MyClass[] expected = new MyClass[4] { myclass2, myclass1, myclass3, myclass4 };
            for (int i = 0; i < 4; i++)
            {
                if (listObject[i] != expected[i])
                {
                    TestLibrary.TestFramework.LogError("006", "The result is not the value as expected,i is: " + i);
                    retVal = false;
                }
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

        TestLibrary.TestFramework.BeginScenario("PosTest4: The list has no element");

        try
        {
            List<int> listObject = new List<int>();
            listObject.Reverse(0, 0);
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
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: The index is a negative number");

        try
        {
            int[] iArray = { 1, 9, 3, 6, -1, 8, 7, 10, 2, 4 };
            List<int> listObject = new List<int>(iArray);
            listObject.Reverse(-1, 3);
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

    public bool NegTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest2: The count is a negative number");

        try
        {
            int[] iArray = { 1, 9, 3, 6, -1, 8, 7, 10, 2, 4 };
            List<int> listObject = new List<int>(iArray);
            listObject.Reverse(3, -2);
            TestLibrary.TestFramework.LogError("103", "The ArgumentOutOfRangeException was not thrown as expected");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("104", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest3: index and count do not denote a valid range of elements in the List");

        try
        {
            string[] strArray = { "dog", "apple", "joke", "banana", "chocolate", "dog", "food" };
            List<string> listObject = new List<string>(strArray);
            listObject.Reverse(3, 10);
            TestLibrary.TestFramework.LogError("105", "The ArgumentException was not thrown as expected");
            retVal = false;
        }
        catch (ArgumentException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("106", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        ListReverse2 test = new ListReverse2();

        TestLibrary.TestFramework.BeginTestCase("ListReverse2");

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
    private T[] reverse<T>(T[] array)
    {
        T temp;
        T[] arrayT = new T[array.Length];
        array.CopyTo(arrayT, 0);
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
