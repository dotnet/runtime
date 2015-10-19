using System;
using System.Reflection.Emit;

/// <summary>
/// Popref_popi_popr4 [v-yishi]
/// </summary>
public class StackBehaviourPopref_popi_popr4
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify the value of Popref_popi_popr4 is 15");

        try
        {
            int expected = 15;
            int actual = (int)StackBehaviour.Popref_popi_popr4;

            if (expected != actual)
            {
                TestLibrary.TestFramework.LogError("001.1", "Popref_popi_popr4's value is not 15");
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
        StackBehaviourPopref_popi_popr4 test = new StackBehaviourPopref_popi_popr4();

        TestLibrary.TestFramework.BeginTestCase("StackBehaviourPopref_popi_popr4");

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
            return 15;
        }
    }
}
