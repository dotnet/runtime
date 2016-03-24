// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// System.Collections.IComparer(object,object)
/// </summary>
public class IComparerCompare
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

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Compare two int value");

        try
        {
            Comparer<object> comparer = Comparer<object>.Default ;
            int a = this.GetInt32(0, Int32.MaxValue);
            int b = this.GetInt32(a + 1, Int32.MaxValue);
            int result = comparer.Compare(a, b);
            if (result >= 0)
            {
                TestLibrary.TestFramework.LogError("001", "The result is not the value as expected,a is:" + a + "b is:" + b);
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Compare two string ");

        try
        {
			string a = "hello";
			string b = "aaaaa";
			CultureInfo cultureInfo = new CultureInfo("en-US");
			CompareInfo comparer = cultureInfo.CompareInfo;
			int result = comparer.Compare(b, a);
			if (result >= 0)
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Compare two char which are equal");

        try
        {
            char a = 'z';
            char b = 'z';
            Comparer<char> comparer = Comparer<char>.Default;
            int result = comparer.Compare(b, a);
            if (result != 0)
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

        TestLibrary.TestFramework.BeginScenario("PosTest4: Using custom class deriving from the Icomparer to implement the Compare method");

        try
        {
            TestClass testClass = new TestClass();
            MyClass a = new MyClass(-10);
            MyClass b = new MyClass(100);
            int result = testClass.Compare(b, a);
            if (result <= 0)
            {
                TestLibrary.TestFramework.LogError("007", "The result is not the value as expected");
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

        TestLibrary.TestFramework.BeginScenario("NegTest1: Neither a nor b implements the IComparable interface");

        try
        {
            Comparer<object> comparer = Comparer<object>.Default;
            MyClassNotCompareTo a = new MyClassNotCompareTo();
            MyClassNotCompareTo b = new MyClassNotCompareTo();
            int result = comparer.Compare(b, a);
            TestLibrary.TestFramework.LogError("101", "The ArgumentException was not thrown as expected");
            retVal = false;
        }
        catch (ArgumentException)
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

        TestLibrary.TestFramework.BeginScenario("NegTest2: The objects to be compared are two different types");

        try
        {
            Comparer<object> comparer = Comparer<object>.Default;
            int a = 10;
            string b = "boy";
            int result = comparer.Compare(b, a);
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
    #endregion
    #endregion

    public static int Main()
    {
        IComparerCompare test = new IComparerCompare();

        TestLibrary.TestFramework.BeginTestCase("IComparerCompare");

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
public class TestClass : IComparer
{
    #region IComparer Members

    public int Compare(object x, object y)
    {
        return (x as IComparable).CompareTo(y);
    }

    #endregion
}
public class MyClass : IComparable
{
    #region IComparable Members

    public int CompareTo(object obj)
    {
        if (this.value < ((MyClass)obj).value)
            return -1;
        else
        {
            if (this.value > ((MyClass)obj).value)
                return 1;
            else
            {
                return 0;
            }
        }
    }

    #endregion
    public int value;
    public MyClass(int a)
    {
        value = a;
    }
}
public class MyClassNotCompareTo
{
}
