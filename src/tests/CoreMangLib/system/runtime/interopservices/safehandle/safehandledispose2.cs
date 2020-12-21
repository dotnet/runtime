// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Security;
using System;
using System.Runtime.InteropServices; // For SafeHandle


[SecurityCritical]
public class MySafeValidHandle : SafeHandle
{
[SecurityCritical]
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

    public void DisposeWrap(bool dispose)
    {
        Dispose(dispose);
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

    public void DisposeWrap(bool dispose)
    {
        Dispose(dispose);
    }

    [SecurityCritical]
    protected override bool ReleaseHandle()
    {
        return true;
    }
}

/// <summary>
/// Dispose(System.Boolean)
/// </summary>
public class SafeHandleDispose2
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

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;

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
            MySafeValidHandle handle = new MySafeValidHandle();
            handle.DisposeWrap(true);

            randValue = TestLibrary.Generator.GetInt32(-55);
            handle = new MySafeValidHandle(new IntPtr(randValue));
            handle.DisposeWrap(true);
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
            MySafeInValidHandle handle = new MySafeInValidHandle();
            handle.DisposeWrap(true);

            randValue = TestLibrary.Generator.GetInt32(-55);
            handle = new MySafeInValidHandle(new IntPtr(randValue));
            handle.DisposeWrap(true);
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: call Dispose on valid SafeHandle instance with false");

        try
        {
            MySafeValidHandle handle = new MySafeValidHandle();
            handle.DisposeWrap(false);

            randValue = TestLibrary.Generator.GetInt32(-55);
            handle = new MySafeValidHandle(new IntPtr(randValue));
            handle.DisposeWrap(false);
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



    [SecuritySafeCritical]
    public bool PosTest4()
    {
        bool retVal = true;
        int randValue = 0;

        TestLibrary.TestFramework.BeginScenario("PosTest4: call Dispose on an invalid SafeHandle instance with false");

        try
        {
            MySafeInValidHandle handle = new MySafeInValidHandle();
            handle.DisposeWrap(false);

            randValue = TestLibrary.Generator.GetInt32(-55);
            handle = new MySafeInValidHandle(new IntPtr(randValue));
            handle.DisposeWrap(false);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation("WARNING: [LOCAL VARIABLES] randValue = " + randValue);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Negative Test Cases


    [SecuritySafeCritical]
    public bool NegTest1()
    {
        bool retVal = true;
        int randValue = 0;

        TestLibrary.TestFramework.BeginScenario("NegTest1: Call Dispose twice");

        try
        {
            randValue = TestLibrary.Generator.GetInt32(-55);
            MySafeValidHandle handle = new MySafeValidHandle(new IntPtr(randValue));
            handle.DisposeWrap(true);
            handle.DisposeWrap(false);
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



    [SecuritySafeCritical]
    public bool NegTest2()
    {
        bool retVal = true;
        int randValue = 0;

        TestLibrary.TestFramework.BeginScenario("NegTest2: Call Dispose twice");

        try
        {
            randValue = TestLibrary.Generator.GetInt32(-55);
            MySafeValidHandle handle = new MySafeValidHandle(new IntPtr(randValue));
            handle.DisposeWrap(false);
            handle.DisposeWrap(true);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation("WARNING: [LOCAL VARIABLES] randValue = " + randValue);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }


    [SecuritySafeCritical]
    public bool NegTest3()
    {
        bool retVal = true;
        int randValue = 0;

        TestLibrary.TestFramework.BeginScenario("NegTest3: Call Dispose twice");

        try
        {
            randValue = TestLibrary.Generator.GetInt32(-55);
            MySafeValidHandle handle = new MySafeValidHandle(new IntPtr(randValue));
            handle.DisposeWrap(true);
            handle.DisposeWrap(true);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("103", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation("WARNING: [LOCAL VARIABLES] randValue = " + randValue);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }



    [SecuritySafeCritical]
    public bool NegTest4()
    {
        bool retVal = true;
        int randValue = 0;

        TestLibrary.TestFramework.BeginScenario("NegTest3: Call Dispose twice");

        try
        {
            randValue = TestLibrary.Generator.GetInt32(-55);
            MySafeValidHandle handle = new MySafeValidHandle(new IntPtr(randValue));
            handle.DisposeWrap(false);
            handle.DisposeWrap(false);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("104", "Unexpected exception: " + e);
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
        SafeHandleDispose2 test = new SafeHandleDispose2();

        TestLibrary.TestFramework.BeginTestCase("SafeHandleDispose2");

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
