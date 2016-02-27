// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// UIntPtr(UInt32)  
/// Initializes a new instance of the UIntPtr structure using the specified 32-bit pointer or handle. 
/// This constructor is not CLS-compliant.  
/// </summary>
public class UIntPtrCtor
{

    public static int Main()
    {
        UIntPtrCtor testObj = new UIntPtrCtor();

        TestLibrary.TestFramework.BeginTestCase("for constructor: UIntPtr(UInt32)");
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

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "P001";
        const string c_TEST_DESC = "PosTest1: random UInt32 value";
        string errorDesc;

        UIntPtr actualUIntPtr;
        bool actualResult;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            UInt32 ui = this.GetUInt32();
            actualUIntPtr = new UIntPtr(ui);

            actualResult = actualUIntPtr.ToUInt32() == ui;

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
        const string c_TEST_DESC = "PosTest2: UInt32.MinValue";
        string errorDesc;

        UIntPtr actualUIntPtr;
        bool actualResult;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            UInt32 ui = UInt32.MinValue;
            actualUIntPtr = new UIntPtr(ui);

            actualResult = actualUIntPtr.ToUInt32() == ui;

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

    public bool PosTest3()
    {
        bool retVal = true;

        const string c_TEST_ID = "P002";
        const string c_TEST_DESC = "PosTest2: UInt32.MaxValue";
        string errorDesc;

        UIntPtr actualUIntPtr;
        bool actualResult;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            UInt32 ui = UInt32.MaxValue;
            actualUIntPtr = new UIntPtr(ui);

            actualResult = actualUIntPtr.ToUInt32() == ui;

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

    #region Helper methods for test
    //Generate a random UInt32 value
    private UInt32 GetUInt32()
    {
        Int64 i = TestLibrary.Generator.GetInt64(-55) % ((Int64)UInt32.MaxValue + (Int64)1);
        return (UInt32)i;
    }
    #endregion
}

