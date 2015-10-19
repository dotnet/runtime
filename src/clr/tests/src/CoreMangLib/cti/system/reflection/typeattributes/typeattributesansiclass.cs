using System;
using System.Reflection;

/// <summary>
/// AnsiClass [v-yishi]
/// </summary>
public class TypeAttributesAnsiClass
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify AnsiClass's value is 0x00000000");

        try
        {
            int expected = 0x00000000;
            int actual = (int)TypeAttributes.AnsiClass;

            if (expected != actual)
            {
                TestLibrary.TestFramework.LogError("001.1", "AnsiClass's value is not 0x00000000");
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
        TypeAttributesAnsiClass test = new TypeAttributesAnsiClass();

        TestLibrary.TestFramework.BeginTestCase("TypeAttributesAnsiClass");

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
