// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Reflection.Emit;

/// <summary>
/// System.Reflection.Emit.OpCode.Size[v-jiajul]
/// </summary>
public class OpCodeSize
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;


        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: The size of the opcode is one");

        try
        {
            retVal = VerifyOpCodeSize1(OpCodes.Add, "001.0") && retVal;
            retVal = VerifyOpCodeSize1(OpCodes.Ldc_I4, "001.1") && retVal;
            retVal = VerifyOpCodeSize1(OpCodes.Conv_Ovf_I1_Un, "001.2") && retVal;
            retVal = VerifyOpCodeSize1(OpCodes.Nop, "001.3") && retVal;
            retVal = VerifyOpCodeSize1(OpCodes.Ldarg_0, "001.4") && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: The size of opcode is two");

        try
        {
            retVal = VerifyOpCodeSize2(OpCodes.Stloc, "002.0") && retVal;
            retVal = VerifyOpCodeSize2(OpCodes.Localloc, "002.1") && retVal;
            retVal = VerifyOpCodeSize2(OpCodes.Clt_Un, "002.2") && retVal;
            retVal = VerifyOpCodeSize2(OpCodes.Ldarg, "002.3") && retVal;
            retVal = VerifyOpCodeSize2(OpCodes.Unaligned, "002.4") && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("020", "Unexpected exception: " + e);
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
        OpCodeSize test = new OpCodeSize();

        TestLibrary.TestFramework.BeginTestCase("OpCodeSize");

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
    private bool VerifyOpCodeSize1(OpCode op1, string errNum)
    {
        bool retVal = true;

        try
        {
            if (op1.Size != 1)
            {
                TestLibrary.TestFramework.LogError(errNum, "Result is not the value as expected,size is: " + op1.Size);
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

    private bool VerifyOpCodeSize2(OpCode op1, string errNum)
    {
        bool retVal = true;

        try
        {
            if (op1.Size != 2)
            {
                TestLibrary.TestFramework.LogError(errNum, "Result is not the value as expected,size is: " + op1.Size);
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