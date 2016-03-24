// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.ArgumentOutOfRangeException.Message
/// </summary>
public class ArgumentOutOfRangeExceptionMessage
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
            ArgumentOutOfRangeException argumentOutOfRangeException = new ArgumentOutOfRangeException(randValue);
            if ((argumentOutOfRangeException.Message != "Specified argument was out of the range of valid values." + Environment.NewLine + "Parameter name: " + randValue) &
                (!argumentOutOfRangeException.Message.Contains("[Arg_ArgumentOutOfRangeException]")))
            {
                TestLibrary.TestFramework.LogError("001", "The result is not the value as expected");
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
            ArgumentOutOfRangeException argumentOutOfRangeException = new ArgumentOutOfRangeException(paramName, randValue);
            if ((argumentOutOfRangeException.Message != randValue + "" + Environment.NewLine + "Parameter name: " + paramName) &
                (argumentOutOfRangeException.Message != randValue))
            {
                TestLibrary.TestFramework.LogError("003", "The result is not the value as expected");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
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
            string Value = null;
            ArgumentOutOfRangeException argumentOutOfRangeException = new ArgumentOutOfRangeException(paramName, Value);
            if ((argumentOutOfRangeException.Message != "Exception of type 'System.ArgumentOutOfRangeException' was thrown." + Environment.NewLine + "Parameter name: " + paramName) &
                (argumentOutOfRangeException.Message != "Exception of type 'System.ArgumentOutOfRangeException' was thrown."))
            {
                TestLibrary.TestFramework.LogError("005", "The result is not the value as expected");
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

    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: The message is string empty");

        try
        {
            string Value = string.Empty;
            string paramName = null;
            ArgumentOutOfRangeException argumentOutOfRangeException = new ArgumentOutOfRangeException(paramName, Value);
            if (!argumentOutOfRangeException.Message.Equals(string.Empty))
            {
                TestLibrary.TestFramework.LogError("007", "The result is not the value as expected");
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
            ArgumentOutOfRangeException argumentOutOfRangeException = new ArgumentOutOfRangeException(paramName, Value);
            if (argumentOutOfRangeException.Message != " ")
            {
                TestLibrary.TestFramework.LogError("009", "The result is not the value as expected");
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
        ArgumentOutOfRangeExceptionMessage test = new ArgumentOutOfRangeExceptionMessage();

        TestLibrary.TestFramework.BeginTestCase("ArgumentOutOfRangeExceptionMessage");

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
