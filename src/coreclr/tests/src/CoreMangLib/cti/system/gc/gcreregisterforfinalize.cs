// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Runtime.CompilerServices;

public class TestClass
{
    public const int c_DEFAULT_VALUE = 2;
    public static int m_TestInt = c_DEFAULT_VALUE;

    private bool m_hasFinalized = false;

    ~TestClass()
    {
        m_TestInt--;

        if (!m_hasFinalized)
        {
            m_hasFinalized = true;
            GC.ReRegisterForFinalize(this);
        }
    }
}

public class TestClass1
{
    public const int c_DEFAULT_VALUE = 2;
    public static int m_TestInt = c_DEFAULT_VALUE;

    ~TestClass1()
    {
        m_TestInt--;
    }
}

/// <summary>
/// ReRegisterForFinalize(System.Object)
/// </summary>
public class GCReRegisterForFinalize
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call ReRegisterForFinalize to register the Finalizer for twice");

        try
        {
            PosTest1Worker();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Call the Finalizer second time
            GC.WaitForPendingFinalizers();
            GC.Collect();

            if (TestClass.m_TestInt != 0)
            {
                TestLibrary.TestFramework.LogError("001.1", "Calling ReRegisterForFinalize has no effect");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] TestClass.m_TestInt = " + TestClass.m_TestInt);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public void PosTest1Worker()
    {
        TestClass tc = new TestClass();
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call ReRegisterForFinalize to register the Finalizer for twice with the caller is not the current object");

        try
        {
            PosTest2Worker();

            GC.Collect();
            // Call the Finalizer second time
            GC.WaitForPendingFinalizers();
            GC.Collect();

            if (TestClass1.m_TestInt != 0)
            {
                TestLibrary.TestFramework.LogError("002.1", "Calling ReRegisterForFinalize has no effect");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] TestClass1.m_TestInt = " + TestClass1.m_TestInt);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public void PosTest2Worker()
    {
        TestClass1 tc = new TestClass1();

        GC.Collect();
        GC.WaitForPendingFinalizers();
	GC.ReRegisterForFinalize(tc);
    }   
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: ArgumentNullException should be thrown when obj is a null reference");

        try
        {
            GC.ReRegisterForFinalize(null);

            TestLibrary.TestFramework.LogError("101.1", "ArgumentNullException is not thrown when obj is a null reference");
            retVal = false;
        }
        catch (ArgumentNullException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("101.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        GCReRegisterForFinalize test = new GCReRegisterForFinalize();

        TestLibrary.TestFramework.BeginTestCase("GCReRegisterForFinalize");

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
