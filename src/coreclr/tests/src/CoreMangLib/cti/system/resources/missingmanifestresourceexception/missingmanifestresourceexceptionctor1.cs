using System;
using System.Resources;

/// <summary>
/// ctor [v-yishi]
/// </summary>
public class MissingManifestResourceExceptionCtor1
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call ctor to create a new instance");

        try
        {
            MissingManifestResourceException ex = new MissingManifestResourceException();
            if (ex == null)
            {
                TestLibrary.TestFramework.LogError("001.1", "Calling ctor returns null reference");
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
        MissingManifestResourceExceptionCtor1 test = new MissingManifestResourceExceptionCtor1();

        TestLibrary.TestFramework.BeginTestCase("MissingManifestResourceExceptionCtor1");

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
