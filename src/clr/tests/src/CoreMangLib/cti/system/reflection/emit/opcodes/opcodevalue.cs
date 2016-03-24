// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Reflection.Emit;

/// <summary>
/// System.Reflection.Emit.OpCode.Value[v-jiajul]
/// </summary>
public class OpCodeValue
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Check the value of the property");

        try
        {
            unchecked
            {
                retVal = VerifyOpCodeValue(OpCodes.Add, (short)0x0058, "001.0") && retVal;
                retVal = VerifyOpCodeValue(OpCodes.Ldc_I4, (short)0x0020, "001.1") && retVal;
                retVal = VerifyOpCodeValue(OpCodes.Stloc, (short)0xfe0e, "001.2") && retVal;
                retVal = VerifyOpCodeValue(OpCodes.Nop, (short)0x0000, "001.3") && retVal;
                retVal = VerifyOpCodeValue(OpCodes.Localloc, (short)0xfe0f, "001.4") && retVal;
                retVal = VerifyOpCodeValue(OpCodes.Ldelem_R8, (short)0x0099, "001.5") && retVal;
                retVal = VerifyOpCodeValue(OpCodes.Ret, (short)0x002a, "001.6") && retVal;
                retVal = VerifyOpCodeValue(OpCodes.Callvirt, (short)0x006f, "001.7") && retVal;
                retVal = VerifyOpCodeValue(OpCodes.Cpobj, (short)0x0070, "001.8") && retVal;
                retVal = VerifyOpCodeValue(OpCodes.Readonly, (short)0xfe1e, "001.9") && retVal;
            }
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
        OpCodeValue test = new OpCodeValue();

        TestLibrary.TestFramework.BeginTestCase("OpCodeValue");

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
    private bool VerifyOpCodeValue(OpCode op1, short sb, string errNum)
    {
        bool retVal = true;

        try
        {
            if (op1.Value != sb)
            {
                TestLibrary.TestFramework.LogError(errNum, "Result is not the value as expected,value is: " + op1.Value + ",Expected is: " + sb);
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
