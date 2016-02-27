// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Globalization;

/// <summary>
/// System.DateTime.Date
/// </summary>
public class DateTimeDate
{
    public static int Main(string[] args)
    {
        DateTimeDate date = new DateTimeDate();
        TestLibrary.TestFramework.BeginTestCase("Testing System.DateTime.Date property...");

        if (date.RunTests())
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
        TestLibrary.TestFramework.BeginScenario("Verify Date property when the created DateTime instance just is assigned to year,month and day...");

        try
        {
            TestLibrary.Utilities.CurrentCulture = new CultureInfo("");
            DateTime myDateTime = new DateTime(1978, 08, 29);
            DateTime myDate = myDateTime.Date;

            if (myDateTime.Year != myDate.Year || myDateTime.Month != myDate.Month || myDateTime.Day != myDate.Day)
            {
                TestLibrary.TestFramework.LogError("001", "The Date is wrong!");
                retVal = false;
            }
            else
            {
                if (myDate.Hour != 0 || myDate.Minute != 0 || myDate.Second != 0)
                {
                    TestLibrary.TestFramework.LogError("002", "The initial time value is not equal to minnight!");
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003", "Unexpected exception occurs: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify Date property when created DateTime instance's time value is less than 12:00:00...");

        try
        {
            TestLibrary.Utilities.CurrentCulture = new CultureInfo("");
            DateTime myDateTime = new DateTime(1978, 08, 29, 03, 00, 00);
            DateTime myDate = myDateTime.Date;

            if (myDate.Year != myDateTime.Year || myDate.Month != myDateTime.Month || myDate.Day != myDateTime.Day)
            {
                TestLibrary.TestFramework.LogError("004", "The Date is wrong!");
                retVal = false;
            }
            else if (myDate.Hour != 0 || myDate.Minute != 0 || myDate.Second != 0)
            {
                TestLibrary.TestFramework.LogError("005", "The initial time value is not equal to minnight!");
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

    public bool PosTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify Date property when created DateTime instance's time value is more than 12:00:00...");

        try
        {
            TestLibrary.Utilities.CurrentCulture = new CultureInfo("");
            DateTime myDateTime = new DateTime(1978, 08, 29, 23, 00, 00);
            DateTime myDate = myDateTime.Date;

            if (myDateTime.Year != myDate.Year || myDateTime.Month != myDateTime.Month || myDateTime.Day != myDate.Day)
            {
                TestLibrary.TestFramework.LogError("007", "The Date is wrong!");
                retVal = false;
            }
            else if (myDate.Hour != 0 || myDate.Minute != 0 || myDate.Second != 0)
            {
                TestLibrary.TestFramework.LogError("008", "The initial time value is not equal to minnight!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("009", "Unexpected exception occurs: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
}
