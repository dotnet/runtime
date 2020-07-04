// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading; // For Thread, Mutex

/// <summary>
/// Ctor()
/// </summary>
/// 
// tests the default mutex constructor creates
// a mutex which is not owned.
public class MutexCtor2
{
    #region Public Fields
    public const int c_DEFAULT_INT_VALUE = 0;

    public static int m_SharedResource = c_DEFAULT_INT_VALUE;
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

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest1: Construct a new Mutex instance");

        // create the mutex in unowned state
        using (m_Mutex = new Mutex())
        {
            try
            {
                do
                {
                    if (null == m_Mutex)
                    {
                        TestLibrary.TestFramework.LogError("001", "Can not construct a new Mutex intance");
                        retVal = false;

                        break;
                    }

                    // Ensure initial owner of the mutex is not the current thread 
                    //  This call should NOT block
                    m_Mutex.WaitOne();
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
        MutexCtor2 test = new MutexCtor2();

        TestLibrary.TestFramework.BeginTestCase("MutexCtor2");

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
