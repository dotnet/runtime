using System;
using TestLibrary;

/// <summary>
/// System.ArgumentException.Message
/// </summary>
public class ArgumentExceptionMessage
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
        retVal = PosTest4() && retVal;
        retVal = PosTest5() && retVal;

        //
        // TODO: Add your negative test cases here
        //
        // TestLibrary.TestFramework.LogInformation("[Negative]");
        // retVal = NegTest1() && retVal;
        // retVal = NegTest2() && retVal;
        // retVal = NegTest3() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Using ctor1 to test the message property");

        try
        {
            string randValue = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);
            ArgumentException argumentException = new ArgumentException(randValue);
            if (argumentException.Message != randValue)
            {
                TestLibrary.TestFramework.LogError("001", "The result is not the value as expected");
                TestLibrary.TestFramework.LogInformation("Expected: " + randValue + "; Actual: " + argumentException.Message);
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

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Using ctor2 to test the message property");

        try
        {
            string randValue = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);
            string paramName = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);
            ArgumentException argumentException = new ArgumentException(randValue, paramName);

            string expectedWindows = randValue + "\r\nParameter name: " + paramName;
            string expectedMac = randValue + "\nParameter name: " + paramName;

            if ((argumentException.Message != randValue))
            {
                if (!Utilities.IsWindows)
                {
                    if (argumentException.Message != expectedMac)
                    {
                        TestLibrary.TestFramework.LogError("003", "The result is not the value as expected");
                        TestLibrary.TestFramework.LogInformation("Expected: " + expectedMac + "; Actual: " + argumentException.Message);
                        retVal = false;
                    }
                }
                else if (argumentException.Message != expectedWindows)
                {
                    TestLibrary.TestFramework.LogError("004", "The result is not the value as expected");
                    TestLibrary.TestFramework.LogInformation("Expected: " + expectedWindows + "; Actual: " + argumentException.Message);
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("005", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: The string argument for message is a null reference");

        try
        {
            string paramName = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);
            ArgumentException argumentException = new ArgumentException(null, paramName);

            string expectedWindows = "Exception of type 'System.ArgumentException' was thrown.\r\nParameter name: " + paramName;
            string expectedMac = "Exception of type 'System.ArgumentException' was thrown.\nParameter name: " + paramName;

            if ((argumentException.Message != "Exception of type 'System.ArgumentException' was thrown."))
            {
                if (!Utilities.IsWindows)
                {
                    if (argumentException.Message != expectedMac)
                    {
                        TestLibrary.TestFramework.LogError("003", "The result is not the value as expected");
                        TestLibrary.TestFramework.LogInformation("Expected: " + expectedMac + "; Actual: " + argumentException.Message);
                        retVal = false;
                    }
                }
                else if (argumentException.Message != expectedWindows)
                {
                    TestLibrary.TestFramework.LogError("006", "The result is not the value as expected");
                    TestLibrary.TestFramework.LogInformation("Expected: " + expectedWindows + "; Actual: " + argumentException.Message);
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: The message is string empty");

        try
        {
            string Value = string.Empty;
            string paramName = null;
            ArgumentException argumentException = new ArgumentException(Value, paramName);
            if (!argumentException.Message.Equals(string.Empty))
            {
                TestLibrary.TestFramework.LogError("007", "Expected String.Empty. Actual: " + argumentException.Message);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest5: The message is white space");

        try
        {
            string Value = " ";
            string paramName = null;
            ArgumentException argumentException = new ArgumentException(Value, paramName);
            if (argumentException.Message != " ")
            {
                TestLibrary.TestFramework.LogError("009", "Expected empty string. Actual: " + argumentException.Message);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception: " + e);
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
        ArgumentExceptionMessage test = new ArgumentExceptionMessage();

        TestLibrary.TestFramework.BeginTestCase("ArgumentExceptionMessage");

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
