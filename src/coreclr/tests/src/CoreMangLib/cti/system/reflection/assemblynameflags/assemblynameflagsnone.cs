// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Reflection;

/// <summary>
/// AssemblyNameFlags.None(v-yaduoj)
/// </summary>
public class AssemblyNameFlagsTest
{
    private enum MyAssemblyNameFlags
    {
        None = 0x0000,
        PublicKey = 0x0001,
        EnableJITcompileOptimizer = 0x4000,
        EnableJITcompileTracking = 0x8000,
        Retargetable = 0x0100,
    }

    public static int Main()
    {
        AssemblyNameFlagsTest testObj = new AssemblyNameFlagsTest();

        TestLibrary.TestFramework.BeginTestCase("for Enumeration: AssemblyNameFlags.None");
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
        const string c_TEST_DESC = "PosTest1: Assembly name flags is None";
        string errorDesc;

        int expectedValue;
        int actualValue;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            expectedValue = (int)MyAssemblyNameFlags.None;
            actualValue = (int)AssemblyNameFlags.None;
            if (actualValue != expectedValue)
            {
                errorDesc = "None value of AssemblyNameFlags is not the value " + expectedValue +
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
