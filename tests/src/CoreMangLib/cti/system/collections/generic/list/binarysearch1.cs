// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

/// <summary>
/// System.Collections.Generic.List.binarySearch(T)
/// </summary>
public class BinarySearch1
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
        retVal = PosTest5() && retVal;

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
            int[] iArray = { 1, 9, 3, 6, 5, 8, 7, 2, 4, 0 };
            List<int> listObject = new List<int>(iArray);
            listObject.Sort();
            int i = this.GetInt32(0, 10);
            int result = listObject.BinarySearch(i);
            if (result != i)
            {
                TestLibrary.TestFramework.LogError("001", "The result is not the value as expected,The result is: " + result);
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: The generic type is a referece type of string");

        try
        {
            string[] strArray = { "apple", "banana", "chocolate", "dog", "food" };
            List<string> listObject = new List<string>(strArray);
            int result = listObject.BinarySearch("egg");
            if (result != -5)
            {
                TestLibrary.TestFramework.LogError("003", "The result is not the value as expected,The result is: " + result);
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: There are many elements with the same value");

        try
        {
            string[] strArray = { "key", "keys", "key", "key", "sky", "key" };
            List<string> listObject = new List<string>(strArray);
            int result = listObject.BinarySearch("key");
            if (result < 0)
            {
                TestLibrary.TestFramework.LogError("005", "The result is not the value as expected,The result is: " + result);
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

        TestLibrary.TestFramework.BeginScenario("PosTest4: The generic type is custom type");

        try
        {
            MyClass myclass1 = new MyClass(10);
            MyClass myclass2 = new MyClass(20);
            MyClass myclass3 = new MyClass(30);
            MyClass[] mc = new MyClass[3] { myclass1, myclass2, myclass3 };
            List<MyClass> listObject = new List<MyClass>(mc);
            listObject.Sort();
            int result = listObject.BinarySearch(new MyClass(20));
            if (result != 1)
            {
                TestLibrary.TestFramework.LogError("007", "The result is not the value as expected,The result is: " + result);
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

    public bool PosTest5()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest5: The item to be search is a null reference");

        try
        {
            string[] strArray = { "apple", "banana", "chocolate", "dog", "food" };
            List<string> listObject = new List<string>(strArray);
            int result = listObject.BinarySearch(null);
            if (result != -1)
            {
                TestLibrary.TestFramework.LogError("009", "The result is not the value as expected,The result is: " + result);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: IComparable generic interface was not implemented");

        try
        {
            TestClass[] tc = new TestClass[2] { new TestClass(), new TestClass() };
            List<TestClass> listObject = new List<TestClass>(tc);
            int result = listObject.BinarySearch(new TestClass());
            TestLibrary.TestFramework.LogError("101", "The InvalidOperationException was not thrown as expected");
            retVal = false;
        }
        catch (InvalidOperationException)
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
        BinarySearch1 test = new BinarySearch1();

        TestLibrary.TestFramework.BeginTestCase("BinarySearch1");

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
public class MyClass : IComparable
{
    public int value;
    public MyClass(int a)
    {
        this.value = a;
    }
    #region IComparable Members

    public int CompareTo(object obj)
    {
        return this.value.CompareTo(((MyClass)obj).value);
    }

    #endregion
}
public class TestClass
{
}
