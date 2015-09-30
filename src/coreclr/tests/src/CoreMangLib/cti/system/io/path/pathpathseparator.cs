using System.Security;
using System;
using System.IO;
using TestLibrary;

[SecuritySafeCritical]

/// <summary>
/// System.IO.Path.PathSeparator
/// </summary>
public class PathPathSeparator
{

    public static int Main()
    {
        PathPathSeparator pPathSeparator = new PathPathSeparator();
        TestLibrary.TestFramework.BeginTestCase("for Field:System.IO.Path.PathSeparator");

        if (pPathSeparator.RunTests())
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

    #region PositiveTesting
    public bool PosTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest1: Verify the Path.PathSeparator value is semicolon ... ";
        const string c_TEST_ID = "P001";

        char correctChar = Env.PathDelimiter[0];

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (Path.PathSeparator != correctChar)
            {
                string errorDesc = "Value is not " + correctChar.ToString() + "as expected: Actual(" + Path.PathSeparator.ToString() + ")";
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }


        return retVal;
    }
    #endregion
}

