// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

/// <summary>
/// System.IDisposable.Dispose
/// </summary>

// Tests disposing of a WaitHandle-derived type
// via the IDisposable interface.
// This test is ok for Desktop, but 
// SL does not support Mutex
public class WaitHandleDispose2
{
    #region Public Constants
    public const int c_DEFAULT_WAIT_TIME = 1000; // 1 second
    #endregion
    
#region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = false;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call Dispose");

        try
        {
            // Mutex derives from WaitHandle, 
            // and normally you don't create a WaitHandle
            WaitHandle handle = new Mutex();

            // used to do a cast to IDisposable, but
            // now WaitHandle implements the IDisposable 
            // interface directly
            IDisposable disposable = handle as IDisposable;
            disposable.Dispose();

            // Dispose 
            //handle.Dispose();

            // Do a waitone on the handle, should immediately throw
            // ObjectDisposedException
            handle.WaitOne(c_DEFAULT_WAIT_TIME);

            // if we get here, it wasn't disposed of, error
            TestLibrary.TestFramework.LogError("001", "handle.Dispose() did not dispose of the handle");
            retVal = false;
        }
        catch (ObjectDisposedException)
        {
            // swallow it, this is where we wanted to go
            retVal = true;
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
    #endregion

    [System.Security.SecurityCritical]
    public static int Main()
    {
        WaitHandleDispose2 test = new WaitHandleDispose2();

        TestLibrary.TestFramework.BeginTestCase("WaitHandleDispose2");

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
