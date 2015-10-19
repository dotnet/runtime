using System;
using System.Reflection;

/// <summary>
/// RTSpecialName [v-yishi]
/// </summary>
public class TypeAttributesRTSpecialName
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify RTSpecialName's value is 0x00000800");

        try
        {
            int expected = 0x00000800;
            int actual = (int)TypeAttributes.RTSpecialName;

            if (expected != actual)
            {
                TestLibrary.TestFramework.LogError("001.1", "RTSpecialName's value is not 0x00000800");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] expected = " + expected + ", actual = " + actual);
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
        TypeAttributesRTSpecialName test = new TypeAttributesRTSpecialName();

        TestLibrary.TestFramework.BeginTestCase("TypeAttributesRTSpecialName");

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
