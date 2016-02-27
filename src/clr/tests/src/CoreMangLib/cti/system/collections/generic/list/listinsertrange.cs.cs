// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

/// <summary>
/// System.Collections.Generic.List.InsertRange(Int32,Collection)
/// </summary>
public class ListInsertRange
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: The generic type is int");

        try
        {
            int[] iArray = { 0, 1, 2, 3, 8, 9, 10, 11, 12, 13, 14 };
            List<int> listObject = new List<int>(iArray);
            int[] insert = { 4, 5, 6, 7 };
            listObject.InsertRange(4, insert);
            for (int i = 0; i < 15; i++)
            {
                if (listObject[i] != i)
                {
                    TestLibrary.TestFramework.LogError("001", "The result is not the value as expected,listObject is: " + listObject[i]);
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Insert the collection to the beginning of the list");

        try
        {
            string[] strArray = { "apple", "dog", "banana", "chocolate", "dog", "food" };
            List<string> listObject = new List<string>(strArray);
            string[] insert = { "Hello", "World" };
            listObject.InsertRange(0, insert);
            if (listObject.Count != 8)
            {
                TestLibrary.TestFramework.LogError("003", "The result is not the value as expected,Count is: " + listObject.Count);
                retVal = false;
            }
            if ((listObject[0] != "Hello") || (listObject[1] != "World"))
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Insert custom class array to the end of the list");

        try
        {
            MyClass myclass1 = new MyClass();
            MyClass myclass2 = new MyClass();
            MyClass myclass3 = new MyClass();
            MyClass myclass4 = new MyClass();
            MyClass myclass5 = new MyClass();
            MyClass[] mc = new MyClass[3] { myclass1, myclass2, myclass3 };
            List<MyClass> listObject = new List<MyClass>(mc);
            MyClass[] insert = new MyClass[2] { myclass4, myclass5 };
            listObject.InsertRange(3, insert);
            for (int i = 0; i < 5; i++)
            {
                if (i < 3)
                {
                    if (listObject[i] != mc[i])
                    {
                        TestLibrary.TestFramework.LogError("006", "The result is not the value as expected,i is: " + i);
                        retVal = false;
                    }
                }
                else
                {
                    if (listObject[i] != insert[i - 3])
                    {
                        TestLibrary.TestFramework.LogError("007", "The result is not the value as expected,i is: " + i);
                        retVal = false;
                    }
                }
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: The collection has null reference element");

        try
        {
            string[] strArray = { "apple", "dog", "banana", "food" };
            List<string> listObject = new List<string>(strArray);
            string[] insert = new string[2] { null, null };
            int index = this.GetInt32(0, 4);
            listObject.InsertRange(index, insert);
            if (listObject.Count != 6)
            {
                TestLibrary.TestFramework.LogError("009", "The result is not the value as expected,Count is: " + listObject.Count);
                retVal = false;
            }
            if ((listObject[index] != null) || (listObject[index + 1] != null))
            {
                TestLibrary.TestFramework.LogError("010", "The result is not the value as expected");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("011", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: The collection is a null reference");

        try
        {
            string[] strArray = { "apple", "dog", "banana", "food" };
            List<string> listObject = new List<string>(strArray);
            string[] insert = null;
            int index = this.GetInt32(0, 4);
            listObject.InsertRange(index, insert);
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

    public bool NegTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest2: The index is negative");

        try
        {
            int[] iArray = { 1, 9, 3, 6, -1, 8, 7, 1, 2, 4 };
            List<int> listObject = new List<int>(iArray);
            int[] insert = { -0, 90, 100 };
            listObject.InsertRange(-1, insert);
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

        TestLibrary.TestFramework.BeginScenario("NegTest3: The index is greater than the count of the list");

        try
        {
            int[] iArray = { 1, 9, 3, 6, -1, 8, 7, 1, 2, 4 };
            List<int> listObject = new List<int>(iArray);
            int[] insert = { -0, 90, 100 };
            listObject.InsertRange(11, insert);
            TestLibrary.TestFramework.LogError("105", "The ArgumentOutOfRangeException was not thrown as expected");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
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
        ListInsertRange test = new ListInsertRange();

        TestLibrary.TestFramework.BeginTestCase("ListInsertRange");

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
