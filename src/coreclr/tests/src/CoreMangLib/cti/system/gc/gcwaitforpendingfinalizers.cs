// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Threading;
using System.Runtime.CompilerServices;

public class TestClass
{
    GCWaitForPendingFinalizers creator;

    public TestClass(GCWaitForPendingFinalizers _creator)
    {
        creator = _creator;
    }

    ~TestClass()
    {
        creator.f_TestClassFinalizerExecuted = true;
    }
}

/// <summary>
/// WaitForPendingFinalizers
/// </summary>
public class GCWaitForPendingFinalizers
{
    public bool f_TestClassFinalizerExecuted = false;

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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call WaitForPendingFinalizers should wait all finalizer are called");

        try
        {
            PosTest1Worker();

            GC.Collect();
            GC.WaitForPendingFinalizers();

            if (!f_TestClassFinalizerExecuted)
            {
                TestLibrary.TestFramework.LogError("001.1", "Call WaitForPendingFinalizers does not wait for finalizer");
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
        TestClass tc = new TestClass(this);
    }
    #endregion
    #endregion

    public static int Main()
    {
        GCWaitForPendingFinalizers test = new GCWaitForPendingFinalizers();

        TestLibrary.TestFramework.BeginTestCase("GCWaitForPendingFinalizers");

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
