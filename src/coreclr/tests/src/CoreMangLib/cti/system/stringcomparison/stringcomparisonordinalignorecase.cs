using System;

/// <summary>
/// OrdinalIgnoreCase [v-yishi]
/// </summary>
public class StringComparisonOrdinalIgnoreCase
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify OrdinalIgnoreCase's value is 5");

        try
        {
            int actual = (int)StringComparison.OrdinalIgnoreCase;
            if (actual != 5)
            {
                TestLibrary.TestFramework.LogError("001.1", "OrdinalIgnoreCase's value is not 5");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] actual = " + actual);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        StringComparisonOrdinalIgnoreCase test = new StringComparisonOrdinalIgnoreCase();

        TestLibrary.TestFramework.BeginTestCase("StringComparisonOrdinalIgnoreCase");

        if (test.RunTests())
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
}
