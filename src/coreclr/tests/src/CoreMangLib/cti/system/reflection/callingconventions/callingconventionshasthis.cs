// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Reflection;

/// <summary>
/// CallingConventions.HasThis [v-yaduoj]
/// </summary>
public class CallingConventionsTest
{
    private enum MyCallingConventions
    {
        Standard = 0x0001,
        VarArgs = 0x0002,
        Any = HasThis | VarArgs,
        HasThis = 0x0020,
        ExplicitThis = 0x0040,
    }

    public static int Main()
    {
        CallingConventionsTest testObj = new CallingConventionsTest();

        TestLibrary.TestFramework.BeginTestCase("for Enumeration: CallingConventions.HasThis");
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
        bool retVal = true;

        const string c_TEST_ID = "P001";
        const string c_TEST_DESC = "PosTest1: Calling convention is HasThis";
        string errorDesc;

        int expectedValue;
        int actualValue;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            expectedValue = (int)MyCallingConventions.HasThis;
            actualValue = (int)CallingConventions.HasThis;
            if (actualValue != expectedValue)
            {
                errorDesc = "HasThis value of CallingConventionsHasThis is not the value " + expectedValue +
                            "as expected: actual(" + actualValue + ")";
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
    #endregion
}
