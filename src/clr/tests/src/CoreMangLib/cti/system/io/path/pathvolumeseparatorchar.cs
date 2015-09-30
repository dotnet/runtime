using System.Security;
using System;
using System.IO;
using TestLibrary;

[SecuritySafeCritical]

/// <summary>
/// System.IO.Path.VolumeSeparatorChar
/// </summary>
public class PathVolumeSeparatorChar
{

    public static int Main()
    {
        PathVolumeSeparatorChar pVolumeSeparatorChar = new PathVolumeSeparatorChar();
        TestLibrary.TestFramework.BeginTestCase("for Field:System.IO.Path.VolumeSeparatorChar");

        if (pVolumeSeparatorChar.RunTests())
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
        const string c_TEST_DESC = "PosTest1: Verify the Path.VolumeSeparatorChar value is colon ... ";
        const string c_TEST_ID = "P001";

        char correctChar = Env.VolumeSeperator[0];

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (Path.VolumeSeparatorChar != correctChar)
            {
                string errorDesc = "Value is not " + correctChar.ToString() + "as expected: Actual(" + Path.VolumeSeparatorChar.ToString() + ")";
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

