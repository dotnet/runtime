// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading; // For Thread, Mutex

/// <summary>
/// Ctor(Boolean)
/// </summary>
/// 
// Tests that you can create a mutex, that if the mutex is 
// owned that other threads cannot change shared state governed 
// by that mutex, and that if it is unowned we can pick it up 
// without blocking.
public class MutexCtor1
{
    #region Public Fields
    public const int c_DEFAULT_INT_VALUE = 0;

    public volatile static int m_SharedResource = c_DEFAULT_INT_VALUE;
    #endregion

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
        retVal = PosTest2() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;
        Thread thread = null;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest1: Construct a new Mutex instance with initiallyOwned set to true (ensure that the thread owns the mutex)");

        using(m_Mutex = new Mutex(true))
        {
            try
            {
                do
                {
                    if (null == m_Mutex)
                    {
                        TestLibrary.TestFramework.LogError("001", "Can not construct a new Mutex intance with initiallyOwned set to true.");
                        retVal = false;

                        break;
                    }

                    // Ensure initial owner of the mutex is current thread 

                    // Create another thread to change value of m_SharedResource
                    thread = new Thread(new ThreadStart(ThreadProc));
                    thread.Start();

                    // Sleep 1 second to wait the thread get started
                    Thread.Sleep(c_DEFAULT_SLEEP_TIME);

                    if (m_SharedResource != c_DEFAULT_INT_VALUE)
                    {
                        TestLibrary.TestFramework.LogError("002", "Call Mutex(true) does not set current thread to be the owner of the mutex.");
                        retVal = false;
                    }
                    m_Mutex.ReleaseMutex();
                } while (false); // do
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("003", "Unexpected exception: " + e);
                TestLibrary.TestFramework.LogInformation(e.StackTrace);
                retVal = false;
            }
            finally
            {
                if (null != thread)
                {
                    // Wait until all threads are terminated
                    thread.Join();
                }

                // Reset the value of m_SharedResource for further usage
                m_SharedResource = c_DEFAULT_INT_VALUE;
            }
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest2: Construct a new Mutex instance with initiallyOwned set to false.  Ensure that the thread does not own the mutex");

        using (m_Mutex = new Mutex(false))
        {
            try
            {
                if (null == m_Mutex)
                {
                    TestLibrary.TestFramework.LogError("004", "Can not construct a new Mutex intance with initiallyOwned set to false.");
                    retVal = false;
                }
                else
                {
                    // this would block this thread if we owned the mutex
                    m_Mutex.WaitOne();
                    m_Mutex.ReleaseMutex();
                }
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
                TestLibrary.TestFramework.LogInformation(e.StackTrace);
                retVal = false;
            }
            finally
            {
                // Reset the value of m_SharedResource for further usage
                m_SharedResource = c_DEFAULT_INT_VALUE;
            }
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        MutexCtor1 test = new MutexCtor1();

        TestLibrary.TestFramework.BeginTestCase("MutexCtor1");

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
    private void ThreadProc()
    {
        m_Mutex.WaitOne();
        m_SharedResource++;
        m_Mutex.ReleaseMutex();
    }
    #endregion
}
