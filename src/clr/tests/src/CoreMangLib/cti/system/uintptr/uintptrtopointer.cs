// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// UIntPtr.ToPointer()
/// Converts the value of this instance to a pointer to an unspecified type.
/// This method is not CLS-compliant.  
/// </summary>
public unsafe class UIntPtrToPointer
{
    public static int Main()
    {
        UIntPtrToPointer testObj = new UIntPtrToPointer();

        TestLibrary.TestFramework.BeginTestCase("for method: UIntPtr.ToPointer()");
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


    [System.Security.SecuritySafeCritical]
    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "P001";
        const string c_TEST_DESC = "PosTest1: UIntPtr.Zero";
        string errorDesc;

        void *expectedPtr;
        void *actualPtr;
        UIntPtr uiPtr;
        bool actualResult;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            expectedPtr = (void *)0;
            uiPtr = UIntPtr.Zero;
            actualPtr = uiPtr.ToPointer();

            actualResult = actualPtr == expectedPtr;
            if (!actualResult)
            {
                errorDesc = "Actual pointer value is not " + (UInt32)expectedPtr + " as expected: Actual(" + (UInt32)actualPtr + ")";
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


    [System.Security.SecuritySafeCritical]
    public bool PosTest2()
    {
        bool retVal = true;

        const string c_TEST_ID = "P002";
        const string c_TEST_DESC = "PosTest2: UIntPtr with a random Int32 value ";
        string errorDesc;

        void *expectedPtr;
        void *actualPtr;
        UIntPtr uiPtr;
        bool actualResult;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            UInt32 ui = (UInt32)TestLibrary.Generator.GetInt32(-55);
            expectedPtr = (void *)ui;
            uiPtr = new UIntPtr(ui);
            actualPtr = uiPtr.ToPointer();

            actualResult = actualPtr == expectedPtr;
            if (!actualResult)
            {
                errorDesc = "Actual pointer value is not " + (UInt32)expectedPtr + " as expected: Actual(" + (UInt32)actualPtr + ")";
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


    [System.Security.SecuritySafeCritical]
    public bool PosTest3()
    {
        bool retVal = true;

        const string c_TEST_ID = "P003";
        const string c_TEST_DESC = "PosTest3: UIntPtr with a value greater than Int32.MaxValue";
        string errorDesc;

        void *expectedPtr;
        void *actualPtr;
        UIntPtr uiPtr;
        bool actualResult;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            UInt32 ui = (UInt32)Int32.MaxValue + (UInt32)TestLibrary.Generator.GetInt32(-55);
            expectedPtr = (void *)ui;
            uiPtr = new UIntPtr(ui);
            actualPtr = uiPtr.ToPointer();

            actualResult = actualPtr == expectedPtr;
            if (!actualResult)
            {
                errorDesc = "Actual hash code is not " + (UInt64)expectedPtr + " as expected: Actual(" + (UInt64)actualPtr + ")";
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

