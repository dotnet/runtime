// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;
using System.Threading;

/// <summary>
/// System.DateTime.TimeOfDay
/// </summary>
public class DateTimeTimeOfDay
{
    public static int Main(string[] args)
    {
        DateTimeTimeOfDay timeOfDay = new DateTimeTimeOfDay();
        TestLibrary.TestFramework.BeginTestCase("Testing System.DateTime.TimeOfDay property...");

        if (timeOfDay.RunTests())
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

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify TimeOfDay when created instance is assigned time...");

        try
        {
            TestLibrary.Utilities.CurrentCulture = new System.Globalization.CultureInfo("");
            DateTime myDateTime = new DateTime(1978,08,29,03,29,22);
            TimeSpan myTimeSpan = myDateTime.TimeOfDay;

            if (myTimeSpan.Days != 0 && myTimeSpan.Hours != 03 && myTimeSpan.Minutes != 29
                && myTimeSpan.Seconds != 22 && myTimeSpan.Milliseconds != 0)
            {
                TestLibrary.TestFramework.LogError("001","The TimeSpan is wrong!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002","Unexpected exception occurs: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify TimeOfDay when created instance is not assigned time...");

        try
        {
            TestLibrary.Utilities.CurrentCulture = new System.Globalization.CultureInfo("");
            DateTime myDateTime = new DateTime(1978,08,29);
            TimeSpan myTimeSpan = myDateTime.TimeOfDay;

            if (myTimeSpan.Hours != 0 && myTimeSpan.Minutes != 0 && myTimeSpan.Seconds != 0)
            {
                TestLibrary.TestFramework.LogError("003","The initial timeSpan is wrong!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004","Unexpected exception occurs: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
}

