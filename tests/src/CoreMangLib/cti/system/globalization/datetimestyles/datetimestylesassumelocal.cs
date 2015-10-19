using System;
using System.Globalization;

///<summary>
///System.Globalization.DateTimeStyles.AssumeLocal
///</summary>

public class DateTimeStylesAssumeLocal
{

    public static int Main()
    {
        DateTimeStylesAssumeLocal testObj = new DateTimeStylesAssumeLocal();
        TestLibrary.TestFramework.BeginTestCase("for property of System.Globalization.DateTimeStyles.AssumeLocal");
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

        UInt64 expectedValue = 0x00000020;
        UInt64 actualValue;


        TestLibrary.TestFramework.BeginScenario("PosTest1:get DateTimeStyles.AssumeLocal");
        try
        {
            actualValue = (UInt64)DateTimeStyles.AssumeLocal;

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
