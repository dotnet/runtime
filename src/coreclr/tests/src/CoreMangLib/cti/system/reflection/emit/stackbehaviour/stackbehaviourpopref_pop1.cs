using System;
using System.Reflection.Emit;

/// <summary>
/// Popref_pop1 [v-yishi]
/// </summary>
public class StackBehaviourPopref_pop1
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify the value of Popref_pop1 is 11");

        try
        {
            int expected = 11;
            int actual = (int)StackBehaviour.Popref_pop1;

            if (expected != actual)
            {
                TestLibrary.TestFramework.LogError("001.1", "Popref_pop1's value is not 11");
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
        StackBehaviourPopref_pop1 test = new StackBehaviourPopref_pop1();

        TestLibrary.TestFramework.BeginTestCase("StackBehaviourPopref_pop1");

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
            return 11;
        }
    }
}
