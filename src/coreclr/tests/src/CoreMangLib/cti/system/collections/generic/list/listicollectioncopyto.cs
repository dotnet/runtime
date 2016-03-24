// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections;
/// <summary>
///(System.Array,System.Int32)
/// </summary>
public class ListICollectionCopyTo
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
        retVal = NegTest4() && retVal;
        retVal = NegTest5() && retVal;
      
        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: The list is type of int and get a random index");

        try
        {
            int count = 10;
            int arraySum = 100;
            int[] iArray = { 1, 9, 3, 6, 5, 8, 7, 2, 4, 0 };
            List<int> listObject = new List<int>(iArray);
            int position = this.GetInt32(0, arraySum-count);
            int[] result = new int[arraySum];
            ((ICollection)listObject).CopyTo(result, position);
            for (int i = 0; i < count; i++)
            {
                if (listObject[i] != result[i + position])
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: The list is type of string and copy the date to the array whose beginning index is zero");

        try
        {
            string[] strArray = { "Tom", "Jack", "Mike" };
            List<string> listObject = new List<string>(strArray);
            string[] result = new string[3];
            ((ICollection)listObject).CopyTo(result, 0);
            if (result[0] != "Tom" || result[1] != "Jack" || result[2] != "Mike")
            {
                TestLibrary.TestFramework.LogError("003", "The result is not the value as expected");
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
            List<MyClass> listObject = new List<MyClass>();
            listObject.Add(myclass1);
            listObject.Add(myclass2);
            listObject.Add(myclass3);
            MyClass[] mc = new MyClass[3];
            ((ICollection)listObject).CopyTo(mc, 0);
            if ((mc[0] != myclass1) || (mc[1] != myclass2) || (mc[2] != myclass3))
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

    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Copy an empty list to the end of an array,the three int32 arguments are zero all");

        try
        {
            List<MyClass> listObject = new List<MyClass>();
            MyClass[] mc = new MyClass[3];
            ((ICollection)listObject).CopyTo(mc, 0);
            for (int i = 0; i < 3; i++)
            {
                if (mc[i] != null)
                {
                    TestLibrary.TestFramework.LogError("007", "The result is not the value as expected");
                    retVal = false;
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
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: The array is a null reference");

        try
        {
            int[] iArray = { 1, 9, 3, 6, 5, 8, 7, 2, 4, 0 };
            List<int> listObject = new List<int>(iArray);
            ((ICollection)listObject).CopyTo(null, 0);
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

        TestLibrary.TestFramework.BeginScenario("NegTest2: The number of elements in the source List is greater than the number of elements that the destination array can contain");

        try
        {
            int[] iArray = { 1, 9, 3, 6, 5, 8, 7, 2, 4, 0 };
            List<int> listObject = new List<int>(iArray);
            int[] result = new int[1];
            ((ICollection)listObject).CopyTo(result, 0);
            TestLibrary.TestFramework.LogError("103", "The ArgumentException was not thrown as expected");
            retVal = false;
        }
        catch (ArgumentException)
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

        TestLibrary.TestFramework.BeginScenario("NegTest3: arrayIndex is equal to the length of array");
        try
        {
            int[] iArray = { 1, 9, 3, 6, 5, 8, 7, 2, 4, 0 };
            List<int> listObject = new List<int>(iArray);
            int[] result = new int[20];
            ((ICollection)listObject).CopyTo(result, 20);
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

    public bool NegTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest4: arrayIndex is greater than the length of array");
        try
        {
            int[] iArray = { 1, 9, 3, 6, 5, 8, 7, 2, 4, 0 };
            List<int> listObject = new List<int>(iArray);
            int[] result = new int[20];
            ((ICollection)listObject).CopyTo(result, 300);
            TestLibrary.TestFramework.LogError("107", "The ArgumentException was not thrown as expected");
            retVal = false;
        }
        catch (ArgumentException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("108", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest5()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest5: arrayIndex is less than 0");
        try
        {
            int[] iArray = { 1, 9, 3, 6, 5, 8, 7, 2, 4, 0 };
            List<int> listObject = new List<int>(iArray);
            int[] result = new int[20];
            ((ICollection)listObject).CopyTo(result, -1);
            TestLibrary.TestFramework.LogError("109", "The ArgumentOutOfRangeException was not thrown as expected");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("110", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

   

   
    #endregion
    #endregion

    public static int Main()
    {
        ListICollectionCopyTo test = new ListICollectionCopyTo();

        TestLibrary.TestFramework.BeginTestCase("ListICollectionCopyTo");

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
