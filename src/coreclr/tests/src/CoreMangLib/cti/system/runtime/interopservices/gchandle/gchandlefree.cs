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
/// Free
/// </summary>

[SecuritySafeCritical]
public class GCHandleFree
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

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call Free to free allocated handle");

        try
        {
            retVal = VerificationHelper(TestLibrary.Generator.GetInt32(-55), "001.1") && retVal;
            retVal = VerificationHelper(TestLibrary.Generator.GetInt64(-55), "001.2") && retVal;
            retVal = VerificationHelper(TestLibrary.Generator.GetDouble(-55), "001.3") && retVal;
            retVal = VerificationHelper(TestLibrary.Generator.GetByte(-55), "001.4") && retVal;
            retVal = VerificationHelper(TestLibrary.Generator.GetSingle(-55), "001.5") && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call Free to free allocated handle");

        try
        {
            string randValue = TestLibrary.Generator.GetString(-55, false, 1, c_ARRAY_SIZE);
            TestNonBittableClass obj = new TestNonBittableClass();
            obj.m_Object = new Object();
            obj.m_String = randValue;

            retVal = VerificationHelper(new TestBittableClass(), "002.1") && retVal;
            retVal = VerificationHelper(new Object(), "002.2") && retVal;
            retVal = VerificationHelper(randValue, "002.3") && retVal;
            retVal = VerificationHelper(obj, "002.4") && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: InvalidOperationException should be thrown when The handle was freed.");

        try
        {
            GCHandle handle = GCHandle.Alloc(new Object());
            handle.Free();

            handle.Free();

            TestLibrary.TestFramework.LogError("101.1", "InvalidOperationException is not thrown when The handle was freed.");
            retVal = false;
        }
        catch (InvalidOperationException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("101.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest2: InvalidOperationException should be thrown when The handle was never initialized.");

        try
        {
            GCHandle handle = (GCHandle)IntPtr.Zero;
            handle.Free();

            TestLibrary.TestFramework.LogError("102.1", "InvalidOperationException is not thrown when The handle was never initialized.");
            retVal = false;
        }
        catch (InvalidOperationException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        GCHandleFree test = new GCHandleFree();

        TestLibrary.TestFramework.BeginTestCase("GCHandleFree");

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
        handle.Free();

        if (handle.IsAllocated)
        {
            TestLibrary.TestFramework.LogError(errorNo, "IsAllocated return true after GCHandle.Free is called");
            retVal = false;
        }

        return retVal;
    }
    #endregion
}
