// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Runtime.CompilerServices;

/// <summary>
/// RuntimeCompatibilityAttribute.WrapNonExceptionThrows [v-yaduoj]
/// </summary>
public class RuntimeCompatibilityAttributeWrapNonExceptionThrows
{
    public static int Main()
    {
        RuntimeCompatibilityAttributeWrapNonExceptionThrows testObj = new RuntimeCompatibilityAttributeWrapNonExceptionThrows();

        TestLibrary.TestFramework.BeginTestCase("for RuntimeCompatibilityAttribute.WrapNonExceptionThrows");
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

        return retVal;
    }

    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "P001";
        string c_TEST_DESC = "PosTest1: Verify get accessor of property RuntimeCompatibilityAttribute.WrapNonExceptionThrows";
        string errorDesc;

        bool expectedValue, actualValue;
        expectedValue = false;
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            RuntimeCompatibilityAttribute runtimeCompaibilityAtt = new RuntimeCompatibilityAttribute();
            actualValue = runtimeCompaibilityAtt.WrapNonExceptionThrows;
            if (actualValue != expectedValue)
            {
                errorDesc = "The value of property RuntimeCompatibilityAttribute.WrapNonExceptionThrows " +
                            "is not the value " + expectedValue + " as expected, actually(" + actualValue +
                            ")";
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
        string c_TEST_DESC = "PosTest2: Verify set accessor of property RuntimeCompatibilityAttribute.WrapNonExceptionThrows";
        string errorDesc;

        bool expectedValue, actualValue;
        expectedValue = (TestLibrary.Generator.GetInt32(-55) & 1) == 0;
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            RuntimeCompatibilityAttribute runtimeCompaibilityAtt = new RuntimeCompatibilityAttribute();
            runtimeCompaibilityAtt.WrapNonExceptionThrows = expectedValue;
            actualValue = runtimeCompaibilityAtt.WrapNonExceptionThrows;
            if (actualValue != expectedValue)
            {
                errorDesc = "The value of property RuntimeCompatibilityAttribute.WrapNonExceptionThrows " +
                            "is not the value " + expectedValue +  " as expected, actually(" + actualValue +
                            ")";
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
    #endregion
}
