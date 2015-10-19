using System;

///<summary>
///System.DayOfWeek.Sunday
///</summary>

public class DayOfWeekSunday
{

    public static int Main()
    {
        DayOfWeekSunday testObj = new DayOfWeekSunday();
        TestLibrary.TestFramework.BeginTestCase("for property of System.DayOfWeek.Sunday");
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

        int expectedValue = 0;
        int actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest1:get the property.");
        try
        {
            actualValue = (int)DayOfWeek.Sunday;
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
