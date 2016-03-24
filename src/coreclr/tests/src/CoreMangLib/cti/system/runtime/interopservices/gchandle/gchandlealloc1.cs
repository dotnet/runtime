// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Security;
using System;
using System.Runtime.InteropServices;


[SecuritySafeCritical]
public class TestNonBittableClass
{
    public Object m_Object;
    public string m_String;
}


[SecuritySafeCritical]
public class TestBittableClass
{
    public int m_TestInt;
}

/// <summary>
/// Alloc(System.Object)
/// </summary>

[SecuritySafeCritical]
public class GCHandleAlloc1
{
    #region Private Fields
    private const int c_ARRAY_SIZE = 256;
    #endregion

    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;

        return retVal;
    }

    #region Positive Test Cases

    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call Alloc to alloc memory for a value type instance");

        try
        {
            retVal = VerificationHelper(TestLibrary.Generator.GetInt32(-55), "001.1") && retVal;
            retVal = VerificationHelper(TestLibrary.Generator.GetInt64(-55), "001.2") && retVal;
            retVal = VerificationHelper(TestLibrary.Generator.GetDouble(-55), "001.3") && retVal;
            retVal = VerificationHelper(TestLibrary.Generator.GetChar(-55), "001.4") && retVal;
            retVal = VerificationHelper(TestLibrary.Generator.GetByte(-55), "001.5") && retVal;
            retVal = VerificationHelper(TestLibrary.Generator.GetSingle(-55), "001.6") && retVal;

            byte[] bytes = new byte[c_ARRAY_SIZE];
            TestLibrary.Generator.GetBytes(-55, bytes);
            retVal = VerificationHelper(bytes, "001.7") && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call Alloc to alloc memory for a class contains value type only instance");

        try
        {
            Object obj = new TestBittableClass();
            retVal = VerificationHelper(obj, "002.1") && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Call Alloc to alloc memory for a non blittable type instance");

        try
        {
            string randValue = TestLibrary.Generator.GetString(-55, false, 1, c_ARRAY_SIZE);
            TestNonBittableClass obj = new TestNonBittableClass();
            obj.m_Object = new Object();
            obj.m_String = randValue;

            retVal = VerificationHelper(new Object(), "003.1") && retVal;
            retVal = VerificationHelper(randValue, "003.2") && retVal;
            retVal = VerificationHelper(obj, "003.3") && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    #endregion

    #endregion

    public static int Main()
    {
        GCHandleAlloc1 test = new GCHandleAlloc1();

        TestLibrary.TestFramework.BeginTestCase("GCHandleAlloc1");

        if (test.RunTests())
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

    #region Private Methods

    private bool VerificationHelper(object obj, string errorNo)
    {
        bool retVal = true;

        GCHandle handle = GCHandle.Alloc(obj);
        if (!handle.IsAllocated)
        {
            TestLibrary.TestFramework.LogError(errorNo, "IsAllocated return false after GCHandle.Alloc is called");
            retVal = false;
        }

        handle.Free();

        return retVal;
    }
    #endregion
}
