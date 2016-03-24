// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Reflection.Emit;

/// <summary>
/// System.Reflection.Emit.OpCode.Equals(Object)[v-jiajul]
/// </summary>
public class OpCodeEquals1
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: The two opcode are equal");

        try
        {
            retVal = VerifyEqualsObjectT(OpCodes.Refanyval, OpCodes.Refanyval, "001.1") && retVal;
            retVal = VerifyEqualsObjectT(OpCodes.Rem_Un, OpCodes.Rem_Un, "001.2") && retVal;
            retVal = VerifyEqualsObjectT(OpCodes.Shr, OpCodes.Shr, "001.3") && retVal;
            retVal = VerifyEqualsObjectT(OpCodes.Stfld, OpCodes.Stfld, "001.4") && retVal;
            retVal = VerifyEqualsObjectT(OpCodes.Stloc_0, OpCodes.Stloc_0, "001.5") && retVal;
            retVal = VerifyEqualsObjectT(OpCodes.Pop, OpCodes.Pop, "001.6") && retVal;
            retVal = VerifyEqualsObjectT(OpCodes.Calli, OpCodes.Calli, "001.7") && retVal;
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
            retVal = VerifyEqualsObjectF(OpCodes.Refanyval, OpCodes.Call, "002.1") && retVal;
            retVal = VerifyEqualsObjectF(OpCodes.Rem_Un, OpCodes.Br_S, "002.2") && retVal;
            retVal = VerifyEqualsObjectF(OpCodes.Shr, OpCodes.Ret, "002.3") && retVal;
            retVal = VerifyEqualsObjectF(OpCodes.Stfld, OpCodes.Add_Ovf, "002.4") && retVal;
            retVal = VerifyEqualsObjectF(OpCodes.Stloc_0, OpCodes.Clt, "002.5") && retVal;
            retVal = VerifyEqualsObjectF(OpCodes.Pop, OpCodes.Div, "002.6") && retVal;
            retVal = VerifyEqualsObjectF(OpCodes.Calli, OpCodes.Xor, "002.7") && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("020", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: The object is not type of opcode ");

        try
        {
            retVal = VerifyEqualsObjectF(OpCodes.Refanyval, '@', "003.1") && retVal;
            retVal = VerifyEqualsObjectF(OpCodes.Rem_Un, "HelloWorld", "003.2") && retVal;
            retVal = VerifyEqualsObjectF(OpCodes.Shr, Int32.MinValue, "003.3") && retVal;
            retVal = VerifyEqualsObjectF(OpCodes.Stfld, -6.13049, "003.4") && retVal;
            retVal = VerifyEqualsObjectF(OpCodes.Stloc_0, new int[2] { 1, 4 }, "003.5") && retVal;
            retVal = VerifyEqualsObjectF(OpCodes.Pop, DayOfWeek.Sunday, "003.6") && retVal;
            retVal = VerifyEqualsObjectF(OpCodes.Calli, string.Empty, "003.7") && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("030", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: The object is a null reference ");

        try
        {
            retVal = VerifyEqualsObjectF(OpCodes.Refanyval, null, "004.1") && retVal;
            retVal = VerifyEqualsObjectF(OpCodes.Rem_Un, null, "004.2") && retVal;
            retVal = VerifyEqualsObjectF(OpCodes.Shr, null, "004.3") && retVal;
            retVal = VerifyEqualsObjectF(OpCodes.Stfld, null, "004.4") && retVal;
            retVal = VerifyEqualsObjectF(OpCodes.Stloc_0, null, "004.5") && retVal;
            retVal = VerifyEqualsObjectF(OpCodes.Pop, null, "004.6") && retVal;
            retVal = VerifyEqualsObjectF(OpCodes.Calli, null, "004.7") && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("040", "Unexpected exception: " + e);
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
        OpCodeEquals1 test = new OpCodeEquals1();

        TestLibrary.TestFramework.BeginTestCase("OpCodeEquals1");

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
    private bool VerifyEqualsObjectT(OpCode op, object ob, string errNum)
    {
        bool retVal = true;

        try
        {
            object obj = ob;
            if (!op.Equals(obj))
            {
                TestLibrary.TestFramework.LogError(errNum.ToString(), "Result is not the value as expected,opcode is: " + op + ", object is: " + obj);
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

    private bool VerifyEqualsObjectF(OpCode op, object ob, string errNum)
    {
        bool retVal = true;

        try
        {
            object obj = ob;
            if (op.Equals(obj))
            {
                TestLibrary.TestFramework.LogError(errNum.ToString(), "Result is not the value as expected,opcode is: " + op + ", object is: " + obj);
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
