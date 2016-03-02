// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Runtime.CompilerServices;

public class TestClass
{
    public const int c_DEFAULT_VALUE = 1;
    public static int m_TestInt = c_DEFAULT_VALUE;

    ~TestClass()
    {
        m_TestInt--;
    }
}

/// <summary>
/// SuppressFinalize(System.Object)
/// </summary>
public class GCSuppressFinalize
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call SuppressFinalize should prevent an object's finalizer is called during GC");

        try
        {
            TestClass.m_TestInt = TestClass.c_DEFAULT_VALUE;

            PosTest1Worker();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            if (TestClass.m_TestInt != TestClass.c_DEFAULT_VALUE)
            {
                TestLibrary.TestFramework.LogError("001.1", "Calling SuppressFinalize does not prevent an object's finalizer is called during GC");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] TestClass.m_TestInt = " + TestClass.m_TestInt);
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
        GC.SuppressFinalize(tc);
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call SuppressFinalize should prevent an object's finalizer is called during GC and then you can re-register is to finalizer calling queue");

        try
        {
            TestClass.m_TestInt = TestClass.c_DEFAULT_VALUE;

            PosTest2Worker();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            if (TestClass.m_TestInt != 0)
            {
                TestLibrary.TestFramework.LogError("002.1", "Calling SuppressFinalize prevents an object's finalizer is called during GC, but then call ReRegisterForFinalize method takes no effect");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] TestClass.m_TestInt = " + TestClass.m_TestInt);
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
        TestClass tc = new TestClass();
        GC.SuppressFinalize(tc);
        GC.ReRegisterForFinalize(tc);
    }
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: ArgumentNullException should be thrown when obj is a null reference ");

        try
        {
            GC.SuppressFinalize(null);

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
        GCSuppressFinalize test = new GCSuppressFinalize();

        TestLibrary.TestFramework.BeginTestCase("GCSuppressFinalize");

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
