// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// UIntPtr.ToString()
/// Converts the value of this instance to a pointer to an unspecified type.
/// This method is not CLS-compliant.  
/// </summary>
public class UIntPtrToString
{
    public static int Main()
    {
        UIntPtrToString testObj = new UIntPtrToString();

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

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "P001";
        const string c_TEST_DESC = "PosTest1: UIntPtr.Zero";
        string errorDesc;

        string actualStr, expectedStr;
        UIntPtr uiPtr;
        bool actualResult;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            expectedStr = (UIntPtr.Size == 4) ? ((UInt32)0).ToString() : ((UInt64)0).ToString();
   
            uiPtr = UIntPtr.Zero;
            actualStr = uiPtr.ToString();

            actualResult = actualStr == expectedStr;
            if (!actualResult)
            {
                errorDesc = "Actual string from UIntPtr is not " + expectedStr + " as expected: Actual(" + actualStr + ")";
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

        string actualStr, expectedStr;
        UIntPtr uiPtr;
        bool actualResult;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            UInt32 ui = (UInt32)TestLibrary.Generator.GetInt32(-55);
            expectedStr = (UIntPtr.Size == 4) ? ui.ToString() : ((UInt64)ui).ToString();
            uiPtr = new UIntPtr(ui);
            actualStr = uiPtr.ToString();

            actualResult = actualStr == expectedStr;
            if (!actualResult)
            {
                errorDesc = "Actual string from UIntPtr is not " + expectedStr + " as expected: Actual(" + actualStr + ")";
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

        string actualStr, expectedStr;
        UIntPtr uiPtr;
        bool actualResult;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            UInt32 ui = (UInt32)Int32.MaxValue + (UInt32)TestLibrary.Generator.GetInt32(-55);
            expectedStr = (UIntPtr.Size == 4) ? ui.ToString() : ((UInt64)ui).ToString();
            uiPtr = new UIntPtr(ui);
            actualStr = uiPtr.ToString();

            actualResult = actualStr == expectedStr;
            if (!actualResult)
            {
                errorDesc = "Actual hash code is not " + expectedStr + " as expected: Actual(" + actualStr + ")";
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
        string testDesc = string.Format("PosTest4: value is max {0}-bit pointer: UInt{0}.MaxValue",
                                                      8 * UIntPtr.Size);
        string errorDesc;

        string actualStr, expectedStr;
        UIntPtr uiPtr;
        bool actualResult;

        TestLibrary.TestFramework.BeginScenario(testDesc);
        try
        {
            UInt64 ui = (UIntPtr.Size == 4) ? UInt32.MaxValue : UInt64.MaxValue;
            expectedStr = (UIntPtr.Size == 4) ? ((UInt32)ui).ToString() : ui.ToString();
            uiPtr = new UIntPtr(ui);
            actualStr = uiPtr.ToString();

            actualResult = actualStr == expectedStr;
            if (!actualResult)
            {
                errorDesc = "Actual hash code is not " + expectedStr + " as expected: Actual(" + actualStr + ")";
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
}

