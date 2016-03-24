// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

public class TestClass
{
    public static int m_TestInt = 1;

    ~TestClass()
    {
        m_TestInt--;
    }
}

/// <summary>
/// KeepAlive(System.Object)
/// </summary>
public class GCKeepAlive
{
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call KeepAlive to prevent an object to be GCed");

        try
        {
            TestClass tc = new TestClass();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            GC.KeepAlive(tc);
            if (TestClass.m_TestInt != 1)
            {
                TestLibrary.TestFramework.LogError("001.1", "Calling KeepAlive can not prevent an object to be GCed");
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
    #endregion
    #endregion

    public static int Main()
    {
        GCKeepAlive test = new GCKeepAlive();

        TestLibrary.TestFramework.BeginTestCase("GCKeepAlive");

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
