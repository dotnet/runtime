// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;
using System.Threading;
using System.Globalization;


public class DateTimeMillisecond
{
    public static int Main(string[] args)
    {
        DateTimeMillisecond milliSecond = new DateTimeMillisecond();
        TestLibrary.TestFramework.BeginScenario("Testing System.DateTime.Millisecond property...");

        if (milliSecond.RunTests())
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

    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify DateTime.Milliscond property when DateTime instance's Millisecond is assigned...");

        try
        {
            TestLibrary.Utilities.CurrentCulture = new CultureInfo("");
            DateTime myDateTime = new DateTime(0978, 08, 29, 03, 00, 00,666);
            int myMillisecond = myDateTime.Millisecond;

            if (myMillisecond != 666)
            {
                TestLibrary.TestFramework.LogError("001", "The Millisecond is not correct!");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception occurs: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify DateTime.Millisecond property When DateTime instance is only assigned year month and day...");

        try
        {
            TestLibrary.Utilities.CurrentCulture = new CultureInfo("");
            DateTime myDateTime = new DateTime(1978, 08, 29);
            int myMillisecond = myDateTime.Millisecond;

            if (myMillisecond != 0)
            {
                TestLibrary.TestFramework.LogError("003", "The millisecond is not zero when no value is assigned at init time!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception occurs: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3() 
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify DateTime.Millisecond property When DateTime instance is only assigned year,month,day,hour,minute and second...");

        try
        {
            TestLibrary.Utilities.CurrentCulture = new CultureInfo("");
            DateTime myDateTime = new DateTime(1978, 08, 29, 03, 00, 00);
            int myMillisecond = myDateTime.Millisecond;

            if (myMillisecond != 0)
            {
                TestLibrary.TestFramework.LogError("005", "The millisecond is not zero when no value is assigned at init time!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception occurs: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
}
