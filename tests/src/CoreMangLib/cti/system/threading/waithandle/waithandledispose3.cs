// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Threading;

/// <summary>
/// System.IDisposable.Dispose
/// </summary>

// Tests disposing of a WaitHandle-derived type
// via the IDisposable interface.
// This test has been made to work for Silverlight
// by using AutoResetEvent instead of Mutex
public class WaitHandleDispose3
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
    // no need for security attributes
    // [System.Security.SecuritySafeCritical]
    // [System.Security.SecurityCritical]
    public bool PosTest1()
    {
        bool retVal = false;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call Dispose");

        try
        {
            // AutoResetEvent derives from WaitHandle, 
            // and normally you don't create a WaitHandle
            // was: WaitHandle handle = new Mutex();
            // create unsignaled
            WaitHandle handle = new AutoResetEvent(false);

            // cast to IDisposable, because waithandle does not implement .dispose
             IDisposable disposable = handle as IDisposable;
             disposable.Dispose();

            
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

    // should be no need for security attributes
    // [System.Security.SecurityCritical]
    public static int Main()
    {
        WaitHandleDispose3 test = new WaitHandleDispose3();

        TestLibrary.TestFramework.BeginTestCase("WaitHandleDispose3");

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
