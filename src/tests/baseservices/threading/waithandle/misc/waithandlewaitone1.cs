// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

/// <summary>
/// WaitOne
/// </summary>

// Verifies that WaitOne can acquire a 
// mutex, that calling it while the mutex is held will
// cause a thread to block, that if the mutex is 
// never released then WaitOne throws an 
// AbandonedMutexException, and that calling it on a
// disposed-of mutex throws ObjectDisposedException
// this test works for Orcas and Client, but Mutex
// is not supported for Silverlight.
public class WaitHandleWaitOne1
{
    #region Private Fields
    private const int c_DEFAULT_SLEEP_TIME = 5000; // 1 second

    private WaitHandle m_Handle = null;
    #endregion

    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    // this one tests that a WaitOne will wait on a Mutex
    // and then grab it when it is free
    public bool PosTest1()
    {
        bool retVal = false;
        Thread thread = null;

        TestLibrary.TestFramework.BeginScenario("PosTest1: WaitOne returns true when current instance receives a signal");

        // m_Handle is of type WaitHandle
        // Mutex is a subclass of WaitHandle
        using(m_Handle = new Mutex())
        {
        try
        {
            // Spin up a thread.  SignalMutex grabs 
            // the Mutex, then goes to sleep for 5 seconds.  It
            // only releases the mutex after sleeping for those
            // 5 seconds
            thread = new Thread(new ThreadStart(SignalMutex));
            // Start it
            thread.Start();
            // Then put this calling thread to sleep
            // for one second so that it doesn't beat the spawned
            // thread to the mutex
            Thread.Sleep(c_DEFAULT_SLEEP_TIME / 5); // To avoid race
            // Now, the spawned thread should already
            // have the mutex and be sleeping on it for several 
            // seconds.  try to grab the mutex now.  We should 
            // simply block until the other thread releases it, 
            // then we should get it.
            // Net result, we should get true back from WaitOne
            if (m_Handle.WaitOne() != true)
            {
                TestLibrary.TestFramework.LogError("001", "WaitOne returns false when current instance receives a signal.");
                retVal = false;

            }
            else
            {
                // got the mutex ok
                retVal = true;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }
        finally
        {
            // Wait for the thread to terminate
            if (null != thread)
            {
                // Wait on that thread
                thread.Join();
            }
        }
        }

        return retVal;
    }
    #endregion

    #region Negative Test Cases
    // this one just tests that a WaitOne will receive
    // a AbandonedMutexException
    public bool NegTest1()
    {
        bool retVal = false;
        Thread thread = null;

        TestLibrary.TestFramework.BeginScenario("NegTest1: AbandonedMutexException should be thrown if a thread exited without releasing a mutex");

        // m_Handle is of type WaitHandle
        // Mutex is a subclass of WaitHandle
        using(m_Handle = new Mutex())
        {
        try
        {
            // Spin up a thread.  SignalMutex grabs 
            // the Mutex, then goes to sleep for 5 seconds.  
            thread = new Thread(new ThreadStart(NeverReleaseMutex));
            // Start it
            thread.Start();
            // Then put this calling thread to sleep
            // for just over one second so that it doesn't beat 
            // the spawned thread to the mutex
            Thread.Sleep(c_DEFAULT_SLEEP_TIME / 3); // To avoid race
            // Now, the spawned thread should already
            // have the mutex and be sleeping on it forever.
            // try to grab the mutex now.  We should simply block until
            // the other thread releases it, which is never.  When that 
            // thread returns from its method, an AbandonedMutexException
            // should be thrown instead
            m_Handle.WaitOne();

            // We should not get here
            TestLibrary.TestFramework.LogError("101", "AbandonedMutexException is not thrown if a thread exited without releasing a mutex");
            retVal = false;
        }
        catch (AbandonedMutexException)
        {
            // Swallow it, this is where we want
            // the test to go
            retVal = true;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }
        finally
        {
            if (null != thread)
            {
                // The spawned thread is still running.
                // Join on it until it is done
                thread.Join();
            }
        }
        }

        return retVal;
    }

    // this one just tests that WaitOne will receive an
    // ObjectDisposedException if the Mutex is disposed of
    // by the spawned thread while WaitOne is waiting on it
    public bool NegTest2()
    {
        bool retVal = false;
        Thread thread = null;

        TestLibrary.TestFramework.BeginScenario("NegTest2: ObjectDisposedException should be thrown if current instance has already been disposed");

        try
        {
            // m_Handle is of type WaitHandle
            // Mutex is a subclass of WaitHandle
            m_Handle = new Mutex();

            // Spin up a thread.  DisposeMutex
            // simply tries to dispose of the mutex.
            thread = new Thread(new ThreadStart(DisposeMutex));
            // Start it
            thread.Start();
            Thread.Sleep(c_DEFAULT_SLEEP_TIME / 5); // To avoid race
            Thread.Sleep(c_DEFAULT_SLEEP_TIME);
            // Now, the spawned thread should have 
            // had plenty of time to dispose of the mutex
            // Calling WaitOne at this point should result
            // in an ObjectDisposedException being thrown
            m_Handle.WaitOne();

            // We should not get here
            TestLibrary.TestFramework.LogError("103", "ObjectDisposedException is not thrown if current instance has already been disposed");
            retVal = false;
        }
        catch (ObjectDisposedException)
        {
            // Swallow it, this is where we want
            // the test to go
            retVal = true;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("104", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }
        finally
        {
            if (null != thread)
            {
                // Wait for the spawned thread to finish
                // cleaning up
                thread.Join();
            }

            if (null != m_Handle)
            {
                // spawned thread was unable to dispose of it
                ((IDisposable)m_Handle).Dispose();
            }
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        WaitHandleWaitOne1 test = new WaitHandleWaitOne1();

        TestLibrary.TestFramework.BeginTestCase("WaitHandleWaitOne1");

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

    #region Private Methods
    private void SignalMutex()
    {
        // Request ownership of the mutex.
        // You can use the WaitHandle.WaitOne 
        // method to request ownership of a mutex. Block 
        // until m_Handle receives a signal
        m_Handle.WaitOne();
        // Put this thread to sleep
        // for 5 seconds
        Thread.Sleep(c_DEFAULT_SLEEP_TIME);
        // Then release the mutex
        (m_Handle as Mutex).ReleaseMutex();
    }

    private void NeverReleaseMutex()
    {
        // Request ownership of the mutex.
        // You can use the WaitHandle.WaitOne 
        // method to request ownership of a mutex. Block 
        // until m_Handle receives a signal
        m_Handle.WaitOne();
        // Put this thread to sleep
        // for 5 seconds
        Thread.Sleep(c_DEFAULT_SLEEP_TIME);
        // And we never release
        // the mutex
    }

    private void DisposeMutex()
    {
        ((IDisposable)m_Handle).Dispose();
    }
    #endregion
}
