// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Security;
using System;
using TestLibrary;

public class WeakReferenceIsAlive
{
    [SecuritySafeCritical]
    public static int Main(string[] args)
    {
        WeakReferenceIsAlive test = new WeakReferenceIsAlive();
        TestFramework.BeginTestCase("Testing WeakReference.IsAlive");

        if (test.RunTests())
        {
            TestFramework.EndTestCase();
            TestFramework.LogInformation("PASS");
            return 100;
        }
        else
        {
            TestFramework.EndTestCase();
            TestFramework.LogInformation("FAIL");
            return 0;
        }
    }


    [SecuritySafeCritical]
    public bool RunTests()
    {
        bool retVal = true;

        TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;

        return retVal;
    }


    [SecuritySafeCritical]

    // ATTENTION!!! ATTENTION!!! ATTENTION!!!
    //
    // If you encounter issues with object lifetime, please see more comments in WeakReferenceCtor2.cs

    public bool PosTest1()
    {
        bool retVal = true;
        TestFramework.BeginScenario("Test IsAlive with short WeakReference");

        try
        {
            WeakReference extWR = new WeakReference(WRHelper.CreateAnObject("Test"), false);

            if (!extWR.IsAlive)
            {
                TestFramework.LogError("001", "WeakReference IsAlive not as expected. Expected : True; Actual: " + extWR.IsAlive);
                retVal = false;
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();

            //Dev10 Bug #413556: WeakReference object incorrectly kept alive. Enable after the test is fixed.
            //
            //if (extWR.IsAlive)
            //{
            //    TestFramework.LogError("002", "WeakReference IsAlive not as expected. Expected : False; Actual: " + extWR.IsAlive);
            //    retVal = false;
            //}
        }
        catch (Exception e)
        {
            TestFramework.LogError("003", "Unexpected exception occured: " + e);
            retVal = false;
        }

        return retVal;
    }


    [SecuritySafeCritical]
    public bool PosTest2()
    {
        bool retVal = true;
        TestFramework.BeginScenario("Test IsAlive with long WeakReference");

        try
        {
            WeakReference extWR = new WeakReference(WRHelper.CreateAnObject("Test"), true);

            if (!extWR.IsAlive)
            {
                TestFramework.LogError("004", "WeakReference IsAlive not as expected. Expected : True; Actual: " + extWR.IsAlive);
                retVal = false;
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();

            if (!extWR.IsAlive)
            {
                TestFramework.LogError("005", "WeakReference IsAlive not as expected. Expected : True; Actual: " + extWR.IsAlive);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestFramework.LogError("006", "Unexpected exception occured: " + e);
            retVal = false;
        }

        return retVal;
    }
}
