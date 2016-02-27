// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Security;
using System;
using TestLibrary;


[SecuritySafeCritical]
public class WeakReferenceCtor
{
    public static int Main(string[] args)
    {
        WeakReferenceCtor test = new WeakReferenceCtor();
        TestFramework.BeginTestCase("Testing WeakReference.Ctor (2)");

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

    public bool RunTests()
    {
        bool retVal = true;

        TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;

        return retVal;
    }

    // ATTENTION!!! ATTENTION!!! ATTENTION!!!
    //
    // There is no guarantee that WeakReference objects will be collected after GC.Collect().
    // The bottom line is, compiler generated temps for objects created locally (within the
    // same function), castings, boxing/unboxing will get their lifetimes extended to the
    // end of the function.
    //
    // All these are due to changes related to the MinOpts feature made during the 
    // Silverlight/Arrowhead timeframe. For more information, see DevDiv Bugs 170524.
    //
    // Many of these behaviors are not documented. If any of the underlying implementation is
    // changed, the tests will need to be updated as well.

    public bool PosTest1()
    {
        bool retVal = true;
        TestFramework.BeginScenario("Test short WeakReference ctor");

        try
        {
            string testStr = "Test";
            WeakReference extWR = new WeakReference(WRHelper.CreateAnObject(testStr), false);

            if (!WRHelper.VerifyObject(extWR, testStr))
            {
                TestFramework.LogError("001", "WeakReference target not as expected. Expected : Test; Actual: " + extWR.Target.ToString());
                retVal = false;
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();

            //Dev10 Bug #413556: WeakReference object incorrectly kept alive. Enable after the test is fixed.
            //
            //if (extWR.Target != null)
            //{
            //    TestFramework.LogError("002", "WeakReference target not as expected. Expected : null; Actual: " + extWR.Target.ToString());
            //    retVal = false;
            //}

            if (extWR.TrackResurrection != false)
            {
                TestFramework.LogError("003", "WeakReference track resurrection not as expected. Expected : false; Actual: " + extWR.TrackResurrection);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestFramework.LogError("004", "Unexpected exception occurred: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        TestFramework.BeginScenario("Test long WeakReference ctor");

        try
        {
            string testStr = "Test";
            WeakReference extWR = new WeakReference(WRHelper.CreateAnObject(testStr), true);

            if (!WRHelper.VerifyObject(extWR, testStr))
            {
                TestFramework.LogError("005", "WeakReference target not as expected. Expected : Test; Actual: " + extWR.Target.ToString());
                retVal = false;
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();

            if (!WRHelper.VerifyObject(extWR, testStr))
            {
                TestFramework.LogError("006", "WeakReference target not as expected. Expected : null; Actual: " + extWR.Target.ToString());
                retVal = false;
            }

            if (extWR.TrackResurrection != true)
            {
                TestFramework.LogError("007", "WeakReference track resurrection not as expected. Expected : true; Actual: " + extWR.TrackResurrection);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestFramework.LogError("008", "Unexpected exception occurred: " + e);
            retVal = false;
        }

        return retVal;
    }
}

