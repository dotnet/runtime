// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Reflection.Emit;

/// <summary>
/// System.Reflection.Emit.OpCode.ToString()[v-jiajul]
/// </summary>
public class OpCodeToString
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Check the method normally");

        try
        {
            retVal = VerifyToString(OpCodes.Add, "add", "001.0") && retVal;
            retVal = VerifyToString(OpCodes.Refanyval, "refanyval", "001.1") && retVal;
            retVal = VerifyToString(OpCodes.Rem_Un, "rem.un", "001.2") && retVal;
            retVal = VerifyToString(OpCodes.Shr, "shr", "001.3") && retVal;
            retVal = VerifyToString(OpCodes.Stfld, "stfld", "001.4") && retVal;
            retVal = VerifyToString(OpCodes.Stloc_0, "stloc.0", "001.5") && retVal;
            retVal = VerifyToString(OpCodes.Pop, "pop", "001.6") && retVal;
            retVal = VerifyToString(OpCodes.Calli, "calli", "001.7") && retVal;
            retVal = VerifyToString(OpCodes.Switch, "switch", "001.8") && retVal;
            retVal = VerifyToString(OpCodes.Sub, "sub", "001.9") && retVal;
            retVal = VerifyToString(OpCodes.Not, "not", "001.10") && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    #endregion
    #endregion

    public static int Main()
    {
        OpCodeToString test = new OpCodeToString();

        TestLibrary.TestFramework.BeginTestCase("OpCodeToString");

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
    #region Private Methods
    private bool VerifyToString(OpCode op1, string expected, string errNum)
    {
        bool retVal = true;

        try
        {
            string value = op1.ToString();
            if (value != expected)
            {
                TestLibrary.TestFramework.LogError(errNum, "Result is not the value as expected,value is: " + value + ",expected is: " + expected);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError(errNum, "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}
