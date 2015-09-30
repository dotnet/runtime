using System;
using System.Collections;

/// <summary>
/// Epsilon
/// </summary>
public class DoubleEpsilon
{
    public static int Main()
    {
        DoubleEpsilon test = new DoubleEpsilon();
        TestLibrary.TestFramework.BeginTestCase("DoubleEpsilon");

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
        TestLibrary.TestFramework.BeginScenario("PosTest1: Ensure Epsilon equals 4.94065645841247e-324");

        try
        {
            if (Double.Epsilon.CompareTo(4.94065645841247e-324) != 0)
            {
                TestLibrary.TestFramework.LogError("001.1", "Epsilon does not equal 4.94065645841247e-324!");
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
