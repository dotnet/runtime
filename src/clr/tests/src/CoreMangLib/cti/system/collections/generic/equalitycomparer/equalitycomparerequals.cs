// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;


/// <summary>
/// System.Collections.Generic.EqualityComparer.Equals(T,T)
/// </summary>
public class GenericEqualityComparerEquals
{
    private int c_MINI_STRING_LENGTH = 8;
    private int c_MAX_STRING_LENGTH = 256;

    public static int Main(string[] args)
    {
        GenericEqualityComparerEquals testObj = new GenericEqualityComparerEquals();
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
        const string c_TEST_DESC = "PosTest1: Verify EqualityCompare.Equals(T,T) when Type is int...";
        const string c_TEST_ID = "P001";

        MyEqualityComparer<int> myEC = new MyEqualityComparer<int>();
        int x = TestLibrary.Generator.GetInt32(-55);
        int y = x;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (!myEC.Equals(x, y))
            {
                string errorDesc = "result should be true when two int both is 1";
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
        const string c_TEST_DESC = "PosTest2: Verify the EqualityComparer.Equals(T,T) when T is reference type... ";
        const string c_TEST_ID = "P002";

        String str1 = TestLibrary.Generator.GetString(-55, false,c_MINI_STRING_LENGTH,c_MAX_STRING_LENGTH);
        String str2 = str1;
        MyEqualityComparer<String> myEC = new MyEqualityComparer<String>();

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (!myEC.Equals(str1, str2))
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
        const string c_TEST_DESC = "PosTest3: Verify EqualityCompare.Equals(T,T) when Type is user-defined class... ";
        const string c_TEST_ID = "P003";

        MyEqualityComparer<MyClass> myEC = new MyEqualityComparer<MyClass>();
        MyClass myClass1 = new MyClass();
        MyClass myClass2 = myClass1;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {

            if (!myEC.Equals(myClass1, myClass2))
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
        const string c_TEST_DESC = "PosTest4: Verify EqualityCompare.Equals(T,T) when two parameters are null reference... ";
        const string c_TEST_ID = "P004";

        MyEqualityComparer<String> myEC = new MyEqualityComparer<String>();
        String str1 = null;
        String str2 = null;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {

            if (!myEC.Equals(str2, str1))
            {
                string errorDesc = "result should be true when two DateTime object is null reference";
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
        const string c_TEST_DESC = "PosTest5: Verify EqualityCompare.Equals(T,T) when a parameters is a null reference and another is not... ";
        const string c_TEST_ID = "P005";

        MyEqualityComparer<String> myEC = new MyEqualityComparer<String>();
        String str1 = null;
        String str2 = TestLibrary.Generator.GetString(-55, false,c_MINI_STRING_LENGTH,c_MAX_STRING_LENGTH);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {

            if (myEC.Equals(str1, str2))
            {
                string errorDesc = "result should be false when a String is null and another is not";
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
    public class MyEqualityComparer<T> : EqualityComparer<T>
    {

        public override bool Equals(T x, T y)
        {
            if (x != null)
            {
                if (y != null) return x.Equals(y);
                return false;
            }
            if (y != null) return false;
            return true;
        }

        public override int GetHashCode(T obj)
        {
            throw new Exception("The method or operation is not implemented.");
        }
    }

    public class MyClass
    {

    }
    #endregion
}
