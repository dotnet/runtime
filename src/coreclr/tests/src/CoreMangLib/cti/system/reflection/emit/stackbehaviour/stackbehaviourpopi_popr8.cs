using System;
using System.Reflection.Emit;

/// <summary>
/// Popi_popr8 [v-yishi]
/// </summary>
public class StackBehaviourPopi_popr8
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify the value of Popi_popr8 is 9");

        try
        {
            int expected = 9;
            int actual = (int)StackBehaviour.Popi_popr8;

            if (expected != actual)
            {
                TestLibrary.TestFramework.LogError("001.1", "Popi_popr8's value is not 9");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] expected = " + expected + ", actual = " + actual);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        StackBehaviourPopi_popr8 test = new StackBehaviourPopi_popr8();

        TestLibrary.TestFramework.BeginTestCase("StackBehaviourPopi_popr8");

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
            return 9;
        }
    }
}
