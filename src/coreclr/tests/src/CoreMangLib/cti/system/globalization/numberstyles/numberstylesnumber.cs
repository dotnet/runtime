using System;
using System.Globalization;

/// <summary>
/// Number [v-jianq]
/// </summary>

public class NumberStylesNumber
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        
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
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify value of NumberStyles.Number .");

        try
        {
            int AllowLeadingWhite = 0x00000001,

                 AllowTrailingWhite = 0x00000002,

                 AllowLeadingSign = 0x00000004,

                 AllowTrailingSign = 0x00000008,

                 AllowDecimalPoint = 0x00000020,

                 AllowThousands = 0x00000040;

            int expected = AllowLeadingWhite | AllowTrailingWhite | AllowLeadingSign | AllowTrailingSign |
                           AllowDecimalPoint | AllowThousands;


            int actual = (int)NumberStyles.Number;

            if (actual != expected)
            {
                TestLibrary.TestFramework.LogError("001.1", "Value of NumberStyles.Number Err .");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] actual = " + actual + ", expected = " + expected);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.2", "Unexpected exception: " + e);
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
        NumberStylesNumber test = new NumberStylesNumber();

        TestLibrary.TestFramework.BeginTestCase("NumberStylesNumber");

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
