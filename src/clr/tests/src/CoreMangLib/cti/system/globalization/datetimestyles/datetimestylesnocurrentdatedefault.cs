using System;
using System.Globalization;

///<summary>
///System.Globalization.DateTimeStyles.NoCurrentDateDefault
///</summary>

public class DateTimeStylesNoCurrentDateDefault
{

    public static int Main()
    {
        DateTimeStylesNoCurrentDateDefault testObj = new DateTimeStylesNoCurrentDateDefault();
        TestLibrary.TestFramework.BeginTestCase("for property of System.Globalization.DateTimeStyles.NoCurrentDateDefault");
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

        UInt64 expectedValue = 0x00000008;
        UInt64 actualValue;


        TestLibrary.TestFramework.BeginScenario("PosTest1:get DateTimeStyles.NoCurrentDateDefault");
        try
        {
            actualValue = (UInt64)DateTimeStyles.NoCurrentDateDefault;

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
