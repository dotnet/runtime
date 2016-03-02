// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// FormatException Constructor (String, Exception) 
/// </summary>
public class FormatExceptionCtor
{
    private const int c_MIN_STRING_LENGTH = 1;
    private const int c_MAX_STRING_LENGTH = 256;

    public static int Main()
    {
        FormatExceptionCtor testObj = new FormatExceptionCtor();

        TestLibrary.TestFramework.BeginTestCase("for constructor: FormatException(String)");
        if (testObj.RunTests())
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

    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;

        return retVal;
    }

    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "P001";
        string testDesc = "PosTest1: Initializes a new instance of FormatException using non-empty message.";
        string errorDesc;

        FormatException formatException;
        string message;
        Exception innerException;
        innerException = new Exception();
        message = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);

        TestLibrary.TestFramework.BeginScenario(testDesc);
        try
        {
            formatException = new FormatException(message, innerException);
            if(null == formatException || 
               !formatException.Message.Contains(message) ||
               formatException.InnerException != innerException)
            {
                errorDesc = "Failed to initialize instance of FormatException using message \"" +
                            message + "\"\n Inner exception: " + innerException;
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        const string c_TEST_ID = "P002";
        string testDesc = "PosTest2: Initializes a new instance of FormatException using null reference.";
        string errorDesc;

        FormatException formatException;
        Exception innerException;
        innerException = new Exception();

        TestLibrary.TestFramework.BeginScenario(testDesc);
        try
        {
            formatException = new FormatException(null, innerException);
            if (null == formatException ||
                formatException.InnerException != innerException)
            {
                errorDesc = "Failed to initialize instance of FormatException using null reference." +
                            "\n Inner exception: " + innerException;
                TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("004" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        const string c_TEST_ID = "P003";
        string testDesc = "PosTest3: Initializes a new instance of FormatException using string.Empty.";
        string errorDesc;

        FormatException formatException;
        Exception innerException;
        innerException = new Exception();

        TestLibrary.TestFramework.BeginScenario(testDesc);
        try
        {
            formatException = new FormatException(string.Empty, innerException);
            if (null == formatException ||
                formatException.InnerException != innerException)
            {
                errorDesc = "Failed to initialize instance of FormatException using null reference." +
                            "\n Inner exception: " + innerException;
                TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("006" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}
