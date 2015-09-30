using System;
using System.Collections;

/// <summary>
/// NaN
/// </summary>
public class DoubleNaN
{
    public static int Main()
    {
        DoubleNaN test = new DoubleNaN();
        TestLibrary.TestFramework.BeginTestCase("DoubleNaN");

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

    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Ensure NaN is not a number");

        try
        {
            if (Double.IsNaN(Double.NaN) != true)
            {
                TestLibrary.TestFramework.LogError("001.1", "NaN is a number!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
}
