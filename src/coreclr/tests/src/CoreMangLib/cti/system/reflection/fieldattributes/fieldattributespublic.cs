using System;
using System.Reflection;

/// <summary>
/// System.Reflection.FiledAttributes.Public [v-jiajul]
/// </summary>
public class FieldAttributesPublic
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Check the Int32 value of the enumeration");

        try
        {
            FieldAttributes fieldAttributes = (FieldAttributes)6;
            if (fieldAttributes != FieldAttributes.Public)
            {
                TestLibrary.TestFramework.LogError("001", "Result is not the value as expected,fieldAttributes is: " + fieldAttributes.ToString());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    #endregion
    #endregion

    public static int Main()
    {
        FieldAttributesPublic test = new FieldAttributesPublic();

        TestLibrary.TestFramework.BeginTestCase("FieldAttributesPublic");

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
