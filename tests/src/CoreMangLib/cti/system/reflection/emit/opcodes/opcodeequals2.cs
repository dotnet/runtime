// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Reflection.Emit;

/// <summary>
/// System.Reflection.Emit.OpCode.Equals(OpCode)[v-jiajul]
/// </summary>
public class OpCodeEquals2
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: The two opcode are equal");

        try
        {
            retVal = VerifyEqualsOpCodeT(OpCodes.Refanyval, OpCodes.Refanyval, "001.1") && retVal;
            retVal = VerifyEqualsOpCodeT(OpCodes.Rem_Un, OpCodes.Rem_Un, "001.2") && retVal;
            retVal = VerifyEqualsOpCodeT(OpCodes.Shr, OpCodes.Shr, "001.3") && retVal;
            retVal = VerifyEqualsOpCodeT(OpCodes.Stfld, OpCodes.Stfld, "001.4") && retVal;
            retVal = VerifyEqualsOpCodeT(OpCodes.Stloc_0, OpCodes.Stloc_0, "001.5") && retVal;
            retVal = VerifyEqualsOpCodeT(OpCodes.Pop, OpCodes.Pop, "001.6") && retVal;
            retVal = VerifyEqualsOpCodeT(OpCodes.Calli, OpCodes.Calli, "001.7") && retVal;
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Check two different opcode");

        try
        {
            retVal = VerifyEqualsOpCodeF(OpCodes.Refanyval, OpCodes.Call, "002.1") && retVal;
            retVal = VerifyEqualsOpCodeF(OpCodes.Rem_Un, OpCodes.Br_S, "002.2") && retVal;
            retVal = VerifyEqualsOpCodeF(OpCodes.Shr, OpCodes.Ret, "002.3") && retVal;
            retVal = VerifyEqualsOpCodeF(OpCodes.Stfld, OpCodes.Add_Ovf, "002.4") && retVal;
            retVal = VerifyEqualsOpCodeF(OpCodes.Stloc_0, OpCodes.Clt, "002.5") && retVal;
            retVal = VerifyEqualsOpCodeF(OpCodes.Pop, OpCodes.Div, "002.6") && retVal;
            retVal = VerifyEqualsOpCodeF(OpCodes.Calli, OpCodes.Xor, "002.7") && retVal;
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
        OpCodeEquals2 test = new OpCodeEquals2();

        TestLibrary.TestFramework.BeginTestCase("OpCodeEquals2");

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
    private bool VerifyEqualsOpCodeT(OpCode op1, OpCode op2, string errNum)
    {
        bool retVal = true;

        try
        {
            if (!op1.Equals(op2))
            {
                TestLibrary.TestFramework.LogError(errNum.ToString(), "Result is not the value as expected,opcode1 is: " + op1 + ",opcode2  is: " + op2);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError(errNum.ToString(), "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    private bool VerifyEqualsOpCodeF(OpCode op1, OpCode op2, string errNum)
    {
        bool retVal = true;

        try
        {
            if (op1.Equals(op2))
            {
                TestLibrary.TestFramework.LogError(errNum.ToString(), "Result is not the value as expected,opcode1 is: " + op1 + ",opcode2  is: " + op2);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError(errNum.ToString(), "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}
public class MyClass
{
}
