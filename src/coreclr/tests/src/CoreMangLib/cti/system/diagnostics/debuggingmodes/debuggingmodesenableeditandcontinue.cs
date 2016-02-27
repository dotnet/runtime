// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Diagnostics;

/// <summary>
/// DebuggingModes.EnableEditAndContinue (v-yaduoj)
/// </summary>
public class DebuggingModesElement
{
    private enum MyDebuggingModes
    {
        None = 0x0,
        Default = 0x1,
        DisableOptimizations = 0x100,
        IgnoreSymbolStoreSequencePoints = 0x2,
        EnableEditAndContinue = 0x4
    }

    public static int Main()
    {
        DebuggingModesElement testObj = new DebuggingModesElement();

        TestLibrary.TestFramework.BeginTestCase("for enum value: DebuggingModes.EnableEditAndContinue");
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

        return retVal;
    }

    #region Positive tests
    public bool PosTest1()
    {
        const string c_TEST_ID = "P001";
        string testDesc = "PosTest1: value of DebuggableAttribute.DebuggingModes.";
        DebuggableAttribute.DebuggingModes modes = DebuggableAttribute.DebuggingModes.EnableEditAndContinue;
        testDesc += modes;
        MyDebuggingModes expectedModes = MyDebuggingModes.EnableEditAndContinue;
        return ExecutePosTest(c_TEST_ID, testDesc, "001", "002", modes, expectedModes);
    }
    #endregion

    #region Helper methods for positive tests
    private bool ExecutePosTest(string testId, string testDesc, 
                                string errorNum1, string errorNum2,
                                DebuggableAttribute.DebuggingModes actualModes,
                                MyDebuggingModes expectedModes)
    {
        bool retVal = true;
        string errorDesc;

        TestLibrary.TestFramework.BeginScenario(testDesc);
        try
        {
            if (actualModes != (DebuggableAttribute.DebuggingModes)expectedModes)
            {
                errorDesc = "Value of " + actualModes + " is not the value " + (int)expectedModes +
                            " as expected: Actually(" + (int)actualModes + ")";
                TestLibrary.TestFramework.LogError(errorNum1 + " TestId-" + testId, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError(errorNum2 + " TestId-" + testId, errorDesc);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}
