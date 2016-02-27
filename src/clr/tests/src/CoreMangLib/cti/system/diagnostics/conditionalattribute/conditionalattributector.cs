// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Diagnostics;

/// <summary>
/// ConditionalAttribute(string)(v-yaduoj)
/// </summary>
public class ConditionalAttributeCtor
{
    public static int Main()
    {
        ConditionalAttributeCtor testObj = new ConditionalAttributeCtor();

        TestLibrary.TestFramework.BeginTestCase("for constructor: ConditionalAttribute(string)");
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
        const string c_TEST_DESC = "PosTest1: Intinalize the conditional attribute using not empty string.";
        string errorDesc;

        string conditionalString;

        conditionalString = "CLR_API_Test";
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            ConditionalAttribute conditionalAtt = new ConditionalAttribute(conditionalString);
            if (conditionalAtt.ConditionString != conditionalString)
            {
                errorDesc = string.Format("Faile to initialize the conditional attribute using string \"{0}\"", conditionalString);
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
        const string c_TEST_DESC = "PosTest2: Intinalize the conditional attribute using string.Empty.";
        string errorDesc;

        string conditionalString;

        conditionalString = string.Empty;
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            ConditionalAttribute conditionalAtt = new ConditionalAttribute(conditionalString);
            if (conditionalAtt.ConditionString != conditionalString)
            {
                errorDesc = "Failed to initialize the conditional attribute using an empty string(string.Empty)";
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
        const string c_TEST_DESC = "PosTest3: Intinalize the conditional attribute using a null reference.";
        string errorDesc;

        string conditionalString;

        conditionalString = null;
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            ConditionalAttribute conditionalAtt = new ConditionalAttribute(conditionalString);
            if (conditionalAtt.ConditionString != conditionalString)
            {
                errorDesc = "Failed to initialize the conditional attribute using a null reference";
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
