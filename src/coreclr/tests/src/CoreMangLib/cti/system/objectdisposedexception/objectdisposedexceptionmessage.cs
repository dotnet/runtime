// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.ObjectDisposedException.Message[v-juwa]
/// </summary>
public class ObjectDisposedExceptionMessage
{
    private int c_MINI_STRING_LENGTH = 8;
    private int c_MAX_STRING_LENGTH = 256;

    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;


        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest1: Verify the objectName is random string";
        const string c_TEST_ID = "P001";

        string name = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
        string message = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
        ObjectDisposedException  exception = new ObjectDisposedException(name,message);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (exception.Message != message)
            {
                int index = exception.Message.IndexOf(name);
                if (index == -1)
                {
                    string errorDesc = "Message shoule contains the object name";
                    errorDesc += Environment.NewLine + "objectName is " + name;
                    errorDesc += Environment.NewLine + "message is " + message;
                    errorDesc += Environment.NewLine + "exception's message is " + exception.Message;
                    TestLibrary.TestFramework.LogError("001 " + "TestID_" + c_TEST_ID, errorDesc);
                    retVal = false;
                }
                index = exception.Message.IndexOf(message);
                if (index == -1)
                {
                    string errorDesc = "Message shoule contains the message";
                    errorDesc += Environment.NewLine + "objectName is " + name;
                    errorDesc += Environment.NewLine + "message is " + message;
                    errorDesc += Environment.NewLine + "exception's message is " + exception.Message;
                    TestLibrary.TestFramework.LogError("001 " + "TestID_" + c_TEST_ID, errorDesc);
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest2: Verify the message parameter is empty";
        const string c_TEST_ID = "P002";

        string name = string.Empty;
        string message = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
        ObjectDisposedException exception = new ObjectDisposedException(name, message);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (exception.Message != "An error occurred.")
            {
                int index = exception.Message.IndexOf(message);
                if (index == -1)
                {
                    string errorDesc = "Message shoule contains the message";
                    errorDesc += Environment.NewLine + "objectName is empty";
                    errorDesc += Environment.NewLine + "message parameter" + message;
                    errorDesc += Environment.NewLine + "exception's message is " + exception.Message;
                    TestLibrary.TestFramework.LogError("003 " + "TestID_" + c_TEST_ID, errorDesc);
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #endregion

    public static int Main()
    {
        ObjectDisposedExceptionMessage test = new ObjectDisposedExceptionMessage();

        TestLibrary.TestFramework.BeginTestCase("For property:System.ObjectDisposedException.Message");

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
