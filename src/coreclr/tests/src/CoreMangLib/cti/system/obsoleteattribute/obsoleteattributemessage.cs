// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.ObsoleteAttribute.Message[v-juwa]
/// </summary>
public class ObsoleteAttributeMessage
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

        const string c_TEST_DESC = "PosTest1: Verify Message property default value is null";
        const string c_TEST_ID = "P001";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            ObsoleteAttribute oa = new ObsoleteAttribute();
            if (oa.Message != null)
            {
                string errorDesc = "Message default value is not null expected:Actual (\""+oa.Message+"\")";
                TestLibrary.TestFramework.LogError("001 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002 " + "TestID_" + c_TEST_ID, "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest2: Verify  Message property is random string";
        const string c_TEST_ID = "P002";

        string message = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            ObsoleteAttribute oa = new ObsoleteAttribute(message);
            if (oa.Message != message)
            {
                string errorDesc = "Message  value is not \""+message+"\" expected:Actual (\"" + oa.Message + "\")";
                TestLibrary.TestFramework.LogError("003 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004 " + "TestID_" + c_TEST_ID, "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest3: Verify  Message property is empty";
        const string c_TEST_ID = "P003";

        string message = String.Empty;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            ObsoleteAttribute oa = new ObsoleteAttribute(message,false);
            if (oa.Message != message)
            {
                string errorDesc = "Message  value is not empty expected:Actual (\"" + oa.Message + "\")";
                TestLibrary.TestFramework.LogError("005 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006 " + "TestID_" + c_TEST_ID, "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest4: Verify  Message property is null";
        const string c_TEST_ID = "P004";

        string message = null;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            ObsoleteAttribute oa = new ObsoleteAttribute(message);
            if (oa.Message != null)
            {
                string errorDesc = "Message  value is not null expected:Actual (\"" + oa.Message + "\")";
                TestLibrary.TestFramework.LogError("007 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008 " + "TestID_" + c_TEST_ID, "Unexpected exception: " + e);
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
        ObsoleteAttributeMessage test = new ObsoleteAttributeMessage();

        TestLibrary.TestFramework.BeginTestCase("For property:System.ObsoleteAttribute.Message");

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
