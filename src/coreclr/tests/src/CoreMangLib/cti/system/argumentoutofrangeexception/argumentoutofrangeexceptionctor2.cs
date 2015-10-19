using System;

/// <summary>
/// System.ArgumentOutOfRangeException.Ctor(String paramname)
/// </summary>
public class ArgumentOutOfRangeExceptionCtor2
{
    private const int c_MIN_STRING_LENGTH = 1;
    private const int c_MAX_STRING_LENGTH = 256;

    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call ctor with a random string argument to construct a new instance");

        try
        {
            string randValue = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);
            ArgumentOutOfRangeException argumentOutOfRangeException = new ArgumentOutOfRangeException(randValue);
            if (argumentOutOfRangeException == null)
            {
                TestLibrary.TestFramework.LogError("001", "The result is not the value as expected");
                retVal = false;
            }
            if ((argumentOutOfRangeException.Message != "Specified argument was out of the range of valid values."+TestLibrary.Env.NewLine+"Parameter name: " + randValue) &
                (!argumentOutOfRangeException.Message.Contains("[Arg_ArgumentOutOfRangeException]")))
            {
                TestLibrary.TestFramework.LogError("002", "The result is not the value as expected" + argumentOutOfRangeException.Message);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call ctor with a null argument to construct a new instance");

        try
        {
            string randValue = null;
            ArgumentOutOfRangeException argumentOutOfRangeException = new ArgumentOutOfRangeException(randValue);
            if (argumentOutOfRangeException == null)
            {
                TestLibrary.TestFramework.LogError("001", "The result is not the value as expected");
                retVal = false;
            }
            if ((argumentOutOfRangeException.Message != "Specified argument was out of the range of valid values.") &
                (!argumentOutOfRangeException.Message.Contains("[Arg_ArgumentOutOfRangeException]")))
            {
                TestLibrary.TestFramework.LogError("005", "The result is not the value as expected" + argumentOutOfRangeException.Message);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Call ctor with stringEmpty to construct a new instance");

        try
        {
            string randValue = String.Empty;
            ArgumentOutOfRangeException argumentOutOfRangeException = new ArgumentOutOfRangeException(randValue);
            if (argumentOutOfRangeException == null)
            {
                TestLibrary.TestFramework.LogError("001", "The result is not the value as expected");
                retVal = false;
            }
            if ((argumentOutOfRangeException.Message != "Specified argument was out of the range of valid values.") &
                (!argumentOutOfRangeException.Message.Contains("[Arg_ArgumentOutOfRangeException]")))
            {
                TestLibrary.TestFramework.LogError("008", "The result is not the value as expected" + argumentOutOfRangeException.Message);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("009", "Unexpected exception: " + e);
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
        ArgumentOutOfRangeExceptionCtor2 test = new ArgumentOutOfRangeExceptionCtor2();

        TestLibrary.TestFramework.BeginTestCase("ArgumentOutOfRangeExceptionCtor2");

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
