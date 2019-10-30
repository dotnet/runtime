// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Security;
using System;
using System.Runtime.InteropServices; // For SafeHandle


[SecurityCritical]
public class MySafeHandle : SafeHandle
{
    [SecurityCritical]
    public MySafeHandle()
        : base(IntPtr.Zero, true)
    {
        if (handle != IntPtr.Zero)
        {
            throw new Exception("Handle value is not IntPtr.Zero, Handle = " + handle.ToString());
        }
    }

    public MySafeHandle(IntPtr handleValue)
        : base(IntPtr.Zero, true)
    {
        handle = handleValue;
        if (handle != handleValue)
        {
            throw new Exception("Handle value is not assigned correctly, handleValue = " + handleValue + ", Handle = " + handle.ToString());
        }
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

/// <summary>
/// handle
/// </summary>
public class SafeHandleHandle
{
    #region Public Methods

    [SecuritySafeCritical]
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;

        return retVal;
    }

    #region Positive Test Cases

    [SecuritySafeCritical]
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify the handle is a protected value");

        try
        {
            MySafeHandle msf = new MySafeHandle();

            if (null == msf)
            {
                TestLibrary.TestFramework.LogError("001.1", "Failed to allocate a new safe handle instance");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }


    [SecuritySafeCritical]
    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify the handle is a protected value, and can set a value");

        try
        {
            int randValue = TestLibrary.Generator.GetInt32(-55);
            IntPtr value = new IntPtr(randValue);
            MySafeHandle msf = new MySafeHandle(value);

            if (null == msf)
            {
                TestLibrary.TestFramework.LogError("002.1", "Failed to allocate a new safe handle instance");
                TestLibrary.TestFramework.LogInformation("[LOCAL VARIABLES] randValue = " + randValue);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.2", "Unexpected exception: " + e);
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
        SafeHandleHandle test = new SafeHandleHandle();

        TestLibrary.TestFramework.BeginTestCase("SafeHandleHandle");

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
