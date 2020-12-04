// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Security;
using System;
using System.Runtime.InteropServices; // For SafeHandle


/// <summary>
///SetHandleAsInvalid
/// </summary>
public class SafeHandleSetHandleAsInvalid
{
    #region Public Methods

    [SecuritySafeCritical]
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases

    [SecuritySafeCritical]
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call SafeHandle.SetHandleAsInvalid ");
        try
        {
            MySafeHandle msh = new MySafeHandle();
            IntPtr myIptr = new IntPtr(1000);
            msh.MySetHandle(myIptr);
            msh.SetHandleAsInvalid();
            if (!msh.IsClosed || msh.GetHandle()!=myIptr )
            {
                TestLibrary.TestFramework.LogError("001.1", "SetHandleAsInvalid has error ");
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

    #endregion
    #endregion


    [SecuritySafeCritical]
    public static int Main()
    {
        SafeHandleSetHandleAsInvalid test = new SafeHandleSetHandleAsInvalid();

        TestLibrary.TestFramework.BeginTestCase("SafeHandleSetHandleAsInvalid");

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

[SecurityCritical]
public class MySafeHandle : SafeHandle
{
    public MySafeHandle()
        : base(IntPtr.Zero, true)
    {
        this.handle = new IntPtr(100);
    }
    bool InvalidValue = true;
    public override bool IsInvalid
    {
        [SecurityCritical]
        get { return InvalidValue; }

    }
    public bool MyReleaseInvoke()
    {
        return ReleaseHandle();
    }
    public void MySetHandle(IntPtr iptr)
    {
        this.SetHandle(iptr);
    }
    public IntPtr GetHandle()
    {
        return this.handle;
    }
    [DllImport("kernel32")]
    private static extern bool CloseHandle(IntPtr handle);

    [SecurityCritical]
    protected override bool ReleaseHandle()
    {
        if (handle == IntPtr.Zero) return true;
        this.SetHandle(IntPtr.Zero);
        return true;
    }
    public bool CheckHandleIsRelease()
    {
        if (handle != IntPtr.Zero)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
   
}
