// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Security;
using System;
using System.Runtime.InteropServices; // For SafeHandle
using Xunit;


[SecurityCritical]
public class MySafeValidHandle : SafeHandle
{
	  [SecurityCritical]
    public MySafeValidHandle()
        : base(IntPtr.Zero, true)
    {
    }

    public override bool IsInvalid
    {
        [SecurityCritical]
        get { return false; }
    }

    [SecurityCritical]
    protected override bool ReleaseHandle()
    {
        return true;
    }
}



[SecurityCritical]
public class MySafeInValidHandle : SafeHandle
{


[SecurityCritical]
    public MySafeInValidHandle()
        : base(IntPtr.Zero, true)
    {
    }

    public override bool IsInvalid
    {
        [SecurityCritical]
        get { return true; }
    }

    [SecurityCritical]
    protected override bool ReleaseHandle()
    {
        return true;
    }
}

/// <summary>
/// DangerousAddRef(System.Boolean@)
/// </summary>
public class SafeHandleDangerousAddRef
{
    #region Public Methods

    [SecuritySafeCritical]
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

    #region Positive Test Cases

    [SecuritySafeCritical]
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call DangerousAddRef on a valid handle");

        try
        {
            SafeHandle handle = new MySafeValidHandle();
            bool success = false;

            handle.DangerousAddRef(ref success);
            if (!success)
            {
                TestLibrary.TestFramework.LogInformation("success returns false after calling DangerousAddRef");
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }


    [SecuritySafeCritical]
    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call DangerousAddRef on an invalid handle");

        try
        {
            SafeHandle handle = new MySafeInValidHandle();
            bool success = false;

            handle.DangerousAddRef(ref success);
            if (!success)
            {
                TestLibrary.TestFramework.LogInformation("success returns false after calling DangerousAddRef");
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }


    [SecuritySafeCritical]
    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Call DangerousAddRef twice on a valid handle");

        try
        {
            SafeHandle handle = new MySafeValidHandle();
            bool success = false;

            handle.DangerousAddRef(ref success);
            if (!success)
            {
                TestLibrary.TestFramework.LogInformation("success returns false after calling DangerousAddRef");
            }
            handle.DangerousAddRef(ref success);
            if (!success)
            {
                TestLibrary.TestFramework.LogInformation("success returns false after calling DangerousAddRef");
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }


    [SecuritySafeCritical]
    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Call DangerousAddRef twice on an invalid handle");

        try
        {
            SafeHandle handle = new MySafeInValidHandle();
            bool success = false;

            handle.DangerousAddRef(ref success);
            if (!success)
            {
                TestLibrary.TestFramework.LogInformation("success returns false after calling DangerousAddRef");
            }

            handle.DangerousAddRef(ref success);
            if (!success)
            {
                TestLibrary.TestFramework.LogInformation("success returns false after calling DangerousAddRef");
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion


    [SecuritySafeCritical]
    [Fact]
    public static int TestEntryPoint()
    {
        SafeHandleDangerousAddRef test = new SafeHandleDangerousAddRef();

        TestLibrary.TestFramework.BeginTestCase("SafeHandleDangerousAddRef");

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
}
