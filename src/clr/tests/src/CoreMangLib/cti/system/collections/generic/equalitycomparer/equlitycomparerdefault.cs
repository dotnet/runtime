// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;


/// <summary>
/// System.Collections.Generic.EqualityComparer.Default
/// </summary>
public class GenericEqualityComparerDefault
{
    
    public static int Main(string[] args)
    {
        GenericEqualityComparerDefault testObj = new GenericEqualityComparerDefault();
        TestLibrary.TestFramework.BeginTestCase("Testing for Property: System.Collections.Generic.EqualityComparer.Default)");

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

        return retVal;
    }
    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest1: Verify EqualityComparer.Defalut when Type is int ...";
        const string c_TEST_ID = "P001";

       
        
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            EqualityComparer<int> defaultEC = EqualityComparer<int>.Default;
      
            if (defaultEC == null)
            {
                string errorDesc = "Value should be not null";
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
        const string c_TEST_DESC = "PosTest2: Verify EqualityComparer.Defalut when Type is byte ...";
        const string c_TEST_ID = "P002";



        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            EqualityComparer<byte> defaultEC = EqualityComparer<byte>.Default;
            if (defaultEC == null)
            {
                string errorDesc = "Value is not EqualityComparer<byte>";
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
        const string c_TEST_DESC = "PosTest3: Verify EqualityComparer.Defalut when Type is reference type ...";
        const string c_TEST_ID = "P003";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            EqualityComparer<Random> defaultEC = EqualityComparer<Random>.Default;
            if (defaultEC == null)
            {
                string errorDesc = "Value should be not null";
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
        const string c_TEST_DESC = "PosTest4: Verify EqualityComparer.Defalut when Type is a user-defined type ...";
        const string c_TEST_ID = "P004";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            EqualityComparer<MyClass> defaultEC = EqualityComparer<MyClass>.Default;
            if (defaultEC == null)
            {
                string errorDesc = "Value should be not null";
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
