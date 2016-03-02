// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// UIntPtr.ToUInt64()
/// Converts the value of this instance to a 64-bit unsigned integer.
/// This method is not CLS-compliant.  
/// </summary>
public class UIntPtrToUInt64
{
    public static int Main()
    {
        UIntPtrToUInt64 testObj = new UIntPtrToUInt64();

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

    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "P001";
        const string c_TEST_DESC = "PosTest1: UIntPtr.Zero";
        string errorDesc;

        UInt64 actualUI, expectedUI;
        UIntPtr uiPtr;
        bool actualResult;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            expectedUI = 0;
   
            uiPtr = UIntPtr.Zero;
            actualUI = uiPtr.ToUInt64();

            actualResult = actualUI == expectedUI;
            if (!actualResult)
            {
                errorDesc = "Actual UInt32 from UIntPtr is not " + expectedUI + " as expected: Actual(" + actualUI + ")";
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
        const string c_TEST_DESC = "PosTest2: UIntPtr with a random value between 0 and UInt32.MaxValue";
        string errorDesc;

        UInt64 actualUI, expectedUI;
        UIntPtr uiPtr;
        bool actualResult;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            expectedUI = this.GetUInt64() % ((UInt64)UInt32.MaxValue + 1);
            uiPtr = new UIntPtr(expectedUI);
            actualUI = uiPtr.ToUInt64();

            actualResult = actualUI == expectedUI;
            if (!actualResult)
            {
                errorDesc = "Actual UInt32 from UIntPtr is not " + expectedUI + " as expected: Actual(" + actualUI + ")";
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
        string testDesc = string.Format("PosTest3: UIntPtr is max {0}-bit pointer: UInt{0}.MaxValue",
                                                      8 * UIntPtr.Size);
        string errorDesc;

        UInt64 actualUI, expectedUI;
        UIntPtr uiPtr;
        bool actualResult;

        TestLibrary.TestFramework.BeginScenario(testDesc);
        try
        {
            expectedUI = (UIntPtr.Size == 4) ? UInt32.MaxValue : UInt64.MaxValue;
            uiPtr = (UIntPtr.Size == 4) ? new UIntPtr((UInt32)expectedUI) : new UIntPtr(expectedUI);
            actualUI = uiPtr.ToUInt64();

            actualResult = actualUI == expectedUI;
            if (!actualResult)
            {
                errorDesc = "Actual hash code is not " + expectedUI + " as expected: Actual(" + actualUI + ")";
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

    #region helper method for tests
    private UInt64 GetUInt64()
    {
        byte[] buffer = new byte[8];
        UInt64 uiVal;

        TestLibrary.Generator.GetBytes(-55, buffer);

        // convert to UInt64
        uiVal = 0;
        for (int i = 0; i < buffer.Length; i++)
        {
            uiVal |= ((UInt64)buffer[i] << (i * 8));
        }

        TestLibrary.TestFramework.LogInformation("Random UInt64 produced: " + uiVal.ToString());
        return uiVal;
    }
    #endregion
}

