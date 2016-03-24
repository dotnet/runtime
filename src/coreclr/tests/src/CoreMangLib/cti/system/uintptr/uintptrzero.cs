// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// UIntPtr.Zero
/// A read-only field that represents a pointer or handle that has been initialized to zero. 
/// This field is not CLS-compliant.  
/// </summary>
public class UIntPtrZero
{
    private static UIntPtr m_zeroUIntPtr = new UIntPtr();

    public static int Main()
    {
        UIntPtrZero testObj = new UIntPtrZero();

        TestLibrary.TestFramework.BeginTestCase("for field: UIntPtr.Zero");
        if(testObj.RunTests())
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

    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "P001";
        const string c_TEST_DESC = "PosTest1: UIntPtr.Zero vs auto initianlized static UIntPtr";
        string errorDesc;

        UIntPtr expectedUIntPtr, actualUIntPtr;
        bool actualResult;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            expectedUIntPtr = UIntPtrZero.m_zeroUIntPtr;
            actualUIntPtr = UIntPtr.Zero;

            actualResult = actualUIntPtr == expectedUIntPtr;

            if (!actualResult)
            {
                errorDesc = "Actual UIntPtr value is not " + expectedUIntPtr + " as expected: Actual(" + actualUIntPtr + ")";
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
        const string c_TEST_DESC = "PosTest2: UIntPtr.Zero vs UIntPtr(0)";
        string errorDesc;

        UIntPtr expectedUIntPtr, actualUIntPtr;
        bool actualResult;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            expectedUIntPtr = new UIntPtr(0);
            actualUIntPtr = UIntPtr.Zero;

            actualResult = actualUIntPtr == expectedUIntPtr;

            if (!actualResult)
            {
                errorDesc = "Actual UIntPtr value is not " + expectedUIntPtr + " as expected: Actual(" + actualUIntPtr + ")";
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
        const string c_TEST_DESC = "PosTest3: UIntPtr.Zero vs nonzero UIntPtr";
        string errorDesc;

        UIntPtr expectedUIntPtr, actualUIntPtr;
        bool actualResult;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            expectedUIntPtr = new UIntPtr(123);
            actualUIntPtr = UIntPtr.Zero;

            actualResult = actualUIntPtr != expectedUIntPtr;

            if (!actualResult)
            {
                errorDesc = "Actual UIntPtr value is not " + expectedUIntPtr + " as expected: Actual(" + actualUIntPtr + ")";
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


    [System.Security.SecuritySafeCritical]
    public unsafe bool PosTest4()
    {
        bool retVal = true;

        const string c_TEST_ID = "P004";
        const string c_TEST_DESC = "PosTest4: UIntPtr.Zero vs new UIntPtr((void *)0)";
        string errorDesc;

        UIntPtr expectedUIntPtr, actualUIntPtr;
        void *zeroPtr = (void *)0;
        bool actualResult;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            expectedUIntPtr = new UIntPtr(zeroPtr);
            actualUIntPtr = UIntPtr.Zero;

            actualResult = actualUIntPtr == expectedUIntPtr;

            if (!actualResult)
            {
                errorDesc = "Actual UIntPtr value is not UIntPtr((void *) 0 )" + expectedUIntPtr + " as expected: Actual(" + actualUIntPtr + ")";
                TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("008" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }


    [System.Security.SecuritySafeCritical]
    public unsafe bool PosTest5()
    {
        bool retVal = true;

        const string c_TEST_ID = "P005";
        const string c_TEST_DESC = "PosTest5: UIntPtr.Zero vs new UIntPtr((ulong)0)";
        string errorDesc;

        UIntPtr expectedUIntPtr, actualUIntPtr;
        bool actualResult;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            expectedUIntPtr = new UIntPtr((ulong) 0);
            actualUIntPtr = UIntPtr.Zero;

            actualResult = actualUIntPtr == expectedUIntPtr;

            if (!actualResult)
            {
                errorDesc = "Actual UIntPtr value is not UIntPtr((void *) 0 )" + expectedUIntPtr + " as expected: Actual(" + actualUIntPtr + ")";
                TestLibrary.TestFramework.LogError("009" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("010" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }
}

