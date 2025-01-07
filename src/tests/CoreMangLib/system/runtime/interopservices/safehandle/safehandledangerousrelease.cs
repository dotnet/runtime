// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Security;
using System;
using System.Runtime.InteropServices; // For SafeHandle
using Xunit;

/// <summary>
/// DangerousRelease
/// </summary>
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

public class SafeHandleDangerousRelease
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
        retVal = PosTest5() && retVal;
        retVal = PosTest6() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;

        return retVal;
    }

    #region Positive Test Cases

    [SecuritySafeCritical]
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call DangerousRelease after call DangerousAddRef for valid handle");

        try
        {
            SafeHandle handle = new MySafeValidHandle();
            bool success = false;

            handle.DangerousAddRef(ref success);
            if (!success)
            {
                TestLibrary.TestFramework.LogInformation("WARNING: Calling DangerousAddRef returns false");
            }

            handle.DangerousRelease();
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
        int randValue = 0;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call DangerousRelease after call DangerousAddRef for valid handle");

        try
        {
            randValue = TestLibrary.Generator.GetInt32(-55);
            SafeHandle handle = new MySafeValidHandle(new IntPtr(randValue));
            bool success = false;

            handle.DangerousAddRef(ref success);
            if (!success)
            {
                TestLibrary.TestFramework.LogInformation("WARNING: Calling DangerousAddRef returns false");
            }

            handle.DangerousRelease();
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation("WARNING: [LOCAL VARIABLES] randValue = " + randValue.ToString());
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }


    [SecuritySafeCritical]
    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Call DangerousRelease after call DangerousAddRef for invalid handle");

        try
        {
            SafeHandle handle = new MySafeInValidHandle();
            bool success = false;

            handle.DangerousAddRef(ref success);
            if (!success)
            {
                TestLibrary.TestFramework.LogInformation("WARNING: Calling DangerousAddRef returns false");
            }

            handle.DangerousRelease();
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
        int randValue = 0;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Call DangerousRelease after call DangerousAddRef for invalid handle");

        try
        {
            randValue = TestLibrary.Generator.GetInt32(-55);
            SafeHandle handle = new MySafeInValidHandle(new IntPtr(randValue));
            bool success = false;

            handle.DangerousAddRef(ref success);
            if (!success)
            {
                TestLibrary.TestFramework.LogInformation("WARNING: Calling DangerousAddRef returns false");
            }

            handle.DangerousRelease();
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation("WARNING: [LOCAL VARIABLES] randValue = " + randValue.ToString());
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }


    [SecuritySafeCritical]
    public bool PosTest5()
    {
        bool retVal = true;
        int randValue = 0;

        TestLibrary.TestFramework.BeginScenario("PosTest5: Call DangerousAddRef after call DangerousRelease for valid handle");

        try
        {
            randValue = TestLibrary.Generator.GetInt32(-55);
            SafeHandle handle = new MySafeValidHandle(new IntPtr(randValue));
            bool success = false;

            handle.DangerousAddRef(ref success);
            if (!success)
            {
                TestLibrary.TestFramework.LogInformation("WARNING: Calling DangerousAddRef returns false");
            }

            handle.DangerousRelease();

            handle.DangerousAddRef(ref success);
            if (!success)
            {
                TestLibrary.TestFramework.LogError("005.1", "Calling DangerousAddRef returns false after calling DangerousRelease");
            }

            handle.DangerousRelease();
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("005.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation("WARNING: [LOCAL VARIABLES] randValue = " + randValue.ToString());
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }


    [SecuritySafeCritical]
    public bool PosTest6()
    {
        bool retVal = true;
        int randValue = 0;

        TestLibrary.TestFramework.BeginScenario("PosTest6: Call DangerousAddRef after call DangerousRelease for invalid handle");

        try
        {
            randValue = TestLibrary.Generator.GetInt32(-55);
            SafeHandle handle = new MySafeInValidHandle(new IntPtr(randValue));
            bool success = false;

            handle.DangerousAddRef(ref success);
            if (!success)
            {
                TestLibrary.TestFramework.LogInformation("WARNING: Calling DangerousAddRef returns false");
            }

            handle.DangerousRelease();

            handle.DangerousAddRef(ref success);
            if (!success)
            {
                TestLibrary.TestFramework.LogError("006.1", "Calling DangerousAddRef returns false after calling DangerousRelease");
            }

            handle.DangerousRelease();
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation("WARNING: [LOCAL VARIABLES] randValue = " + randValue.ToString());
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    // The following two test case will cause a ObjectDispose exception occurs during process unload

    [SecuritySafeCritical]
    public bool NegTest1()
    {
        bool retVal = true;
        int randValue = 0;

        TestLibrary.TestFramework.BeginScenario("NegTest1: Call DangerousRelease without call DangerousAddRef");

        try
        {
            randValue = TestLibrary.Generator.GetInt32(-55);
            SafeHandle handle = new MySafeValidHandle(new IntPtr(randValue));

            // if this object gets finalized it will throw an exception on the finalizer thread
            GC.SuppressFinalize(handle);

            handle.DangerousRelease();
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("101", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation("WARNING: [LOCAL VARIABLES] randValue = " + randValue.ToString());
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

        TestLibrary.TestFramework.BeginScenario("NegTest2: Call DangerousRelease twice with one call DangerousAddRef for valid handle");

        try
        {
            randValue = TestLibrary.Generator.GetInt32(-55);
            SafeHandle handle = new MySafeValidHandle(new IntPtr(randValue));
            bool success = false;

            // if this object gets finalized it will throw an exception on the finalizer thread
            GC.SuppressFinalize(handle);

            handle.DangerousAddRef(ref success);
            if (!success)
            {
                TestLibrary.TestFramework.LogInformation("WARNING: Calling DangerousAddRef returns false");
            }

            handle.DangerousRelease();
            handle.DangerousRelease();
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102.1", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation("WARNING: [LOCAL VARIABLES] randValue = " + randValue.ToString());
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
        SafeHandleDangerousRelease test = new SafeHandleDangerousRelease();

        TestLibrary.TestFramework.BeginTestCase("SafeHandleDangerousRelease");

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
