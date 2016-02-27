// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Reflection.Emit;

/// <summary>
/// System.Reflection.Emit.OpCode.OperandType[v-jiajul]
/// </summary>
public class OpCodeOperandType
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
            retVal = VerifyOpCodeType(OpCodes.Add, OperandType.InlineNone, "001.0") && retVal;
            retVal = VerifyOpCodeType(OpCodes.Ldc_I4, OperandType.InlineI, "001.1") && retVal;
            retVal = VerifyOpCodeType(OpCodes.Stloc, OperandType.InlineVar, "001.2") && retVal;
            retVal = VerifyOpCodeType(OpCodes.Nop, OperandType.InlineNone, "001.3") && retVal;
            retVal = VerifyOpCodeType(OpCodes.Localloc, OperandType.InlineNone, "001.4") && retVal;
            retVal = VerifyOpCodeType(OpCodes.Endfilter, OperandType.InlineNone, "001.5") && retVal;
            retVal = VerifyOpCodeType(OpCodes.Ret, OperandType.InlineNone, "001.6") && retVal;
            retVal = VerifyOpCodeType(OpCodes.Callvirt, OperandType.InlineMethod, "001.7") && retVal;
            retVal = VerifyOpCodeType(OpCodes.Cpobj, OperandType.InlineType, "001.8") && retVal;
            retVal = VerifyOpCodeType(OpCodes.Ldobj, OperandType.InlineType, "001.9") && retVal;
            retVal = VerifyOpCodeType(OpCodes.Prefix3, OperandType.InlineNone, "001.10") && retVal;
            retVal = VerifyOpCodeType(OpCodes.Prefix6, OperandType.InlineNone, "001.11") && retVal;
            retVal = VerifyOpCodeType(OpCodes.Prefixref, OperandType.InlineNone, "001.12") && retVal;
            retVal = VerifyOpCodeType(OpCodes.Constrained, OperandType.InlineType, "001.13") && retVal;
            retVal = VerifyOpCodeType(OpCodes.Readonly, OperandType.InlineNone, "001.14") && retVal;
            retVal = VerifyOpCodeType(OpCodes.Beq, OperandType.InlineBrTarget, "001.15") && retVal;
            retVal = VerifyOpCodeType(OpCodes.Bge, OperandType.InlineBrTarget, "001.16") && retVal;
            retVal = VerifyOpCodeType(OpCodes.Blt, OperandType.InlineBrTarget, "001.17") && retVal;
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
        OpCodeOperandType test = new OpCodeOperandType();

        TestLibrary.TestFramework.BeginTestCase("OpCodeOperandType");

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
    private bool VerifyOpCodeType(OpCode op1, OperandType ot, string errNum)
    {
        bool retVal = true;

        try
        {
            if (op1.OperandType != ot)
            {
                TestLibrary.TestFramework.LogError(errNum, "Result is not the value as expected,OperandType is: " + op1.OperandType + ",Expected is: " + ot);
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
