using System.Security;
using System;
using System.IO;
using TestLibrary;

  [SecuritySafeCritical]

/// <summary>
/// System.IO.Path.DirectorySeparatorChar
/// </summary>
public class PathDirectorySeparatorChar
{

    public static int Main()
    {
        PathDirectorySeparatorChar pDirectorySeparatorChar = new PathDirectorySeparatorChar();
        TestLibrary.TestFramework.BeginTestCase("for Field:System.IO.Path.DirectorySeparatorChar");

        if (pDirectorySeparatorChar.RunTests())
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
        const string c_TEST_DESC = "PosTest1: Verify the Path.DirectorySeparatorChar value is backslash... ";
        const string c_TEST_ID = "P001";

        char correctChar = Env.FileSeperator[0];

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (Path.DirectorySeparatorChar != correctChar)
            {
                string errorDesc = "Value is not " + correctChar.ToString() + "as expected: Actual(" + Path.DirectorySeparatorChar.ToString() + ")";
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

