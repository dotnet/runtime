using System;
using System.Globalization;

///<summary>
///System.Globalization.CalendarWeekRule.FirstFourDayWeek
///</summary>

public class CalendarWeekRuleFirstFourDayWeek
{

    public static int Main()
    {
        CalendarWeekRuleFirstFourDayWeek testObj = new CalendarWeekRuleFirstFourDayWeek();
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

        int expectedValue = 2;
        int actualValue;


        TestLibrary.TestFramework.BeginScenario("PosTest1:get CalendarWeekRule.FirstFourDayWeek");
        try
        {
            actualValue = (int)CalendarWeekRule.FirstFourDayWeek;

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
