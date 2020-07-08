// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Security;
using System;
using System.Runtime.InteropServices;



[SecurityCritical]
public class MySafeHandle : SafeHandle
{
    public IntPtr Handle
    {
        get
        {
            return handle;
        }
    }

    [SecurityCritical]
    public MySafeHandle()
        : base(IntPtr.Zero, true)
    {
    }

    [SecurityCritical]
    public MySafeHandle(IntPtr handleValue)
        : base(handleValue, true)
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

/// <summary>
/// ctor(System.IntPtr,System.Boolean)
/// </summary>
public class SafeHandleCtor
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify ctor can set correct handle value");

        try
        {
            MySafeHandle msf = new MySafeHandle();

            if (msf.Handle != IntPtr.Zero)
            {
                TestLibrary.TestFramework.LogError("001.1", "Ctor can not set correct handle value");
                TestLibrary.TestFramework.LogInformation("WARNING: [LOCAL VARIABLES] msf.Handle = " + msf.Handle.ToString());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }


    [SecuritySafeCritical]
    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify ctor can set correct handle value for constructor with parameters");

        try
        {
            IntPtr ptr = new IntPtr(TestLibrary.Generator.GetInt32(-55));
            MySafeHandle msf = new MySafeHandle(ptr);

            if (msf.Handle != ptr)
            {
                TestLibrary.TestFramework.LogError("002.1", "Ctor can not set correct handle value");
                TestLibrary.TestFramework.LogInformation("WARNING: [LOCAL VARIABLES] msf.Handle = " + msf.Handle.ToString() +
                                                                                                                   ", desiredValue = " + ptr.ToString());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.0", "Unexpected exception: " + e);
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
        SafeHandleCtor test = new SafeHandleCtor();

        TestLibrary.TestFramework.BeginTestCase("SafeHandleCtor");

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
