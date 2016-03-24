// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// System.Collections.Generic.List.binarySearch(T,Collections.Generic.IComparer<T>)
/// </summary>
public class BinarySearch2
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
        retVal = PosTest6() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: The generic type is int and using custom IComparer");

        try
        {
            int[] iArray = { 1, 9, 3, 6, 5, 8, 7, 2, 4, 0 };
            List<int> listObject = new List<int>(iArray);
            listObject.Sort();
            IntClass intClass = new IntClass();
            int i = this.GetInt32(0, 10);
            int result = listObject.BinarySearch(i, intClass);
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: The generic type is a referece type of string and using the custom IComparer");

        try
        {
            string[] strArray = { "apple", "banana", "chocolate", "dog", "food" };
            List<string> listObject = new List<string>(strArray);
            StrClass strClass = new StrClass();
            listObject.Sort(strClass);
            int result = listObject.BinarySearch("egg", strClass);
            if (result != -2)
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
            StrClass strClass = new StrClass();
            int result = listObject.BinarySearch("key", strClass);
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
            MyClassIC myclassIC = new MyClassIC();
            listObject.Sort(myclassIC);
            int result = listObject.BinarySearch(new MyClass(10), myclassIC);
            if (result != 2)
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
            listObject.Sort();
            StrClass strClass = new StrClass();
            int result = listObject.BinarySearch(null, strClass);
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

    public bool PosTest6()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest6: The IComparer is a null reference");

        try
        {
            MyClass myclass1 = new MyClass(10);
            MyClass myclass2 = new MyClass(20);
            MyClass myclass3 = new MyClass(30);
            MyClass[] mc = new MyClass[3] { myclass1, myclass2, myclass3 };
            List<MyClass> listObject = new List<MyClass>(mc);
            MyClassIC myclassIC = new MyClassIC();
            listObject.Sort();
            int result = listObject.BinarySearch(new MyClass(10), null);
            if (result != 0)
            {
                TestLibrary.TestFramework.LogError("011", "The result is not the value as expected,The result is: " + result);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "Unexpected exception: " + e);
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
            int result = listObject.BinarySearch(new TestClass(), null);
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
        BinarySearch2 test = new BinarySearch2();

        TestLibrary.TestFramework.BeginTestCase("BinarySearch2");

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
public class IntClass : IComparer<int>
{
    #region IComparer<int> Members

    public int Compare(int x, int y)
    {
        return x.CompareTo(y);
    }

    #endregion
}
public class StrClass : IComparer<string>
{
    #region IComparer<string> Members

    public int Compare(string x, string y)
    {
        {
            if (x == null)
            {
                if (y == null)
                {
                    // If x is null and y is null, they're
                    // equal. 
                    return 0;
                }
                else
                {
                    // If x is null and y is not null, y
                    // is greater. 
                    return -1;
                }
            }
            else
            {
                // If x is not null...
                //
                if (y == null)
                // ...and y is null, x is greater.
                {
                    return 1;
                }
                else
                {
                    // ...and y is not null, compare the 
                    // lengths of the two strings.
                    //
                    int retval = x.Length.CompareTo(y.Length);

                    if (retval != 0)
                    {
                        // If the strings are not of equal length,
                        // the longer string is greater.
                        //
                        return retval;
                    }
                    else
                    {
                        // If the strings are of equal length,
                        // sort them with ordinary string comparison.
                        //
                        return x.CompareTo(y);
                    }
                }
            }
        }
    }

    #endregion
}

public class MyClassIC : IComparer<MyClass>
{
    #region IComparer<MyClass> Members

    public int Compare(MyClass x, MyClass y)
    {
        return (-1) * (x.value.CompareTo(y.value));
    }

    #endregion
}
