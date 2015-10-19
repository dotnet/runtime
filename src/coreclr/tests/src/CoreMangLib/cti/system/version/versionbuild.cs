using System;

/// <summary>
/// Build
/// </summary>

public class VersionBuild
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        //
        // TODO: Add your negative test cases here
        //
        // TestLibrary.TestFramework.LogInformation("[Negative]");
        // retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify property Build with default ctor.");
/*
        try
        {
            Version version = new Version();

            if (version.Build != -1)
            {
                TestLibrary.TestFramework.LogError("001.1", "Property Build Err !");
                retVal = false;
                return retVal;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }
*/
        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify property Build with args ctor.");

        try
        {
            int intMajor = TestLibrary.Generator.GetInt32(-55);
            int intMinor = TestLibrary.Generator.GetInt32(-55);
            int intBuild = TestLibrary.Generator.GetInt32(-55);
            int intRevision = TestLibrary.Generator.GetInt32(-55);
            Version version = new Version(intMajor, intMinor, intBuild, intRevision);

            if (version.Build != intBuild)
            {
                Console.WriteLine("excepted value is :" + version.Build);
                Console.WriteLine("actual value is :" + intBuild);

                TestLibrary.TestFramework.LogError("002.1", "Property Build Err !");
                retVal = false;
                return retVal;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    //public bool NegTest1()
    //{
    //    bool retVal = true;

    //    TestLibrary.TestFramework.BeginScenario("NegTest1: ");

    //    try
    //    {
    //          //
    //          // Add your test logic here
    //          //
    //    }
    //    catch (Exception e)
    //    {
    //        TestLibrary.TestFramework.LogError("101", "Unexpected exception: " + e);
    //        TestLibrary.TestFramework.LogInformation(e.StackTrace);
    //        retVal = false;
    //    }

    //    return retVal;
    //}
    #endregion
    #endregion

    public static int Main()
    {
        VersionBuild test = new VersionBuild();

        TestLibrary.TestFramework.BeginTestCase("VersionBuild");

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
