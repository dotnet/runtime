// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Reflection;

/// <summary>
/// NestedFamANDAssem [v-yishi]
/// </summary>
public class TypeAttributesNestedFamANDAssem
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify NestedFamANDAssem's value is 0x00000006");

        try
        {
            int expected = 0x00000006;
            int actual = (int)TypeAttributes.NestedFamANDAssem;

            if (expected != actual)
            {
                TestLibrary.TestFramework.LogError("001.1", "NestedFamANDAssem's value is not 0x00000006");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] expected = " + expected + ", actual = " + actual);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        TypeAttributesNestedFamANDAssem test = new TypeAttributesNestedFamANDAssem();

        TestLibrary.TestFramework.BeginTestCase("TypeAttributesNestedFamANDAssem");

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
