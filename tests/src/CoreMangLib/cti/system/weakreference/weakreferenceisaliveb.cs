// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Security;
using System;

public class DisposableClass : IDisposable
{
    public void Dispose()
    {
    }
}

/// <summary>
/// IsAlive
/// </summary>

[SecuritySafeCritical]
public class WeakReferenceIsAlive
{
    #region Private Fields
    private const int c_MIN_STRING_LENGTH = 8;
    private const int c_MAX_STRING_LENGTH = 1024;

    private WeakReference[] m_References;
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

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: IsAlive should return true before GC collecting memory for short weak references");

        try
        {
            // Reclaim memories
            GC.Collect();
            GC.WaitForPendingFinalizers();

            GenerateUnusedData1();

            for (int i = 0; i < m_References.Length; ++i)
            {
                if (!m_References[i].IsAlive)
                {
                    TestLibrary.TestFramework.LogError("001.1", "IsAlive return false before GC collecting memory");
                    if (m_References[i].Target != null)
                    {
                        TestLibrary.TestFramework.LogInformation("WARNING: m_References[i] = " + m_References[i].Target.ToString());
                    }
                    else
                    {
                        TestLibrary.TestFramework.LogInformation("WARNING: m_References[i] = null");
                    }
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }
        finally
        {
            m_References = null;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: IsAlive should return false before GC collecting memory for weak references reference null");

        try
        {
            WeakReference reference = new WeakReference(null, false);
            if (reference.IsAlive)
            {
                TestLibrary.TestFramework.LogError("002.1", "IsAlive returns true before GC collecting memory");
                retVal = false;
            }

            reference = new WeakReference(null, true);
            if (reference.IsAlive)
            {
                TestLibrary.TestFramework.LogError("002.2", "IsAlive returns true before GC collecting memory");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.3", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: IsAlive should return false after GC collecting memory for short weak references");

        try
        {
            // Reclaim memories
            GC.Collect();
            GC.WaitForPendingFinalizers();
            
            GenerateUnusedData1();

            // Reclaim memories
            GC.Collect();
            GC.WaitForPendingFinalizers();

            for (int i = 0; i < m_References.Length; ++i)
            {
                if (m_References[i].IsAlive)
                {
                    TestLibrary.TestFramework.LogError("003.1", "IsAlive returns true after GC collecting memory");
                    if (m_References[i].Target != null)
                    {
                        TestLibrary.TestFramework.LogInformation("WARNING: m_References[i] = " + m_References[i].Target.ToString());
                    }
                    else
                    {
                        TestLibrary.TestFramework.LogInformation("WARNING: m_References[i] = null");
                    }
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }
        finally
        {
            m_References = null;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: IsAlive should return true before GC collecting memory for long weak references");

        try
        {
            // Reclaim memories
            GC.Collect();
            GC.WaitForPendingFinalizers();

            GenerateUnusedData2();

            for (int i = 0; i < m_References.Length; ++i)
            {
                if (!m_References[i].IsAlive)
                {
                    TestLibrary.TestFramework.LogError("004.1", "IsAlive return false before GC collecting memory");
                    if (m_References[i].Target != null)
                    {
                        TestLibrary.TestFramework.LogInformation("WARNING: m_References[" + i.ToString() + "] = " + m_References[i].Target.ToString());
                    }
                    else
                    {
                        TestLibrary.TestFramework.LogInformation("WARNING: m_References[" + i.ToString() + "] = null");
                    }
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }
        finally
        {
            m_References = null;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest5: IsAlive should return true after GC collecting memory for long weak references");

        try
        {
            // Reclaim memories
            GC.Collect();
            GC.WaitForPendingFinalizers();

            GenerateUnusedData2();

            // Reclaim memories
            GC.Collect();
            GC.WaitForPendingFinalizers();

            for (int i = 0; i < m_References.Length; ++i)
            {
                if (m_References[i].IsAlive)
                {
                    TestLibrary.TestFramework.LogError("005.1", "IsAlive returns true before GC collecting memory");
                    if (m_References[i].Target != null)
                    {
                        TestLibrary.TestFramework.LogInformation("WARNING: m_References[i] = " + m_References[i].Target.ToString());
                    }
                    else
                    {
                        TestLibrary.TestFramework.LogInformation("WARNING: m_References[i] = null");
                    }
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("005.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }
        finally
        {
            m_References = null;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        WeakReferenceIsAlive test = new WeakReferenceIsAlive();

        TestLibrary.TestFramework.BeginTestCase("WeakReferenceIsAlive");

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
    private void GenerateUnusedData1()
    {
        int intRandValue = TestLibrary.Generator.GetInt32(-55);
        long longRandValue = TestLibrary.Generator.GetInt64(-55);
        double doubleRandValue = TestLibrary.Generator.GetDouble(-55);

        object obj = new object();
        string str = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);
        Byte[] randBytes = new Byte[c_MAX_STRING_LENGTH];
        TestLibrary.Generator.GetBytes(-55, randBytes);

        WeakReferenceIsAlive thisClass = new WeakReferenceIsAlive();
        DisposableClass dc = new DisposableClass();
        IntPtr ptr = new IntPtr(TestLibrary.Generator.GetInt32(-55));

        m_References = new WeakReference[] {
            new WeakReference(obj, false),
            new WeakReference(str, false),
            new WeakReference(randBytes, false),
            new WeakReference(thisClass, false),
            new WeakReference(ptr, false),
            new WeakReference(IntPtr.Zero, false),
            new WeakReference(dc, false)
        };
    }

    private void GenerateUnusedData2()
    {
        object obj = new object();
        string str = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);
        Byte[] randBytes = new Byte[c_MAX_STRING_LENGTH];
        TestLibrary.Generator.GetBytes(-55, randBytes);

        WeakReferenceIsAlive thisClass = new WeakReferenceIsAlive();
        DisposableClass dc = new DisposableClass();
        IntPtr ptr = new IntPtr(TestLibrary.Generator.GetInt32(-55));
        m_References = new WeakReference[] {
            new WeakReference(obj, true),
            new WeakReference(str, true),
            new WeakReference(randBytes, true),
            new WeakReference(thisClass, true),
            new WeakReference(ptr, true),
            new WeakReference(IntPtr.Zero, true),
            new WeakReference(dc, true)
        };
    }
    #endregion
}
