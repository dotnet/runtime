// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// UIntPtr.Equals(Object)  
/// Returns a value indicating whether this instance is equal to a specified object. 
/// This method is not CLS-compliant.
/// </summary>
public unsafe class UIntPtrEquals
{
    public static int Main()
    {
        UIntPtrEquals testObj = new UIntPtrEquals();

        TestLibrary.TestFramework.BeginTestCase("for method: UIntPtr.Equals(Object)");
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
        retVal = PosTest6() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "P001";
        const string c_TEST_DESC = "PosTest1: UIntPtr.Zero vs UIntPtr(0)";
        string errorDesc;

        UIntPtr srcUIntPtr, expUIntPtr;
        bool actualResult;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            expUIntPtr = new UIntPtr();
            srcUIntPtr = UIntPtr.Zero;

            actualResult = srcUIntPtr.Equals(expUIntPtr);

            if (!actualResult)
            {
                errorDesc = "Source UIntPtr value does not equal" + expUIntPtr + " as expected: Actual(" + srcUIntPtr + ")";
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
        const string c_TEST_DESC = "PosTest2: UIntPtr.Zero vs non-zero UIntPtr";
        string errorDesc;

        UIntPtr srcUIntPtr, expUIntPtr;
        bool actualResult;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            expUIntPtr = new UIntPtr((UInt32)TestLibrary.Generator.GetInt32(-55) + 1);
            srcUIntPtr = UIntPtr.Zero;

            actualResult = !srcUIntPtr.Equals(expUIntPtr);

            if (!actualResult)
            {
                errorDesc = "Source UIntPtr value does not equal" + expUIntPtr + " as expected: Actual(" + srcUIntPtr + ")";
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
        const string c_TEST_DESC = "PosTest3: two UIntPtrs with random value";
        string errorDesc;

        UIntPtr uiPtrA, uiPtrB;
        bool actualResult;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            UInt32 uiA = (UInt32)TestLibrary.Generator.GetInt32(-55);
            UInt32 uiB = (UInt32)TestLibrary.Generator.GetInt32(-55);
            uiPtrA = new UIntPtr(uiA);
            uiPtrB = new UIntPtr(uiB);

            actualResult = uiPtrA.Equals(uiPtrB);
            actualResult = !((uiA == uiB) ^ actualResult);

            if (!actualResult)
            {
                errorDesc = "UIntPtr " + uiPtrA + " vs UIntPtr " + uiPtrB + " is " + actualResult +
                                 ", that differs from UInt32 " + uiA + " vs UInt32 " + uiB;
                
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

    public bool PosTest4()
    {
        bool retVal = true;

        const string c_TEST_ID = "P004";
        const string c_TEST_DESC = "PosTest4: UIntPtr vs UInt32";
        string errorDesc;

        UIntPtr srcUIntPtr;
        bool actualResult;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            UInt32 ui = (UInt32)TestLibrary.Generator.GetInt32(-55);
            srcUIntPtr = new UIntPtr(ui);

            actualResult = !srcUIntPtr.Equals(ui);

            if (!actualResult)
            {
                errorDesc = "UIntPtr " + srcUIntPtr + " should not equal UInt" + ui;
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

    public bool PosTest5()
    {
        bool retVal = true;

        const string c_TEST_ID = "P005";
        const string c_TEST_DESC = "PosTest5: UIntPtr vs Object";
        string errorDesc;

        UIntPtr srcUIntPtr;
        bool actualResult;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            object obj = new object();
            srcUIntPtr = new UIntPtr();

            actualResult = !srcUIntPtr.Equals(obj);

            if (!actualResult)
            {
                errorDesc = "UIntPtr " + srcUIntPtr + " should not equal object" + obj;
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

    [System.Security.SecuritySafeCritical]
    public bool PosTest6()
    {
        bool retVal = true;

        const string c_TEST_ID = "P006";
        const string c_TEST_DESC = "PosTest6: UIntPtr vs IntPtr";
        string errorDesc;

        UIntPtr uiPtr;
        IntPtr iPtr;
        bool actualResult;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            void * ptr = (void *)TestLibrary.Generator.GetInt32(-55);
            uiPtr = new UIntPtr(ptr);
            iPtr = new IntPtr(ptr);


            actualResult = !uiPtr.Equals(iPtr);

            if (!actualResult)
            {
                errorDesc = "UIntPtr " + uiPtr + " should not equal IntPtr " + iPtr;
                TestLibrary.TestFramework.LogError("011" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("012" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }
}
