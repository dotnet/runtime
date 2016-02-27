// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.ObsoleteAttribute.Ctor(String)[v-juwa]
/// </summary>
public class ObsoleteAttributeCtor2
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

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;
        
        const string c_TEST_DESC = "PosTest1: Verify the message is the random string";
        const string c_TEST_ID = "P001";

        string message = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            ObsoleteAttribute oa = new ObsoleteAttribute(message);
            if (oa.Message != message)
            {
                string errorDesc = "Message is not \""+message+"\" as expected:Actual(\""+oa.Message+"\")";
                TestLibrary.TestFramework.LogError("001 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }

            if (oa.IsError)
            {
                string errorDesc = "created ObsoleteAttribute IsError property should  be false";
                TestLibrary.TestFramework.LogError("002 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003 ", "Unexpected exception: " + e+"message is "+message);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest2: Verify the message is empty";
        const string c_TEST_ID = "P002";

        string message = String.Empty;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            ObsoleteAttribute oa = new ObsoleteAttribute(message);
            if (oa.Message != message)
            {
                string errorDesc = "Message is not empty as expected:Actual(\"" + oa.Message + "\")";
                TestLibrary.TestFramework.LogError("004 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }

            if (oa.IsError)
            {
                string errorDesc = "created ObsoleteAttribute IsError property should  be false";
                TestLibrary.TestFramework.LogError("005 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006 ", "Unexpected exception: " + e+"message is empty");
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest3: Verify the message is null";
        const string c_TEST_ID = "P003";

        string message = null;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            ObsoleteAttribute oa = new ObsoleteAttribute(message);
            if (oa.Message != null)
            {
                string errorDesc = "Message is not null as expected:Actual(\"" + oa.Message + "\")";
                TestLibrary.TestFramework.LogError("007 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }

            if (oa.IsError)
            {
                string errorDesc = "created ObsoleteAttribute IsError property should  be false";
                TestLibrary.TestFramework.LogError("008 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("009 ", "Unexpected exception: " + e+"message is null");
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }
        return retVal;
    }
    #endregion

    #endregion

    public static int Main()
    {
        ObsoleteAttributeCtor2 test = new ObsoleteAttributeCtor2();

        TestLibrary.TestFramework.BeginTestCase("For method:System.ObsoleteAttribute.Ctor(String)");

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
