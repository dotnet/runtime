// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

public class ObsoleteAttributeCtor3
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
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest1: Verify the message is the random string and isError is false";
        const string c_TEST_ID = "P001";

        string message = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            ObsoleteAttribute oa = new ObsoleteAttribute(message,false);
            if (oa.Message != message)
            {
                string errorDesc = "Message is not \"" + message + "\" as expected:Actual(\"" + oa.Message + "\")";
                TestLibrary.TestFramework.LogError("001 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }

            if (oa.IsError)
            {
                string errorDesc = "IsError should  be false";
                TestLibrary.TestFramework.LogError("002 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003 " + "TestID_" + c_TEST_ID, "Unexpected exception: " + e +"IsError is false and message is "+message);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest2: Verify the message is the random string and isError is true";
        const string c_TEST_ID = "P002";

        string message = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            ObsoleteAttribute oa = new ObsoleteAttribute(message, true);
            if (oa.Message != message)
            {
                string errorDesc = "Message is not \"" + message + "\" as expected:Actual(\"" + oa.Message + "\")";
                TestLibrary.TestFramework.LogError("004 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }

            if (!oa.IsError)
            {
                string errorDesc = "IsError should  be true";
                TestLibrary.TestFramework.LogError("005 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006 " + "TestID_" + c_TEST_ID, "Unexpected exception: " + e + "message is " + "IsError is true and message is " + message);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest3: Verify the message is empty and isError is true";
        const string c_TEST_ID = "P003";

        string message = String.Empty;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            ObsoleteAttribute oa = new ObsoleteAttribute(message, true);
            if (oa.Message != message)
            {
                string errorDesc = "Message is not empty as expected:Actual(\"" + oa.Message + "\")";
                TestLibrary.TestFramework.LogError("007 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }

            if (!oa.IsError)
            {
                string errorDesc = "IsError should  be true";
                TestLibrary.TestFramework.LogError("008 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("009 " + "TestID_" + c_TEST_ID, "Unexpected exception: " + e + "message is " + "IsError is true and message is empty");
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest4: Verify the message is null and isError is true";
        const string c_TEST_ID = "P004";

        string message = null;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            ObsoleteAttribute oa = new ObsoleteAttribute(message, true);
            if (oa.Message != null)
            {
                string errorDesc = "Message is not null as expected:Actual(\"" + oa.Message + "\")";
                TestLibrary.TestFramework.LogError("010 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }

            if (!oa.IsError)
            {
                string errorDesc = "IsError should  be true";
                TestLibrary.TestFramework.LogError("011 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012 " + "TestID_" + c_TEST_ID, "Unexpected exception: " + e + "message is " + "IsError is true and message is null");
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #endregion

    public static int Main()
    {
        ObsoleteAttributeCtor3 test = new ObsoleteAttributeCtor3();

        TestLibrary.TestFramework.BeginTestCase("For method:System.ObsoleteAttributeCtor(String,Boolean)");

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
