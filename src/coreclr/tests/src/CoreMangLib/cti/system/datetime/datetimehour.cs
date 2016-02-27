// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;
using System.Threading;
using System.Globalization;


/// <summary>
/// System.DateTime.Hour
/// </summary>
public class DateTimeHour
{
    public static int Main(string[] args)
    {
        DateTimeHour hour = new DateTimeHour();
        TestLibrary.TestFramework.BeginTestCase("Testing System.DateTime.Hour property...");

        if (hour.RunTests())
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
        TestLibrary.TestFramework.BeginScenario("Verify DateTime.Hour property when DateTime instance's hour is assigned...");

        try
        {
            TestLibrary.Utilities.CurrentCulture = new CultureInfo("");
            DateTime myDateTime = new DateTime(0978, 08, 29, 03, 00, 00);
            int hour = myDateTime.Hour;

            if (hour != 3)
            {
                TestLibrary.TestFramework.LogError("001","The hour is not correct!");
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
        TestLibrary.TestFramework.BeginScenario("Verify DateTime.Hour property When DateTime instance is only assigned year month and day...");

        try
        {
            TestLibrary.Utilities.CurrentCulture = new CultureInfo("");
            DateTime myDateTime = new DateTime(1978,08,29);
            int hour = myDateTime.Hour;

            if (hour != 0)
            {
                TestLibrary.TestFramework.LogError("003","The hour is not zero when no value is assigned at init time!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("", "");
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
}
