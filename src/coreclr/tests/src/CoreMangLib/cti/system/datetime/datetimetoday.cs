// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Threading;


/// <summary>
/// System.DateTime.Today
/// </summary>
public class DateTimeToday
{
    public static int Main(string[] args)
    {
        DateTimeToday today = new DateTimeToday();
        TestLibrary.TestFramework.BeginTestCase("Testing System.DateTime.Today property...");

        if (today.RunTests())
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
        TestLibrary.TestFramework.BeginScenario("Verify the value of hour minute and second in Today property...");

        try
        {
            TestLibrary.Utilities.CurrentCulture = new System.Globalization.CultureInfo("");
            DateTime today = DateTime.Today;

            if (today.Hour != 0 && today.Minute != 0 && today.Second != 0)
            {
                TestLibrary.TestFramework.LogError("001","The initial value of today is wrong!");
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
        TestLibrary.TestFramework.BeginScenario("Verify the type of today property is System.DateTime...");

        try
        {
            DateTime today = DateTime.Today;
            Type typeOfToday = today.GetType();

            if (typeOfToday.ToString() != "System.DateTime")
            {
                TestLibrary.TestFramework.LogError("003","The type of Today property is wrong!");
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
