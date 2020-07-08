// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading; // For Thread, Mutex

public class TestContextBoundObject
{
    public void TestMethod()
    {
        Thread.Sleep(MutexWaitOne2.c_LONG_SLEEP_TIME);
    }
}

/// <summary>
/// WaitOne(System.Int32, System.Boolean)
/// </summary>
/// 

// Exercises Mutex:
// Waits infinitely, 
// Waits a finite time, 
// Times out appropriately,
// Throws Exceptions appropriately.
public class MutexWaitOne2
{
    #region Public Constants
    public const int c_DEFAULT_SLEEP_TIME = 1000; // 1 second
    public const int c_LONG_SLEEP_TIME = 5000; // 5 second
    #endregion

    #region Private Fields
    private Mutex m_Mutex = null;
    #endregion

    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;
        retVal = PosTest5() && retVal;
        retVal = PosTest6() && retVal;
        retVal = PosTest7() && retVal;
        
        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;
        Thread thread = null;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Wait Infinite");

        try
        {
            do
            {
                m_Mutex = new Mutex();

                thread = new Thread(new ThreadStart(SleepLongTime));
                thread.Start();

                if (m_Mutex.WaitOne(Timeout.Infinite) != true)
                {
                    TestLibrary.TestFramework.LogError("001", "Can not wait Infinite");
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

            if (null != m_Mutex)
            {
                m_Mutex.Dispose();
            }
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        Thread thread = null;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Wait some finite time");

        try
        {
            m_Mutex = new Mutex();

            thread = new Thread(new ThreadStart(SignalMutex));
            thread.Start();

            m_Mutex.WaitOne(2 * c_DEFAULT_SLEEP_TIME);

            m_Mutex.ReleaseMutex();
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003", "Unexpected exception: " + e);
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

            if (null != m_Mutex)
            {
                m_Mutex.Dispose();
            }
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        Thread thread = null;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Wait some finite time will quit for timeout");

        try
        {
            m_Mutex = new Mutex();

            thread = new Thread(new ThreadStart(SignalMutex));
            thread.Start();
            Thread.Sleep(c_DEFAULT_SLEEP_TIME / 5); // To avoid race
            if (false != m_Mutex.WaitOne(c_DEFAULT_SLEEP_TIME / 10))
            {
                m_Mutex.ReleaseMutex();
                TestLibrary.TestFramework.LogError("004", "WaitOne returns true when wait time out");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("005", "Unexpected exception: " + e);
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

            if (null != m_Mutex)
            {
                m_Mutex.Dispose();
            }
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;
        Thread thread = null;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Wait some finite time will quit for timeout when another thread is in nondefault managed context");

        try
        {
            m_Mutex = new Mutex();

            thread = new Thread(new ThreadStart(CallContextBoundObjectMethod));
            thread.Start();
            Thread.Sleep(c_DEFAULT_SLEEP_TIME); // To avoid race
            if (false != m_Mutex.WaitOne(c_DEFAULT_SLEEP_TIME / 5))
            {
                m_Mutex.ReleaseMutex();
                TestLibrary.TestFramework.LogError("006", "WaitOne returns true when wait some finite time will quit for timeout when another thread is in nondefault managed context");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("007", "Unexpected exception: " + e);
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

            if (null != m_Mutex)
            {
                m_Mutex.Dispose();
            }
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;
        Thread thread = null;

        TestLibrary.TestFramework.BeginScenario("PosTest5: Wait some finite time will quit for timeout when another thread is in nondefault managed context");

        try
        {
            m_Mutex = new Mutex();

            thread = new Thread(new ThreadStart(CallContextBoundObjectMethod));
            thread.Start();
            Thread.Sleep(c_DEFAULT_SLEEP_TIME / 5); // To avoid race
            if (false != m_Mutex.WaitOne(c_DEFAULT_SLEEP_TIME))
            {
                m_Mutex.ReleaseMutex();
                TestLibrary.TestFramework.LogError("008", "WaitOne returns true when wait some finite time will quit for timeout when another thread is in nondefault managed context");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("009", "Unexpected exception: " + e);
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

            if (null != m_Mutex)
            {
                m_Mutex.Dispose();
            }
        }

        return retVal;
    }

    public bool PosTest6()
    {
        bool retVal = true;
        Thread thread = null;

        TestLibrary.TestFramework.BeginScenario("PosTest6: Wait infinite time when another thread is in nondefault managed context");

        try
        {
            m_Mutex = new Mutex();

            thread = new Thread(new ThreadStart(CallContextBoundObjectMethod));
            thread.Start();
            Thread.Sleep(c_DEFAULT_SLEEP_TIME / 5); // To avoid race
            if (true != m_Mutex.WaitOne(Timeout.Infinite))
            {
                m_Mutex.ReleaseMutex();
                TestLibrary.TestFramework.LogError("010", "WaitOne returns false when wait infinite time when another thread is in nondefault managed context");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("011", "Unexpected exception: " + e);
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

            if (null != m_Mutex)
            {
                m_Mutex.Dispose();
            }
        }

        return retVal;
    }

    public bool PosTest7()
    {
        bool retVal = true;
        Thread thread = null;

        TestLibrary.TestFramework.BeginScenario("PosTest7: Wait infinite time when another thread is in nondefault managed context");

        try
        {
            m_Mutex = new Mutex();

            thread = new Thread(new ThreadStart(CallContextBoundObjectMethod));
            thread.Start();
            Thread.Sleep(c_DEFAULT_SLEEP_TIME / 5); // To avoid race
            if (true != m_Mutex.WaitOne(Timeout.Infinite))
            {
                m_Mutex.ReleaseMutex();
                TestLibrary.TestFramework.LogError("012", "WaitOne returns false when wait infinite time when another thread is in nondefault managed context");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("013", "Unexpected exception: " + e);
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

            if (null != m_Mutex)
            {
                m_Mutex.Dispose();
            }
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

        try
        {
            m_Mutex = new Mutex();

            thread = new Thread(new ThreadStart(NeverReleaseMutex));
            thread.Start();
            Thread.Sleep(c_DEFAULT_SLEEP_TIME / 5); // To avoid race
            m_Mutex.WaitOne(Timeout.Infinite);

            // AbandonedMutexException is not thrown on Windows 98 or Windows ME
            //if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            //{
                TestLibrary.TestFramework.LogError("101", "AbandonedMutexException is not thrown if a thread exited without releasing a mutex");
                retVal = false;
            //}
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
        finally
        {
            if (null != thread)
            {
                thread.Join();
            }

            if (null != m_Mutex)
            {
                m_Mutex.Dispose();
            }
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest2: ObjectDisposedException should be thrown if current instance has already been disposed");

        try
        {
            m_Mutex = new Mutex();

            var thread = new Thread(new ThreadStart(DisposeMutex));
            thread.IsBackground = true;
            thread.Start();
            thread.Join();
            m_Mutex.WaitOne(Timeout.Infinite);

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

    public bool NegTest3()
    {
        bool retVal = true;
        int testInt = 0;

        TestLibrary.TestFramework.BeginScenario("NegTest3: Check ArgumentOutOfRangeException will be thrown if millisecondsTimeout is a negative number other than -1");

        try
        {
            testInt = TestLibrary.Generator.GetInt32();

            if (testInt > 0)
            {
                testInt = 0 - testInt;
            }

            if (testInt == -1)
            {
                testInt--;
            }

            m_Mutex = new Mutex();
            m_Mutex.WaitOne(testInt);

            TestLibrary.TestFramework.LogError("105", "ArgumentOutOfRangeException is not thrown if millisecondsTimeout is a negative number other than -1");
            TestLibrary.TestFramework.LogInformation("[LOCAL VARIABLES] testInt = " + testInt);
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("106", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation("[LOCAL VARIABLES] testInt = " + testInt);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }
        finally
        {
            if (null != m_Mutex)
            {
                m_Mutex.Dispose();
            }
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        MutexWaitOne2 test = new MutexWaitOne2();

        TestLibrary.TestFramework.BeginTestCase("MutexWaitOne2");

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
    private void SleepLongTime()
    {
        m_Mutex.WaitOne();
        Thread.Sleep(c_LONG_SLEEP_TIME);
        m_Mutex.ReleaseMutex();
    }

    private void SignalMutex()
    {
        m_Mutex.WaitOne();
        Thread.Sleep(c_DEFAULT_SLEEP_TIME);
        m_Mutex.ReleaseMutex();
    }

    private void NeverReleaseMutex()
    {
        m_Mutex.WaitOne();
        Thread.Sleep(c_DEFAULT_SLEEP_TIME);
    }

    private void DisposeMutex()
    {
        ((IDisposable)m_Mutex).Dispose();
    }

    private void CallContextBoundObjectMethod()
    {
        m_Mutex.WaitOne();
        TestContextBoundObject obj = new TestContextBoundObject();
        obj.TestMethod();
        m_Mutex.ReleaseMutex();
    }
    #endregion
}
