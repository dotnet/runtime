using System;
using System.Runtime.InteropServices;


///<summary>
///System.Runtime.InteropServices.CallingConvention.Winapi [v-zuolan]
///</summary>

public class CallingConventionWinapi
{

    public static int Main()
    {
        CallingConventionWinapi testObj = new CallingConventionWinapi();
        TestLibrary.TestFramework.BeginTestCase("for property of System.Runtime.InteropServices.CallingConvention.Winapi");
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
        TestLibrary.TestFramework.LogInformation("Positive");
        retVal = PosTest1() && retVal;

        return retVal;
    }


    #region Test Logic
    public bool PosTest1()
    {
        bool retVal = true;

        int expectedValue = 1;
        int actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest1:get the property");
        try
        {
            actualValue = (int)CallingConvention.Winapi;
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
