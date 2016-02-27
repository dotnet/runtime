// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

/// <summary>
/// System.Collections.Generic.List.GetRange(Int32,Int32)
/// </summary>
public class ListGetRange
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
            int[] iArray = { 1, 9, 3, 6, -1, 8, 7, 1, 2, 4 };
            List<int> listObject = new List<int>(iArray);
            int startIdx = this.GetInt32(0, 9);     //The starting index of the section to make a shallow copy
            int endIdx = this.GetInt32(startIdx, 10);//The end index of the section to make a shallow copy
            int count = endIdx - startIdx + 1;
            List<int> listResult = listObject.GetRange(startIdx, count);
            for (int i = 0; i < count; i++)
            {
                if (listResult[i] != iArray[i + startIdx])
                {
                    TestLibrary.TestFramework.LogError("001", "The result is not the value as expected,result is: " + listResult[i] + " expected value is: " + iArray[i + startIdx]);
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
            string[] strArray = { "apple", "banana", "chocolate", "dog", "food" };
            List<string> listObject = new List<string>(strArray);
            int startIdx = this.GetInt32(0, 4);     //The starting index of the section to make a shallow copy
            int endIdx = this.GetInt32(startIdx, 5);//The end index of the section to make a shallow copy
            int count = endIdx - startIdx + 1;
            List<string> listResult = listObject.GetRange(startIdx, count);
            for (int i = 0; i < count; i++)
            {
                if (listResult[i] != strArray[i + startIdx])
                {
                    TestLibrary.TestFramework.LogError("003", "The result is not the value as expected,result is: " + listResult[i] + " expected value is: " + strArray[i + startIdx]);
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
            MyClass[] mc = new MyClass[3] { myclass1, myclass2, myclass3 };
            List<MyClass> listObject = new List<MyClass>(mc);
            int startIdx = this.GetInt32(0, 2);     //The starting index of the section to make a shallow copy
            int endIdx = this.GetInt32(startIdx, 3);//The end index of the section to make a shallow copy
            int count = endIdx - startIdx + 1;
            List<MyClass> listResult = listObject.GetRange(startIdx, count);
            for (int i = 0; i < count; i++)
            {
                if (listResult[i] != mc[i + startIdx])
                {
                    TestLibrary.TestFramework.LogError("005", "The result is not the value as expected,result is: " + listResult[i] + " expected value is: " + mc[i + startIdx]);
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

        TestLibrary.TestFramework.BeginScenario("PosTest4: Copy no elements to the new list");

        try
        {
            int[] iArray = { 1, 9, 3, 6, -1, 8, 7, 1, 2, 4 };
            List<int> listObject = new List<int>(iArray);
            List<int> listResult = listObject.GetRange(5, 0);
            if (listResult == null)
            {
                TestLibrary.TestFramework.LogError("007", "The result is not the value as expected");
                retVal = false;
            }
            if (listResult.Count != 0)
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
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: The index is a negative number");

        try
        {
            int[] iArray = { 1, 9, 3, 6, -1, 8, 7, 1, 2, 4 };
            List<int> listObject = new List<int>(iArray);
            List<int> listResult = listObject.GetRange(-1, 4);
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
            int[] iArray = { 1, 9, 3, 6, -1, 8, 7, 1, 2, 4 };
            List<int> listObject = new List<int>(iArray);
            List<int> listResult = listObject.GetRange(6, -4);
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
            char[] iArray = { '#', ' ', '&', 'c', '1', '_', 'A' };
            List<char> listObject = new List<char>(iArray);
            List<char> listResult = listObject.GetRange(4, 4);
            TestLibrary.TestFramework.LogError("103", "The ArgumentException was not thrown as expected");
            retVal = false;
        }
        catch (ArgumentException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("101", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        ListGetRange test = new ListGetRange();

        TestLibrary.TestFramework.BeginTestCase("ListGetRange");

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
