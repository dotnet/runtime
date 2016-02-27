// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;


/// <summary>
/// System.Collections.Generic.EqualityComparer.GetHashCode(T)
/// </summary>
public class GenericEqualityComparerGetHashCode
{
    private int c_MINI_STRING_LENGTH = 8;
    private int c_MAX_STRING_LENGTH = 256;

    public static int Main(string[] args)
    {
        GenericEqualityComparerGetHashCode testObj = new GenericEqualityComparerGetHashCode();
        TestLibrary.TestFramework.BeginTestCase("Testing for Methord: System.Collections.Generic.EqualityComparer.GetHashCode(T)");

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

        TestLibrary.TestFramework.LogInformation("[Netativ]");
        retVal = NegTest1() && retVal;

        return retVal;
    }
    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest1: Verify EqualityCompare.Equals(T,T) when Type is int...";
        const string c_TEST_ID = "P001";

        MyEqualityComparer<int> myEC = new MyEqualityComparer<int>();
        int x = TestLibrary.Generator.GetInt32(-55);

        int expectedValue = x.GetHashCode();

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            int actualValue = myEC.GetHashCode(x);
            if (expectedValue != actualValue)
            {
                string errorDesc = "Value is not " + actualValue + " as expected: Actual(" + expectedValue + ")";
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
        const string c_TEST_DESC = "PosTest2: Verify EqualityCompare.Equals(T,T) when Type is a reference type...";
        const string c_TEST_ID = "P002";

        MyEqualityComparer<String> myEC = new MyEqualityComparer<String>();
        string str = TestLibrary.Generator.GetString(-55, false,c_MINI_STRING_LENGTH,c_MAX_STRING_LENGTH);

        int expectedValue = str.GetHashCode();

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            int actualValue = myEC.GetHashCode(str);
            if (expectedValue != actualValue)
            {
                string errorDesc = "Value is not " + actualValue + " as expected: Actual(" + expectedValue + ")";
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
        const string c_TEST_DESC = "PosTest3: Verify EqualityCompare.Equals(T,T) when Type is a reference type...";
        const string c_TEST_ID = "P003";

        MyEqualityComparer<MyClass> myEC = new MyEqualityComparer<MyClass>();
        MyClass myclass1 = new MyClass();

        int expectedValue = myclass1.GetHashCode();

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            int actualValue = myEC.GetHashCode(myclass1);
            if (expectedValue != actualValue)
            {
                string errorDesc = "Value is not " + actualValue + " as expected: Actual(" + expectedValue + ")";
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
    #endregion

    #region Negative tests
    public bool NegTest1()
    {
        bool retVal = true;

        const string c_TEST_DESC = "NegTest1: object is a null reference";
        const string c_TEST_ID = "N001";

        MyEqualityComparer<MyClass> myEC = new MyEqualityComparer<MyClass>();
        
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            myEC.GetHashCode(null);
            TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, "ArgumentNullException is not thrown as expected." );
            retVal = false;
        }
        catch (ArgumentNullException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e );
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Help Class
    public class MyEqualityComparer<T> : EqualityComparer<T>
    {

        public override bool Equals(T x, T y)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public override int GetHashCode(T obj)
        {
            if (obj == null)
                throw new ArgumentNullException();
            return obj.GetHashCode();
        }
    }

    public class MyClass
    {  
    }
    #endregion
}
