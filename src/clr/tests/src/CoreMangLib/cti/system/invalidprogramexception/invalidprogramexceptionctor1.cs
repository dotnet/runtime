// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
/// <summary>
/// InvalidProgramException.ctor() [v-minch]
/// </summary>
public class InvalidProgramExceptionctor1
{
    public static int Main()
    {
        InvalidProgramExceptionctor1 invalidProgramExceptionctor1 = new InvalidProgramExceptionctor1();
        TestLibrary.TestFramework.BeginTestCase("InvalidProgramExceptionctor1");
        if (invalidProgramExceptionctor1.RunTests())
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
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1:Initialize the InvalidProgramException instance");
        try
        {
            InvalidProgramException myException = new InvalidProgramException();
            if (myException == null)
            {
                TestLibrary.TestFramework.LogError("001", "the InvalidProgramException instance creating failed");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}