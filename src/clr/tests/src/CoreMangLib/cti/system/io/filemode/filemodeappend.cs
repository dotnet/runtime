using System;
using System.IO;

/// <summary>
/// System.IO.FileMode.Append
/// </summary>
public class FileModeAppend
{
    static public int Main()
    {
        FileModeAppend test = new FileModeAppend();

        TestLibrary.TestFramework.BeginTestCase("System.IO.FileMode.Append");

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

        const string c_TEST_DESC = "PosTest1:check the FileMode.Append value is 6...";
        const string c_TEST_ID = "P001";
        FileMode FLAG_VALUE = (FileMode)6;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {

            if (FileMode.Append != FLAG_VALUE)
            {
                string errorDesc = "value is not " + FLAG_VALUE.ToString() + " as expected: Actual is " + FileMode.Append.ToString();
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


}