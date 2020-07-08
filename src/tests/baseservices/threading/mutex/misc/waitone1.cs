// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading; // For Thread, Mutex

/// <summary>
/// WaitOne()
/// </summary>
/// 
// Tests that Mutex.WaitOne will block when another
// thread holds the mutex, and then will grab the mutex
// when it is released.  Also that appropriate exceptions
// are thrown if the other thread abandons or disposes of
// the mutex
public class MutexWaitOne1
{
    #region Private Fields
    private const int c_DEFAULT_SLEEP_TIME = 1000; // 1 second

    private Mutex m_Mutex = null;
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
    public bool PosTest1()
    {
        bool retVal = true;
        Thread thread = null;

        TestLibrary.TestFramework.BeginScenario("PosTest1: WaitOne returns true when current instance receives a signal");

        try
        {
            do
            {
                m_Mutex = new Mutex();
                thread = new Thread(new ThreadStart(SignalMutex));
                thread.Start();

                if (m_Mutex.WaitOne() != true)
                {
                    TestLibrary.TestFramework.LogError("001", "WaitOne returns false when current instance receives a signal.");
                    retVal = false;

                    break;
                }

                m_Mutex.ReleaseMutex();
            } while (false); // do
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
                thread.Join();
            }

            m_Mutex.Dispose();
        }

        return retVal;
    }
    #endregion

    #region Negative Test Cases
    public bool NegTest1()
    {
        bool retVal = true;
        Thread thread = null;

        TestLibrary.TestFramework.BeginScenario("NegTest1: AbandonedMutexException should be thrown if a thread exited without releasing a mutex");

        using (m_Mutex = new Mutex(false))
        {
            try
            {
                thread = new Thread(new ThreadStart(NeverReleaseMutex));
                thread.Start();

                thread.Join();
                m_Mutex.WaitOne();

                TestLibrary.TestFramework.LogError("101", "AbandonedMutexException is not thrown if a thread exited without releasing a mutex");
                retVal = false;
            }
            catch (AbandonedMutexException)
            {
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("102", "Unexpected exception: " + e);
                TestLibrary.TestFramework.LogInformation(e.StackTrace);
                retVal = false;
            }
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;
        Thread thread = null;

        TestLibrary.TestFramework.BeginScenario("NegTest2: ObjectDisposedException should be thrown if current instance has already been disposed");

        try
        {
            m_Mutex = new Mutex();

            thread = new Thread(new ThreadStart(DisposeMutex));
            thread.Start();

            thread.Join();
            m_Mutex.WaitOne();

            TestLibrary.TestFramework.LogError("103", "ObjectDisposedException is not thrown if current instance has already been disposed");
            retVal = false;
        }
        catch (ObjectDisposedException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("104", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }
        finally
        {
            if (null != m_Mutex)
            {
                ((IDisposable)m_Mutex).Dispose();
            }
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        MutexWaitOne1 test = new MutexWaitOne1();

        TestLibrary.TestFramework.BeginTestCase("MutexWaitOne1");

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
        m_Mutex.WaitOne();
        Thread.Sleep(c_DEFAULT_SLEEP_TIME);
        m_Mutex.ReleaseMutex();
    }

    private void NeverReleaseMutex()
    {
        m_Mutex.WaitOne();
    }

    private void DisposeMutex()
    {
        ((IDisposable)m_Mutex).Dispose();
    }
    #endregion
}
