// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.ObsoleteAttribute.IsError[v-juwa]
/// </summary>
public class ObsoleteAttributeIsError
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

        const string c_TEST_DESC = "PosTest1: Verify  IsError property default value is false";
        const string c_TEST_ID = "P001";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            ObsoleteAttribute oa = new ObsoleteAttribute();
            if (oa.IsError)
            {
                string errorDesc = "IsError property default value should  be false";
                TestLibrary.TestFramework.LogError("001 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002 "+"TestID_" + c_TEST_ID, "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest2: Verify  IsError property  is false";
        const string c_TEST_ID = "P002";

        string message = TestLibrary.Generator.GetString(-55, false,c_MINI_STRING_LENGTH,c_MAX_STRING_LENGTH);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            ObsoleteAttribute oa = new ObsoleteAttribute(message,false);
            if (oa.IsError)
            {
                string errorDesc = "IsError property  value should  be false";
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

        const string c_TEST_DESC = "PosTest3: Verify  IsError property  is true";
        const string c_TEST_ID = "P003";

        string message = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            ObsoleteAttribute oa = new ObsoleteAttribute(message, true);
            if (!oa.IsError)
            {
                string errorDesc = "IsError property  value should  be true";
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
    #endregion

    #endregion

    public static int Main()
    {
        ObsoleteAttributeIsError test = new ObsoleteAttributeIsError();

        TestLibrary.TestFramework.BeginTestCase("For property:System.ObsoleteAttribute.IsError");

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
