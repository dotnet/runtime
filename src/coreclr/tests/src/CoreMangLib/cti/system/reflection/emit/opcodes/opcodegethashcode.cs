// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Reflection.Emit;

/// <summary>
/// System.Reflection.Emit.OpCode.GetHashCode[v-jiajul]
/// </summary>
public class OpCodeGetHashCode
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");

        try
        {
            retVal = VerifyEqualsGetHashCodeF(OpCodes.Refanyval, OpCodes.Call, "002.1") && retVal;
            retVal = VerifyEqualsGetHashCodeF(OpCodes.Rem_Un, OpCodes.Br_S, "002.2") && retVal;
            retVal = VerifyEqualsGetHashCodeF(OpCodes.Shr, OpCodes.Ret, "002.3") && retVal;
            retVal = VerifyEqualsGetHashCodeF(OpCodes.Stfld, OpCodes.Add_Ovf, "002.4") && retVal;
            retVal = VerifyEqualsGetHashCodeF(OpCodes.Stloc_0, OpCodes.Clt, "002.5") && retVal;
            retVal = VerifyEqualsGetHashCodeF(OpCodes.Pop, OpCodes.Div, "002.6") && retVal;
            retVal = VerifyEqualsGetHashCodeF(OpCodes.Calli, OpCodes.Xor, "002.7") && retVal;
            retVal = VerifyEqualsGetHashCodeF(OpCodes.Newobj, OpCodes.Beq, "002.8") && retVal;
            retVal = VerifyEqualsGetHashCodeF(OpCodes.Blt, OpCodes.Refanytype, "002.9") && retVal;
            retVal = VerifyEqualsGetHashCodeF(OpCodes.Volatile, OpCodes.Add, "002.10") && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("020", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    public static int Main()
    {
        OpCodeGetHashCode test = new OpCodeGetHashCode();

        TestLibrary.TestFramework.BeginTestCase("OpCodeGetHashCode");

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
    
    private bool VerifyEqualsGetHashCodeF(OpCode op1, OpCode op2, string errNum)
    {
        bool retVal = true;

        try
        {
            int hash1 = op1.GetHashCode();
            int hash2 = op2.GetHashCode();
            if (hash1 == hash2)
            {
                TestLibrary.TestFramework.LogError(errNum, "Result is not the value as expected,hash1 is: " + hash1 + ",hash2 is: " + hash2);
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
