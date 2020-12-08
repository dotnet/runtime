// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Security;
using System;
using System.Runtime.InteropServices; // For SafeHandle

[assembly: SecurityCritical]

/// <summary>
///IsClosed
/// </summary>
public class SafeHandleIsClosed
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Check IsClosed return true when SetHandleAsInvalid method is called  ");
        try
        {
            MySafeHandle msh = new MySafeHandle();
            IntPtr myIptr = new IntPtr(1000);
            msh.MySetHandle(myIptr);
            msh.SetHandleAsInvalid();
            if (!msh.IsClosed )
            {
                TestLibrary.TestFramework.LogError("001.1", "IsClosed should return true");
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
    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Check IsClosed return false when SetHandleAsInvalid method is not called and don't excute any close handle operation  ");
        try
        {
            MySafeHandle msh = new MySafeHandle();
            IntPtr myIptr = new IntPtr(1000);
            msh.MySetHandle(myIptr);
            if (msh.IsClosed)
            {
                TestLibrary.TestFramework.LogError("002.1", "IsClosed should return false ");
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

    public static int Main()
    {
        SafeHandleIsClosed test = new SafeHandleIsClosed();

        TestLibrary.TestFramework.BeginTestCase("SafeHandleIsClosed");

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
