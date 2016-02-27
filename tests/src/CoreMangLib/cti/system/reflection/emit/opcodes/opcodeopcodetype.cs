// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Reflection.Emit;

/// <summary>
/// System.Reflection.Emit.OpCode.OpCodeType[v-jiajul]
/// </summary>
public class OpCodeOpCodeType
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
            retVal = VerifyOpCodeType(OpCodes.Add, OpCodeType.Primitive, "001.0") && retVal;
            retVal = VerifyOpCodeType(OpCodes.Ldc_I4, OpCodeType.Primitive, "001.1") && retVal;
            retVal = VerifyOpCodeType(OpCodes.Stloc, OpCodeType.Primitive, "001.2") && retVal;
            retVal = VerifyOpCodeType(OpCodes.Nop, OpCodeType.Primitive, "001.3") && retVal;
            retVal = VerifyOpCodeType(OpCodes.Localloc, OpCodeType.Primitive, "001.4") && retVal;
            retVal = VerifyOpCodeType(OpCodes.Endfilter, OpCodeType.Primitive, "001.5") && retVal;
            retVal = VerifyOpCodeType(OpCodes.Ret, OpCodeType.Primitive, "001.6") && retVal;
            retVal = VerifyOpCodeType(OpCodes.Callvirt, OpCodeType.Objmodel, "001.7") && retVal;
            retVal = VerifyOpCodeType(OpCodes.Cpobj, OpCodeType.Objmodel, "001.8") && retVal;
            retVal = VerifyOpCodeType(OpCodes.Ldobj, OpCodeType.Objmodel, "001.9") && retVal;
            retVal = VerifyOpCodeType(OpCodes.Prefix3, OpCodeType.Nternal, "001.10") && retVal;
            retVal = VerifyOpCodeType(OpCodes.Prefix6, OpCodeType.Nternal, "001.11") && retVal;
            retVal = VerifyOpCodeType(OpCodes.Prefixref, OpCodeType.Nternal, "001.12") && retVal;
            retVal = VerifyOpCodeType(OpCodes.Constrained, OpCodeType.Prefix, "001.13") && retVal;
            retVal = VerifyOpCodeType(OpCodes.Readonly, OpCodeType.Prefix, "001.14") && retVal;
            retVal = VerifyOpCodeType(OpCodes.Beq, OpCodeType.Macro, "001.15") && retVal;
            retVal = VerifyOpCodeType(OpCodes.Bge, OpCodeType.Macro, "001.16") && retVal;
            retVal = VerifyOpCodeType(OpCodes.Blt, OpCodeType.Macro, "001.17") && retVal;
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
        OpCodeOpCodeType test = new OpCodeOpCodeType();

        TestLibrary.TestFramework.BeginTestCase("OpCodeOpCodeType");

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
    private bool VerifyOpCodeType(OpCode op1, OpCodeType oct, string errNum)
    {
        bool retVal = true;

        try
        {
            if (op1.OpCodeType != oct)
            {
                TestLibrary.TestFramework.LogError(errNum, "Result is not the value as expected,OpCodeType is: " + op1.OpCodeType + ",Expected is: " + oct);
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
