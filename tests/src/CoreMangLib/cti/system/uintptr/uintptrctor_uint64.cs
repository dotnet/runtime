// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// UIntPtr(UInt64)  
/// Initializes a new instance of the UIntPtr structure using the specified 32-bit pointer or handle. 
/// This constructor is not CLS-compliant.  
/// </summary>

public class UIntPtrCtor
{
    public static int Main()
    {
        UIntPtrCtor testObj = new UIntPtrCtor();

        TestLibrary.TestFramework.BeginTestCase("for constructor: UIntPtr(UInt64)");
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

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;

        return retVal;
    }
    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "P001";
        const string c_TEST_DESC = "PosTest1: random valid UInt64 value";
        string errorDesc;

        UIntPtr actualUIntPtr;
        bool actualResult;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            UInt64 ui = this.GetValidUInt64();
            actualUIntPtr = new UIntPtr(ui);

            actualResult = actualUIntPtr.ToUInt64() == ui;

            if (!actualResult)
            {
                errorDesc = "Actual UIntPtr value is not " + ui + " as expected: Actual(" + actualUIntPtr + ")";
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
        const string c_TEST_DESC = "PosTest2: value is UInt64.MinValue"; // that is 0

        string errorDesc;

        UIntPtr actualUIntPtr;
        bool actualResult;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            UInt64 ui = UInt64.MinValue;
            actualUIntPtr = new UIntPtr(ui);

            actualResult = actualUIntPtr.ToUInt64() == ui;

            if (!actualResult)
            {
                errorDesc = "Actual UIntPtr value is not " + ui + " as expected: Actual(" + actualUIntPtr + ")";
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
        string testDesc = string.Format("PosTest3: value is UInt{0}.MaxValue", 8 * UIntPtr.Size);

        string errorDesc;

        UIntPtr actualUIntPtr;
        bool actualResult;

        TestLibrary.TestFramework.BeginScenario(testDesc);
        try
        {
            UInt64 ui = (UIntPtr.Size == 4) ? UInt32.MaxValue : UInt64.MaxValue;
            actualUIntPtr = new UIntPtr(ui);

            actualResult = actualUIntPtr.ToUInt64() == ui;

            if (!actualResult)
            {
                errorDesc = "Actual UIntPtr value is not " + ui + " as expected: Actual(" + actualUIntPtr + ")";
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

    #endregion

    #region Negative tests
    //OverflowException
    public bool NegTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "N001";
        const string c_TEST_DESC = "NegTest1: Value is UInt64.MaxValue";
        string errorDesc;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            if (UIntPtr.Size == 4) // platform is 32-bit
            {
                UInt64 ui = UInt64.MaxValue;
                UIntPtr actualUIntPtr = new UIntPtr(ui);

                errorDesc = "OverflowException is not thrown as expected";
                TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, errorDesc);
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

    public bool NegTest2()
    {
        bool retVal = true;

        const string c_TEST_ID = "N002";
        const string c_TEST_DESC = "NegTest2: Value is UInt32.MaxValue + 1";
        string errorDesc;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            if (UIntPtr.Size == 4) // platform is 32-bit
            {
                UInt64 ui = 1 + (UInt64)UInt32.MaxValue;
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
            TestLibrary.TestFramework.LogError("010" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Helper methods for test
    //Generate a random UInt64 value between 0 and UInt32.MaxValue
    private UInt64 GetValidUInt64()
    {
        Int64 i = TestLibrary.Generator.GetInt64(-55) % ((Int64)UInt32.MaxValue + (Int64)1);
        return (UInt64)i;
    }
    #endregion
}

