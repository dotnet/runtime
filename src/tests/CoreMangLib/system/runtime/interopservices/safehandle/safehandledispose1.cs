// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Security;
using System;
using System.Runtime.InteropServices; // For SafeHanlde



[SecurityCritical]
public class MySafeValidHandle : SafeHandle
{
    public MySafeValidHandle()
        : base(IntPtr.Zero, true)
    {
    }

    public MySafeValidHandle(IntPtr handleValue)
        : base(IntPtr.Zero, true)
    {
        handle = handleValue;
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
    public MySafeInValidHandle()
        : base(IntPtr.Zero, true)
    {
    }

    public MySafeInValidHandle(IntPtr handleValue)
        : base(IntPtr.Zero, true)
    {
        handle = handleValue;
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
/// Dispose
/// </summary>
public class SafeHandleDispose1
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

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases

    [SecuritySafeCritical]
    public bool PosTest1()
    {
        bool retVal = true;
        int randValue = 0;

        TestLibrary.TestFramework.BeginScenario("PosTest1: call Dispose on valid SafeHandle instance");

        try
        {
            SafeHandle handle = new MySafeValidHandle();
            handle.Dispose();

            randValue = TestLibrary.Generator.GetInt32(-55);
            handle = new MySafeValidHandle(new IntPtr(randValue));
            handle.Dispose();
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation("WARNING: [LOCAL VARIABLES] randValue = " + randValue);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }


    [SecuritySafeCritical]
    public bool PosTest2()
    {
        bool retVal = true;
        int randValue = 0;

        TestLibrary.TestFramework.BeginScenario("PosTest2: call Dispose on an invalid SafeHandle instance");

        try
        {
            SafeHandle handle = new MySafeInValidHandle();
            handle.Dispose();

            randValue = TestLibrary.Generator.GetInt32(-55);
            handle = new MySafeInValidHandle(new IntPtr(randValue));
            handle.Dispose();
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation("WARNING: [LOCAL VARIABLES] randValue = " + randValue);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }


    [SecuritySafeCritical]
    public bool PosTest3()
    {
        bool retVal = true;
        int randValue = 0;

        TestLibrary.TestFramework.BeginScenario("PosTest3: call Dispose through IDispose interface");

        try
        {
            SafeHandle handle = new MySafeInValidHandle();
            IDisposable idisp = handle as IDisposable;
            idisp.Dispose();

            randValue = TestLibrary.Generator.GetInt32(-55);
            handle = new MySafeInValidHandle(new IntPtr(randValue));
            idisp = handle as IDisposable;
            idisp.Dispose();

            handle = new MySafeValidHandle();
            idisp = handle as IDisposable;
            idisp.Dispose();

            randValue = TestLibrary.Generator.GetInt32(-55);
            handle = new MySafeValidHandle(new IntPtr(randValue));
            idisp = handle as IDisposable;
            idisp.Dispose();
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation("WARNING: [LOCAL VARIABLES] randValue = " + randValue);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases

    [SecuritySafeCritical]
    public bool NegTest1()
    {
        bool retVal = true;
        int randValue = 0;

        TestLibrary.TestFramework.BeginScenario("NegTest1: Call Dispose twice");

        try
        {
            randValue = TestLibrary.Generator.GetInt32(-55);
            SafeHandle handle = new MySafeValidHandle(new IntPtr(randValue));
            handle.Dispose();
            handle.Dispose();
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("101", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation("WARNING: [LOCAL VARIABLES] randValue = " + randValue);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion


    [SecuritySafeCritical]
    public static int Main()
    {
        SafeHandleDispose1 test = new SafeHandleDispose1();

        TestLibrary.TestFramework.BeginTestCase("SafeHandleDispose1");

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
