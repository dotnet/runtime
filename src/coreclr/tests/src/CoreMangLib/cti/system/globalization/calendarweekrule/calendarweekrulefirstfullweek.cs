// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

///<summary>
///System.Globalization.CalendarWeekRule.FirstFullWeek
///</summary>

public class CalendarWeekRuleFirstFullWeek
{

    public static int Main()
    {
        CalendarWeekRuleFirstFullWeek testObj = new CalendarWeekRuleFirstFullWeek();
        TestLibrary.TestFramework.BeginTestCase("for the System.CalendarWeekRule.FirstDay");
        if (testObj.RunTests())
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
        retVal = PosTest1() && retVal;
        return retVal;
    }


    #region Test Logic
    public bool PosTest1()
    {
        bool retVal = true;

        int expectedValue = 1;
        int actualValue;


        TestLibrary.TestFramework.BeginScenario("PosTest1:get CalendarWeekRule.FirstFullWeek");
        try
        {
            actualValue = (int)CalendarWeekRule.FirstFullWeek;

            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("001", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}
