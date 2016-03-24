// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// UIntPtr.ToUInt32()
/// Converts the value of this instance to a 32-bit unsigned integer.
/// This method is not CLS-compliant.  
/// </summary>
public class UIntPtrToUInt32
{
    public static int Main()
    {
        UIntPtrToUInt32 testObj = new UIntPtrToUInt32();

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
        retVal = PosTest4() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "P001";
        const string c_TEST_DESC = "PosTest1: UIntPtr.Zero";
        string errorDesc;

        UInt32 actualUI, expectedUI;
        UIntPtr uiPtr;
        bool actualResult;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            expectedUI = 0;

            uiPtr = UIntPtr.Zero;
            actualUI = uiPtr.ToUInt32();

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
        const string c_TEST_DESC = "PosTest2: UIntPtr with a random Int32 value ";
        string errorDesc;

        UInt32 actualUI, expectedUI;
        UIntPtr uiPtr;
        bool actualResult;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            expectedUI = (UInt32)TestLibrary.Generator.GetInt32(-55);
            uiPtr = new UIntPtr(expectedUI);
            actualUI = uiPtr.ToUInt32();

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
        const string c_TEST_DESC = "PosTest3: UIntPtr with a value greater than Int32.MaxValue";
        string errorDesc;

        UInt32 actualUI, expectedUI;
        UIntPtr uiPtr;
        bool actualResult;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            expectedUI = (UInt32)Int32.MaxValue + (UInt32)TestLibrary.Generator.GetInt32(-55);
            uiPtr = new UIntPtr(expectedUI);
            actualUI = uiPtr.ToUInt32();

            actualResult = actualUI == expectedUI;
            if (!actualResult)
            {
                errorDesc = "Actual hash code is not " + expectedUI + " as expected: Actual(" + actualUI + ")";
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
        const string c_TEST_DESC = "PosTest4: UIntPtr with a value UInt32.MaxValue";
        string errorDesc;

        UInt32 actualUI, expectedUI;
        UIntPtr uiPtr;
        bool actualResult;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            expectedUI = UInt32.MaxValue;
            uiPtr = new UIntPtr(expectedUI);
            actualUI = uiPtr.ToUInt32();

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

    #region Negative tests
    //OverflowException
    public bool NegTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "N001";
        const string c_TEST_DESC = "NegTest1: On a 32-bit platform, the value of this instance is too large to represent as a 32-bit unsigned integer.";
        string errorDesc;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            if (UIntPtr.Size != 8) // platform is 64-bit
            {
                UInt64 ui = UInt32.MaxValue + this.GetUInt64() % (UInt64.MaxValue - UInt32.MaxValue) + 1;
                UIntPtr actualUIntPtr = new UIntPtr(ui);

                errorDesc = "OverflowException is not thrown as expected";
                TestLibrary.TestFramework.LogError("009" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
            else
            {
                retVal = true;
            }
        }
        catch (OverflowException)
        {
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("08" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }
    #endregion

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
