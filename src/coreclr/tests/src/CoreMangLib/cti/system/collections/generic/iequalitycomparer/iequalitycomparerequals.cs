// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;


/// <summary>
/// System.Collections.Generic.IEqualityComparer.Equals(T,T)
/// </summary>
public class IEqualityComparerEquals
{
    private int c_MINI_STRING_LENGTH = 8;
    private int c_MAX_STRING_LENGTH = 256;

    public static int Main(string[] args)
    {
        IEqualityComparerEquals testObj = new IEqualityComparerEquals();
        TestLibrary.TestFramework.BeginTestCase("Testing for Methord: System.Collections.Generic.EqualityComparer.Equals(T,T)");

        if (testObj.RunTests())
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

    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;
        retVal = PosTest5() && retVal;

        return retVal;
    }
    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest1: Using EqualityComparer<T> which implemented the Equals method in IEqualityComparer<T> and Type is int...";
        const string c_TEST_ID = "P001";

        EqualityComparer<int> equalityComparer = EqualityComparer<int>.Default;

        int x = TestLibrary.Generator.GetInt32(-55);
        int y = x;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (!((IEqualityComparer<int>)equalityComparer).Equals(x, y))
            {
                string errorDesc = "result should be true when two int both are " + x;
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unecpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest2: Using EqualityComparer<T> which implemented the Equals method in IEqualityComparer<T> and Type is String...";
        const string c_TEST_ID = "P002";

        String x = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
        String y = x;
        EqualityComparer<String> equalityComparer = EqualityComparer<String>.Default;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (!((IEqualityComparer<String>)equalityComparer).Equals(x, y))
            {
                string errorDesc = "result should be true when two String object is the same reference";
                TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unecpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest3: Using EqualityComparer<T> which implemented the Equals method in IEqualityComparer<T> and Type is user-defined class...";
        const string c_TEST_ID = "P003";

        MyClass x = new MyClass();
        MyClass y = x;

        EqualityComparer<MyClass> equalityComparer = EqualityComparer<MyClass>.Default;


        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {

            if (!((IEqualityComparer<MyClass>)equalityComparer).Equals(x, y))
            {
                string errorDesc = "result should be true when two MyClass object is the same reference";
                TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unecpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest4: Using user-defined class which implemented the Equals method in IEqualityComparer<T>...";
        const string c_TEST_ID = "P004";

        MyEqualityComparer<String> myEC = new MyEqualityComparer<String>();
        String str1 = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
        String str2 = str1 + TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {

            if (((IEqualityComparer<String>)myEC).Equals(str1, str2))
            {
                string errorDesc = "result should be true when two string are difference";
                errorDesc += "\n str1 is " + str1;
                errorDesc += "\n str2 is " + str2;
                TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unecpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest5: Using user-defined class which implemented the Equals method in IEqualityComparer<T> and two parament are null...";
        const string c_TEST_ID = "P005";

        MyEqualityComparer<Object> myEC = new MyEqualityComparer<Object>();
        Object x = null;
        Object y = null;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {

            if (!((IEqualityComparer<Object>)myEC).Equals(x, y))
            {
                string errorDesc = "result should be true when two object are null";
                TestLibrary.TestFramework.LogError("009" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unecpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Help Class
    public class MyEqualityComparer<T> : IEqualityComparer<T>
    {
        #region IEqualityComparer<T> Members

        bool IEqualityComparer<T>.Equals(T x, T y)
        {
            if (x != null)
            {
                if (y != null) return x.Equals(y);
                return false;
            }
            if (y != null) return false;
            return true;
        }

        int IEqualityComparer<T>.GetHashCode(T obj)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion
    }

    public class MyClass
    {

    }
    #endregion
}
