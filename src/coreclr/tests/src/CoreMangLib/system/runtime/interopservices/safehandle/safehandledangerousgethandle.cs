// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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

    [SecurityCritical]
    protected override bool ReleaseHandle()
    {
        return true;
    }
}

/// <summary>
/// DangerousGetHandle
/// </summary>
public class SafeHandleDangerousGetHandle
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: DangerousGetHandle should return handle value for valid safe handle");

        try
        {
            SafeHandle handle = new MySafeValidHandle();
            IntPtr handleValue = handle.DangerousGetHandle();

            if (handleValue != IntPtr.Zero)
            {
                TestLibrary.TestFramework.LogError("001.1", "DangerousGetHandle returns wrong handle value for valid safe handle");
                TestLibrary.TestFramework.LogInformation("WARNING: [LOCAL VARIABLES] handleValue = " + handleValue.ToString() + ", desiredValue = IntPtr.Zero");
                retVal = false;
            }

            // Get it twice
            handleValue = handle.DangerousGetHandle();

            if (handleValue != IntPtr.Zero)
            {
                TestLibrary.TestFramework.LogError("001.2", "DangerousGetHandle returns wrong handle value for valid safe handle");
                TestLibrary.TestFramework.LogInformation("WARNING: [LOCAL VARIABLES] handleValue = " + handleValue.ToString() + ", desiredValue = IntPtr.Zero");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.3", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }


    [SecuritySafeCritical]
    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: DangerousGetHandle should return handle value for valid safe handle");

        try
        {
            int randValue = TestLibrary.Generator.GetInt32(-55);
            IntPtr desiredValue = new IntPtr(randValue);
            SafeHandle handle = new MySafeValidHandle(desiredValue);
            IntPtr handleValue = handle.DangerousGetHandle();

            if (handleValue != desiredValue)
            {
                TestLibrary.TestFramework.LogError("002.1", "DangerousGetHandle returns wrong handle value for valid safe handle");
                TestLibrary.TestFramework.LogInformation("WARNING: [LOCAL VARIABLES] handleValue = " + handleValue.ToString() + ", desiredValue = " + desiredValue.ToString());
                retVal = false;
            }

            handleValue = handle.DangerousGetHandle();

            if (handleValue != desiredValue)
            {
                TestLibrary.TestFramework.LogError("002.2", "DangerousGetHandle returns wrong handle value for valid safe handle");
                TestLibrary.TestFramework.LogInformation("WARNING: [LOCAL VARIABLES] handleValue = " + handleValue.ToString() + ", desiredValue = " + desiredValue.ToString());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.3", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }


    [SecuritySafeCritical]
    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: DangerousGetHandle should return handle value for invalid safe handle");

        try
        {
            SafeHandle handle = new MySafeInValidHandle();
            IntPtr handleValue = handle.DangerousGetHandle();

            if (handleValue != IntPtr.Zero)
            {
                TestLibrary.TestFramework.LogError("003.1", "DangerousGetHandle returns wrong handle value for invalid safe handle");
                TestLibrary.TestFramework.LogInformation("WARNING: [LOCAL VARIABLES] handleValue = " + handleValue.ToString() + ", desiredValue = IntPtr.Zero");
                retVal = false;
            }

            handleValue = handle.DangerousGetHandle();

            if (handleValue != IntPtr.Zero)
            {
                TestLibrary.TestFramework.LogError("003.2", "DangerousGetHandle returns wrong handle value for invalid safe handle");
                TestLibrary.TestFramework.LogInformation("WARNING: [LOCAL VARIABLES] handleValue = " + handleValue.ToString() + ", desiredValue = IntPtr.Zero");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003.3", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }


    [SecuritySafeCritical]
    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: DangerousGetHandle should return handle value for valid safe handle");

        try
        {
            int randValue = TestLibrary.Generator.GetInt32(-55);
            IntPtr desiredValue = new IntPtr(randValue);
            SafeHandle handle = new MySafeInValidHandle(desiredValue);
            IntPtr handleValue = handle.DangerousGetHandle();

            if (handleValue != desiredValue)
            {
                TestLibrary.TestFramework.LogError("004.1", "DangerousGetHandle returns wrong handle value for valid safe handle");
                TestLibrary.TestFramework.LogInformation("WARNING: [LOCAL VARIABLES] handleValue = " + handleValue.ToString() + ", desiredValue = " + desiredValue.ToString());
                retVal = false;
            }

            handleValue = handle.DangerousGetHandle();

            if (handleValue != desiredValue)
            {
                TestLibrary.TestFramework.LogError("004.2", "DangerousGetHandle returns wrong handle value for valid safe handle");
                TestLibrary.TestFramework.LogInformation("WARNING: [LOCAL VARIABLES] handleValue = " + handleValue.ToString() + ", desiredValue = " + desiredValue.ToString());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004.3", "Unexpected exception: " + e);
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
        SafeHandleDangerousGetHandle test = new SafeHandleDangerousGetHandle();

        TestLibrary.TestFramework.BeginTestCase("SafeHandleDangerousGetHandle");

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
