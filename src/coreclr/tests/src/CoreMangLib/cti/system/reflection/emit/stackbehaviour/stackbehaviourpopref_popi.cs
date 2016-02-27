// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Reflection.Emit;

/// <summary>
/// Popref_popi [v-yishi]
/// </summary>
public class StackBehaviourPopref_popi
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify the value of Popref_popi is 12");

        try
        {
            int expected = 12;
            int actual = (int)StackBehaviour.Popref_popi;

            if (expected != actual)
            {
                TestLibrary.TestFramework.LogError("001.1", "Popref_popi's value is not 12");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] expected = " + expected + ", actual = " + actual);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion


    public static int Main()
    {
        StackBehaviourPopref_popi test = new StackBehaviourPopref_popi();

        TestLibrary.TestFramework.BeginTestCase("StackBehaviourPopref_popi");

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
            return 12;
        }
    }
}
