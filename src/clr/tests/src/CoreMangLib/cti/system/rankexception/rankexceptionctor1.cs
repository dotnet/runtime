using System;

/// <summary>
/// RankException constructor() [v-yaduoj]
/// </summary>
public class RankExceptionCtor
{
    public static int Main()
    {
        RankExceptionCtor testObj = new RankExceptionCtor();

        TestLibrary.TestFramework.BeginTestCase("for RankException()");
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

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;

        return retVal;
    }

    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "P001";
        string c_TEST_DESC = "PosTest1: initialize an instance of type RankException via default constructor";
        string errorDesc;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            RankException e = new RankException();
            if (null == e)
            {
                errorDesc = "Failed to initialize an instance of type RankException via default constructor.";
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    #endregion
}
