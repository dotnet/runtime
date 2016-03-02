// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Threading;

// WaitHandle is typically used as a base 
// class for synchronization objects.  In this case,
// we just subclass it.
public class TestWaitHandle1 : WaitHandle
{
    public TestWaitHandle1()
    {
    }

    public void DoDispose(bool explicitDisposing)
    {
        Dispose(explicitDisposing);
    }
}

/// <summary>
/// Dispose(System.Boolean)
/// </summary>


// Tests that we can call Dispose on a subclass
// of WaitHandle, with explicitDisposing = true and false.
// Dispose is called by the WaitHandle.Dispose() 
// method and the Finalize method. Dispose() invokes this 
// protected method with the explicitDisposing parameter set to 
// true (Dispose both managed and unmanaged resources). 
// Finalize invokes Dispose with explicitDisposing set to false 
// (Dispose only unmanaged resources).
public class WaitHandleDispose1
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
        retVal = PosTest2() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = false;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call Dispose with explicitDisposing set to true");

        try
        {
            // Create a WaitHandle-derived object
            TestWaitHandle1 handle = new TestWaitHandle1();

            // Dispose both managed and 
            // unmanaged resources (DoDispose just calls
            // Dispose)
            handle.DoDispose(true);

            // Do a waitone on the handle, should immediately throw
            // ObjectDisposedException
            handle.WaitOne(c_DEFAULT_WAIT_TIME);

            // if we get here, it wasn't disposed of, error
            TestLibrary.TestFramework.LogError("001", "handle.Dispose(true) did not dispose of the handle");
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

    public bool PosTest2()
    {
        bool retVal = false;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call Dispose with explicitDisposing set to false");

        try
        {
            TestWaitHandle1 handle = new TestWaitHandle1();

            // Dispose only unmanaged resources.
            // (DoDispose just calls Dispose)
            handle.DoDispose(false);

            // Do a waitone on the handle, should get ObjectDisposedException
            handle.WaitOne(c_DEFAULT_WAIT_TIME);

            // if we get here, it wasn't disposed of, error
            TestLibrary.TestFramework.LogError("003", "handle.Dispose(false) did not dispose of the handle");
            retVal = false;
        }
        catch (ObjectDisposedException)
        {
            // swallow it, this is where we wanted to go
            retVal = true;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }
        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        WaitHandleDispose1 test = new WaitHandleDispose1();

        TestLibrary.TestFramework.BeginTestCase("WaitHandleDispose1");

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
