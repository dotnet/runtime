// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// UIntPtr.GetHashCode()
/// Returns the hash code for this instance. 
/// This method is not CLS-compliant.  
/// </summary>
public class UIntPtrGetHashCode
{
    public static int Main()
    {
        UIntPtrGetHashCode testObj = new UIntPtrGetHashCode();

        TestLibrary.TestFramework.BeginTestCase("for method: UIntPtr.GetHashCode()");
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

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "P001";
        const string c_TEST_DESC = "PosTest1: UIntPtr.Zero";
        string errorDesc;

        UIntPtr uiPtr;
        int actualValue, expectedValue;
        bool actualResult;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            uiPtr = UIntPtr.Zero;
            expectedValue = 0;
            actualValue = uiPtr.GetHashCode();

            actualResult = actualValue == expectedValue;

            if (!actualResult)
            {
                errorDesc = "Actual hash code is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        const string c_TEST_ID = "P002";
        const string c_TEST_DESC = "PosTest2: UIntPtr with a random Int32 value ";
        string errorDesc;

        UIntPtr uiPtr;
        int actualValue, expectedValue;
        bool actualResult;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            UInt32 ui = (UInt32)TestLibrary.Generator.GetInt32(-55);
            uiPtr = new UIntPtr(ui);
            expectedValue = (Int32)ui;
            actualValue = uiPtr.GetHashCode();

            actualResult = actualValue == expectedValue;

            if (!actualResult)
            {
                errorDesc = "Actual hash code is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("004" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        const string c_TEST_ID = "P003";
        const string c_TEST_DESC = "PosTest3: UIntPtr with a value greater than Int32.MaxValue";
        string errorDesc;

        UIntPtr uiPtr;
        int actualValue, expectedValue;
        bool actualResult;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            UInt32 ui = (UInt32)Int32.MaxValue + (UInt32)TestLibrary.Generator.GetInt32(-55);
            uiPtr = new UIntPtr(ui);
            expectedValue = unchecked((Int32)((Int64)ui)) & 0x7fffffff;

            actualValue = uiPtr.GetHashCode();

            actualResult = actualValue == expectedValue;

            if (!actualResult)
            {
                errorDesc = "Actual hash code is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("006" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }
}

