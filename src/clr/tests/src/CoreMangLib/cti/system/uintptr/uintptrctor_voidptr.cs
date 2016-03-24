// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// UIntPtr(Void*)  
/// Initializes a new instance of UIntPtr using the specified pointer to an unspecified type. 
/// This constructor is not CLS-compliant.  
/// </summary>

public unsafe class UIntPtrCtor
{
    public static int Main()
    {
        UIntPtrCtor testObj = new UIntPtrCtor();

        TestLibrary.TestFramework.BeginTestCase("for constructor: UIntPtr(Void*)");
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
    #region Positive tests

    [System.Security.SecuritySafeCritical]
    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "P001";
        string testDesc = string.Format("PosTest1: value is a random {0}-bit c-style generic pointer", 
                                                      8*UIntPtr.Size);

        string errorDesc;

        UIntPtr actualUIntPtr;
        bool actualResult;

        TestLibrary.TestFramework.BeginScenario(testDesc);
        try
        {
            void * ptr;

            if(UIntPtr.Size == 4) //32-bit platform
            {
                ptr = (void *)TestLibrary.Generator.GetInt32(-55);
            }
            else //64-bit platform
            {
                ptr = (void *)TestLibrary.Generator.GetInt64(-55);
            }

            actualUIntPtr = new UIntPtr(ptr);

            actualResult = actualUIntPtr.ToPointer() == ptr;

            if (!actualResult)
            {
                errorDesc = "Actual UIntPtr value is not " + (UInt64)ptr + " as expected: Actual(" + actualUIntPtr + ")";
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
        const string c_TEST_DESC = "PosTest2: value is 0";

        string errorDesc;

        UIntPtr actualUIntPtr;
        bool actualResult;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            void *ptr = (void *) 0;

            actualUIntPtr = new UIntPtr(ptr);

            actualResult = actualUIntPtr.ToPointer() == ptr;

            if (!actualResult)
            {
                errorDesc = "Actual UIntPtr value is not " + (int)ptr + " as expected: Actual(" + actualUIntPtr + ")";
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
    public bool PosTest3()
    {
        bool retVal = true;

        const string c_TEST_ID = "P002";
        string testDesc = string.Format("PosTest1: value is max {0}-bit pointer: UInt{0}.MaxValue",
                                                      8 * UIntPtr.Size);

        string errorDesc;

        UIntPtr actualUIntPtr;
        bool actualResult;

        TestLibrary.TestFramework.BeginScenario(testDesc);
        try
        {
            void* ptr;

            if (UIntPtr.Size == 4)
            {
                ptr = (void*)UInt32.MaxValue;
            }
            else
            {
                ptr = (void *)UInt64.MaxValue;
            }

            actualUIntPtr = new UIntPtr(ptr);

            actualResult = actualUIntPtr.ToPointer() == ptr;

            if (!actualResult)
            {
                errorDesc = "Actual UIntPtr value is not " + (UInt64)ptr + " as expected: Actual(" + actualUIntPtr + ")";
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

    #endregion
}

