// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Security;
using System;

public struct TestStruct
{
    public int IntValue;
    public string StringValue;
}

public class DisposableClass : IDisposable
{
    public void Dispose()
    {
    }
}

/// <summary>
/// Target
/// </summary>


[SecuritySafeCritical]
public class WeakReferenceTarget
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

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call Target on short weak reference to instance of an object");

        try
        {
            // Reclaim memories
            GC.Collect();
            GC.WaitForPendingFinalizers();

            retVal = GenerateUnusedData1("001.1") && retVal;

            // Reclaim memories
            GC.Collect();
            GC.WaitForPendingFinalizers();

            for (int i = 0; i < m_References.Length; ++i)
            {
                if (m_References[i].Target != null)
                {
                    TestLibrary.TestFramework.LogError("001.2", "Target is not null after GC collecting memory");
                    TestLibrary.TestFramework.LogInformation("WARNING: m_References[i] = " + m_References[i].Target.ToString());
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.3", "Unexpected exception: " + e);
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call Target on long weak reference to instance of an object");

        try
        {
            // Reclaim memories
            GC.Collect();
            GC.WaitForPendingFinalizers();

            retVal = GenerateUnusedData2("002.1") && retVal;

            // Reclaim memories
            GC.Collect();
            GC.WaitForPendingFinalizers();

            for (int i = 0; i < m_References.Length; ++i)
            {
                if (m_References[i].Target != null)
                {
                    TestLibrary.TestFramework.LogError("002.2", "Target is not null after GC collecting memory");
                    TestLibrary.TestFramework.LogInformation("WARNING: m_References[i] = " + m_References[i].Target.ToString());
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.3", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }
        finally
        {
            m_References = null;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Call set_Target on short weak reference to instance of an object before GC");

        try
        {
            // Reclaim memories
            GC.Collect();
            GC.WaitForPendingFinalizers();

            retVal = GenerateUnusedData1("003.1") && retVal;

            object obj = new object();
            string str = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);
            Byte[] randBytes = new Byte[c_MAX_STRING_LENGTH];
            TestLibrary.Generator.GetBytes(-55, randBytes);
;

            WeakReferenceTarget thisClass = new WeakReferenceTarget();
            DisposableClass dc = new DisposableClass();
            IntPtr ptr = new IntPtr(TestLibrary.Generator.GetInt32(-55));

            Object[] objs = new object[] {
                obj,
                str,
                randBytes,
                thisClass,
                dc,
                ptr,
                IntPtr.Zero
            };

            for (int i = 0; i < m_References.Length; ++i)
            {
                m_References[i].Target = objs[i];

                if (!m_References[i].Target.Equals(objs[i]))
                {
                    TestLibrary.TestFramework.LogError("003.2", "Target is not set correctly");
                    TestLibrary.TestFramework.LogInformation("WARNING: m_References[i].Target = " + m_References[i].Target.ToString() +
                                                                                                                  ", objs = " + objs[i].ToString() +
                                                                                                                  ", i = " + i.ToString());
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003.3", "Unexpected exception: " + e);
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

        TestLibrary.TestFramework.BeginScenario("PosTest4: Call set_Target on long weak reference to instance of an object before GC");

        try
        {
            // Reclaim memories
            GC.Collect();
            GC.WaitForPendingFinalizers();

            retVal = GenerateUnusedData2("004.1") && retVal;

            object obj = new object();
            string str = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);
            Byte[] randBytes = new Byte[c_MAX_STRING_LENGTH];
            TestLibrary.Generator.GetBytes(-55, randBytes);

            WeakReferenceTarget thisClass = new WeakReferenceTarget();
            DisposableClass dc = new DisposableClass();
            IntPtr ptr = new IntPtr(TestLibrary.Generator.GetInt32(-55));

            Object[] objs = new object[] {
                obj,
                str,
                randBytes,
                thisClass,
                dc,
                ptr,
                IntPtr.Zero
            };

            for (int i = 0; i < m_References.Length; ++i)
            {
                m_References[i].Target = objs[i];

                if (!m_References[i].Target.Equals(objs[i]))
                {
                    TestLibrary.TestFramework.LogError("004.2", "Target is not set correctly");
                    TestLibrary.TestFramework.LogInformation("WARNING: m_References[i].Target = " + m_References[i].Target.ToString() +
                                                                                                                  ", objs = " + objs[i].ToString() +
                                                                                                                  ", i = " + i.ToString());
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004.3", "Unexpected exception: " + e);
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
        WeakReferenceTarget test = new WeakReferenceTarget();

        TestLibrary.TestFramework.BeginTestCase("WeakReferenceTarget");

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
    private bool GenerateUnusedData1(string errorNo)
    {
        bool retVal = true;

        object obj = new object();
        string str = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);
        Byte[] randBytes = new Byte[c_MAX_STRING_LENGTH];
        TestLibrary.Generator.GetBytes(-55, randBytes);

        WeakReferenceTarget thisClass = new WeakReferenceTarget();
        DisposableClass dc = new DisposableClass();
        IntPtr ptr = new IntPtr(TestLibrary.Generator.GetInt32(-55));

        Object[] objs = new object[] {
            obj,
            str,
            randBytes,
            thisClass,
            dc,
            ptr,
            IntPtr.Zero
        };

        m_References = new WeakReference[objs.Length];
        for (int i = 0; i < objs.Length; ++i)
        {
            m_References[i] = new WeakReference(objs[i], false);
        }

        for (int i = 0; i < m_References.Length; ++i)
        {
            if (!m_References[i].Target.Equals(objs[i]))
            {
                TestLibrary.TestFramework.LogError(errorNo, "Target returns wrong value for weak references");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] i = " + i.ToString() +
                                                                                                                  ", m_References[i].Target = " + m_References[i].Target.ToString() +
                                                                                                                  ", objs[i] = " + objs[i].ToString());
                retVal = false;
            }
        }

        return retVal;
    }

    private bool GenerateUnusedData2(string errorNo)
    {
        bool retVal = true;

        object obj = new object();
        string str = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);
        Byte[] randBytes = new Byte[c_MAX_STRING_LENGTH];
        TestLibrary.Generator.GetBytes(-55, randBytes);

        TestStruct ts = new TestStruct();
        ts.IntValue = TestLibrary.Generator.GetInt32(-55);

        WeakReferenceTarget thisClass = new WeakReferenceTarget();
        DisposableClass dc = new DisposableClass();
        IntPtr ptr = new IntPtr(TestLibrary.Generator.GetInt32(-55));

        Object[] objs = new object[] {
            obj,
            str,
            randBytes,
            thisClass,
            dc,
            ptr,
            IntPtr.Zero
        };

        m_References = new WeakReference[objs.Length];
        for (int i = 0; i < objs.Length; ++i)
        {
            m_References[i] = new WeakReference(objs[i], true);
        }

        for (int i = 0; i < m_References.Length; ++i)
        {
            if (!m_References[i].Target.Equals(objs[i]))
            {
                TestLibrary.TestFramework.LogError(errorNo, "Target returns wrong value for weak references");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] i = " + i.ToString() +
                                                                                                                  ", m_References[i].Target = " + m_References[i].Target.ToString() +
                                                                                                                  ", objs[i] = " + objs[i].ToString());
                retVal = false;
            }
        }

        return retVal;
    }

    private void GenerateUnusedDisposableData()
    {
        DisposableClass dc = null;
        m_References = new WeakReference[1];

        try
        {
            dc = new DisposableClass();
            m_References[0] = new WeakReference(dc);
        }
        finally
        {
            if (null != dc)
            {
                dc.Dispose();
            }
        }

        GC.Collect();
    }
    #endregion
}
